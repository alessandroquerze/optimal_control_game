using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;

namespace OptCon.Tests
{
    public sealed class ProductionLinearizationTests
    {
        const string A="Assets/OptimalControl/";
        private CourseDefinition Course=>AssetDatabase.LoadAssetAtPath<CourseDefinition>(A+"Data/Courses/OptimalControlCourse.asset");
        private LessonDefinition Definition=>Course.Chapters.Single().Lessons.Single(l=>l.Id=="C1.L2");
        private SimulationSession Open()=>Definition.Experiment.Model.CreateSession(Definition.Experiment);
        private readonly RuntimeMetricResolver metrics=new RuntimeMetricResolver();
        private static void Select(LessonSession l,string term)=>l.RecordTermSelection(l.Presentation.Step.FormulaId,term);
        private static void Next(LessonSession l,string answer)
        { Assert.That(l.Answer(answer),Is.True); Assert.That(l.Next(),Is.True,l.Presentation.Step?.Id); }
        private static void ToNominal(LessonSession l)
        {
            Select(l,"input.applied"); l.Execute(SimulationCommand.StepForward()); Next(l,"pair");
            Select(l,"state.current"); l.Execute(SimulationCommand.StepForward()); Next(l,"constant");
            l.Execute(SimulationCommand.SetInput(0,new NumericVector(9.81),InputEditScope.Tail)); Next(l,"balance");
        }
        private static void ToInitial(LessonSession l)
        {
            ToNominal(l); l.Execute(SimulationCommand.CaptureComparison()); Select(l,"nominal.state"); Next(l,"valid");
            l.Execute(SimulationCommand.SetInputPerturbation(0,0,1)); Select(l,"delta.input"); Next(l,"zero");
            Select(l,"A10"); Select(l,"B10"); Next(l,"nominal"); Select(l,"delta.next"); Next(l,"first");
            l.Execute(SimulationCommand.StepForward()); Select(l,"error"); Next(l,"first-only");
        }
        private static void Complete(LessonSession l)
        {
            ToInitial(l); l.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); l.Execute(SimulationCommand.StepForward()); Select(l,"error"); Next(l,"separate");
            l.Execute(SimulationCommand.SetInitialPerturbation(0,.8)); Next(l,"local"); Select(l,"error"); Next(l,"approximation");
            l.Execute(SimulationCommand.Seek(100)); Select(l,"delta.input"); Next(l,"terminal");
        }
        [Test] public void ProductionCatalogExposesIndependentC1L2AndOnlyC1L1Prerequisite()
        {
            Assert.That(Course.Chapters.Single().Lessons.Select(l=>l.Id),Is.EqualTo(new[]{"C1.L1","C1.L2","C1.L3","C1.L4","C1.L5","C1.L6"}));
            Assert.That(Definition.ContentVersion,Is.EqualTo("2")); Assert.That(Definition.SchemaVersion,Is.EqualTo(1));
            Assert.That(Definition.PrerequisiteLessonIds,Is.EqualTo(new[]{"C1.L1"}));
            Assert.That(Definition.Experiment.Id,Is.EqualTo("c1.linearization.pendulum")); Assert.That(Definition.Experiment.Cost,Is.Null);
            Assert.That(Definition.Experiment.ScenarioPrefab.name,Is.EqualTo("PendulumLinearizationScenario"));
            var validation=CatalogValidator.ValidateExecutionConfiguration(Course,verifySourceFiles:true);
            Assert.That(validation.IsValid,Is.True,string.Join("\n",validation.Issues));
        }
        [Test] public void PrototypeAndF10FixtureNeverSatisfyProductionPrerequisiteAndAdvisoryPolicyIsPreserved()
        {
            var store=new MemoryProgressStore(); store.Save(new ProgressDocument(new[]{
                new CompletionRecord("prototype.course","1","prototype.pendulum","1",1),
                new CompletionRecord("fixture.f10.course","1","fixture.f10.static","1",1)}));
            var progress=new ProgressionService(Course,store); var status=progress.GetStatus("C1.L2");
            Assert.That(status.IsCompleted,Is.False); Assert.That(status.MissingPrerequisiteIds,Is.EqualTo(new[]{"C1.L1"}));
            Assert.That(status.IsAvailable,Is.True,"F09 prerequisites remain advisory; no hidden promotion or hard-lock redesign.");
        }
        [Test] public void StepSequenceAndDisclosureSeparateFixedAndPerturbedInitialAndNeverExposeCosts()
        {
            Assert.That(Definition.Steps.Select(s=>s.Id),Is.EqualTo(new[]{"trajectory","equilibrium","balance","nominal","differences","jacobians","linear-model","compare-input","initial-perturbed","amplitude","error-meaning","terminal"}));
            Assert.That(Definition.Steps.All(s=>!s.ShowCosts && !s.VisibleLayerIds.Contains("cost") && !s.VisibleLayerIds.Contains("target")),Is.True);
            var fixedStep=Definition.Steps.Single(s=>s.Id=="differences");
            Assert.That(fixedStep.EnabledControlIds,Does.Contain(LessonControls.InputPerturbation)); Assert.That(fixedStep.EnabledControlIds,Does.Not.Contain(LessonControls.InitialPerturbation));
            Assert.That(Definition.Steps.Single(s=>s.Id=="initial-perturbed").EnabledControlIds,Does.Contain(LessonControls.InitialPerturbation));
            Assert.That(Definition.Steps.Single(s=>s.Id=="jacobians").FormulaFocus.Expression,Does.Contain("cos(θ̄_t)"));
            Assert.That(Definition.Steps.Single(s=>s.Id=="linear-model").FormulaFocus.Expression,Does.Contain("=A_t"));
            Assert.That(Definition.Steps.Single(s=>s.Id=="amplitude").FormulaFocus.Expression,Does.Contain("sin(ε)"));
        }
        [TestCase(LinearizationMetricSource.TrueDelta,2,"rad")]
        [TestCase(LinearizationMetricSource.LinearDelta,2,"rad")]
        [TestCase(LinearizationMetricSource.Prediction,2,"rad")]
        [TestCase(LinearizationMetricSource.Error,2,"rad")]
        [TestCase(LinearizationMetricSource.DeltaInput,1,"N·m")]
        public void TypedComparisonMetricsUseSameRevisionAndExplicitComponentSelection(string id,int rows,string firstUnit)
        {
            using var s=Open(); s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); s.Execute(SimulationCommand.Seek(3));
            var r=metrics.Resolve(s.State,id,FormulaValueKind.Vector,FormulaScope.CurrentTimestep);
            Assert.That(r.Validity,Is.EqualTo(MetricValidity.Available),r.Diagnostic); Assert.That(r.Value.Rows,Is.EqualTo(rows));
            Assert.That(r.Result,Is.SameAs(s.State.Result)); Assert.That(r.SampleIndex,Is.EqualTo(3)); Assert.That(r.Value.Units[0],Is.EqualTo(firstUnit));
            Assert.That(metrics.Resolve(s.State,id,FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.Invalid));
            Assert.That(metrics.Resolve(s.State,id,FormulaValueKind.Scalar,FormulaScope.CurrentTimestep,0).Validity,Is.EqualTo(MetricValidity.Available));
            Assert.That(metrics.Resolve(s.State,id,FormulaValueKind.Scalar,FormulaScope.CurrentTimestep,rows).Diagnostic,Does.Contain("fuori range"));
        }
        [Test] public void JacobianCellsKeepScalarKindRatioUnitsAndNominalEvaluation()
        {
            using var s=Open(); s.Execute(SimulationCommand.SetInitialState(new NumericVector(0,0))); s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInitialPerturbation(0,.8));
            var a=metrics.Resolve(s.State,"linearization.a.1.0",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep,expectedUnits:new[]{"(rad/s)/(rad)"});
            Assert.That(a.Validity,Is.EqualTo(MetricValidity.Available)); Assert.That(a.Value.Scalar,Is.EqualTo(-.1962).Within(1e-13)); Assert.That(a.SampleIndex,Is.Zero);
            Assert.That(metrics.Resolve(s.State,"linearization.b.1.0",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Value.Scalar,Is.EqualTo(.02));
            Assert.That(metrics.Resolve(s.State,"linearization.a.1.0",FormulaValueKind.Vector,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.Invalid));
            Assert.That(metrics.Resolve(s.State,"linearization.a.1.0",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep,expectedUnits:new[]{"kg"}).Validity,Is.EqualTo(MetricValidity.Invalid));
            Assert.That(metrics.Resolve(s.State,"linearization.a.2.0",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.Invalid));
        }
        [Test] public void TerminalHasSavedPredictionErrorAndNominalButNoInputNextOrJacobian()
        {
            using var s=Open(); s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.Seek(100));
            foreach(string id in new[]{LinearizationMetricSource.Prediction,LinearizationMetricSource.Error,LinearizationMetricSource.NominalState})
            { var r=metrics.Resolve(s.State,id,FormulaValueKind.Vector,FormulaScope.CurrentTimestep); Assert.That(r.Validity,Is.EqualTo(MetricValidity.Available)); Assert.That(r.SampleIndex,Is.EqualTo(100)); }
            foreach(string id in new[]{LinearizationMetricSource.DeltaInput,LinearizationMetricSource.NominalInput})
            { var r=metrics.Resolve(s.State,id,FormulaValueKind.Vector,FormulaScope.CurrentTimestep); Assert.That(r.Validity,Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(r.Value.Vector,Is.Null); Assert.That(r.SampleIndex,Is.Null); }
            Assert.That(metrics.Resolve(s.State,LinearizationMetricSource.NextLinearDelta,FormulaValueKind.Vector,FormulaScope.NextTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(metrics.Resolve(s.State,"linearization.a.1.0",FormulaValueKind.Scalar,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
        }
        [Test] public void NextIsResolvedOnceAndTerminalScopePreservesComparisonIdentity()
        {
            using var s=Open(); s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); s.Execute(SimulationCommand.Seek(2));
            var next=metrics.Resolve(s.State,LinearizationMetricSource.NextLinearDelta,FormulaValueKind.Vector,FormulaScope.NextTimestep);
            Assert.That(next.SampleIndex,Is.EqualTo(3)); Assert.That(next.Value.Vector,Is.SameAs(s.State.Linearization.LinearDeltaStates[3]));
            var terminal=metrics.Resolve(s.State,LinearizationMetricSource.Prediction,FormulaValueKind.Vector,FormulaScope.Terminal);
            Assert.That(terminal.SampleIndex,Is.EqualTo(100)); Assert.That(terminal.Value.Vector,Is.SameAs(s.State.Linearization.PredictedStates[100]));
            var initial=metrics.Resolve(s.State,LinearizationMetricSource.InitialDelta,FormulaValueKind.Vector,FormulaScope.Snapshot); Assert.That(initial.SampleIndex,Is.Null);
        }
        [Test] public void MissingNominalIsNAWithShapeUnitsAndDiagnosticAndOldSnapshotRetainsItsPair()
        {
            using var s=Open(); var missing=metrics.Resolve(s.State,LinearizationMetricSource.Error,FormulaValueKind.Vector,FormulaScope.CurrentTimestep);
            Assert.That(missing.Validity,Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(missing.Value.Rows,Is.EqualTo(2)); Assert.That(missing.Diagnostic,Is.Not.Empty);
            s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); s.Execute(SimulationCommand.Seek(1)); var a=s.State;
            s.Execute(SimulationCommand.SetInitialPerturbation(0,.8)); var b=s.State;
            Assert.That(metrics.Resolve(a,LinearizationMetricSource.Error,FormulaValueKind.Vector,FormulaScope.CurrentTimestep).Value.Vector,Is.SameAs(a.Linearization.Errors[1]));
            Assert.That(metrics.Resolve(b,LinearizationMetricSource.Error,FormulaValueKind.Vector,FormulaScope.CurrentTimestep).Value.Vector,Is.SameAs(b.Linearization.Errors[1]));
            Assert.That(a.Result.Configuration.InitialState[0],Is.Not.EqualTo(b.Result.Configuration.InitialState[0]));
        }
        [Test] public void WrongAnswerNoopAndDisallowedActionCannotAdvanceOrComplete()
        {
            using var s=Open(); using var l=new LessonSession(Definition,s);
            l.Answer("curve"); Assert.That(l.Next(),Is.False); Assert.That(l.Presentation.Feedback,Is.Not.Empty);
            Assert.That(l.Execute(SimulationCommand.SetInitialPerturbation(0,.1)),Is.False);
            ToInitial(l); Assert.That(l.Execute(SimulationCommand.SetInitialPerturbation(0,0)),Is.True);
            l.Execute(SimulationCommand.StepForward()); Select(l,"error"); l.Answer("separate"); Assert.That(l.Next(),Is.False);
            Assert.That(l.Presentation.IsCompleted,Is.False);
        }
        [Test] public void EntireLessonCompletesOnlyC1L2AndReloadDoesNotPromoteIncompleteLessons()
        {
            var store=new MemoryProgressStore(); var progress=new ProgressionService(Course,store);
            using(var s=Open()) using(var l=new LessonSession(Definition,s)) using(var binding=new LessonProgressBinding(l,s,progress,()=>true))
            { Complete(l); Assert.That(l.Presentation.IsCompleted,Is.True); Assert.That(progress.GetStatus("C1.L2").IsCompleted,Is.True); l.Restart(); Assert.That(store.SaveCount,Is.EqualTo(1)); }
            var reloaded=new ProgressionService(Course,store); Assert.That(reloaded.GetStatus("C1.L2").IsCompleted,Is.True); Assert.That(reloaded.GetStatus("C1.L1").IsCompleted,Is.False);
            Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo("C1.L2"));
        }
        [Test] public void PreviousRestartFailureAndDisposalInvalidateEducationalFacts()
        {
            using var s=Open(); using var l=new LessonSession(Definition,s); ToInitial(l);
            l.Execute(SimulationCommand.SetInitialPerturbation(0,.1)); l.Execute(SimulationCommand.StepForward()); Select(l,"error"); l.Answer("separate");
            Assert.That(l.Presentation.CanAdvance,Is.True); Assert.That(l.Previous(),Is.True); Assert.That(l.Presentation.CanAdvance,Is.False);
            Assert.That(l.Restart(),Is.True); Assert.That(l.Presentation.Step.Id,Is.EqualTo("trajectory")); Assert.That(s.State.Comparison,Is.Null);
            l.Explore(); Assert.That(l.Execute(SimulationCommand.SetParameter("model.mass",0)),Is.False); Assert.That(l.Next(),Is.False);
            l.Execute(SimulationCommand.Reset()); Assert.That(s.State.Error,Is.Null); l.Restart(); l.Dispose(); Assert.That(l.Next(),Is.False);
        }
        [Test] public void SmallMatricesUseStructuredAuthoringAndScalarCellsRatherThanMatrixRuntime()
        {
            var f=Definition.Formulas.Single(f=>f.Id=="c1.pendulum-jacobians"); f.Layout.Validate(f.Terms);
            Assert.That(f.Layout.Nodes.Count(n=>n.Kind==FormulaLayoutKind.Matrix),Is.EqualTo(2));
            Assert.That(f.Terms.Where(t=>t.Id.StartsWith("A")||t.Id.StartsWith("B")).All(t=>t.Value.Kind==FormulaValueKind.Scalar),Is.True);
            Assert.That(f.Assumptions,Does.Contain("riga")); Assert.That(f.Expression,Does.Contain("∇₁f_tᵀ"));
        }
        [Test] public void EveryPreviousEntryRestoresItsOwnMathematicalActivityWithoutMissingNominal()
        {
            using var s=Open(); using var l=new LessonSession(Definition,s); Complete(l);
            for(int target=10;target>=0;target--)
            {
                Assert.That(l.Previous(),Is.True); Assert.That(l.Presentation.StepIndex,Is.EqualTo(target));
                Assert.That(l.Presentation.CanAdvance,Is.False); Assert.That(l.Presentation.Error,Is.Null);
                string id=l.Presentation.Step.Id;
                if(target>=4) Assert.That(s.State.Linearization,Is.Not.Null,id);
                if(id=="amplitude")
                {
                    Assert.That(s.State.Sample.Timestep,Is.EqualTo(1)); Assert.That(s.State.Result.Configuration.InitialState[0],Is.EqualTo(.1));
                    Assert.That(s.State.Linearization.Errors[1][1],Is.EqualTo(.00003268365389231713).Within(1e-13));
                }
                if(id=="jacobians"||id=="linear-model"||id=="compare-input")
                {
                    Assert.That(s.State.Linearization.HasFixedInitialState,Is.True); Assert.That(s.State.Result.Configuration.Controls[0][0],Is.EqualTo(1));
                    Assert.That(s.State.Linearization.LinearDeltaStates[1].ToArray(),Is.EqualTo(new[]{0d,.02}));
                }
            }
        }
    }
}
