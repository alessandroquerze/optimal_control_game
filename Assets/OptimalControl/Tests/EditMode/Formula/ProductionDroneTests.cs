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
namespace OptCon.Tests
{
    public sealed class ProductionDroneTests
    {
        const string A="Assets/OptimalControl/";
        private CourseDefinition Course=>AssetDatabase.LoadAssetAtPath<CourseDefinition>(A+"Data/Courses/OptimalControlCourse.asset");
        private LessonDefinition Definition=>Course.Chapters.Single().Lessons.Single(l=>l.Id=="C1.L5");
        private SimulationSession Open()=>Definition.Experiment.Model.CreateSession(Definition.Experiment);
        private static void Select(LessonSession lesson,string id)=>lesson.RecordTermSelection(lesson.Presentation.Step.FormulaId,id);
        private static void Act(LessonSession l)
        {
            switch(l.Presentation.Step.Id) {
                case "state": Select(l,"state.current");Select(l,"input.applied");Select(l,"output.current");break;
                case "same-position": l.Execute(SimulationCommand.SetInitialState(new NumericVector(0,0,1,.5,0,0)));break;
                case "hover": l.Execute(SimulationCommand.SetInput(0,new NumericVector(4.905,4.905),InputEditScope.Tail));break;
                case "collective": l.Execute(SimulationCommand.SetInput(0,new NumericVector(5.905,5.905),InputEditScope.Tail));break;
                case "differential": l.Execute(SimulationCommand.SetInput(0,new NumericVector(5.905,3.905),InputEditScope.Tail));break;
                case "tilt": l.Execute(SimulationCommand.SetInitialState(new NumericVector(0,0,1,0,.2,0)));break;
                case "trajectory": l.Execute(SimulationCommand.Seek(10));Select(l,"output.current");break;
                case "reference": l.Execute(SimulationCommand.SetParameter("cost.reference.state.10.0",.4));Select(l,"desired-h");break;
                case "cost": l.Execute(SimulationCommand.SetParameter("cost.q.3",2));break;
                case "input-limit": l.Execute(SimulationCommand.SetInput(0,new NumericVector(9,4.905),InputEditScope.Single));break;
                case "altitude-limit": l.Execute(SimulationCommand.SetInitialState(new NumericVector(0,0,2.5,0,0,0)));break;
                case "terminal": l.Execute(SimulationCommand.Seek(50));Select(l,"input.applied");Select(l,"state.next");Select(l,"stage");break;
                default: Assert.Fail("Uncovered activity");break;
            }
        }
        private static void FinishStep(LessonSession l){Act(l);Assert.That(l.Answer(l.Presentation.Step.Id+"-ok"),Is.True);Assert.That(l.Next(),Is.True,l.Presentation.Step?.Id+": "+l.Presentation.Error);}
        private static void Reach(LessonSession l,string id){while(l.Presentation.Step.Id!=id)FinishStep(l);}
        [Test] public void ProductionCatalogProviderBindingsAndAdvisoryPrerequisiteAreValid()
        {
            Assert.That(Definition.ContentVersion,Is.EqualTo("1"));Assert.That(Definition.SchemaVersion,Is.EqualTo(1));Assert.That(Definition.PrerequisiteLessonIds,Is.EqualTo(new[]{"C1.L4"}));
            Assert.That(Definition.Experiment.Provider,Is.TypeOf<PlanarDroneModelDefinition>());Assert.That(Definition.Experiment.Id,Is.EqualTo("c1.drone.planar"));
            var p=new ProgressionService(Course,new MemoryProgressStore());Assert.That(p.GetStatus("C1.L5").IsAvailable,Is.True);Assert.That(p.GetStatus("C1.L5").IsCompleted,Is.False);
            var report=CatalogValidator.ValidateExecutionConfiguration(Course,verifySourceFiles:true);Assert.That(report.IsValid,Is.True,string.Join("\n",report.Issues));
        }
        [Test] public void SourceCostHasIndependentKnownStageTerminalAndTotal()
        {
            using var s=Open();var r=s.State.Result;
            Assert.That(r.Costs.Stages[0].Find("state-deviation").Value,Is.EqualTo(.125).Within(1e-12));
            Assert.That(r.Costs.Stages[0].Find("control-deviation").Value,Is.EqualTo(.4811805).Within(1e-12));
            Assert.That(r.Costs.Terminal.Total,Is.EqualTo(.875).Within(1e-12));Assert.That(r.Costs.Total,Is.EqualTo(35.226525).Within(1e-10));
            Assert.That(r.Reference.Reference.Inputs.All(u=>u.All(v=>v==0)),Is.True);Assert.That(r.Constraints.DesiredSatisfiesDynamics,Is.False);
            Assert.That(r.Reference.Outputs[50],Is.EqualTo(new[]{.5,1}));Assert.That(r.Outputs.Samples[50],Is.EqualTo(new[]{0d,1d}));
        }
        [Test] public void ShapesComponentsSampleIndicesAndTerminalAvailabilityUseOneResolver()
        {
            using var s=Open();var r=s.State.Result;var m=new RuntimeMetricResolver();
            Assert.That(m.Resolve(r,4,"state.current",FormulaValueKind.Vector).Value.Rows,Is.EqualTo(6));
            Assert.That(m.Resolve(r,4,"input.applied",FormulaValueKind.Vector).Value.Rows,Is.EqualTo(2));
            Assert.That(m.Resolve(r,4,"output.current",FormulaValueKind.Vector).Value.Rows,Is.EqualTo(2));
            Assert.That(m.Resolve(r,4,"input.applied",FormulaValueKind.Scalar).Validity,Is.EqualTo(MetricValidity.Invalid));
            var one=m.Resolve(r,4,"input.applied",FormulaValueKind.Scalar,component:1);Assert.That(one.Value.Scalar,Is.EqualTo(4.905));Assert.That(one.Value.Units.Single(),Is.EqualTo("N"));Assert.That(one.SampleIndex,Is.EqualTo(4));
            Assert.That(m.Resolve(r,4,"state.next",FormulaValueKind.Scalar,FormulaScope.NextTimestep,3).SampleIndex,Is.EqualTo(5));
            foreach(var id in new[]{"input.applied","reference.input","state.next"}) Assert.That(m.Resolve(r,50,id,FormulaValueKind.Vector,id=="state.next"?FormulaScope.NextTimestep:FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(m.Resolve(r,50,"constraint.g.altitude",FormulaValueKind.Scalar).Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(m.Resolve(r,50,"time.physical",FormulaValueKind.Scalar).Value.Scalar,Is.EqualTo(1));
        }
        [Test] public void ResultAIsNeverReinterpretedUsingChangedMassReferenceOrFailedRequest()
        {
            using var s=Open();var a=s.State.Result;s.Execute(SimulationCommand.SetParameter("model.mass",2));var b=s.State.Result;
            Assert.That(((PlanarDroneConfiguration)a.Configuration.Model).Mass,Is.EqualTo(1));Assert.That(a.History.States[1][3],Is.Zero);Assert.That(b.History.States[1][3],Is.EqualTo(-.0981).Within(1e-12));
            s.Execute(SimulationCommand.SetParameter("cost.reference.state.10.0",.4));var c=s.State.Result;Assert.That(a.Reference.Outputs[10][0],Is.EqualTo(.1));Assert.That(c.Reference.Outputs[10][0],Is.EqualTo(.4));
            s.Execute(SimulationCommand.SetParameter("cost.r.0",0));Assert.That(s.State.Error,Is.Not.Null);Assert.That(s.State.Result,Is.SameAs(c));Assert.That(s.State.TryGetEditableInput(0,out _),Is.False);
            s.Execute(SimulationCommand.Reset());Assert.That(s.State.Error,Is.Null);Assert.That(s.State.Result.History.States[1][3],Is.Zero);
        }
        [Test] public void EveryProjectionDeclaresSourceUnitsAndOmittedDimensions()
        {
            using var s=Open();var plots=Definition.Experiment.ScenarioPrefab.GetComponentsInChildren<StateTrajectoryView>(true);Assert.That(plots.Length,Is.EqualTo(7));
            foreach(var p in plots){Assert.DoesNotThrow(()=>p.Projection.Validate(s.State.Result.History.Model));Assert.That(p.Projection.OmittedComponents(s.State.Result.History.Model),Does.Contain("omesse"));}
            Assert.That(plots.SelectMany(p=>new[]{p.Projection.X,p.Projection.Y}).Where(a=>a.coordinate==ProjectionCoordinate.StateComponent).Select(a=>a.component).Distinct().OrderBy(i=>i),Is.EqualTo(Enumerable.Range(0,6)));
        }
        [Test] public void CompleteOutcomePathPersistsOnlyC1L5AndRestartDoesNotDuplicate()
        {
            var store=new MemoryProgressStore();var p=new ProgressionService(Course,store);using var s=Open();using var l=new LessonSession(Definition,s);using var binding=new LessonProgressBinding(l,s,p,()=>true);
            while(!l.Presentation.IsCompleted)FinishStep(l);
            Assert.That(p.GetStatus("C1.L5").IsCompleted,Is.True);Assert.That(p.GetStatus("C1.L4").IsCompleted,Is.False);Assert.That(store.SaveCount,Is.EqualTo(1));
            l.Restart();while(!l.Presentation.IsCompleted)FinishStep(l);Assert.That(store.SaveCount,Is.EqualTo(1));
            Assert.That(new ProgressionService(Course,store).GetStatus("C1.L5").IsCompleted,Is.True);
        }
        [TestCase("same-position")] [TestCase("hover")] [TestCase("collective")] [TestCase("differential")] [TestCase("tilt")] [TestCase("reference")] [TestCase("cost")] [TestCase("input-limit")] [TestCase("altitude-limit")]
        public void CorrectAnswerCannotReplaceNumericOutcomeAndPreviousRecapturesBaseline(string id)
        {
            using var s=Open();using var l=new LessonSession(Definition,s);Reach(l,id);
            l.Answer(id+"-ok");Assert.That(l.Presentation.CanAdvance,Is.False);Assert.That(l.Next(),Is.False);
            Act(l);l.Answer("wrong");Assert.That(l.Next(),Is.False);l.Answer(id+"-ok");Assert.That(l.Next(),Is.True);
            Assert.That(l.Previous(),Is.True);l.Answer(id+"-ok");Assert.That(l.Next(),Is.False);Act(l);Assert.That(l.Next(),Is.True);
        }
        [TestCase("collective",3.905,3.905)] [TestCase("collective",5.905,4.905)] [TestCase("differential",3.905,5.905)] [TestCase("differential",5.905,4.905)]
        public void WrongDirectionOrWrongOtherComponentNeverPasses(string id,double u1,double u2)
        {
            using var s=Open();using var l=new LessonSession(Definition,s);Reach(l,id);l.Answer(id+"-ok");l.Execute(SimulationCommand.SetInput(0,new NumericVector(u1,u2),InputEditScope.Tail));
            Assert.That(l.Presentation.CanAdvance,Is.False);Assert.That(l.Next(),Is.False);Assert.That(l.Presentation.IsCompleted,Is.False);
        }
        [Test] public void SamePositionRequiresPositionPreservedAndTiltRequiresCorrectSign()
        {
            using var s=Open();using var l=new LessonSession(Definition,s);Reach(l,"same-position");l.Answer("same-position-ok");l.Execute(SimulationCommand.SetInitialState(new NumericVector(0,0,2,.5,0,0)));Assert.That(l.Next(),Is.False);
            Act(l);l.Next();Reach(l,"tilt");l.Answer("tilt-ok");l.Execute(SimulationCommand.SetInitialState(new NumericVector(0,0,1,0,-.2,0)));Assert.That(l.Next(),Is.False);
        }
        [Test] public void EarlierCompletionsNeverAccreditTheDrone()
        {
            var store=new MemoryProgressStore();store.Save(new ProgressDocument(new[]{new CompletionRecord("optimal-control","1","C1.L1","2",1),new CompletionRecord("optimal-control","1","C1.L4","1",1)}));
            var p=new ProgressionService(Course,store);Assert.That(p.GetStatus("C1.L5").IsCompleted,Is.False);Assert.That(p.GetStatus("C1.L5").MissingPrerequisiteIds,Is.Empty);
        }
        [Test] public void EveryWorldBindingHasAConfiguredSemanticTargetAndDisclosureIsValid()
        {
            var roles=Definition.Experiment.ScenarioPrefab.GetComponentsInChildren<HighlightTarget>(true).Select(t=>t.SemanticRole).ToArray();
            foreach(var f in Definition.Formulas)foreach(var term in f.Terms)
                if(!string.IsNullOrWhiteSpace(term.Highlight?.SemanticRole))Assert.That(roles,Does.Contain(term.Highlight.SemanticRole),f.Id+"/"+term.Id);
            Assert.That(Definition.Formulas.SelectMany(f=>f.Terms).First(t=>t.Id=="output.current").Highlight.SemanticRole,Is.EqualTo("output.position"));
            var plan=new LessonPlan(Definition);Assert.That(plan.Steps.Single(s=>s.Id=="same-position").VisibleInitialStateComponents,Is.EqualTo(new[]{3}));
            Assert.That(plan.Steps.Single(s=>s.Id=="differential").VisibleInputComponents,Is.EqualTo(new[]{0,1}));
            Assert.That(plan.Steps.Single(s=>s.Id=="differential").Conditions.Any(c=>c.Evidence?.Scope==FormulaScope.NextTimestep),Is.True);
            Assert.That(Definition.Experiment.Cost.TotalSymbol,Is.EqualTo("ℓ_tot"));
        }
        [Test] public void FailedEntryDoesNotCommitStepIdentityOrPersistentCompletion()
        {
            var definition=UnityEngine.Object.Instantiate(Definition);
            try
            {
                var so=new SerializedObject(definition);var action=so.FindProperty("steps").GetArrayElementAtIndex(1).FindPropertyRelative("entryActions").GetArrayElementAtIndex(0);
                action.FindPropertyRelative("kind").enumValueIndex=(int)SimulationCommandKind.Seek;action.FindPropertyRelative("index").intValue=51;so.ApplyModifiedPropertiesWithoutUndo();
                var store=new MemoryProgressStore();var progress=new ProgressionService(Course,store);using var s=Open();using var l=new LessonSession(definition,s);using var binding=new LessonProgressBinding(l,s,progress,()=>true);
                Act(l);l.Answer("state-ok");Assert.That(l.Next(),Is.False);Assert.That(l.Presentation.StepIndex,Is.Zero);Assert.That(l.Presentation.Error,Is.Not.Null);
                Assert.That(l.Next(),Is.False);Assert.That(l.Presentation.IsCompleted,Is.False);Assert.That(store.Load(),Is.Null);Assert.That(l.Restart(),Is.True);Assert.That(l.Presentation.Error,Is.Null);
            }
            finally{UnityEngine.Object.DestroyImmediate(definition);}
        }
        [TestCase(-1)] [TestCase(6)]
        public void EditorialStateDisclosureRejectsOutOfRangeComponents(int component)
        {
            var definition=UnityEngine.Object.Instantiate(Definition);
            try{var so=new SerializedObject(definition);so.FindProperty("steps").GetArrayElementAtIndex(1).FindPropertyRelative("visibleInitialStateComponents").GetArrayElementAtIndex(0).intValue=component;so.ApplyModifiedPropertiesWithoutUndo();Assert.Throws<ArgumentException>(()=>new LessonPlan(definition));}
            finally{UnityEngine.Object.DestroyImmediate(definition);}
        }
        [Test] public void EvidenceCannotRequireAnInventedNextStateAtTerminal()
        {
            var definition=UnityEngine.Object.Instantiate(Definition);
            try
            {
                var so=new SerializedObject(definition);var conditions=so.FindProperty("steps").GetArrayElementAtIndex(1).FindPropertyRelative("conditions");
                for(int i=0;i<conditions.arraySize;i++){var e=conditions.GetArrayElementAtIndex(i).FindPropertyRelative("evidence");if(e.FindPropertyRelative("metricId").stringValue=="state.next")e.FindPropertyRelative("sampleIndex").intValue=50;}
                so.ApplyModifiedPropertiesWithoutUndo();using var s=Open();Assert.Throws<ArgumentException>(()=>new LessonSession(definition,s));
            }
            finally{UnityEngine.Object.DestroyImmediate(definition);}
        }
        [TestCase("unit")] [TestCase("component")] [TestCase("dimension")] [TestCase("interval")]
        public void ProviderRejectsInvalidDroneMetadata(string error)
        {
            var e=UnityEngine.Object.Instantiate(Definition.Experiment);var constraints=UnityEngine.Object.Instantiate(e.Constraints);
            try {var s=new SerializedObject(e);
                if(error=="unit")s.FindProperty("parameters").GetArrayElementAtIndex(0).FindPropertyRelative("unit").stringValue="m";
                if(error=="dimension")s.FindProperty("controlDimension").intValue=1;
                if(error=="component"||error=="interval"){var c=new SerializedObject(constraints);var b=c.FindProperty("path").GetArrayElementAtIndex(0);if(error=="component")b.FindPropertyRelative("component").intValue=2;else b.FindPropertyRelative("minimum").doubleValue=9;c.ApplyModifiedPropertiesWithoutUndo();s.FindProperty("constraints").objectReferenceValue=constraints;}
                s.ApplyModifiedPropertiesWithoutUndo();Assert.Throws<ArgumentException>(()=>e.Model.CreateConfiguration(e));
            }finally{UnityEngine.Object.DestroyImmediate(e);UnityEngine.Object.DestroyImmediate(constraints);}
        }
    }
}
