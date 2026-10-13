using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Tests
{
    public sealed class ProductionCostReferenceTests
    {
        const string A="Assets/OptimalControl/";
        private CourseDefinition Course=>AssetDatabase.LoadAssetAtPath<CourseDefinition>(A+"Data/Courses/OptimalControlCourse.asset");
        private LessonDefinition Definition(string id)=>Course.Chapters.Single().Lessons.Single(l=>l.Id==id);
        private SimulationSession Open(string id)=>Definition(id).Experiment.Model.CreateSession(Definition(id).Experiment);
        private static void Select(LessonSession l,string id)=>l.RecordTermSelection(l.Presentation.Step.FormulaId,id);
        private static void Next(LessonSession l,string answer) { Assert.That(l.Answer(answer),Is.True);Assert.That(l.Next(),Is.True,l.Presentation.Step?.Id); }
        private static void Costs(LessonSession l)
        {
            Select(l,"total"); Next(l,"sum"); Select(l,"error"); Select(l,"state-part"); Next(l,"deviation");
            l.Execute(SimulationCommand.SetParameter("cost.q.0",4)); Next(l,"criterion");
            l.Execute(SimulationCommand.SetParameter("cost.r.0",.4)); Next(l,"weight");
            l.Execute(SimulationCommand.SetParameter("cost.qt.0",10)); Select(l,"terminal"); Next(l,"final");
            l.Execute(SimulationCommand.SetParameter("constraint.torque.limit",.5)); Next(l,"hard"); Select(l,"r"); Next(l,"endpoint");
            l.Execute(SimulationCommand.SetInput(0,new NumericVector(.75),InputEditScope.Tail)); Next(l,"infeasible");
            l.Execute(SimulationCommand.Seek(20)); Select(l,"input.applied"); Select(l,"stage"); Next(l,"terminal");
        }
        private static void Desired(LessonSession l)
        {
            Select(l,"reference.state"); Select(l,"reference.output"); Next(l,"different"); Select(l,"desired-h"); Next(l,"jump");
            l.Execute(SimulationCommand.SetInput(0,new NumericVector(1),InputEditScope.Tail)); l.Execute(SimulationCommand.StepForward()); Select(l,"h"); Next(l,"manual");
            Select(l,"error"); Select(l,"state-part"); Next(l,"same-sample");
            l.Execute(SimulationCommand.SetParameter("cost.reference.state.10.0",.8)); Select(l,"reference.state"); Next(l,"reference");
            Select(l,"g"); Next(l,"roles"); l.Execute(SimulationCommand.Seek(20)); Select(l,"reference.terminal"); Select(l,"input.applied"); Next(l,"terminal");
        }
        [Test] public void ProductionCatalogExposesBothF13LessonsWithStableIdentitiesAndAdvisoryPrerequisites()
        {
            Assert.That(Course.Chapters.Single().Lessons.Select(l=>l.Id),Is.EqualTo(new[]{"C1.L1","C1.L2","C1.L3","C1.L4","C1.L5","C1.L6"}));
            foreach(var id in new[]{"C1.L3","C1.L4"}) { var l=Definition(id); Assert.That(l.ContentVersion,Is.EqualTo(id=="C1.L3" ? "2" : "1")); Assert.That(l.SchemaVersion,Is.EqualTo(1)); Assert.That(l.Experiment.Id,Does.StartWith("c1.")); Assert.That(l.Experiment.ScenarioPrefab.name,Is.EqualTo("PendulumReferenceConstraintsScenario")); }
            Assert.That(Definition("C1.L3").PrerequisiteLessonIds,Is.EqualTo(new[]{"C1.L2"})); Assert.That(Definition("C1.L4").PrerequisiteLessonIds,Is.EqualTo(new[]{"C1.L3"}));
            var p=new ProgressionService(Course,new MemoryProgressStore()); Assert.That(p.GetStatus("C1.L3").IsAvailable,Is.True); Assert.That(p.GetStatus("C1.L4").IsCompleted,Is.False);
            var valid=CatalogValidator.ValidateExecutionConfiguration(Course,verifySourceFiles:true); Assert.That(valid.IsValid,Is.True,string.Join("\n",valid.Issues));
        }
        [Test] public void AuthoredSequenceDisclosesFormulaBeforeEditingAndSeparatesRoles()
        {
            Assert.That(Definition("C1.L3").Steps.Select(s=>s.Id),Is.EqualTo(new[]{"decisions","tracking","weight","effort","terminal-weight","path-limit","endpoint","cost-not-feasibility","terminal"}));
            Assert.That(Definition("C1.L4").Steps.Select(s=>s.Id),Is.EqualTo(new[]{"desired","jump","manual","tracking","change-reference","reference-not-constraint","terminal"}));
            foreach(var l in new[]{Definition("C1.L3"),Definition("C1.L4")}) foreach(var step in l.Steps)
            { Assert.That(step.FormulaFocus,Is.Not.Null); Assert.That(step.Instruction,Is.Not.Empty); Assert.That(step.EntryActions.First().CreateCommand().Kind,Is.EqualTo(SimulationCommandKind.Reset)); }
            Assert.That(Definition("C1.L3").Steps.Single(s=>s.Id=="weight").FormulaFocus.Expression,Does.Contain("Q_ii"));
            Assert.That(Definition("C1.L4").Steps.Single(s=>s.Id=="change-reference").FormulaFocus.Expression,Does.Contain("x_t^des"));
            Assert.That(Definition("C1.L4").Steps.Where(s=>s.Id!="reference-not-constraint").All(s=>s.VisibleLayerIds.Contains("desired")),Is.True);
        }
        [TestCase("C1.L3")] [TestCase("C1.L4")]
        public void EntireProductionLessonCompletesAndPersistsOnlyItsOwnId(string id)
        {
            var store=new MemoryProgressStore();var p=new ProgressionService(Course,store);
            using(var s=Open(id)) using(var l=new LessonSession(Definition(id),s)) using(var binding=new LessonProgressBinding(l,s,p,()=>true))
            { if(id=="C1.L3") Costs(l); else Desired(l); Assert.That(l.Presentation.IsCompleted,Is.True); Assert.That(p.GetStatus(id).IsCompleted,Is.True); l.Restart(); Assert.That(store.SaveCount,Is.EqualTo(1)); }
            var reload=new ProgressionService(Course,store); Assert.That(reload.GetStatus(id).IsCompleted,Is.True); Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo(id));
            foreach(var other in Course.Chapters.Single().Lessons.Where(l=>l.Id!=id)) Assert.That(reload.GetStatus(other.Id).IsCompleted,Is.False);
        }
        [TestCase("C1.L3")] [TestCase("C1.L4")]
        public void EveryPreviousEntryRestoresAutonomousConfigurationAndNoStudentCredit(string id)
        {
            using var s=Open(id);using var l=new LessonSession(Definition(id),s);if(id=="C1.L3") Costs(l);else Desired(l);
            for(int i=Definition(id).Steps.Count-2;i>=0;i--)
            {
                Assert.That(l.Previous(),Is.True);Assert.That(l.Presentation.StepIndex,Is.EqualTo(i));Assert.That(l.Presentation.Error,Is.Null);Assert.That(l.Presentation.CanAdvance,Is.False);
                Assert.That(s.State.Result.Costs,Is.Not.Null);Assert.That(s.State.Result.Reference,Is.Not.Null);
                if(l.Presentation.Step.Id=="change-reference") { Assert.That(s.State.Comparison,Is.Not.Null);Assert.That(s.State.Sample.Timestep,Is.EqualTo(10));Assert.That(s.State.Result.Reference.Reference.States[10][0],Is.EqualTo(.5)); }
                if(l.Presentation.Step.Id=="path-limit") Assert.That(s.State.Result.Constraints.Configuration.Path[0].Limit,Is.EqualTo(2));
            }
            Assert.That(l.Restart(),Is.True);Assert.That(s.State.Sample.Timestep,Is.Zero);Assert.That(l.Presentation.StepIndex,Is.Zero);
        }
        [Test] public void WrongAnswerNoopAndIncompleteNavigationCannotAwardProgress()
        {
            var store=new MemoryProgressStore();var p=new ProgressionService(Course,store);using var s=Open("C1.L3");using var l=new LessonSession(Definition("C1.L3"),s);using var binding=new LessonProgressBinding(l,s,p,()=>false);
            Select(l,"total");l.Answer("wrong");Assert.That(l.Next(),Is.False);Next(l,"sum");Select(l,"error");Select(l,"state-part");Next(l,"deviation");
            l.Execute(SimulationCommand.SetParameter("cost.q.0",2));l.Answer("criterion");Assert.That(l.Next(),Is.False,"No-op must not satisfy the requested edit.");
            Assert.That(l.Execute(SimulationCommand.SetParameter("model.mass",2)),Is.False);Assert.That(store.Load(),Is.Null);
            l.Execute(SimulationCommand.Reset());Assert.That(l.Presentation.StepIndex,Is.Zero);Assert.That(p.GetStatus("C1.L3").IsCompleted,Is.False);
        }
        [Test] public void WeightLimitAndReferenceEditsChangeOnlyTheirMathematicalDomains()
        {
            using var s=Open("C1.L3");var a=s.State.Result;
            s.Execute(SimulationCommand.SetParameter("cost.q.0",4));var b=s.State.Result;
            Assert.That(b.Costs.Stages[0].Find("state-deviation").Value-a.Costs.Stages[0].Find("state-deviation").Value,Is.EqualTo(.1225).Within(1e-12));
            for(int t=0;t<=20;t++) Assert.That(b.History.States[t],Is.SameAs(a.History.States[t]));
            s.Execute(SimulationCommand.SetParameter("constraint.torque.limit",.5));var c=s.State.Result;
            Assert.That(c.Costs.Total,Is.EqualTo(b.Costs.Total));Assert.That(c.Constraints.PathResiduals[0][0],Is.EqualTo(.5));Assert.That(c.History.Controls[0][0],Is.EqualTo(1));
            using var desired=Open("C1.L4");var d=desired.State.Result;desired.Execute(SimulationCommand.SetParameter("cost.reference.state.10.0",.8));var e=desired.State.Result;
            Assert.That(e.Costs.Stages[10].Find("state-deviation").Value-d.Costs.Stages[10].Find("state-deviation").Value,Is.EqualTo(.39).Within(1e-12));
            Assert.That(e.History.States[10],Is.SameAs(d.History.States[10]));Assert.That(d.Reference.Reference.States[10][0],Is.EqualTo(.5));
        }
        [Test] public void FormulaPresenterRendersRetainedReferenceAndNamedCostsFromItsOwnRevision()
        {
            using var s=Open("C1.L4");var a=s.State.Result;var f=Definition("C1.L4").Formulas.Single(f=>f.Id=="c1.sampled-tracking");
            var tree=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(A+"UI/LessonHud.uxml").CloneTree();using var presenter=new FormulaPresenter(tree,new[]{f},null,new RuntimeMetricResolver());
            s.Execute(SimulationCommand.SetParameter("cost.reference.state.10.0",.8));var b=s.State.Result;
            presenter.Render(a,10);Assert.That(presenter.Resolutions["reference.state"].Value.Vector[0],Is.EqualTo(.5));Assert.That(presenter.Resolutions["state-part"].Value.Scalar,Is.EqualTo(.25));Assert.That(presenter.Resolutions.Values.All(r=>ReferenceEquals(r.Result,a)),Is.True);
            presenter.Render(b,10);Assert.That(presenter.Resolutions["reference.state"].Value.Vector[0],Is.EqualTo(.8));Assert.That(presenter.Resolutions["state-part"].Value.Scalar,Is.EqualTo(.64).Within(1e-12));
        }
        [Test] public void ProductionJumpAndTerminalHaveCorrectResidualsReferencesOutputAndPhysicalTime()
        {
            using var s=Open("C1.L4");var r=s.State.Result;Assert.That(r.Constraints.DynamicsResiduals.All(v=>v.All(x=>x==0)),Is.True);
            Assert.That(r.Constraints.DesiredDynamicsResiduals[9].ToArray(),Is.EqualTo(new[]{-.5,0d}).Within(1e-12));Assert.That(r.Constraints.DesiredSatisfiesDynamics,Is.False);
            var m=new RuntimeMetricResolver();Assert.That(m.Resolve(r,9,"reference.state",FormulaValueKind.Vector).Value.Vector[0],Is.Zero);Assert.That(m.Resolve(r,10,"reference.state",FormulaValueKind.Vector).Value.Vector[0],Is.EqualTo(.5));
            Assert.That(r.Reference.Outputs[20].ToArray(),Is.EqualTo(new[]{.5}));Assert.That(r.Reference.Reference.States[20].Count,Is.EqualTo(2));
            Assert.That(r.History.PhysicalTimeAt(20),Is.EqualTo(.4));Assert.That(r.Costs.Terminal.Total,Is.EqualTo(.625));
            Assert.That(m.Resolve(r,20,"reference.input",FormulaValueKind.Vector).Validity,Is.EqualTo(MetricValidity.NotApplicable));
        }
        [Test] public void UnknownPrototypeAndFixtureProgressCannotCompleteOrSatisfyProductionPrerequisites()
        {
            var store=new MemoryProgressStore();store.Save(new ProgressDocument(new[]{new CompletionRecord("prototype.course","1","prototype.pendulum","1",1),new CompletionRecord("fixture.f10.course","1","fixture.f10.static","1",1)}));
            var p=new ProgressionService(Course,store);foreach(var id in new[]{"C1.L3","C1.L4"}) { Assert.That(p.GetStatus(id).IsCompleted,Is.False);Assert.That(p.GetStatus(id).MissingPrerequisiteIds,Is.Not.Empty);Assert.That(p.GetStatus(id).IsAvailable,Is.True); }
        }
        [TestCase("horizon")] [TestCase("terminal")] [TestCase("unit")] [TestCase("contribution")]
        public void CatalogRejectsImpossibleNewReferenceCostContracts(string error)
        {
            var course=UnityEngine.Object.Instantiate(Course);var chapter=UnityEngine.Object.Instantiate(Course.Chapters.Single());
            var lesson=UnityEngine.Object.Instantiate(Definition("C1.L4"));var experiment=UnityEngine.Object.Instantiate(lesson.Experiment);
            var cost=UnityEngine.Object.Instantiate((QuadraticTrackingCostDefinition)experiment.Cost);var reference=UnityEngine.Object.Instantiate(cost.SampledReference);
            void Set(UnityEngine.Object o,string field,UnityEngine.Object value) { var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
            try
            {
                Set(cost,"sampledReference",reference);Set(experiment,"cost",cost);Set(lesson,"experiment",experiment);
                var chapters=new SerializedObject(course);chapters.FindProperty("chapters").GetArrayElementAtIndex(0).objectReferenceValue=chapter;chapters.ApplyModifiedPropertiesWithoutUndo();
                var lessons=new SerializedObject(chapter);lessons.FindProperty("lessons").GetArrayElementAtIndex(3).objectReferenceValue=lesson;lessons.ApplyModifiedPropertiesWithoutUndo();
                if(error=="horizon") { var s=new SerializedObject(experiment);s.FindProperty("discreteTime.horizon").intValue=1;s.ApplyModifiedPropertiesWithoutUndo(); }
                if(error=="terminal") { var s=new SerializedObject(reference);s.FindProperty("states").arraySize=20;s.ApplyModifiedPropertiesWithoutUndo(); }
                if(error=="unit") { var s=new SerializedObject(reference);s.FindProperty("stateUnits").GetArrayElementAtIndex(0).stringValue="m";s.ApplyModifiedPropertiesWithoutUndo(); }
                if(error=="contribution") { var s=new SerializedObject(cost);s.FindProperty("inputContribution.id").stringValue="state-deviation";s.ApplyModifiedPropertiesWithoutUndo(); }
                var result=CatalogValidator.ValidateExecutionConfiguration(course,verifySourceFiles:false);Assert.That(result.IsValid,Is.False);Assert.That(result.Issues.Any(i=>i.Code=="ProviderConfiguration" || i.Code=="ProviderResult"),Is.True,string.Join("\n",result.Issues));
            }
            finally { foreach(var o in new UnityEngine.Object[]{course,chapter,lesson,experiment,cost,reference}) UnityEngine.Object.DestroyImmediate(o); }
        }
    }
}
