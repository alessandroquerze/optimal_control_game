using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Tests
{
    public sealed class LinearizationTests
    {
        private static SimulationSession Pendulum(double angle = 0, int horizon = 4) => new SimulationSession(
            new SimulationConfiguration("linearization.test", new PendulumConfiguration(new PendulumParameters(1, 1, 9.81), .02),
                new NumericVector(angle, 0), Enumerable.Repeat(new NumericVector(0), horizon).ToArray()));
        [Test] public void StandardJacobianKnownAnswerAtOriginHasCorrectOrientationAndEulerStep()
        {
            var d = new PendulumDynamics(new PendulumParameters(1, 1, 9.81), .02);
            var a = d.StateJacobian(0, new NumericVector(0, 0), new NumericVector(0));
            var b = d.InputJacobian(0, new NumericVector(0, 0), new NumericVector(0));
            Assert.That(a.Rows, Is.EqualTo(2)); Assert.That(a.Columns, Is.EqualTo(2));
            Assert.That(new[] { a[0,0], a[0,1], a[1,0], a[1,1] }, Is.EqualTo(new[] { 1d, .02, -.1962, 1d }).Within(1e-13));
            Assert.That(b.Rows, Is.EqualTo(2)); Assert.That(b.Columns, Is.EqualTo(1));
            Assert.That(new[] { b[0,0], b[1,0] }, Is.EqualTo(new[] { 0d, .02 }).Within(1e-13));
        }
        [TestCase(0)] [TestCase(.6)] [TestCase(-1.2)]
        public void AnalyticJacobiansAgreeWithIndependentCentralDifferences(double angle)
        {
            var d = new PendulumDynamics(new PendulumParameters(2, 3, 9.81), .03);
            var x = new NumericVector(angle, .7); var u = new NumericVector(-1.3); const double h = 1e-6;
            var a = d.StateJacobian(7, x, u); var b = d.InputJacobian(7, x, u);
            for (int c=0;c<2;c++)
            {
                var plus=x.ToArray(); var minus=x.ToArray(); plus[c]+=h; minus[c]-=h;
                var p=d.Evaluate(7,new NumericVector(plus),u); var m=d.Evaluate(7,new NumericVector(minus),u);
                for(int r=0;r<2;r++) Assert.That(a[r,c], Is.EqualTo((p[r]-m[r])/(2*h)).Within(2e-10));
            }
            var up=d.Evaluate(7,x,new NumericVector(u[0]+h)); var um=d.Evaluate(7,x,new NumericVector(u[0]-h));
            for(int r=0;r<2;r++) Assert.That(b[r,0], Is.EqualTo((up[r]-um[r])/(2*h)).Within(2e-10));
        }
        [Test] public void MatrixCopiesEntriesAndOnlyMultipliesCompatibleFiniteVectors()
        {
            var values=new[] { 1d,2,3,4 }; var a=new NumericMatrix(2,2,values); values[0]=99;
            Assert.That(a.Multiply(new NumericVector(5,6)).ToArray(), Is.EqualTo(new[] {17d,39}));
            Assert.Throws<ArgumentException>(()=>a.Multiply(new NumericVector(1)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new NumericMatrix(1,1,double.NaN));
            Assert.Throws<ArgumentException>(()=>new NumericMatrix(2,1,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>{var ignored=a[2,0];});
        }
        [TestCase(-1,2,1)] [TestCase(0,1,1)] [TestCase(0,2,2)]
        public void DerivativesRejectInvalidIndexAndShape(int t,int n,int m)
        {
            var d=new PendulumDynamics(new PendulumParameters(1,1,9.81),.02);
            Assert.Catch<ArgumentException>(()=>d.StateJacobian(t,new NumericVector(new double[n]),new NumericVector(new double[m])));
            Assert.Catch<ArgumentException>(()=>d.InputJacobian(t,new NumericVector(new double[n]),new NumericVector(new double[m])));
        }
        [Test] public void ZeroEquilibriumAndNonzeroBalancedAngleRemainStationaryAcrossWholeHorizon()
        {
            using var s=Pendulum(0,100); Assert.That(s.State.Result.IsConstantStateInput,Is.True);
            Assert.That(s.State.Result.History.States.All(x=>x.SequenceEqual(new double[] {0,0})),Is.True);
            s.Execute(SimulationCommand.SetInitialState(new NumericVector(Math.PI/2,0)));
            Assert.That(s.State.Result.IsConstantStateInput,Is.False);
            s.Execute(SimulationCommand.SetInput(0,new NumericVector(9.81),InputEditScope.Tail));
            Assert.That(s.State.Result.IsConstantStateInput,Is.True);
            Assert.That(s.State.Result.History.States.All(x=>x[0]==Math.PI/2 && x[1]==0),Is.True);
        }
        [Test] public void FixedInitialInputPerturbationHasKnownFirstAndSecondLinearStates()
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison());
            s.Execute(SimulationCommand.SetInputPerturbation(0,0,1)); var c=s.State.Linearization;
            Assert.That(c.HasFixedInitialState,Is.True); Assert.That(c.LinearDeltaStates[0].ToArray(),Is.EqualTo(new double[]{0,0}));
            Assert.That(c.LinearDeltaStates[1].ToArray(),Is.EqualTo(new[]{0d,.02}).Within(1e-13));
            Assert.That(c.LinearDeltaStates[2].ToArray(),Is.EqualTo(new[]{.0004,.02}).Within(1e-13));
            Assert.That(c.Errors[1].ToArray(),Is.EqualTo(new double[]{0,0}));
            Assert.That(c.DeltaInputs.Count,Is.EqualTo(4)); Assert.That(c.PredictedStates.Count,Is.EqualTo(5));
            Assert.That(s.State.Result.Configuration.Controls[1][0],Is.Zero);
        }
        [TestCase(.1,-.019587316346107685,-.01962,.00003268365389231713)]
        [TestCase(.8,-.1407452650344864,-.15696,.01621473496551364)]
        public void PerturbedInitialHasIndependentNonlinearLinearAndErrorKnownAnswers(double angle,double truth,double linear,double error)
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInitialPerturbation(0,angle));
            var c=s.State.Linearization;
            Assert.That(c.HasFixedInitialState,Is.False); Assert.That(c.LinearDeltaStates[0][0],Is.EqualTo(angle));
            Assert.That(c.Perturbed.History.States[1][1],Is.EqualTo(truth).Within(1e-13));
            Assert.That(c.PredictedStates[1][1],Is.EqualTo(linear).Within(1e-13));
            Assert.That(c.Errors[1][1],Is.EqualTo(error).Within(1e-13));
            Assert.That(c.Errors[1][0],Is.Zero); Assert.That(c.DeltaInputs.All(x=>x[0]==0),Is.True);
        }
        [Test] public void JacobiansAreEvaluatedAlongNominalNotPerturbedState()
        {
            using var s=Pendulum(.6); s.Execute(SimulationCommand.CaptureComparison()); var a=s.State.Result;
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.8)); var c=s.State.Linearization;
            Assert.That(c.A[0][1,0],Is.EqualTo(-.161930847645279).Within(1e-13)); // -0.1962 cos(.6), independent oracle
            Assert.That(c.Nominal,Is.SameAs(a));
            Assert.That(c.A[1][1,0],Is.EqualTo(-.1962*Math.Cos(a.History.States[1][0])).Within(1e-13));
            Assert.That(c.A[2][1,0],Is.Not.EqualTo(-.1962*Math.Cos(c.Perturbed.History.States[2][0])).Within(1e-4));
        }
        [Test] public void LocalErrorIsSecondOrderAtNonzeroNominalAngle()
        {
            // For a one-step perturbation at pi/6: e_omega/eps² -> .1962 sin(pi/6)/2 = .04905.
            using var s=Pendulum(Math.PI/6,1); s.Execute(SimulationCommand.CaptureComparison());
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.001)); double e1=s.State.Linearization.Errors[1][1];
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.0005)); double e2=s.State.Linearization.Errors[1][1];
            Assert.That(e1/1e-6,Is.EqualTo(.04905).Within(.00004));
            Assert.That(e1/e2,Is.EqualTo(4).Within(.003));
        }
        [Test] public void ZeroNominalHasCubicLeadingSineErrorRatherThanFalseQuadraticClaim()
        {
            using var s=Pendulum(0,1); s.Execute(SimulationCommand.CaptureComparison());
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.01)); double e1=s.State.Linearization.Errors[1][1];
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.005)); double e2=s.State.Linearization.Errors[1][1];
            Assert.That(e1/e2,Is.EqualTo(8).Within(.001));
        }
        [Test] public void RetainedComparisonAndSamplesKeepTheirOwnRevisionAfterNewPerturbationAndFailure()
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInitialPerturbation(0,.1));
            var a=s.State; var values=a.Linearization.Errors.Select(x=>x.ToArray()).ToArray();
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.8)); var b=s.State;
            Assert.That(a.Linearization.Perturbed,Is.SameAs(a.Result)); Assert.That(b.Linearization.Perturbed,Is.SameAs(b.Result));
            Assert.That(a.Result.Configuration.InitialState[0],Is.EqualTo(.1)); Assert.That(b.Result.Configuration.InitialState[0],Is.EqualTo(.8));
            for(int i=0;i<values.Length;i++) Assert.That(a.Linearization.Errors[i].ToArray(),Is.EqualTo(values[i]));
            Assert.That(s.Execute(SimulationCommand.SetParameter("model.mass",0)),Is.False);
            Assert.That(s.State.Result,Is.SameAs(b.Result)); Assert.That(s.State.Linearization,Is.SameAs(b.Linearization));
            Assert.That(s.Execute(SimulationCommand.SetInitialPerturbation(0,.2)),Is.False);
            Assert.That(s.Execute(SimulationCommand.Reset()),Is.True); Assert.That(s.State.Error,Is.Null);
        }
        [Test] public void SeekAndPlaybackReuseTheComparisonWithoutRecomputingJacobians()
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison()); var c=s.State.Linearization;
            s.Execute(SimulationCommand.Seek(3)); Assert.That(s.State.Linearization,Is.SameAs(c));
            s.Execute(SimulationCommand.Play()); s.AdvancePlayback(.2); Assert.That(s.State.Linearization,Is.SameAs(c));
            Assert.That(s.State.Sample.IsTerminal,Is.True);
        }
        [Test] public void PhysicsOrHorizonChangeMakesComparisonExplicitlyUnavailableUntilRecapture()
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison());
            s.Execute(SimulationCommand.SetParameter("model.mass",2)); Assert.That(s.State.Linearization,Is.Null); Assert.That(s.State.LinearizationDiagnostic,Is.Not.Empty);
            Assert.That(s.Execute(SimulationCommand.SetInitialPerturbation(0,.1)),Is.False);
            s.Execute(SimulationCommand.CaptureComparison()); Assert.That(s.State.Linearization,Is.Not.Null);
            s.Execute(SimulationCommand.SetHorizon(1)); Assert.That(s.State.Linearization,Is.Null);
            s.Execute(SimulationCommand.CaptureComparison()); Assert.That(s.State.Linearization.Horizon,Is.EqualTo(1));
            s.Execute(SimulationCommand.ClearComparison()); Assert.That(s.State.Linearization,Is.Null);
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidPerturbationDoesNotInventARevision(double value)
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison()); var before=s.State;
            Assert.That(s.Execute(SimulationCommand.SetInitialPerturbation(0,value)),Is.False);
            Assert.That(s.State,Is.SameAs(before));
        }
        [Test] public void InvalidComponentAndTerminalInputAreRejectedAndSlidersDoNotAccumulate()
        {
            using var s=Pendulum(); s.Execute(SimulationCommand.CaptureComparison());
            Assert.That(s.Execute(SimulationCommand.SetInitialPerturbation(9,.1)),Is.False);
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); var first=s.State;
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); Assert.That(s.State,Is.SameAs(first));
            Assert.That(s.State.Result.Configuration.InitialState[0],Is.EqualTo(.1));
            s.Execute(SimulationCommand.Seek(4)); var before=s.State;
            Assert.That(s.Execute(SimulationCommand.SetInputPerturbation(4,0,1)),Is.False); Assert.That(s.State,Is.SameAs(before));
        }
        private sealed class Additive : ModelConfiguration, IDifferentiableDynamics
        {
            public override ModelDescriptor Descriptor {get;}=new ModelDescriptor("index.additive","1",new ModelDimensions(1,1,0),new[]{"1"},new[]{"1"},Array.Empty<string>());
            public override IReadOnlyList<ModelParameterValue> Parameters=>Array.Empty<ModelParameterValue>();
            public override IDiscreteDynamics CreateDynamics()=>this;
            public override ModelConfiguration WithParameter(string id,double value)=>throw new ArgumentException();
            public override ModelConfiguration WithPhysicalStep(double? value)=>throw new ArgumentException();
            public NumericVector Evaluate(int t,NumericVector x,NumericVector u)=>new NumericVector(x[0]+u[0]);
            public NumericMatrix StateJacobian(int t,NumericVector x,NumericVector u)=>new NumericMatrix(1,1,1);
            public NumericMatrix InputJacobian(int t,NumericVector x,NumericVector u)=>new NumericMatrix(1,1,1);
        }
        [Test] public void IndexOnlyDifferentiableDynamicsNeedsNoPhysicalTimeAndHasExactLinearPrediction()
        {
            using var s=new SimulationSession(new SimulationConfiguration("index",new Additive(),new NumericVector(2),new[]{new NumericVector(3)}));
            s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInputPerturbation(0,0,1)); var c=s.State.Linearization;
            Assert.That(c.Nominal.History.States[1][0],Is.EqualTo(5)); Assert.That(c.Perturbed.History.States[1][0],Is.EqualTo(6));
            Assert.That(c.PredictedStates[1][0],Is.EqualTo(6)); Assert.That(c.Errors[1][0],Is.Zero);
            Assert.That(c.Nominal.History.PhysicalTimeAt(1),Is.Null); Assert.That(c.Perturbed.History.Model.DiscretizationStepSeconds,Is.Null);
        }
    }
}
