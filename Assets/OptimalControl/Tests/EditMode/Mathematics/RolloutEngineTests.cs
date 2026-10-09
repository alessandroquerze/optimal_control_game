using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Tests
{
    public sealed class RolloutEngineTests
    {
        // A second mathematical model exists only in tests, proving that rollout/snapshots are generic.
        private sealed class ScalarDynamics : IDiscreteDynamics
        {
            public ModelDescriptor Descriptor { get; }
            public int Evaluations { get; private set; }
            private readonly Func<int, NumericVector, NumericVector, NumericVector> transition;

            public ScalarDynamics(Func<int, NumericVector, NumericVector, NumericVector> transition = null,
                int controls = 1, double? step = null)
            {
                this.transition = transition ?? ((t, x, u) => new NumericVector(x[0] + u[0]));
                Descriptor = new ModelDescriptor("fixture.scalar", "1", new ModelDimensions(1, controls, 0),
                    new[] { "1" }, Enumerable.Repeat("1", controls).ToArray(), Array.Empty<string>(), step);
            }

            public NumericVector Evaluate(int t, NumericVector x, NumericVector u)
            {
                Evaluations++;
                return transition(t, x, u);
            }
        }

        [Test]
        public void KnownScalarAdditionProducesFiveAndTerminalHasNoTransition()
        {
            var history = RolloutEngine.Rollout(new ScalarDynamics(), new NumericVector(2), new[] { new NumericVector(3) }, "fixture", 7);
            Assert.That(history.States.Count, Is.EqualTo(2));
            Assert.That(history.Controls.Count, Is.EqualTo(1));
            Assert.That(history.States[1][0], Is.EqualTo(5));
            var first = history.GetSnapshot(0);
            var terminal = history.GetSnapshot(1);
            Assert.That(first.NextState[0], Is.EqualTo(5));
            Assert.That(first.AppliedControl[0], Is.EqualTo(3));
            Assert.That(first.Metrics[SimulationMetricIds.NextState].SampleIndex, Is.EqualTo(1));
            Assert.That(terminal.CurrentState[0], Is.EqualTo(5));
            Assert.That(terminal.AppliedControl, Is.Null);
            Assert.That(terminal.NextState, Is.Null);
            foreach (string id in new[] { SimulationMetricIds.AppliedInput, SimulationMetricIds.NextState })
            {
                Assert.That(terminal.Metrics[id].Validity, Is.EqualTo(MetricValidity.NotApplicable));
                Assert.That(terminal.Metrics[id].SampleIndex, Is.Null);
                Assert.That(terminal.Metrics[id].Value, Is.Null);
            }
            Assert.That(first.PhysicalTimeSeconds, Is.Null, "A mathematical discrete model need not invent physical time.");
            Assert.That(terminal.RevisionId, Is.EqualTo(7));
            Assert.That(terminal.ExperimentId, Is.EqualTo("fixture"));
            TestContext.WriteLine("t=0: x=[2], u=[3], next=[5] @1; t=1: x=[5], u=N/A, next=N/A.");
        }

        [Test]
        public void SnapshotsAndMetricsShareStoredSamplesAndRewindDoesNotEvaluate()
        {
            var model = new ScalarDynamics();
            var history = RolloutEngine.Rollout(model, new NumericVector(2), new[] { new NumericVector(3), new NumericVector(-1) }, "fixture");
            var later = history.GetSnapshot(1);
            var earlier = history.GetSnapshot(0);
            Assert.That(model.Evaluations, Is.EqualTo(2));
            Assert.That(later.CurrentState, Is.SameAs(history.States[1]));
            Assert.That(earlier.NextState, Is.SameAs(later.CurrentState));
            Assert.That(earlier.Metrics[SimulationMetricIds.CurrentState].Value, Is.SameAs(earlier.CurrentState));
            Assert.That(later.Metrics[SimulationMetricIds.NextState].Value, Is.SameAs(history.States[2]));
            Assert.That(later.Metrics[SimulationMetricIds.NextState].SampleIndex, Is.EqualTo(2));
            Assert.That(later.NextState[0], Is.EqualTo(4));
        }

        [Test]
        public void ExternalArraysListsAndReturnedCopiesCannotMutateHistory()
        {
            var state = new[] { 2.0 };
            var control = new[] { 3.0 };
            var controls = new[] { new NumericVector(control) };
            var history = RolloutEngine.Rollout(new ScalarDynamics(), new NumericVector(state), controls, "fixture");
            var snapshot = history.GetSnapshot(0);
            state[0] = control[0] = 999;
            controls[0] = new NumericVector(999);
            snapshot.CurrentState.ToArray()[0] = 999;
            Assert.That(snapshot.CurrentState[0], Is.EqualTo(2));
            Assert.That(snapshot.AppliedControl[0], Is.EqualTo(3));
            Assert.That(snapshot.NextState[0], Is.EqualTo(5));
            Assert.Throws<NotSupportedException>(() => ((IList<NumericVector>)history.States)[0] = new NumericVector(999));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, VectorMetric>)snapshot.Metrics).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)snapshot.Model.StateUnits)[0] = "wrong");
        }

        [Test]
        public void PendulumRolloutIsRepeatableAndPhysicalTimeUsesDeclaredStep()
        {
            var model = new PendulumDynamics(new PendulumParameters(1, 1, 9.81), 0.02);
            var controls = Enumerable.Range(0, 100).Select(t => new NumericVector(t % 2 == 0 ? 0.5 : -0.25)).ToArray();
            var first = RolloutEngine.Rollout(model, new NumericVector(0.35, 0), controls, "pendulum", 1);
            var second = RolloutEngine.Rollout(model, new NumericVector(0.35, 0), controls, "pendulum", 2);
            Assert.That(first.States.Count, Is.EqualTo(101));
            for (int t = 0; t <= first.Horizon; t++)
                Assert.That(first.States[t].ToArray(), Is.EqualTo(second.States[t].ToArray()), "Deterministic rerun at sample " + t);
            var snapshot = first.GetSnapshot(50);
            Assert.That(snapshot.PhysicalTimeSeconds, Is.EqualTo(1));
            Assert.That(first.GetSnapshot(100).PhysicalTimeSeconds, Is.EqualTo(2));
            Assert.That(snapshot.Metrics[SimulationMetricIds.CurrentState].Descriptor.ComponentUnits, Is.EqualTo(new[] { "rad", "rad/s" }));
            Assert.That(snapshot.Metrics[SimulationMetricIds.AppliedInput].Descriptor.ComponentUnits, Is.EqualTo(new[] { "N·m" }));
        }

        [Test]
        public void TimeVaryingModelReceivesTransitionIndex()
        {
            var history = RolloutEngine.Rollout(new ScalarDynamics((t, x, u) => new NumericVector(x[0] + t + u[0])), new NumericVector(2),
                new[] { new NumericVector(3), new NumericVector(4) }, "fixture");
            Assert.That(history.States.Select(x => x[0]), Is.EqualTo(new[] { 2.0, 5.0, 10.0 }));
        }

        [Test]
        public void ModelWithNoControlUsesEmptyInputVectors()
        {
            var model = new ScalarDynamics((t, x, u) => new NumericVector(2 * x[0]), controls: 0);
            var history = RolloutEngine.Rollout(model, new NumericVector(2), new[] { new NumericVector() }, "fixture");
            Assert.That(history.GetSnapshot(0).AppliedControl.Count, Is.Zero);
            Assert.That(history.States[1][0], Is.EqualTo(4));
        }

        [Test]
        public void InvalidRolloutRequestsAreRejectedBeforeEvaluation()
        {
            var model = new ScalarDynamics();
            var x = new NumericVector(2);
            var u = new[] { new NumericVector(3) };
            Assert.Throws<ArgumentNullException>(() => RolloutEngine.Rollout(null, x, u, "fixture"));
            Assert.Throws<ArgumentNullException>(() => RolloutEngine.Rollout(model, null, u, "fixture"));
            Assert.Throws<ArgumentException>(() => RolloutEngine.Rollout(model, new NumericVector(1, 2), u, "fixture"));
            Assert.Throws<ArgumentNullException>(() => RolloutEngine.Rollout(model, x, null, "fixture"));
            Assert.Throws<ArgumentOutOfRangeException>(() => RolloutEngine.Rollout(model, x, Array.Empty<NumericVector>(), "fixture"));
            Assert.Throws<ArgumentException>(() => RolloutEngine.Rollout(model, x, new[] { new NumericVector() }, "fixture"));
            Assert.Throws<ArgumentNullException>(() => RolloutEngine.Rollout(model, x, new NumericVector[] { null }, "fixture"));
            Assert.Throws<ArgumentException>(() => RolloutEngine.Rollout(model, x, u, " "));
            Assert.Throws<ArgumentOutOfRangeException>(() => RolloutEngine.Rollout(model, x, u, "fixture", -1));
            Assert.That(model.Evaluations, Is.Zero);
        }

        [Test]
        public void InvalidSnapshotIndicesAreRejected()
        {
            var history = RolloutEngine.Rollout(new ScalarDynamics(), new NumericVector(2), new[] { new NumericVector(3) }, "fixture");
            Assert.Throws<ArgumentOutOfRangeException>(() => history.GetSnapshot(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => history.GetSnapshot(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => history.PhysicalTimeAt(2));
        }

        [Test]
        public void FailedCandidatePublishesNoPartialHistoryAndKeepsExistingResult()
        {
            var good = RolloutEngine.Rollout(new ScalarDynamics(), new NumericVector(2), new[] { new NumericVector(3) }, "fixture", 1);
            var broken = new ScalarDynamics((t, x, u) => t == 1 ? throw new ArithmeticException("overflow") : new NumericVector(x[0] + u[0]));
            var error = Assert.Throws<RolloutException>(() => RolloutEngine.Rollout(broken, new NumericVector(2), new[] { new NumericVector(3), new NumericVector(4) }, "fixture", 2));
            Assert.That(error.Timestep, Is.EqualTo(1));
            Assert.That(error.InnerException, Is.TypeOf<ArithmeticException>());
            Assert.That(good.GetSnapshot(1).CurrentState[0], Is.EqualTo(5));
            Assert.That(good.RevisionId, Is.EqualTo(1));
        }

        [Test]
        public void NullWrongDimensionAndNonFiniteModelResultsFailExplicitly()
        {
            foreach (var model in new[]
            {
                new ScalarDynamics((t, x, u) => null),
                new ScalarDynamics((t, x, u) => new NumericVector(1, 2)),
                new ScalarDynamics((t, x, u) => new NumericVector(double.NaN))
            })
                Assert.That(Assert.Throws<RolloutException>(() => RolloutEngine.Rollout(model, new NumericVector(2), new[] { new NumericVector(3) }, "fixture")).Timestep, Is.Zero);
        }

        [Test]
        public void PhysicalDurationOverflowIsRejectedBeforeEvaluation()
        {
            var model = new ScalarDynamics(step: double.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => RolloutEngine.Rollout(model, new NumericVector(2), new[] { new NumericVector(3), new NumericVector(4) }, "fixture"));
            Assert.That(model.Evaluations, Is.Zero);
        }

        [Test]
        public void DescriptorCopiesUnitsAndRejectsInconsistentMetadata()
        {
            var units = new[] { "m" };
            var descriptor = new ModelDescriptor("fixture", "1", new ModelDimensions(1, 0, 0), units, Array.Empty<string>(), Array.Empty<string>());
            units[0] = "wrong";
            Assert.That(descriptor.StateUnits[0], Is.EqualTo("m"));
            Assert.Throws<ArgumentException>(() => new ModelDescriptor("", "1", new ModelDimensions(1, 0, 0), units, Array.Empty<string>(), Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => new ModelDescriptor("fixture", "1", default, units, Array.Empty<string>(), Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => new ModelDescriptor("fixture", "1", new ModelDimensions(2, 0, 0), units, Array.Empty<string>(), Array.Empty<string>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ModelDescriptor("fixture", "1", new ModelDimensions(1, 0, 0), units, Array.Empty<string>(), Array.Empty<string>(), double.NaN));
        }

        [Test]
        public void CoreAndSimulationHaveNoUnityContentPresentationOrApplicationDependencies()
        {
            foreach (var assembly in new[] { typeof(PendulumDynamics).Assembly, typeof(RolloutEngine).Assembly })
                Assert.That(assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity", StringComparison.Ordinal)
                    || a.Name == "OptCon.Content" || a.Name == "OptCon.Presentation" || a.Name == "OptCon.Application"), Is.False);
        }
    }
}
