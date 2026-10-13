using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Tests
{
    public sealed class ContinuousFunctionalTests
    {
        private sealed class ScalarModel : ModelConfiguration, IDiscreteDynamics
        {
            public override ModelDescriptor Descriptor { get; }
            public override IReadOnlyList<ModelParameterValue> Parameters => Array.Empty<ModelParameterValue>();
            public ScalarModel(double? step) => Descriptor = new ModelDescriptor("fixture.physical-addition", "1", new ModelDimensions(1,1,0), new[]{"1"},new[]{"1"},Array.Empty<string>(),step);
            public override IDiscreteDynamics CreateDynamics()=>this;
            public NumericVector Evaluate(int t,NumericVector x,NumericVector u)=>new NumericVector(x[0]+u[0]);
            public override ModelConfiguration WithParameter(string target,double value)=>throw new ArgumentException();
            public override ModelConfiguration WithPhysicalStep(double? step)=>new ScalarModel(step);
        }
        private sealed class Density : CostConfiguration
        {
            private readonly double step, terminal, constant;
            private readonly bool linear;
            public override string Id=>"fixture.density";
            public override string CostUnit=>"1";
            public override string RunningUnit=>"1/s";
            public override CostSemantics Semantics=>CostSemantics.ContinuousFunctionalLeftRectangle;
            public override NumericVector ReferenceState=>new NumericVector(0);
            public override IReadOnlyList<ModelParameterValue> Parameters=>Array.Empty<ModelParameterValue>();
            public Density(double step,double constant,double terminal,bool linear=false){this.step=step;this.constant=constant;this.terminal=terminal;this.linear=linear;}
            public override void Validate(ModelDescriptor m){if(m.DiscretizationStepSeconds!=step)throw new ArgumentException("Physical time is required.");}
            public override CostConfiguration WithParameter(string target,double value)=>throw new ArgumentException();
            public override CostContribution EvaluateStage(int t,NumericVector x,NumericVector u)=>new CostContribution(new[]{new CostTerm("density","Density","L","Known profile","1/s",linear?t*step:constant)});
            public override CostContribution EvaluateTerminal(NumericVector x)=>new CostContribution(new[]{new CostTerm("terminal","Terminal","Phi","Known terminal","1",terminal)});
        }
        private static SimulationSession Constant(double h,double delta,double c,double terminal,bool linear=false)
        {
            var grid=PhysicalTimeGrid.ForDuration(h,delta);
            return new SimulationSession(new SimulationConfiguration("fixture.integral",new ScalarModel(delta),new NumericVector(0),
                Enumerable.Repeat(new NumericVector(0),grid.Transitions).ToArray(),cost:new Density(delta,c,terminal,linear)));
        }
        [TestCase(.1)] [TestCase(.05)] [TestCase(.02)] [TestCase(.25)]
        public void ConstantDensityHasKnownIntegralAndTerminalExactlyOnce(double delta)
        {
            using var s=Constant(2,delta,3,7);var c=s.State.Result.Costs;
            Assert.That(c.Cumulative.Last(),Is.EqualTo(6).Within(1e-12));Assert.That(c.Terminal.Total,Is.EqualTo(7));Assert.That(c.Total,Is.EqualTo(13).Within(1e-12));
            Assert.That(c.RunningDensities[0].Total,Is.EqualTo(3));Assert.That(c.Stages[0].Total,Is.EqualTo(3*delta).Within(1e-12));
            Assert.That(c.RunningDensities[0].Terms[0].Unit,Is.EqualTo("1/s"));Assert.That(c.Stages[0].Terms[0].Unit,Is.EqualTo("1"));
            Assert.That(c.Terminal.SampleIndex,Is.EqualTo(s.State.Result.History.Horizon));Assert.That(c.Terminal.RevisionId,Is.EqualTo(s.State.Result.RevisionId));
        }
        [TestCase(0,7,7)] [TestCase(3,0,6)] [TestCase(0,0,0)]
        public void ZeroRunningAndZeroTerminalAreIndependent(double c,double terminal,double expected)
        {using var s=Constant(2,.1,c,terminal);Assert.That(s.State.Result.Costs.Total,Is.EqualTo(expected).Within(1e-12));}
        [Test] public void LinearIntegrandLeftRuleHasHandComputedValueAndRefinesTowardKnownIntegral()
        {
            // Integral_0^1 tau d tau = 1/2. Left rectangles .25*(0+.25+.5+.75)=.375, .125*(0+...+.875)=.4375.
            using var a=Constant(1,.25,0,0,true);using var b=Constant(1,.125,0,0,true);
            Assert.That(a.State.Result.Costs.Total,Is.EqualTo(.375));Assert.That(b.State.Result.Costs.Total,Is.EqualTo(.4375));
            Assert.That(.5-b.State.Result.Costs.Total,Is.LessThan(.5-a.State.Result.Costs.Total));
            Assert.That(a.State.Result.History.PhysicalTimeAt(4),Is.EqualTo(1));Assert.That(b.State.Result.History.PhysicalTimeAt(8),Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidStepIsRejected(double step)=>Assert.Throws<ArgumentOutOfRangeException>(()=>PhysicalTimeGrid.ForDuration(2,step));
        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidDurationIsRejected(double duration)=>Assert.Throws<ArgumentOutOfRangeException>(()=>PhysicalTimeGrid.ForDuration(duration,.1));
        [TestCase(0)] [TestCase(-1)] [TestCase(10001)]
        public void InvalidTransitionCountIsRejected(int count)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicalTimeGrid(2,.1,count));
        [Test] public void IncompatibleHorizonIsRejectedWithoutTruncationOrInventedSeconds()
        {Assert.Throws<ArgumentException>(()=>new PhysicalTimeGrid(2,.1,10));Assert.Throws<ArgumentException>(()=>PhysicalTimeGrid.ForDuration(2,.03));}
        [Test] public void IndexOnlyCannotAcquireContinuousCost()
        {
            Assert.Throws<ArgumentException>(()=>new SimulationConfiguration("fixture.index-only",new ScalarModel(null),new NumericVector(2),new[]{new NumericVector(3)},cost:new Density(1,3,7)));
            using var s=new SimulationSession(new SimulationConfiguration("fixture.index-only",new ScalarModel(null),new NumericVector(2),new[]{new NumericVector(3)}));
            Assert.That(s.State.Result.History.States[1][0],Is.EqualTo(5));Assert.That(s.State.Sample.DiscretizationStepSeconds,Is.Null);
        }
        [Test] public void TerminalHasNoRunningCellOrInputOrNext()
        {
            using var s=Constant(1,.25,2,7);s.Execute(SimulationCommand.Seek(4));
            Assert.That(s.State.Result.Costs.StageAt(4),Is.Null);Assert.That(s.State.Sample.AppliedControl,Is.Null);Assert.That(s.State.Sample.NextState,Is.Null);
            Assert.That(s.State.Result.Costs.Cumulative,Is.EqualTo(new[]{0,.5,1,1.5,2}));Assert.That(s.State.Result.Costs.Total,Is.EqualTo(9));
        }
        [TestCase(0,0)] [TestCase(Math.PI/2,9.81)] [TestCase(-Math.PI/2,-9.81)]
        public void EquilibriumInputIsDerivedAndSatisfiesContinuousAndDiscreteModels(double angle,double input)
        {
            var p=new PendulumParameters(1,1,9.81);var d=new PendulumDynamics(p,.1);var x=p.EquilibriumState(angle);var u=p.EquilibriumInput(angle);
            Assert.That(u[0],Is.EqualTo(input).Within(1e-12));Assert.That(d.EvaluateDerivative(x,u),Is.EqualTo(new[]{0d,0d}).Within(1e-12));Assert.That(d.Evaluate(0,x,u),Is.EqualTo(x).Within(1e-12));
        }
        [Test] public void PhysicalProfileIsRightContinuousAndSampledBySeconds()
        {
            var p=new PhysicalStepProfile(2,new[]{0d,1},new[]{new NumericVector(2),new NumericVector(7)},new[]{"1"});
            var sampling=new ContinuousSampling(p,p,p);
            var a=sampling.SampleReference(PhysicalTimeGrid.ForDuration(2,.1));var b=sampling.SampleReference(PhysicalTimeGrid.ForDuration(2,.05));
            Assert.That(a.States[9][0],Is.EqualTo(2));Assert.That(a.States[10][0],Is.EqualTo(7));Assert.That(b.States[19][0],Is.EqualTo(2));Assert.That(b.States[20][0],Is.EqualTo(7));
            Assert.That(a.States.Last()[0],Is.EqualTo(7));Assert.That(a.Inputs.Count,Is.EqualTo(20));Assert.That(b.Inputs.Count,Is.EqualTo(40));
        }
        [Test] public void InputIntervalEditRetainsPhysicalLawUnderRefinementAndIsImmutable()
        {
            var p=new PhysicalStepProfile(2,new[]{0d},new[]{new NumericVector(0)},new[]{"1"});var edited=p.WithInterval(.5,1,new NumericVector(3));
            Assert.That(p.At(.75)[0],Is.Zero);Assert.That(edited.At(.499)[0],Is.Zero);Assert.That(edited.At(.5)[0],Is.EqualTo(3));Assert.That(edited.At(.999)[0],Is.EqualTo(3));Assert.That(edited.At(1)[0],Is.Zero);
            var s=new ContinuousSampling(p,p,edited);var g=PhysicalTimeGrid.ForDuration(2,.05);var reference=s.SampleReference(g);var config=s.Compose("fixture.sample",new ScalarModel(.05),new NumericVector(0),new ContinuousTrackingCost(reference,new NumericVector(1),new NumericVector(1),new NumericVector(1)));
            Assert.That(config.Controls[10][0],Is.EqualTo(3));Assert.That(config.Controls[19][0],Is.EqualTo(3));Assert.That(config.Controls[20][0],Is.Zero);
        }
        [Test] public void MismatchedReferenceGridIsRejectedEvenWithSameSampleCount()
        {
            var p=new PhysicalStepProfile(2,new[]{0d},new[]{new NumericVector(0)},new[]{"1"});var s=new ContinuousSampling(p,p,p);var reference=s.SampleReference(PhysicalTimeGrid.ForDuration(2,.1));
            var cost=new ContinuousTrackingCost(reference,new NumericVector(1),new NumericVector(1),new NumericVector(1));
            Assert.Throws<ArgumentException>(()=>cost.Validate(new ScalarModel(.2).Descriptor,20));
            Assert.Throws<ArgumentException>(()=>new ContinuousTrackingCost(new SampledReference(reference.States,reference.Inputs,reference.StateUnits,reference.InputUnits),new NumericVector(1),new NumericVector(1),new NumericVector(1)));
        }
        [Test] public void DensityUnitMismatchIsExplicit()=>Assert.Throws<ArgumentException>(()=>LeftRectangleQuadrature.IntegrateCell(new CostContribution(2,3),.1,"1"));
        [TestCase(true)] [TestCase(false)]
        public void SampleArraysCannotClaimDifferentFrozenPhysicalLaws(bool alterInput)
        {
            var profile=new PhysicalStepProfile(1,new[]{0d},new[]{new NumericVector(0)},new[]{"1"});var sampling=new ContinuousSampling(profile,profile,profile);var grid=PhysicalTimeGrid.ForDuration(1,.1);var reference=sampling.SampleReference(grid);
            var controls=Enumerable.Repeat(new NumericVector(0),10).ToArray();
            if(alterInput)controls[5]=new NumericVector(9);
            else {var states=reference.States.ToArray();states[5]=new NumericVector(9);reference=new SampledReference(states,reference.Inputs,reference.StateUnits,reference.InputUnits,grid);}
            var cost=new ContinuousTrackingCost(reference,new NumericVector(1),new NumericVector(1),new NumericVector(1));
            Assert.Throws<ArgumentException>(()=>new SimulationConfiguration("fixture.forged",new ScalarModel(.1),new NumericVector(0),controls,cost:cost,continuousSampling:sampling));
        }
        [Test] public void LegacyDiscreteCostKeepsItsHalfAndNoStepFactor()
        {
            var cost=new QuadraticTrackingCost(new NumericVector(0),new NumericVector(0),new NumericVector(2),new NumericVector(2),new NumericVector(2),new[]{"1"},new[]{"1"});
            using var s=new SimulationSession(new SimulationConfiguration("fixture.legacy",new ScalarModel(.02),new NumericVector(2),new[]{new NumericVector(3)},cost:cost));
            Assert.That(s.State.Result.Costs.Total,Is.EqualTo(38));Assert.That(s.State.Result.Costs.RunningDensities,Is.Null);Assert.That(cost.Semantics,Is.EqualTo(CostSemantics.DiscreteAdditive));
        }
    }
}
