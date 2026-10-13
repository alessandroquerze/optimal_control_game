using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Presentation;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine;

namespace OptCon.Tests
{
    public sealed class ProductionContinuousTransferTests
    {
        const string A = "Assets/OptimalControl/";
        private CourseDefinition Course => AssetDatabase.LoadAssetAtPath<CourseDefinition>(A+"Data/Courses/OptimalControlCourse.asset");
        private LessonDefinition Definition => Course.Chapters.Single().Lessons.Single(l=>l.Id=="C1.L6");
        private SimulationSession Open() => Definition.Experiment.Model.CreateSession(Definition.Experiment);
        private static void Select(LessonSession l,string id) => l.RecordTermSelection(l.Presentation.Step.FormulaId,id);
        private static void Act(LessonSession l)
        {
            switch(l.Presentation.Step.Id)
            {
                case "continuous": Select(l,"duration"); Select(l,"terminal"); break;
                case "dynamics": Select(l,"time.index"); Select(l,"time.physical"); Select(l,"time.step"); break;
                case "equilibria": l.Execute(SimulationCommand.Seek(10)); Select(l,"reference.state"); Select(l,"reference.input"); break;
                case "desired": Select(l,"desired-h"); break;
                case "density": Select(l,"density"); Select(l,"cell"); break;
                case "weights": l.Execute(SimulationCommand.SetParameter("cost.q.0",2)); break;
                case "cumulative": l.Execute(SimulationCommand.Seek(20)); Select(l,"cumulative"); Select(l,"terminal"); break;
                case "manual": l.Execute(SimulationCommand.SetInput(0,new NumericVector(1),InputEditScope.Tail)); Select(l,"state.next"); break;
                case "refinement": l.Execute(SimulationCommand.SetPhysicalStep(.05)); Select(l,"cost-difference"); break;
                case "accuracy": l.Execute(SimulationCommand.SetPhysicalStep(.05)); Select(l,"oracle-error"); break;
                case "definitions": Select(l,"cell"); break;
                case "terminal": l.Execute(SimulationCommand.Seek(20)); Select(l,"input.applied"); Select(l,"density"); Select(l,"cell"); break;
                default: Assert.Fail("Uncovered F15 activity: "+l.Presentation.Step.Id); break;
            }
        }
        private static void Finish(LessonSession l)
        {
            Act(l); Assert.That(l.Answer(l.Presentation.Step.Id+"-ok"),Is.True);
            Assert.That(l.Next(),Is.True,l.Presentation.Step?.Id+": "+l.Presentation.Error);
        }
        private static void Reach(LessonSession l,string id) { while(l.Presentation.Step.Id!=id) Finish(l); }
        [Test] public void SavedProductionCatalogHasSixLessonsAndF15IsDistinctAndAvailable()
        {
            Assert.That(Course.Chapters.Single().Lessons.Select(l=>l.Id),Is.EqualTo(Enumerable.Range(1,6).Select(i=>"C1.L"+i)));
            Assert.That(Definition.ContentVersion,Is.EqualTo("1")); Assert.That(Definition.SchemaVersion,Is.EqualTo(1));
            Assert.That(Definition.PrerequisiteLessonIds,Is.EqualTo(new[]{"C1.L5"}));
            Assert.That(Definition.Experiment.Id,Is.EqualTo("c1.transfer.pendulum")); Assert.That(Definition.Experiment.Provider,Is.TypeOf<PendulumTransferModelDefinition>());
            var progress=new ProgressionService(Course,new MemoryProgressStore()); Assert.That(progress.GetStatus("C1.L6").IsAvailable,Is.True); Assert.That(progress.GetStatus("C1.L6").IsCompleted,Is.False);
            var report=CatalogValidator.ValidateExecutionConfiguration(Course,verifySourceFiles:true);
            Assert.That(report.IsValid,Is.True,string.Join("\n",report.Issues));
        }
        [Test] public void SourceDensityHasNoHalfAndTerminalIsOutsideQuadrature()
        {
            using var s=Open(); var r=s.State.Result; var cost=(ContinuousTrackingCost)r.Configuration.Cost;
            Assert.That(cost.Semantics,Is.EqualTo(CostSemantics.ContinuousFunctionalLeftRectangle));
            // x=u=0, second equilibrium theta=.5, u_des=9.81*sin(.5), from tau=1 onward.
            double density=.25+.05*4.703164533707231*4.703164533707231;
            Assert.That(r.Costs.RunningDensities[9].Total,Is.Zero);
            Assert.That(r.Costs.RunningDensities[10].Total,Is.EqualTo(density).Within(1e-12));
            Assert.That(r.Costs.Stages[10].Total,Is.EqualTo(.1*density).Within(1e-12));
            Assert.That(r.Costs.Terminal.Total,Is.EqualTo(1).Within(1e-12)); Assert.That(r.Costs.Total,Is.EqualTo(density+1).Within(1e-12));
            Assert.That(r.Constraints.DesiredSatisfiesDynamics,Is.False);
        }
        [Test] public void TwoGridsSampleSamePhysicalProfilesWithoutReinterpretingA()
        {
            using var s=Open(); s.Execute(SimulationCommand.SetInput(0,new NumericVector(1),InputEditScope.Tail));
            var a=s.State.Result; var at=a.Costs.Total; s.Execute(SimulationCommand.CaptureComparison()); s.Execute(SimulationCommand.SetPhysicalStep(.05)); var b=s.State.Result;
            Assert.That(a.Configuration.Horizon,Is.EqualTo(20)); Assert.That(b.Configuration.Horizon,Is.EqualTo(40));
            Assert.That(a.History.GetSnapshot(20).PhysicalTimeSeconds,Is.EqualTo(2)); Assert.That(b.History.GetSnapshot(40).PhysicalTimeSeconds,Is.EqualTo(2));
            Assert.That(a.Configuration.Reference.States[9][0],Is.Zero); Assert.That(a.Configuration.Reference.States[10][0],Is.EqualTo(.5));
            Assert.That(b.Configuration.Reference.States[19][0],Is.Zero); Assert.That(b.Configuration.Reference.States[20][0],Is.EqualTo(.5));
            Assert.That(a.Configuration.Reference,Is.Not.SameAs(b.Configuration.Reference)); Assert.That(a.Costs.Total,Is.EqualTo(at));
            Assert.That(s.State.DiscretizationComparison.A,Is.SameAs(a)); Assert.That(s.State.DiscretizationComparison.B,Is.SameAs(b)); Assert.That(s.State.DiscretizationComparison.SamePhysicalProblem,Is.True);
            Assert.That(b.Costs.Total,Is.Not.EqualTo(at));
            var resolver=new RuntimeMetricResolver();
            Assert.That(resolver.Resolve(a,0,"time.step",FormulaValueKind.Scalar).Value.Scalar,Is.EqualTo(.1));
            Assert.That(resolver.Resolve(b,0,"time.step",FormulaValueKind.Scalar).Value.Scalar,Is.EqualTo(.05));
            Assert.That(resolver.Resolve(a,0,"continuous.total",FormulaValueKind.Scalar,FormulaScope.Snapshot).Value.Scalar,Is.EqualTo(at));
            s.Execute(SimulationCommand.SetParameter("cost.q.0",2)); Assert.That(s.State.DiscretizationComparison.SamePhysicalProblem,Is.False); Assert.That(a.Costs.Total,Is.EqualTo(at));
        }
        [Test] public void RefinementErrorUsesAnIndependentAnalyticOracle()
        {
            using var s=Open(); s.Execute(SimulationCommand.SetParameter("model.gravity",0)); s.Execute(SimulationCommand.SetInput(0,new NumericVector(1),InputEditScope.Tail)); var a=s.State.Result;
            Assert.That(a.Accuracy.ExactTerminal,Is.EqualTo(new[]{2d,2d})); Assert.That(a.Accuracy.TerminalError[0],Is.EqualTo(-.1).Within(1e-12));
            s.Execute(SimulationCommand.SetPhysicalStep(.05)); var b=s.State.Result;
            Assert.That(b.Accuracy.ExactTerminal,Is.EqualTo(new[]{2d,2d})); Assert.That(b.Accuracy.TerminalError[0],Is.EqualTo(-.05).Within(1e-12));
            Assert.That(a.Accuracy.TerminalError[0],Is.EqualTo(-.1).Within(1e-12));
            s.Execute(SimulationCommand.SetInput(1,new NumericVector(2),InputEditScope.Single)); Assert.That(s.State.Result.Accuracy,Is.Null);
            s.Execute(SimulationCommand.Reset()); Assert.That(s.State.Result.Accuracy,Is.Null);
        }
        [Test] public void CentralResolverKeepsTimeUnitsIndicesAndTerminalUnavailableExplicit()
        {
            using var s=Open(); var r=s.State.Result; var resolver=new RuntimeMetricResolver();
            MetricResolution M(string id,int t=10,FormulaScope scope=FormulaScope.CurrentTimestep)=>resolver.Resolve(r,t,id,FormulaValueKind.Scalar,scope);
            Assert.That(M("time.index").Value.Scalar,Is.EqualTo(10)); Assert.That(M("time.physical").Value.Scalar,Is.EqualTo(1)); Assert.That(M("time.step").Value.Scalar,Is.EqualTo(.1));
            Assert.That(M("continuous.duration",scope:FormulaScope.Snapshot).Value.Scalar,Is.EqualTo(2)); Assert.That(M("continuous.transitions",scope:FormulaScope.Snapshot).Value.Scalar,Is.EqualTo(20));
            Assert.That(M("continuous.density").Value.Units.Single(),Is.EqualTo("1/s")); Assert.That(M("continuous.contribution").Value.Units.Single(),Is.EqualTo("1")); Assert.That(M("continuous.density").SampleIndex,Is.EqualTo(10));
            foreach(string id in new[]{"continuous.density","continuous.contribution","input.applied","state.next"})
                Assert.That(resolver.Resolve(r,20,id,id.StartsWith("continuous.")?FormulaValueKind.Scalar:FormulaValueKind.Vector,id=="state.next"?FormulaScope.NextTimestep:FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable),id);
            Assert.That(M("continuous.terminal",20,FormulaScope.Terminal).Value.Scalar,Is.EqualTo(1));
            Assert.That(M("continuous.cumulative",20).Value.Scalar+M("continuous.terminal",20,FormulaScope.Terminal).Value.Scalar,Is.EqualTo(r.Costs.Total));
            foreach(var p in r.Configuration.Cost.Parameters.Where(p=>p.Target.StartsWith("cost.q.")||p.Target.StartsWith("cost.r."))) Assert.That(p.Unit,Does.EndWith("/s"));
            foreach(var p in r.Configuration.Cost.Parameters.Where(p=>p.Target.StartsWith("cost.qt."))) Assert.That(p.Unit,Does.Not.EndWith("/s"));
        }
        [Test] public void FailedRequestedConfigurationPreservesGridMetricsAndBlocksInputUntilReset()
        {
            using var s=Open(); s.Execute(SimulationCommand.SetPhysicalStep(.02)); s.Execute(SimulationCommand.Seek(50)); var a=s.State.Result;
            s.Execute(SimulationCommand.SetInitialState(new NumericVector(1e200,0))); Assert.That(s.State.Error,Is.Not.Null); Assert.That(s.State.Result,Is.SameAs(a));
            s.Execute(SimulationCommand.SetPhysicalStep(2)); Assert.That(s.State.Error,Is.Not.Null); Assert.That(s.State.Result,Is.SameAs(a));
            Assert.DoesNotThrow(()=>s.Execute(SimulationCommand.SetInput(50,new NumericVector(1)))); Assert.That(s.State.TryGetEditableInput(50,out _),Is.False);
            Assert.That(a.Configuration.Horizon,Is.EqualTo(100)); Assert.That(a.Configuration.Reference.States.Count,Is.EqualTo(101));
            s.Execute(SimulationCommand.Reset()); Assert.That(s.State.Error,Is.Null); Assert.That(s.State.Result.Configuration.Horizon,Is.EqualTo(20));
        }
        [Test] public void IndexOnlyResolverKeepsIndexButHasNoContinuousFunctionalOrSeconds()
        {
            using var s=IndexOnlyConfiguration.CreateSession(); var metrics=new RuntimeMetricResolver();
            Assert.That(metrics.Resolve(s.State.Result,0,"time.index",FormulaValueKind.Scalar).Validity,Is.EqualTo(MetricValidity.Available));
            foreach(string id in new[]{"time.physical","time.step","continuous.density","continuous.contribution","continuous.cumulative"})
                Assert.That(metrics.Resolve(s.State.Result,0,id,FormulaValueKind.Scalar).Validity,Is.EqualTo(MetricValidity.NotApplicable),id);
            foreach(string id in new[]{"continuous.duration","continuous.transitions","continuous.total"})
                Assert.That(metrics.Resolve(s.State.Result,0,id,FormulaValueKind.Scalar,FormulaScope.Snapshot).Validity,Is.EqualTo(MetricValidity.NotApplicable),id);
        }
        [TestCase(null)] [TestCase(0d)] [TestCase(-.1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(.03)]
        public void BadStepDoesNotDestroyLastValidResult(double? delta)
        {
            using var s=Open(); var a=s.State.Result; Assert.DoesNotThrow(()=>s.Execute(SimulationCommand.SetPhysicalStep(delta)));
            Assert.That(s.State.Error,Is.Not.Null); Assert.That(s.State.Result,Is.SameAs(a)); s.Execute(SimulationCommand.Reset()); Assert.That(s.State.Error,Is.Null);
        }
        [TestCase("stateWeightUnits")] [TestCase("inputWeightUnits")] [TestCase("terminalWeightUnits")]
        public void EditorialWeightUnitMismatchIsRejectedAndDiagnosedByCatalog(string field)
        {
            var definition=UnityEngine.Object.Instantiate(Definition.Experiment.Cost); var experiment=UnityEngine.Object.Instantiate(Definition.Experiment);
            var lesson=UnityEngine.Object.Instantiate(Definition); var chapter=UnityEngine.Object.Instantiate(Course.Chapters.Single()); var course=UnityEngine.Object.Instantiate(Course);
            try { var so=new SerializedObject(definition); so.FindProperty(field).GetArrayElementAtIndex(0).stringValue="kg"; so.ApplyModifiedPropertiesWithoutUndo();
                var e=new SerializedObject(experiment); e.FindProperty("cost").objectReferenceValue=definition; e.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentException>(()=>experiment.Model.CreateConfiguration(experiment));
                var l=new SerializedObject(lesson); l.FindProperty("experiment").objectReferenceValue=experiment; l.ApplyModifiedPropertiesWithoutUndo();
                var c=new SerializedObject(chapter); c.FindProperty("lessons").arraySize=1; c.FindProperty("lessons").GetArrayElementAtIndex(0).objectReferenceValue=lesson; c.ApplyModifiedPropertiesWithoutUndo();
                var catalog=new SerializedObject(course); catalog.FindProperty("chapters").GetArrayElementAtIndex(0).objectReferenceValue=chapter; catalog.ApplyModifiedPropertiesWithoutUndo();
                var report=CatalogValidator.ValidateExecutionConfiguration(course); Assert.That(report.IsValid,Is.False); Assert.That(string.Join("\n",report.Issues),Does.Contain("units")); }
            finally { foreach(var o in new UnityEngine.Object[]{definition,experiment,lesson,chapter,course}) UnityEngine.Object.DestroyImmediate(o); }
        }
        [Test] public void FormulaBindingsAndSemanticTargetsAreConsistent()
        {
            using var s=Open(); var resolver=new RuntimeMetricResolver(); var roles=Definition.Experiment.ScenarioPrefab.GetComponentsInChildren<HighlightTarget>(true).Select(t=>t.SemanticRole).ToArray();
            Assert.That(Definition.Formulas.Count,Is.EqualTo(9));
            foreach(var f in Definition.Formulas) foreach(var term in f.Terms)
            {
                if(!string.IsNullOrWhiteSpace(term.Highlight?.SemanticRole)) Assert.That(roles,Does.Contain(term.Highlight.SemanticRole),f.Id+"/"+term.Id);
                if(!string.IsNullOrWhiteSpace(term.Value?.MetricId)) Assert.That(resolver.Resolve(s.State.Result,0,term.Value.MetricId,term.Value.Kind,term.Value.Scope,term.Value.Component).Validity,Is.Not.EqualTo(MetricValidity.Invalid),f.Id+"/"+term.Id);
            }
            Assert.That(Definition.Experiment.ScenarioPrefab.GetComponentsInChildren<ContinuousFunctionalView>(true).Length,Is.EqualTo(1));
        }
        [Test] public void FullProductionLessonPersistsOnlyItsStableIdentityAndRestartDoesNotDuplicate()
        {
            var store=new MemoryProgressStore(); var progress=new ProgressionService(Course,store); using var s=Open(); using var l=new LessonSession(Definition,s); using var binding=new LessonProgressBinding(l,s,progress,()=>true);
            while(!l.Presentation.IsCompleted) Finish(l);
            Assert.That(progress.GetStatus("C1.L6").IsCompleted,Is.True); Assert.That(progress.GetStatus("C1.L5").IsCompleted,Is.False); Assert.That(store.SaveCount,Is.EqualTo(1));
            l.Restart(); while(!l.Presentation.IsCompleted) Finish(l); Assert.That(store.SaveCount,Is.EqualTo(1)); Assert.That(new ProgressionService(Course,store).GetStatus("C1.L6").IsCompleted,Is.True);
        }
        [TestCase("weights")] [TestCase("manual")] [TestCase("refinement")] [TestCase("accuracy")]
        public void AnswersNeverReplaceOutcomeAndPreviousRequiresFreshEvidence(string id)
        {
            using var s=Open(); using var l=new LessonSession(Definition,s); Reach(l,id); l.Answer(id+"-ok"); Assert.That(l.Next(),Is.False);
            Act(l); l.Answer("wrong"); Assert.That(l.Next(),Is.False); l.Answer(id+"-ok"); Assert.That(l.Next(),Is.True);
            Assert.That(l.Previous(),Is.True); l.Answer(id+"-ok"); Assert.That(l.Next(),Is.False); Act(l); Assert.That(l.Next(),Is.True);
        }
        [TestCase("refinement")] [TestCase("accuracy")]
        public void LargerDeltaOrChangedProblemCannotSatisfyRefinement(string id)
        {
            using var s=Open(); using var l=new LessonSession(Definition,s); Reach(l,id); l.Answer(id+"-ok");
            l.Execute(SimulationCommand.SetPhysicalStep(.2)); Select(l,id=="refinement"?"cost-difference":"oracle-error"); Assert.That(l.Next(),Is.False);
            Assert.That(s.Execute(SimulationCommand.SetParameter("cost.q.0",2)),Is.True); l.Execute(SimulationCommand.SetPhysicalStep(.05)); Assert.That(l.Next(),Is.False);
        }
        [Test] public void FailedEntryCannotCommitCompletionAndRestartRecovers()
        {
            var definition=UnityEngine.Object.Instantiate(Definition);
            try
            {
                var so=new SerializedObject(definition); var action=so.FindProperty("steps").GetArrayElementAtIndex(1).FindPropertyRelative("entryActions").GetArrayElementAtIndex(0);
                action.FindPropertyRelative("kind").enumValueIndex=(int)SimulationCommandKind.Seek; action.FindPropertyRelative("index").intValue=21; so.ApplyModifiedPropertiesWithoutUndo();
                var store=new MemoryProgressStore();var progress=new ProgressionService(Course,store);using var s=Open();using var l=new LessonSession(definition,s);using var binding=new LessonProgressBinding(l,s,progress,()=>true);
                Act(l);l.Answer("continuous-ok");Assert.That(l.Next(),Is.False);Assert.That(l.Presentation.StepIndex,Is.Zero);Assert.That(l.Presentation.Error,Is.Not.Null);
                Assert.That(l.Next(),Is.False);Assert.That(store.Load(),Is.Null);Assert.That(l.Restart(),Is.True);Assert.That(l.Presentation.Error,Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }
        [Test] public void EarlierLessonCompletionNeverAccreditsF15()
        {
            var store=new MemoryProgressStore(); store.Save(new ProgressDocument(new[]{new CompletionRecord("optimal-control","1","C1.L5","1",1)}));
            var progress=new ProgressionService(Course,store); Assert.That(progress.GetStatus("C1.L6").IsCompleted,Is.False); Assert.That(progress.GetStatus("C1.L6").MissingPrerequisiteIds,Is.Empty);
        }
    }
}
