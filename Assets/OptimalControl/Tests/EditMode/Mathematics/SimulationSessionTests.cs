using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Tests
{
    public sealed class SimulationSessionTests
    {
        private static SimulationConfiguration Scalar(int horizon = 4, ScalarConfiguration model = null) =>
            new SimulationConfiguration("fixture.scalar", model ?? new ScalarConfiguration(), new NumericVector(2),
                Enumerable.Repeat(new NumericVector(3), horizon).ToArray());

        [Test]
        public void IndexOnlySessionProducesFiveAndNeverInventsPhysicalTime()
        {
            using (var session = new SimulationSession(Scalar(1)))
            {
                Assert.That(session.State.Result.History.States[1][0], Is.EqualTo(5));
                foreach (int t in new[] { 0, 1 })
                {
                    session.Execute(SimulationCommand.Seek(t));
                    Assert.That(session.State.Sample.DiscretizationStepSeconds, Is.Null);
                    Assert.That(session.State.Sample.PhysicalTimeSeconds, Is.Null);
                }
                Assert.That(session.State.Sample.AppliedControl, Is.Null);
                Assert.That(session.State.Sample.NextState, Is.Null);
            }
        }

        [Test]
        public void InputEditRecomputesOnlyAffectedSuffixAndPublishesOnce()
        {
            var model = new ScalarConfiguration();
            using (var session = new SimulationSession(Scalar(model: model)))
            {
                var original = session.State.Result;
                model.Calls.Clear();
                var publications = new List<SessionState>();
                session.Changed += publications.Add;
                Assert.That(session.Execute(SimulationCommand.SetInput(2, new NumericVector(10))), Is.True);
                Assert.That(model.Calls, Is.EqualTo(new[] { 2, 3 }));
                Assert.That(publications.Count, Is.EqualTo(1));
                var updated = publications.Single();
                Assert.That(updated.Result.RevisionId, Is.EqualTo(1));
                Assert.That(updated.Result.Configuration.Controls[2][0], Is.EqualTo(10));
                Assert.That(updated.Result.History.States.Select(x => x[0]), Is.EqualTo(new double[] { 2, 5, 8, 18, 21 }));
                Assert.That(original.History.States.Select(x => x[0]), Is.EqualTo(new double[] { 2, 5, 8, 11, 14 }));
                Assert.That(updated.Result.History.States[2], Is.SameAs(original.History.States[2]));
                Assert.That(session.Execute(SimulationCommand.SetInput(1, new NumericVector(1), InputEditScope.Tail)), Is.True);
                Assert.That(session.State.Result.Configuration.Controls.Select(u => u[0]), Is.EqualTo(new double[] { 3, 1, 1, 1 }));
            }
        }

        [Test]
        public void SeekRewindAndPlaybackNeverEvaluateTheModel()
        {
            var model = new ScalarConfiguration();
            using (var session = new SimulationSession(Scalar(model: model)))
            {
                model.Calls.Clear(); var result = session.State.Result;
                session.Execute(SimulationCommand.StepForward()); session.Execute(SimulationCommand.StepBackward());
                session.Execute(SimulationCommand.Seek(2)); session.Execute(SimulationCommand.Play()); session.AdvancePlayback(1);
                Assert.That(model.Calls, Is.Empty);
                Assert.That(session.State.Result, Is.SameAs(result));
                Assert.That(session.State.Sample.Timestep, Is.EqualTo(4));
                Assert.That(session.State.IsPlaying, Is.False);
                Assert.That(session.Execute(SimulationCommand.Seek(5)), Is.False);
                Assert.That(session.Execute(SimulationCommand.Play()), Is.True);
                Assert.That(session.State.IsPlaying, Is.False);
            }
        }

        [Test]
        public void ResetRestoresCapturedPresetAndCursorWithoutChangingIt()
        {
            var preset = Scalar();
            using (var session = new SimulationSession(preset))
            {
                session.Execute(SimulationCommand.SetInput(0, new NumericVector(9), InputEditScope.Tail));
                session.Execute(SimulationCommand.SetInitialState(new NumericVector(7)));
                session.Execute(SimulationCommand.Seek(4));
                session.Execute(SimulationCommand.Reset());
                Assert.That(session.State.Result.Configuration, Is.SameAs(preset));
                Assert.That(session.State.Result.History.States.Select(x => x[0]), Is.EqualTo(new double[] { 2, 5, 8, 11, 14 }));
                Assert.That(session.State.Sample.Timestep, Is.Zero);
                Assert.That(session.State.IsPlaying, Is.False);
                Assert.That(session.State.Result.RevisionId, Is.EqualTo(3));
            }
        }

        [Test]
        public void ResultsRetainTheirOwnImmutablePhysicalConfigurationAndSamples()
        {
            var preset = new SimulationConfiguration("fixture.pendulum", new PendulumConfiguration(new PendulumParameters(1, 1, 0), 0.02),
                new NumericVector(0, 0), new[] { new NumericVector(3) });
            using (var session = new SimulationSession(preset))
            {
                var a = session.State.Result;
                Assert.That(session.Execute(SimulationCommand.SetParameter("model.mass", 2)), Is.True);
                var b = session.State.Result;
                Assert.That(((PendulumConfiguration)a.Configuration.Model).Physics.Mass, Is.EqualTo(1));
                Assert.That(((PendulumConfiguration)b.Configuration.Model).Physics.Mass, Is.EqualTo(2));
                Assert.That(a.History.States[1][1], Is.EqualTo(0.06).Within(1e-12));
                Assert.That(b.History.States[1][1], Is.EqualTo(0.03).Within(1e-12));
                Assert.That(b.Configuration.Model.Parameters.Single(p => p.Target == "model.mass").Value, Is.EqualTo(2));
                Assert.That(b.History.PhysicalTimeAt(1), Is.EqualTo(0.02));
                Assert.That(a.Configuration.Model, Is.Not.SameAs(b.Configuration.Model));
                Assert.Throws<NotSupportedException>(() => ((IList<ModelParameterValue>)a.Configuration.Model.Parameters).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<NumericVector>)a.Configuration.Controls).Clear());
            }
        }

        [Test]
        public void FailedRevisionKeepsLastValidConfigurationAndHistoryTogether()
        {
            using (var session = new SimulationSession(Scalar()))
            {
                var a = session.State.Result;
                session.Execute(SimulationCommand.SetParameter("fixture.gain", 2));
                var b = session.State.Result;
                var notices = new List<SessionState>(); session.Changed += notices.Add;
                Assert.That(session.Execute(SimulationCommand.SetParameter("fixture.gain", double.MaxValue)), Is.False);
                Assert.That(notices.Count, Is.EqualTo(1));
                Assert.That(session.State.Result, Is.SameAs(b));
                Assert.That(session.State.Result.Configuration.Model.Parameters[0].Value, Is.EqualTo(2));
                Assert.That(session.State.RequestedConfiguration.Model.Parameters[0].Value, Is.EqualTo(double.MaxValue));
                Assert.That(session.State.Error, Is.Not.Empty);
                Assert.That(session.State.AttemptRevisionId, Is.EqualTo(2));
                Assert.That(b.RevisionId, Is.EqualTo(1));
                Assert.That(a.History.States[1][0], Is.EqualTo(5));
                Assert.That(b.History.States[1][0], Is.EqualTo(8));
                session.Execute(SimulationCommand.Reset());
                Assert.That(session.State.Error, Is.Null);
                Assert.That(session.State.Result.RevisionId, Is.EqualTo(3));
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void PresentationFrequencyDoesNotChangeMathOrSelectedSample(int frequency)
        {
            using (var session = new SimulationSession(Scalar(100)))
            {
                var result = session.State.Result;
                session.Execute(SimulationCommand.Play());
                for (int frame = 0; frame < frequency; frame++) session.AdvancePlayback(1d / frequency);
                Assert.That(session.State.Sample.Timestep, Is.EqualTo(10));
                session.Execute(SimulationCommand.SetPlaybackRate(20));
                for (int frame = 0; frame < frequency; frame++) session.AdvancePlayback(1d / frequency);
                Assert.That(session.State.Sample.Timestep, Is.EqualTo(30));
                Assert.That(session.State.Result, Is.SameAs(result));
                Assert.That(session.State.Sample.PhysicalTimeSeconds, Is.Null);
                session.Execute(SimulationCommand.Pause()); session.AdvancePlayback(10);
                Assert.That(session.State.Sample.Timestep, Is.EqualTo(30));
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidPlaybackRateDoesNotAlterMathematicalResult(double rate)
        {
            using (var session = new SimulationSession(Scalar()))
            {
                var before = session.State;
                Assert.That(session.Execute(SimulationCommand.SetPlaybackRate(rate)), Is.False);
                Assert.That(session.State, Is.SameAs(before));
            }
        }

        [Test]
        public void AuthoredRangeAndModelInvariantsBothApply()
        {
            var config = new SimulationConfiguration("fixture.pendulum", new PendulumConfiguration(new PendulumParameters(1, 1, 9.81), 0.02),
                new NumericVector(0, 0), new[] { new NumericVector(0) });
            using (var session = new SimulationSession(config, new[] { new ParameterRule("model.mass", 0.1, 10) }))
            {
                Assert.That(session.Execute(SimulationCommand.SetParameter("model.mass", 11)), Is.False);
                Assert.That(session.State.Result.Configuration, Is.SameAs(config));
                Assert.That(session.Execute(SimulationCommand.SetParameter("model.length", 0)), Is.False);
                Assert.That(session.Execute(SimulationCommand.SetPhysicalStep(null)), Is.False);
                Assert.That(session.Execute(SimulationCommand.SetPhysicalStep(0.01)), Is.True);
                Assert.That(session.State.Sample.DiscretizationStepSeconds, Is.EqualTo(0.01));
            }
        }

        [Test]
        public void DisposeClearsSubscriptionsAndRejectsFurtherCommands()
        {
            var session = new SimulationSession(Scalar());
            int calls = 0; session.Changed += _ => calls++;
            session.Dispose(); session.Dispose();
            Assert.That(session.Execute(SimulationCommand.Reset()), Is.False);
            session.AdvancePlayback(1);
            Assert.That(calls, Is.Zero);
            Assert.That(session.State.IsClosed, Is.True);
        }

        [Test]
        public void ObserverCannotReenterCommandDispatch()
        {
            using (var session = new SimulationSession(Scalar()))
            {
                bool? accepted = null; int calls = 0;
                session.Changed += _ => { calls++; accepted = session.Execute(SimulationCommand.Reset()); };
                session.Execute(SimulationCommand.SetInput(0, new NumericVector(4)));
                Assert.That(accepted, Is.False);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(session.State.Result.RevisionId, Is.EqualTo(1));
            }
        }

        // Counter is instrumentation only; the mathematical configuration and transition are immutable.
        private sealed class ScalarConfiguration : ModelConfiguration, IDiscreteDynamics
        {
            public readonly List<int> Calls = new List<int>();
            private readonly double gain;
            public override ModelDescriptor Descriptor { get; } = new ModelDescriptor("fixture.addition", "1", new ModelDimensions(1, 1, 0), new[] { "1" }, new[] { "1" }, Array.Empty<string>());
            public override IReadOnlyList<ModelParameterValue> Parameters { get; }
            public ScalarConfiguration(double gain = 1)
            { this.gain = gain; Parameters = Array.AsReadOnly(new[] { new ModelParameterValue("fixture.gain", gain, "1") }); }
            public override IDiscreteDynamics CreateDynamics() => this;
            public NumericVector Evaluate(int t, NumericVector x, NumericVector u) { Calls.Add(t); return new NumericVector(x[0] + gain * u[0]); }
            public override ModelConfiguration WithParameter(string target, double value)
            {
                if (target != "fixture.gain") throw new ArgumentException("Unknown target.");
                return new ScalarConfiguration(value);
            }
            public override ModelConfiguration WithPhysicalStep(double? value)
            { if (value.HasValue) throw new ArgumentException("Index-only fixture."); return this; }
        }
    }
}
