using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Content;
using OptCon.Core;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;

namespace OptCon.Tests
{
    public sealed class ReferenceCostConstraintTests
    {
        private static NumericVector V(params double[] x) => new NumericVector(x);
        private static SampledReference Reference() => new SampledReference(new[] { V(0), V(1), V(1) }, new[] { V(0), V(2) }, new[] { "1" }, new[] { "1" });
        private static QuadraticTrackingCost Cost(SampledReference reference = null) => new QuadraticTrackingCost(reference ?? Reference(), V(2), V(2), V(2),
            new CostTerm("state-deviation", "Deviazione stato", "ℓˣ", "Errore quadratico dello stato", "1", 0),
            new CostTerm("control-deviation", "Deviazione controllo", "ℓᵘ", "Errore quadratico del controllo", "1", 0));
        private static ConstraintConfiguration Constraints(double limit = 1) => new ConstraintConfiguration(new[] { new ComponentBound("input", "Input", false, 0, limit, "1") }, new[] { new EndpointConstraint("return", "Ritorno", 0, 0, "1") });
        private static SimulationSession Open(ConstraintConfiguration constraints = null) => new SimulationSession(new SimulationConfiguration("reference.test", new IndexOnlyConfiguration(), V(1), new[] { V(1), V(1) }, cost: Cost(), constraints: constraints));
        [Test] public void SampledKnownAnswerSeparatesEveryStagePrefixTerminalAndTotal()
        {
            // x=[1,2,3], xdes=[0,1,1], u=[1,1], udes=[0,2]; Q=R=QT=2.
            // ℓ0=1+1=2, ℓ1=1+1=2, ℓT=(3-1)^2=4, J=8. No implementation used as oracle.
            using var s = Open(); var r = s.State.Result;
            Assert.That(r.History.States.Select(x => x[0]), Is.EqualTo(new double[] { 1, 2, 3 }));
            Assert.That(r.Reference.Reference.States.Select(x => x[0]), Is.EqualTo(new double[] { 0, 1, 1 }));
            Assert.That(r.Costs.Stages.Select(c => c.Total), Is.EqualTo(new double[] { 2, 2 }));
            Assert.That(r.Costs.Stages.Select(c => c.Find("state-deviation").Value), Is.EqualTo(new double[] { 1, 1 }));
            Assert.That(r.Costs.Stages.Select(c => c.Find("control-deviation").Value), Is.EqualTo(new double[] { 1, 1 }));
            Assert.That(r.Costs.Cumulative, Is.EqualTo(new double[] { 0, 2, 4 }));
            Assert.That(r.Costs.Terminal.Total, Is.EqualTo(4)); Assert.That(r.Costs.Total, Is.EqualTo(8));
        }
        [TestCase(0,0,0,1,2)] [TestCase(1,1,2,1,2)] [TestCase(2,1,0,2,0)]
        public void ReferenceAndCostLookupFollowSeekWithoutRecalculation(int t, double xd, double ud, double error, double stage)
        {
            using var s = Open(); var r = s.State.Result; s.Execute(SimulationCommand.Seek(t)); var m = new RuntimeMetricResolver();
            var state = m.Resolve(s.State, "reference.state", FormulaValueKind.Vector, FormulaScope.CurrentTimestep);
            Assert.That(state.Value.Vector[0], Is.EqualTo(xd)); Assert.That(state.SampleIndex, Is.EqualTo(t)); Assert.That(state.Result, Is.SameAs(r));
            Assert.That(m.Resolve(s.State,"reference.state-error",FormulaValueKind.Vector,FormulaScope.CurrentTimestep).Value.Vector[0],Is.EqualTo(error));
            var input = m.Resolve(s.State,"reference.input",FormulaValueKind.Vector,FormulaScope.CurrentTimestep);
            var cost = m.Resolve(s.State,"cost.stage",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep);
            if(t==2) { Assert.That(input.Validity,Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(cost.Validity,Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(input.SampleIndex,Is.Null); }
            else { Assert.That(input.Value.Vector[0],Is.EqualTo(ud)); Assert.That(cost.Value.Scalar,Is.EqualTo(stage)); }
            Assert.That(s.State.Result, Is.SameAs(r));
        }
        [Test] public void ContributionsCarryGenericIdentityMetadataRoleIndexRevisionAndNoTerminalInput()
        {
            using var s=Open(); var c=s.State.Result.Costs;
            Assert.That(c.Stages[1].Terms.Select(t=>t.Id),Is.EqualTo(new[]{"state-deviation","control-deviation"}));
            Assert.That(c.Stages[1].Terms[0].Name,Is.EqualTo("Deviazione stato")); Assert.That(c.Stages[1].Terms[0].Unit,Is.EqualTo("1"));
            Assert.That(c.Stages[1].Role,Is.EqualTo(CostRole.Stage)); Assert.That(c.Stages[1].SampleIndex,Is.EqualTo(1)); Assert.That(c.Stages[1].RevisionId,Is.EqualTo(s.State.Result.RevisionId));
            Assert.That(c.Terminal.Role,Is.EqualTo(CostRole.Terminal)); Assert.That(c.Terminal.SampleIndex,Is.EqualTo(2)); Assert.That(c.Terminal.Find("control-deviation"),Is.Null);
            var m=new RuntimeMetricResolver(); Assert.That(m.Resolve(s.State,"cost.contribution.state-deviation",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Value.Scalar,Is.EqualTo(1));
            var terminal=m.Resolve(s.State,"cost.terminal-contribution.state-deviation",FormulaValueKind.Scalar,FormulaScope.Terminal);
            Assert.That(terminal.SampleIndex,Is.EqualTo(2)); Assert.That(terminal.Value.Scalar,Is.EqualTo(4));
            Assert.That(m.Resolve(s.State,"cost.contribution.unknown",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.Invalid));
            Assert.That(m.Resolve(s.State,"cost.terminal-contribution.control-deviation",FormulaValueKind.Scalar,FormulaScope.Terminal).Validity,Is.EqualTo(MetricValidity.NotApplicable));
        }
        [Test] public void InvalidShapeAndMissingTerminalSampleAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new SampledReference(new[]{V(0),V(1)},new[]{V(0),V(1)},new[]{"1"},new[]{"1"}));
            Assert.Throws<ArgumentException>(()=>new SampledReference(new[]{V(0,0),V(1,0)},new[]{V(0)},new[]{"1"},new[]{"1"}));
            Assert.Throws<ArgumentException>(()=>Reference().Validate(new IndexOnlyConfiguration(2).Descriptor,2));
        }
        [Test] public void UnitsAndHorizonCannotBeImplicitlyReinterpretedOrResampled()
        {
            var bad=new SampledReference(new[]{V(0),V(1)},new[]{V(0)},new[]{"m"},new[]{"1"});
            Assert.Throws<ArgumentException>(()=>bad.Validate(new IndexOnlyConfiguration().Descriptor,1));
            Assert.Throws<ArgumentException>(()=>Reference().Validate(new IndexOnlyConfiguration().Descriptor,3));
            using var s=Open(); var a=s.State.Result; Assert.That(s.Execute(SimulationCommand.SetHorizon(1)),Is.False);
            Assert.That(s.State.Result,Is.SameAs(a)); Assert.That(s.State.Error,Does.Contain("horizon")); s.Execute(SimulationCommand.Reset()); Assert.That(s.State.Error,Is.Null);
        }
        [Test] public void RetainedAOwnsReferenceWeightsContributionsAndHorizonAfterBAndFailedCandidate()
        {
            using var s=Open(); var a=s.State.Result; s.Execute(SimulationCommand.CaptureComparison());
            Assert.That(s.Execute(SimulationCommand.SetParameter("cost.reference.state.2.0",2)),Is.True);
            Assert.That(s.Execute(SimulationCommand.SetParameter("cost.q.0",4)),Is.True); var b=s.State.Result;
            Assert.That(a.Reference.Reference.States[2][0],Is.EqualTo(1)); Assert.That(a.Costs.Total,Is.EqualTo(8));
            Assert.That(b.Reference.Reference.States[2][0],Is.EqualTo(2)); Assert.That(b.Costs.Total,Is.EqualTo(7));
            Assert.That(a.Configuration.Horizon,Is.EqualTo(2)); Assert.That(s.State.Comparison.Result,Is.SameAs(a));
            Assert.That(s.Execute(SimulationCommand.SetParameter("cost.q.0",-1)),Is.False); Assert.That(s.State.Result,Is.SameAs(b));
            Assert.That(b.Costs.Configuration,Is.SameAs(b.Configuration.Cost)); Assert.That(a.History.States[2][0],Is.EqualTo(3));
            var m=new RuntimeMetricResolver(); Assert.That(m.Resolve(a,2,"reference.state",FormulaValueKind.Vector).Value.Vector[0],Is.EqualTo(1));
            Assert.That(m.Resolve(b,2,"reference.state",FormulaValueKind.Vector).Value.Vector[0],Is.EqualTo(2));
        }
        [Test] public void IndexOnlyAndTerminalDomainsRemainExplicit()
        {
            using var s=Open(Constraints()); s.Execute(SimulationCommand.Seek(2)); var m=new RuntimeMetricResolver();
            foreach(var id in new[]{"time.physical","time.step"}) Assert.That(m.Resolve(s.State,id,FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(m.Resolve(s.State,"time.index",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Value.Scalar,Is.EqualTo(2));
            foreach(var id in new[]{"input.applied","reference.input","state.next","constraint.h","constraint.desired-h"})
                Assert.That(m.Resolve(s.State,id,FormulaValueKind.Vector,id=="state.next"?FormulaScope.NextTimestep:FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(m.Resolve(s.State,"constraint.g.input",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
        }
        [TestCase(0,-1)] [TestCase(1,0)] [TestCase(2,1)]
        public void InequalityInsideOnAndOutsideBorderHasSignedKnownResidual(double input,double expected)
        { Assert.That(new ComponentBound("u","Input",false,0,1,"1").Evaluate(V(0),V(input)),Is.EqualTo(expected)); }
        [Test] public void DynamicAndEndpointResidualsAreNotTrackingErrorsAndBoundsDoNotSaturate()
        {
            using var s=Open(Constraints(.5)); var d=s.State.Result.Constraints;
            Assert.That(d.DynamicsResiduals.All(h=>h[0]==0),Is.True); Assert.That(d.DesiredDynamicsResiduals.Select(h=>h[0]),Is.EqualTo(new double[]{-1,2}));
            Assert.That(d.DesiredSatisfiesDynamics,Is.False); Assert.That(d.PathResiduals.Select(g=>g[0]),Is.EqualTo(new double[]{.5,.5}));
            Assert.That(d.EndpointResiduals[0],Is.EqualTo(2)); Assert.That(d.IsFeasible,Is.False);
            Assert.That(s.State.Result.History.Controls[0][0],Is.EqualTo(1),"Diagnostics never clamp an applied input.");
            s.Execute(SimulationCommand.SetParameter("constraint.input.limit",2));
            Assert.That(s.State.Result.Constraints.PathResiduals[0][0],Is.EqualTo(-1)); Assert.That(d.PathResiduals[0][0],Is.EqualTo(.5));
        }
        [Test] public void DesiredOutputsUseOutputMapNotStateIdentityAndPendulumStepIsNotARollout()
        {
            var model=new PendulumConfiguration(new PendulumParameters(1,1,9.81),.02);
            var reference=new SampledReference(new[]{V(0,0),V(.5,0),V(.5,0)},new[]{V(0),V(4.703164533707231)},new[]{"rad","rad/s"},new[]{"N·m"});
            var cost=new QuadraticTrackingCost(reference,V(2,0),V(1),V(2,0));
            using var s=new SimulationSession(new SimulationConfiguration("pendulum.reference",model,V(0,0),new[]{V(0),V(0)},cost:cost,constraints:new ConstraintConfiguration(Array.Empty<ComponentBound>(),Array.Empty<EndpointConstraint>())));
            Assert.That(s.State.Result.Reference.Outputs[1].Count,Is.EqualTo(1)); Assert.That(s.State.Result.Reference.Outputs[1][0],Is.EqualTo(.5));
            Assert.That(s.State.Result.Reference.Reference.States[1].Count,Is.EqualTo(2)); Assert.That(s.State.Result.Constraints.DesiredDynamicsResiduals[0][0],Is.EqualTo(-.5));
            Assert.That(s.State.Result.Constraints.DesiredSatisfiesDynamics,Is.False); Assert.That(s.State.Result.Constraints.IsFeasible,Is.True);
            Assert.That(s.State.Result.History.PhysicalTimeAt(2),Is.EqualTo(.04)); Assert.That(s.State.Result.Reference.Outputs[2][0],Is.EqualTo(.5));
        }
        [Test] public void ReferenceArraysAndContributionCollectionsAreImmutableAndMetadataIsValidated()
        {
            var states=new[]{V(0),V(1)}; var r=new SampledReference(states,new[]{V(0)},new[]{"1"},new[]{"1"}); states[0]=V(99); Assert.That(r.States[0][0],Is.Zero);
            Assert.Throws<NotSupportedException>(()=>((IList<NumericVector>)r.States)[0]=V(99));
            Assert.Throws<ArgumentException>(()=>new CostContribution(new[]{new CostTerm("same","Name","s","Description","1",1),new CostTerm("same","Name","s","Description","1",2)}));
            Assert.Throws<ArgumentException>(()=>new CostTerm("","Name","s","Description","1",1));
            Assert.Throws<ArgumentException>(()=>Constraints().Validate(new PendulumConfiguration(new PendulumParameters(1,1,9.81),.02).Descriptor));
        }
        [Test] public void ReferenceBindingsKeepTypeComponentShapeUnitsAndRevisionCentral()
        {
            using var s=Open();var m=new RuntimeMetricResolver();
            var vector=m.Resolve(s.State,"reference.input",FormulaValueKind.Vector,FormulaScope.CurrentTimestep,rows:1,columns:1,expectedUnits:new[]{"1"});
            Assert.That(vector.Validity,Is.EqualTo(MetricValidity.Available));Assert.That(vector.Result,Is.SameAs(s.State.Result));
            Assert.That(m.Resolve(s.State,"reference.input",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.Invalid));
            Assert.That(m.Resolve(s.State,"reference.input",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep,0).Value.Scalar,Is.Zero);
            Assert.That(m.Resolve(s.State,"reference.input",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep,1).Diagnostic,Does.Contain("fuori range"));
            Assert.That(m.Resolve(s.State,"reference.state",FormulaValueKind.Vector,FormulaScope.CurrentTimestep,expectedUnits:new[]{"m"}).Validity,Is.EqualTo(MetricValidity.Invalid));
            Assert.That(m.Resolve(s.State,"reference.state",FormulaValueKind.Vector,FormulaScope.CurrentTimestep,rows:2).Validity,Is.EqualTo(MetricValidity.Invalid));
        }
        [Test] public void DesiredOutputRequiringControlIsExplicitlyUnavailableAtTerminal()
        {
            using var s=new SimulationSession(new SimulationConfiguration("reference.output",new InputOutputModel(),V(1),new[]{V(1),V(1)},cost:Cost()));
            Assert.That(s.State.Result.Reference.Outputs.Select(v=>v?[0]),Is.EqualTo(new double?[]{0,3,null}));
            Assert.That(s.State.Result.Reference.Reference.States[2][0],Is.EqualTo(1));
            var value=new RuntimeMetricResolver().Resolve(s.State.Result,2,"reference.output",FormulaValueKind.Vector);
            Assert.That(value.Validity,Is.EqualTo(MetricValidity.NotApplicable));Assert.That(value.SampleIndex,Is.Null);
        }
        private sealed class InputOutputModel : ModelConfiguration,IDiscreteDynamics,IOutputFunction
        {
            public override ModelDescriptor Descriptor { get; }=new ModelDescriptor("test.input-output","1",new ModelDimensions(1,1,1),new[]{"1"},new[]{"1"},new[]{"1"});
            public override IReadOnlyList<ModelParameterValue> Parameters=>Array.Empty<ModelParameterValue>();
            public int Dimension=>1; public bool RequiresControl=>true;
            public override IDiscreteDynamics CreateDynamics()=>this;
            public override IOutputFunction CreateOutputFunction()=>this;
            public NumericVector Evaluate(int t,NumericVector x,NumericVector u)=>V(x[0]+u[0]);
            public override ModelConfiguration WithParameter(string target,double value)=>throw new ArgumentException();
            public override ModelConfiguration WithPhysicalStep(double? seconds)=>throw new ArgumentException();
        }
    }
}
