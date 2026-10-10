using System;
using NUnit.Framework;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Tests
{
    public sealed class StateProjectionTests
    {
        private StateProjection projection;
        [SetUp] public void SetUp() => projection = ScriptableObject.CreateInstance<StateProjection>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(projection);
        [Test] public void AF08_01_TrajectoryProjectionIsPreparedBeforeVisibleCommit()
        {
            Axis(projection.X, ProjectionCoordinate.StateComponent, 0, "1");
            Axis(projection.Y, ProjectionCoordinate.Constant, 0, "1"); Axis(projection.Z, ProjectionCoordinate.DiscreteIndex, 0, "1");
            var root = new GameObject("atomic trajectory", typeof(LineRenderer), typeof(TrajectoryRenderer));
            try
            {
                var renderer = root.GetComponent<TrajectoryRenderer>(); var line = root.GetComponent<LineRenderer>();
                var valid = RolloutEngine.Rollout(new Addition(), new NumericVector(2), new[] { new NumericVector(3) }, "valid", 0);
                var huge = RolloutEngine.Rollout(new Addition(), new NumericVector(2), new[] { new NumericVector(1e40), new NumericVector(0) }, "huge", 1);
                renderer.Render(valid, projection); var before = new Vector3[line.positionCount]; line.GetPositions(before);
                Assert.Throws<NonRepresentableDisplayException>(() => renderer.Render(huge, projection));
                Assert.That(renderer.Source, Is.SameAs(valid)); Assert.That(line.positionCount, Is.EqualTo(before.Length));
                var after = new Vector3[line.positionCount]; line.GetPositions(after); Assert.That(after, Is.EqualTo(before));
                Assert.That(huge.States[1][0], Is.EqualTo(1e40));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private void Axis(ProjectionAxis axis, ProjectionCoordinate coordinate, int component, string unit, double scale = 1)
        { axis.coordinate = coordinate; axis.component = component; axis.label = coordinate.ToString(); axis.unit = unit; axis.scale = scale; }

        [Test]
        public void KnownSavedPendulumSamplesProjectToPhaseAndPhysicalTimeWithoutChangingHistory()
        {
            var history = RolloutEngine.Rollout(new PendulumDynamics(new PendulumParameters(2, 3, 0), 0.5),
                new NumericVector(0, 0), new[] { new NumericVector(18), new NumericVector(18) }, "projection.known", 7);
            Axis(projection.X, ProjectionCoordinate.StateComponent, 0, "rad", 4);
            Axis(projection.Y, ProjectionCoordinate.StateComponent, 1, "rad/s");
            Assert.That(projection.Project(history, 1), Is.EqualTo(new Vector3(0, 0.5f, 0)));
            Assert.That(projection.Project(history, 2), Is.EqualTo(new Vector3(1, 1, 0)));
            Axis(projection.X, ProjectionCoordinate.StateComponent, 0, "rad", 3);
            Axis(projection.Y, ProjectionCoordinate.StateComponent, 1, "rad/s", 0.65);
            Axis(projection.Z, ProjectionCoordinate.PhysicalTime, 0, "s", 1.4);
            Assert.That(projection.Project(history, 1), Is.EqualTo(new Vector3(0, 0.325f, 0.7f)));
            Assert.That(projection.Project(history, 2), Is.EqualTo(new Vector3(0.75f, 0.65f, 1.4f)));
            Assert.That(history.States[2].ToArray(), Is.EqualTo(new[] { 0.25, 1d }));
            Assert.That(projection.Z.Description, Does.Contain("[s]"));
            Assert.That(projection.Y.Description, Does.Contain("unità scena/(rad/s)"));
        }

        [Test]
        public void IndexOnlyModelRejectsSecondsButSupportsAnExplicitIndexAxis()
        {
            var history = RolloutEngine.Rollout(new Addition(), new NumericVector(2), new[] { new NumericVector(3) }, "projection.index", 0);
            Axis(projection.X, ProjectionCoordinate.StateComponent, 0, "1");
            Axis(projection.Y, ProjectionCoordinate.Constant, 0, "1");
            Axis(projection.Z, ProjectionCoordinate.PhysicalTime, 0, "s");
            Assert.Throws<ArgumentException>(() => projection.Project(history, 1));
            Axis(projection.Z, ProjectionCoordinate.DiscreteIndex, 0, "1", 2);
            Assert.That(projection.Project(history, 1), Is.EqualTo(new Vector3(5, 0, 2)));
            Assert.That(history.PhysicalTimeAt(0), Is.Null);
            Assert.That(history.PhysicalTimeAt(1), Is.Null);
        }

        [Test]
        public void IncompatibleUnitsAndStateComponentsAreRejectedExplicitly()
        {
            var model = new Addition().Descriptor;
            Axis(projection.X, ProjectionCoordinate.StateComponent, 0, "m");
            Axis(projection.Y, ProjectionCoordinate.Constant, 0, "1");
            Assert.Throws<ArgumentException>(() => projection.Validate(model));
            projection.X.unit = "1"; projection.X.component = 1;
            Assert.Throws<ArgumentException>(() => projection.Validate(model));
            projection.X.component = 0; projection.X.scale = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => projection.Validate(model));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.MaxValue)]
        public void RenderingBoundaryRejectsNonRepresentableCoordinates(double value) =>
            Assert.Throws<NonRepresentableDisplayException>(() => StateProjection.ToFloat(value));

        [Test]
        public void HigherDimensionalStateIsAnExplicitProjectionAndAnglesAreNotWrapped()
        {
            var descriptor = new ModelDescriptor("higher.dimension", "1", new ModelDimensions(3, 1, 0),
                new[] { "rad", "rad/s", "m" }, new[] { "1" }, Array.Empty<string>());
            Axis(projection.X, ProjectionCoordinate.StateComponent, 0, "rad");
            Axis(projection.Y, ProjectionCoordinate.StateComponent, 1, "rad/s");
            Assert.That(projection.OmittedComponents(descriptor), Does.Contain("x[2]"));
            var history = RolloutEngine.Rollout(new Identity(descriptor), new NumericVector(8, 2, 3), new[] { new NumericVector(0) }, "projection.omitted", 0);
            Assert.That(projection.Project(history, 1), Is.EqualTo(new Vector3(8, 2, 0)));
        }

        private sealed class Addition : IDiscreteDynamics
        {
            public ModelDescriptor Descriptor { get; } = new ModelDescriptor("addition", "1", new ModelDimensions(1, 1, 0),
                new[] { "1" }, new[] { "1" }, Array.Empty<string>());
            public NumericVector Evaluate(int t, NumericVector state, NumericVector control) => new NumericVector(state[0] + control[0]);
        }
        private sealed class Identity : IDiscreteDynamics
        {
            public ModelDescriptor Descriptor { get; }
            public Identity(ModelDescriptor descriptor) => Descriptor = descriptor;
            public NumericVector Evaluate(int t, NumericVector state, NumericVector control) => state;
        }
    }
}
