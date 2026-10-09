using System;
using NUnit.Framework;
using OptCon.Core;

namespace OptCon.Tests
{
    public sealed class PendulumDynamicsTests
    {
        private static PendulumDynamics Model(double step = 0.1) => new PendulumDynamics(new PendulumParameters(1, 1, 9.81), step);

        // Absolute and relative tolerances: 1e-12 each for these small double-valued cases.
        private static void Near(double actual, double expected) => Assert.That(actual, Is.EqualTo(expected).Within(1e-12 + 1e-12 * Math.Abs(expected)));

        [Test]
        public void KnownTorqueUsesBothOldStateComponents()
        {
            var next = Model().Evaluate(0, new NumericVector(0, 2), new NumericVector(3));
            Near(next[0], 0.2);
            Near(next[1], 2.3); // sin(theta_old)=0, rather than sin(theta_next).
        }

        [Test]
        public void ZeroTorqueAtRightAngleHasKnownGravityAcceleration()
        {
            var next = Model().Evaluate(0, new NumericVector(Math.PI / 2, 0), new NumericVector(0));
            Near(next[0], Math.PI / 2);
            Near(next[1], -0.981);
        }

        [Test]
        public void MassAndLengthScaleTorqueByMomentOfInertia()
        {
            var model = new PendulumDynamics(new PendulumParameters(2, 3, 0), 0.5);
            var next = model.Evaluate(0, new NumericVector(0, 0), new NumericVector(18));
            Near(next[0], 0);
            Near(next[1], 0.5);
            Near(model.Parameters.MomentOfInertia, 18);
        }

        [TestCase(0)]
        [TestCase(0.5235987755982988)]
        [TestCase(-1.2)]
        public void ConstantEquilibriumIsPreserved(double angle)
        {
            var model = new PendulumDynamics(new PendulumParameters(2, 3, 9.81), 0.02);
            var next = model.Evaluate(5, new NumericVector(angle, 0), new NumericVector(2 * 9.81 * 3 * Math.Sin(angle)));
            Near(next[0], angle);
            Near(next[1], 0);
        }

        [Test]
        public void AnglesRemainUnwrappedAndInputsAreNotSaturated()
        {
            var next = Model().Evaluate(0, new NumericVector(10 * Math.PI, 1), new NumericVector(1000));
            Near(next[0], 10 * Math.PI + 0.1);
            Near(next[1], 101);
        }

        [TestCase(0, 1, 9.81)]
        [TestCase(-1, 1, 9.81)]
        [TestCase(1, 0, 9.81)]
        [TestCase(1, -1, 9.81)]
        [TestCase(1, 1, -1)]
        [TestCase(double.NaN, 1, 9.81)]
        [TestCase(1, double.PositiveInfinity, 9.81)]
        [TestCase(1, 1, double.NaN)]
        [TestCase(1, 1, double.PositiveInfinity)]
        public void InvalidPhysicalParametersAreRejected(double mass, double length, double gravity)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new PendulumParameters(mass, length, gravity));

        [Test]
        public void UnrepresentableDerivedCoefficientsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PendulumParameters(double.MaxValue, 2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PendulumParameters(1e-300, 1e-100, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PendulumParameters(1, 1e-100, double.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PendulumParameters(1e-300, 1e150, double.Epsilon));
        }

        [TestCase(0)]
        [TestCase(-0.01)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidStepIsRejected(double step) => Assert.Throws<ArgumentOutOfRangeException>(() => Model(step));

        [Test]
        public void DimensionsNullsAndNegativeTimestepAreRejected()
        {
            var model = Model();
            Assert.Throws<ArgumentException>(() => model.Evaluate(0, new NumericVector(0), new NumericVector(0)));
            Assert.Throws<ArgumentException>(() => model.Evaluate(0, new NumericVector(0, 0), new NumericVector()));
            Assert.Throws<ArgumentNullException>(() => model.Evaluate(0, null, new NumericVector(0)));
            Assert.Throws<ArgumentNullException>(() => model.Evaluate(0, new NumericVector(0, 0), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Evaluate(-1, new NumericVector(0, 0), new NumericVector(0)));
            Assert.Throws<ArgumentNullException>(() => new PendulumDynamics(null, 0.1));
        }

        [Test]
        public void NumericalOverflowIsExplicit()
            => Assert.Throws<ArithmeticException>(() => Model(2).Evaluate(0, new NumericVector(0, double.MaxValue), new NumericVector(0)));

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NonFiniteVectorComponentsAreRejected(double value)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new NumericVector(value));
    }
}
