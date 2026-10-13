using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Core;
using OptCon.Simulation;
namespace OptCon.Tests
{
    public sealed class PlanarDroneTests
    {
        private static PlanarDroneConfiguration Model(double step=.1) => new PlanarDroneConfiguration(2,4,10,2,step);
        private static NumericVector Rest => new NumericVector(0,0,1,0,0,0);
        [Test] public void HoverIsAnEquilibriumForAnEntireRollout()
        {
            using var s=new SimulationSession(new SimulationConfiguration("oracle.drone",Model(),Rest,Enumerable.Repeat(new NumericVector(10,10),10).ToArray()));
            Assert.That(s.State.Result.IsConstantStateInput,Is.True);
            foreach(var x in s.State.Result.History.States) Assert.That(x,Is.EqualTo(new double[]{0,0,1,0,0,0}));
            Assert.That(s.State.Result.History.States.Count,Is.EqualTo(11));
        }
        [TestCase(12,12,.2,0)] [TestCase(8,8,-.2,0)] [TestCase(12,8,0,.4)] [TestCase(8,12,0,-.4)] [TestCase(0,0,-1,0)]
        public void IndependentAnswersProtectSumDifferenceAndGravity(double u1,double u2,double vy,double omega)
        {
            var x=Model().CreateDynamics().Evaluate(0,Rest,new NumericVector(u1,u2));
            Assert.That(x[3],Is.EqualTo(vy).Within(1e-12)); Assert.That(x[5],Is.EqualTo(omega).Within(1e-12));
            Assert.That(x[2],Is.EqualTo(1)); Assert.That(x[4],Is.Zero);
        }
        [TestCase(.2,-.19866933079506122)] [TestCase(-.2,.19866933079506122)]
        public void TiltProtectsHorizontalSign(double angle,double vx)
        {
            var x=Model().CreateDynamics().Evaluate(0,new NumericVector(0,0,1,0,angle,0),new NumericVector(10,10));
            Assert.That(x[1],Is.EqualTo(vx).Within(1e-12)); Assert.That(x[3],Is.EqualTo(-.01993342215875837).Within(1e-12));
        }
        [Test] public void SixRowKnownEulerStepUsesOnlyTheOldState()
        {
            var x=Model().CreateDynamics().Evaluate(7,new NumericVector(1,2,3,4,Math.PI/2,.5),new NumericVector(6,2));
            Assert.That(x.ToArray(),Is.EqualTo(new[]{1.2,1.6,3.4,3,Math.PI/2+.05,.9}).Within(1e-12));
        }
        [Test] public void ThreeConstantAccelerationStepsHaveIndependentKnownPositions()
        {
            using var s=new SimulationSession(new SimulationConfiguration("oracle.drone",Model(.5),Rest,Enumerable.Repeat(new NumericVector(12,12),3).ToArray()));
            Assert.That(s.State.Result.History.States.Select(x=>x[2]),Is.EqualTo(new[]{1,1,1.5,2.5}));
            Assert.That(s.State.Result.History.States.Select(x=>x[3]),Is.EqualTo(new[]{0,1,2,3}));
        }
        [Test] public void OutputSelectsOnlyPositionAndRemainsAvailableAtTerminal()
        {
            var output=Model().CreateOutputFunction();Assert.That(output.Dimension,Is.EqualTo(2));Assert.That(output.RequiresControl,Is.False);
            Assert.That(output.Evaluate(2,new NumericVector(1,2,3,4,5,6),null),Is.EqualTo(new[]{1d,3d}));
        }
        [Test] public void SelectionConfigurationIsFrozenAndValidated()
        {
            var indices=new[]{0,2};var h=new StateComponentsOutput(6,2,indices);indices[0]=5;
            Assert.That(h.Evaluate(0,new NumericVector(1,2,3,4,5,6),null),Is.EqualTo(new[]{1d,3d}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new StateComponentsOutput(6,2,6));
            Assert.That(new StateComponentsOutput(6,2,4).Dimension,Is.EqualTo(1));
        }
        [Test] public void DescriptorAndParameterUnitsArePerComponent()
        {
            var m=Model();Assert.That(m.Descriptor.Dimensions.State,Is.EqualTo(6));Assert.That(m.Descriptor.Dimensions.Control,Is.EqualTo(2));Assert.That(m.Descriptor.Dimensions.Output,Is.EqualTo(2));
            Assert.That(m.Descriptor.StateUnits,Is.EqualTo(new[]{"m","m/s","m","m/s","rad","rad/s"}));
            Assert.That(m.Descriptor.ControlUnits,Is.EqualTo(new[]{"N","N"}));Assert.That(m.Descriptor.OutputUnits,Is.EqualTo(new[]{"m","m"}));
            Assert.That(m.Parameters.Single(p=>p.Target=="model.moment-of-inertia").Unit,Is.EqualTo("kg·m²"));
        }
        [TestCase(0)] [TestCase(-.1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void PhysicalStepMustBeFiniteAndPositive(double d) => Assert.Throws<ArgumentOutOfRangeException>(()=>Model(d));
        [Test] public void PhysicalStepCannotBeOmitted() => Assert.Throws<ArgumentException>(()=>Model().WithPhysicalStep(null));
        [TestCase("model.mass")] [TestCase("model.arm-length")] [TestCase("model.moment-of-inertia")]
        public void PhysicalParametersMustBePositive(string target) => Assert.Throws<ArgumentOutOfRangeException>(()=>Model().WithParameter(target,0));
        [Test] public void DimensionsAreRejectedRatherThanTruncated()
        {
            Assert.Throws<ArgumentException>(()=>Model().CreateDynamics().Evaluate(0,new NumericVector(0,0),new NumericVector(1,1)));
            Assert.Throws<ArgumentException>(()=>Model().CreateDynamics().Evaluate(0,Rest,new NumericVector(1)));
        }
        [TestCase(-1,1)] [TestCase(0,0)] [TestCase(3,-3)] [TestCase(8,0)] [TestCase(9,1)]
        public void AsymmetricBoundHasKnownResidualAndNoEnforcement(double input,double residual)
        {
            var b=new ComponentBound("force","Force",false,1,0,8,"N");
            Assert.That(b.Evaluate(Rest,new NumericVector(100,input)),Is.EqualTo(residual));
            Assert.That(b.Minimum,Is.Zero);Assert.That(b.Maximum,Is.EqualTo(8));
        }
        [Test] public void IntervalMetadataEditsAreImmutableAndSymmetricLegacyStaysValid()
        {
            var b=new ComponentBound("altitude","Altitude",true,2,.5,2,"m");
            Assert.That(b.Evaluate(new NumericVector(0,0,2.5,0,0,0),new NumericVector(0,0)),Is.EqualTo(.5));
            var c=new ConstraintConfiguration(new[]{b},new EndpointConstraint[0]);var d=c.WithParameter("constraint.altitude.minimum",1);
            Assert.That(c.Path[0].Minimum,Is.EqualTo(.5));Assert.That(d.Path[0].Minimum,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>c.WithParameter("constraint.altitude.minimum",3));
            Assert.That(new ComponentBound("old","Old symmetric",false,0,2,"N").Evaluate(Rest,new NumericVector(-3,0)),Is.EqualTo(1));
        }
    }
}
