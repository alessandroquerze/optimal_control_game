#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class LessonOrchestrationTests
    {
        private const string A="Assets/OptimalControl/";
        private AppBootstrap bootstrap;
        private readonly List<Object> assets=new List<Object>();
        private Keyboard keyboard;
        private LessonHudPresenter Hud=>Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root=>Hud.GetComponent<UIDocument>().rootVisualElement;
        private LessonSession Lesson=>bootstrap.ActiveHost.Lesson;
        private SimulationSession Session=>bootstrap.ActiveHost.Session;
        private LayerController Layers=>bootstrap.ActiveHost.GetComponent<LayerController>();
        [UnitySetUp] public IEnumerator Setup(){yield return RestartBootstrap();}
        private IEnumerator RestartBootstrap(CourseDefinition overrideCourse=null)
        {
            foreach(var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.Destroy(old.gameObject);
            yield return null;
            void Configure(Scene scene,LoadSceneMode mode){if(scene.path==A+"Scenes/Bootstrap.unity"&&overrideCourse!=null)Set(Object.FindFirstObjectByType<AppBootstrap>(),"course",p=>p.objectReferenceValue=overrideCourse);}
            SceneManager.sceneLoaded+=Configure;
            try{yield return SceneManager.LoadSceneAsync(A+"Scenes/Bootstrap.unity",LoadSceneMode.Single);}finally{SceneManager.sceneLoaded-=Configure;}
            yield return Ready();
        }
        private IEnumerator Ready()
        {
            float deadline=Time.realtimeSinceStartup+20;
            do{yield return null;Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));bootstrap=Object.FindFirstObjectByType<AppBootstrap>();}
            while(bootstrap==null||bootstrap.Navigation==null||!bootstrap.Navigation.IsReady);
        }
        private IEnumerator Open(){Assert.That(bootstrap.Navigation.OpenLesson(bootstrap.Navigation.Course.Chapters[0].Lessons[0].Id),Is.True);yield return Ready();}
        [UnityTest] public IEnumerator AF08_02_MenuRequestedBeforeHudWaitsForDispatchBoundary()
        {
            yield return Open(); Lesson.Explore();
            var old = Session; var hud = Hud; var destination = bootstrap.Navigation.ActiveLesson;
            hud.Bind(null, null, null);
            old.Changed += _ =>
            {
                Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True);
                Assert.That(old.State.IsClosed, Is.False, "Teardown must wait until consumers finish.");
            };
            hud.Bind(destination, old, bootstrap.ActiveHost.GetComponent<HighlightRegistry>(), Lesson);
            Assert.DoesNotThrow(() => old.Execute(SimulationCommand.Seek(1)));
            yield return Ready();
            Assert.That(old.State.IsClosed, Is.True);
            Assert.That(bootstrap.Navigation.ActiveLesson, Is.Null);
        }
        [UnityTest] public IEnumerator AF08_02_MenuAfterHudIsDeferredAcrossTheLessonOperation() { yield return NavigationDuringPublication(false, false); }
        [UnityTest] public IEnumerator AF08_02_AnotherLessonBeforeHudKeepsDestinationIdentity() { yield return NavigationDuringPublication(true, true); }
        [UnityTest] public IEnumerator AF08_02_AnotherLessonAfterHudKeepsDestinationIdentity() { yield return NavigationDuringPublication(false, true); }
        private IEnumerator NavigationDuringPublication(bool beforeHud, bool anotherLesson)
        {
            LessonDefinition second = null;
            if (anotherLesson)
            {
                var course = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CourseDefinition>(A + "Data/Courses/PrototypeCourse.asset")); assets.Add(course);
                var chapter = Object.Instantiate(course.Chapters[0]); assets.Add(chapter);
                second = Object.Instantiate(chapter.Lessons[0]); assets.Add(second); Set(second, "id", p => p.stringValue = "fixture.dispatch.destination");
                Set(chapter, "lessons", p => { p.arraySize = 2; p.GetArrayElementAtIndex(0).objectReferenceValue = chapter.Lessons[0]; p.GetArrayElementAtIndex(1).objectReferenceValue = second; });
                Set(course, "chapters", p => p.GetArrayElementAtIndex(0).objectReferenceValue = chapter); yield return RestartBootstrap(course);
            }
            yield return Open(); Lesson.Explore();
            var old = Session; var oldLesson = Lesson; var hud = Hud; var view = bootstrap.ActiveHost.GetComponent<ExperimentVisualization>();
            var registry = bootstrap.ActiveHost.GetComponent<HighlightRegistry>(); var destination = bootstrap.Navigation.ActiveLesson;
            if (beforeHud) hud.Bind(null, null, null);
            bool requested = false, lessonFinishedBeforeClose = false; int callbacks = 0;
            old.Changed += _ =>
            {
                callbacks++;
                requested = anotherLesson ? bootstrap.Navigation.OpenLesson(second.Id) : bootstrap.Navigation.ReturnToMenu();
                Assert.That(requested, Is.True); Assert.That(old.State.IsClosed, Is.False); Assert.That(bootstrap.Navigation.IsTransitioning, Is.True);
                Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.False, "First pending destination wins.");
            };
            if (beforeHud) hud.Bind(destination, old, registry, oldLesson, view);
            oldLesson.Changed += _ => { lessonFinishedBeforeClose = true; Assert.That(old.State.IsClosed, Is.False); };
            Assert.DoesNotThrow(() => Assert.That(oldLesson.Execute(SimulationCommand.Seek(1)), Is.True));
            Assert.That(lessonFinishedBeforeClose, Is.True); Assert.That(old.State.IsClosed, Is.True);
            yield return Ready();
            Assert.That(requested, Is.True); Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(old.Notifications.IsActive, Is.False);
            Assert.That(typeof(SimulationSession).GetField("Changed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(old), Is.Null);
            Assert.That(oldLesson.Execute(SimulationCommand.Reset()), Is.False);
            if (anotherLesson)
            {
                Assert.That(bootstrap.Navigation.ActiveLesson.Id, Is.EqualTo(second.Id)); Assert.That(Session, Is.Not.SameAs(old));
                Assert.That(Hud.BoundSession, Is.SameAs(Session)); Assert.That(Hud.DisplayedState.Result, Is.SameAs(Session.State.Result));
                Assert.That(bootstrap.ActiveHost.GetComponent<ExperimentVisualization>().BoundSession, Is.SameAs(Session));
                Assert.That(Lesson.Presentation.LessonId, Is.EqualTo(second.Id));
            }
            else { Assert.That(bootstrap.ActiveHost, Is.Null); Assert.That(bootstrap.Navigation.ActiveLesson, Is.Null); }
        }
        [UnityTest] public IEnumerator AF08_02_CapturedCallbacksCannotOverwriteAReboundConsumer()
        {
            yield return Open(); Lesson.Explore();
            var old = Session; var hud = Hud; var view = bootstrap.ActiveHost.GetComponent<ExperimentVisualization>();
            var diagnostics = Object.FindFirstObjectByType<SessionDiagnosticsPresenter>(); var destination = bootstrap.Navigation.ActiveLesson;
            var registry = bootstrap.ActiveHost.GetComponent<HighlightRegistry>(); var education = Lesson;
            using (var replacement = new SimulationSession(old.State.Result.Configuration))
            {
                replacement.Execute(SimulationCommand.Seek(2));
                hud.Bind(null, null, null); view.Bind(null); diagnostics.Bind(null);
                Action<OptCon.Simulation.SessionState> rebind = _ => { hud.Bind(destination, replacement, registry, visualStatus: view); view.Bind(replacement); diagnostics.Bind(replacement); };
                old.Changed += rebind;
                hud.Bind(destination, old, registry, education, view); view.Bind(old, education); diagnostics.Bind(old);
                Assert.DoesNotThrow(() => old.Execute(SimulationCommand.Seek(1)));
                Assert.That(hud.DisplayedState, Is.SameAs(replacement.State)); Assert.That(view.DisplayedState, Is.SameAs(replacement.State));
                Assert.That(hud.Formula.DisplayedResult, Is.SameAs(replacement.State.Result));
                Assert.That(diagnostics.GetComponent<UIDocument>().rootVisualElement.Q<Label>("session-diagnostic").text, Does.Contain("t=2/"));
                old.Changed -= rebind; hud.Bind(destination, old, registry, education, view); view.Bind(old, education); diagnostics.Bind(old);
            }
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard); keyboard=null;
            foreach(var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.Destroy(old.gameObject);
            yield return null;
            foreach(string path in new[]{A+"Scenes/MainMenu.unity",A+"Scenes/LessonWorkspace.unity"}){var scene=SceneManager.GetSceneByPath(path);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
            foreach(var asset in assets)Object.Destroy(asset);assets.Clear();
        }
        private static void Set(Object owner,string name,Action<SerializedProperty> write){var so=new SerializedObject(owner);write(so.FindProperty(name));so.ApplyModifiedPropertiesWithoutUndo();}
        private void Submit(string name){var button=Root.Q<Button>(name);Assert.That(button,Is.Not.Null,name);using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}}
        private void Next(){Assert.That(Lesson.Presentation.CanAdvance,Is.True);Submit("lesson-next");}
        private void ReachInput(){Submit("term-state.current");Submit("answer-angle-speed");Next();}
        private void ReachTransition(){ReachInput();Root.Q<DoubleField>("input-0").value=1;Submit("answer-omega");Next();}
        private void ReachTrajectory(){ReachTransition();Submit("term-state.next");Submit("step-next");Next();}
        private void ReachCost(){ReachTrajectory();Root.Q<DoubleField>("input-0").value=2;Submit("step-next");Next();}
        private void ReachComparison(){ReachCost();Root.Q<DoubleField>("parameter-cost.q.0").value=30;Submit("answer-criterion");Next();}
        private void ReachTerminal(){ReachComparison();Submit("comparison-save");Root.Q<DoubleField>("input-0").value=3;Submit("term-cost.total");Next();}

        [UnityTest] public IEnumerator LessonAndConsumersAreComposedBeforeReady()
        {
            bool observed=false;((CourseManager)bootstrap.Navigation).LifecycleChanged+=update=>
            {
                if(update.Phase!=NavigationPhase.Ready||update.Transition.Destination==null)return;
                observed=true;Assert.That(Lesson,Is.Not.Null);Assert.That(Hud.BoundLesson,Is.SameAs(Lesson));Assert.That(Hud.BoundSession,Is.SameAs(update.Session));
                Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Layers.IsVisible("PhysicalBody"),Is.True);Assert.That(Layers.IsVisible("next"),Is.False);
            };
            yield return Open();Assert.That(observed,Is.True);Assert.That(Lesson.Presentation.CanAdvance,Is.False,"Authored selection must not count as a user action.");
        }
        [UnityTest] public IEnumerator FirstStepDisclosesOnlyStateAndBlocksCommandsAndHiddenTerms()
        {
            yield return Open();var result=Session.State.Result;
            Assert.That(Hud.Formula.Formula.Id,Is.EqualTo(bootstrap.Navigation.ActiveLesson.Formulas[0].Id));
            Assert.That(Root.Q<Button>("term-state.next").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(Root.Q<Button>("step-next").enabledSelf,Is.False);Assert.That(Root.Q<VisualElement>("cost-panel").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(Lesson.Execute(SimulationCommand.SetInput(0,new NumericVector(8))),Is.False);
            Assert.That(Lesson.Execute(SimulationCommand.SetParameter("model.mass",2)),Is.False);Assert.That(Lesson.Execute(SimulationCommand.CaptureComparison()),Is.False);
            Hud.Formula.SelectTerm("state.next");Assert.That(Root.Q<Label>("term-detail-title").text,Does.StartWith("x_t"));Assert.That(Session.State.Result,Is.SameAs(result));
            Assert.That(Lesson.Next(),Is.False);
        }
        [UnityTest] public IEnumerator EntireGuidedVerticalSliceUsesTheRealResultAndCompletesOnlyPrototype()
        {
            yield return Open();string authored=EditorJsonUtility.ToJson(bootstrap.Navigation.ActiveLesson);
            ReachInput();Assert.That(Lesson.Presentation.StepIndex,Is.EqualTo(1));Assert.That(Layers.IsVisible("input"),Is.True);Assert.That(Layers.IsVisible("next"),Is.False);
            Root.Q<DoubleField>("input-0").value=1;Submit("answer-theta");Assert.That(Lesson.Next(),Is.False);Submit("answer-omega");Next();
            var predicted=Session.State.Sample.NextState;Submit("term-state.next");Assert.That(Root.Q<Label>("term-detail-status").text,Does.Contain("@1"));Submit("step-next");
            Assert.That(Session.State.Sample.CurrentState,Is.SameAs(predicted));Next();
            Root.Q<DoubleField>("input-0").value=2;Submit("step-next");Next();
            var before=Session.State.Result;Root.Q<DoubleField>("parameter-cost.q.0").value=30;var weighted=Session.State.Result;
            Assert.That(weighted.Costs.Total,Is.Not.EqualTo(before.Costs.Total));for(int t=0;t<=weighted.History.Horizon;t++)Assert.That(weighted.History.States[t],Is.SameAs(before.History.States[t]));
            Submit("answer-criterion");Next();Submit("comparison-save");var retained=Session.State.Comparison.Result;double retainedJ=retained.Costs.Total;
            Root.Q<DoubleField>("input-0").value=3;Submit("term-cost.total");Assert.That(Session.State.Comparison.Result,Is.SameAs(retained));Assert.That(retained.Costs.Total,Is.EqualTo(retainedJ));Next();
            Root.Q<SliderInt>("timeline-seek").value=100;Submit("term-cost.terminal");Submit("answer-terminal");
            Assert.That(Session.State.Sample.AppliedControl,Is.Null);Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.StageCost].Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Session.State.Result.Costs.Total,Is.EqualTo(Session.State.Result.Costs.Cumulative[100]+Session.State.Result.Costs.Terminal.Total));
            Assert.That(Hud.DisplayedState.Result,Is.SameAs(bootstrap.ActiveHost.GetComponent<ExperimentVisualization>().DisplayedState.Result));
            Assert.That(bootstrap.ActiveHost.GetComponentInChildren<CostContributionVisualizer>(true).DisplayedResult,Is.SameAs(Hud.Formula.DisplayedResult));
            Next();Assert.That(Lesson.Presentation.IsCompleted,Is.True);Assert.That(Lesson.Presentation.LessonId,Is.EqualTo("prototype.pendulum"));
            Assert.That(EditorJsonUtility.ToJson(bootstrap.Navigation.ActiveLesson),Is.EqualTo(authored));Assert.That(Root.Q<Label>("lesson-instruction").text,Does.Contain("nessun'altra lezione"));
        }
        [UnityTest] public IEnumerator CostStepRestrictsParametersAndRetainsTheSameRevisionInAllConsumers()
        {
            yield return Open();ReachCost();Assert.That(Hud.Formula.Formula.Id,Is.EqualTo("cost.additive"));
            Assert.That(Root.Q<DoubleField>("parameter-cost.q.0").enabledSelf,Is.True);Assert.That(Root.Q<DoubleField>("parameter-cost.q.1").enabledSelf,Is.False);
            Assert.That(Lesson.Execute(SimulationCommand.SetParameter("cost.r.0",1)),Is.False);Assert.That(Lesson.Execute(SimulationCommand.SetHorizon(2)),Is.False);
            var current=Session.State.Result;Root.Q<DoubleField>("parameter-cost.q.0").value=30;
            Assert.That(Hud.Formula.DisplayedResult,Is.SameAs(Session.State.Result));Assert.That(current.Costs.Configuration.Parameters.First(p=>p.Target=="cost.q.0").Value,Is.EqualTo(10));
            Assert.That(Layers.IsVisible("cost"),Is.True);Assert.That(Layers.IsVisible("comparison"),Is.False);
        }
        [UnityTest] public IEnumerator TerminalHighlightUsesTWhileTheCursorIsElsewhereAndUnavailableIsExplicit()
        {
            yield return Open();ReachCost();Session.Execute(SimulationCommand.Seek(7));Submit("term-cost.terminal");
            var targets=bootstrap.ActiveHost.GetComponentsInChildren<HighlightTarget>(true).Where(t=>t.SemanticRole=="state.terminal").ToArray();
            Assert.That(targets.All(t=>t.IsHighlighted&&t.SampleIndex==100),Is.True);Assert.That(Session.State.Sample.Timestep,Is.EqualTo(7));
            Root.Q<SliderInt>("timeline-seek").value=100;Assert.That(Hud.Formula.Resolutions[SimulationMetricIds.AppliedInput].Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TerminalCost].SampleIndex,Is.EqualTo(100));
            Assert.That(bootstrap.ActiveHost.GetComponentsInChildren<HighlightTarget>(true).Where(t=>t.SemanticRole=="state.next").All(t=>!t.gameObject.activeInHierarchy),Is.True);
        }
        [UnityTest] public IEnumerator RestartAndBackClearFutureControlsSelectionsComparisonAndFacts()
        {
            yield return Open();ReachComparison();Submit("comparison-save");Root.Q<DoubleField>("input-0").value=3;
            Submit("lesson-previous");Assert.That(Lesson.Presentation.StepIndex,Is.EqualTo(4));Assert.That(Session.State.Comparison,Is.Null);Assert.That(Session.State.Result.Configuration.Controls[0][0],Is.Zero);
            Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(Layers.IsVisible("comparison"),Is.False);
            Submit("lesson-restart");Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Session.State.Sample.Timestep,Is.Zero);Assert.That(Layers.IsVisible("cost"),Is.False);
            Assert.That(Root.Q<Button>("step-next").enabledSelf,Is.False);Assert.That(Root.Q<Label>("term-detail-title").text,Does.StartWith("x_t"));Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Submit("lesson-next");Assert.That(Lesson.Presentation.StepIndex,Is.Zero);
        }
        [UnityTest] public IEnumerator ExitAndReenterReleaseLessonAndPresentationSubscriptions()
        {
            yield return Open();ReachComparison();Submit("comparison-save");var oldLesson=Lesson;var oldSession=Session;int callbacks=0;oldLesson.Changed+=_=>callbacks++;
            bootstrap.Navigation.ReturnToMenu();yield return Ready();Assert.That(oldSession.State.IsClosed,Is.True);Assert.That(oldLesson.Next(),Is.False);int closedCount=callbacks;
            yield return Open();Assert.That(Lesson,Is.Not.SameAs(oldLesson));Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Session.State.Comparison,Is.Null);Assert.That(callbacks,Is.EqualTo(closedCount));
            Assert.That(Hud.BoundLesson,Is.SameAs(Lesson));Assert.That(Hud.Formula.DisplayedResult,Is.SameAs(Session.State.Result));
        }
        [UnityTest] public IEnumerator TwoLessonsSharingWorkspaceRecomposeEducationalIdentityBeforeReady()
        {
            var course=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CourseDefinition>(A+"Data/Courses/PrototypeCourse.asset"));assets.Add(course);
            var chapter=Object.Instantiate(course.Chapters[0]);assets.Add(chapter);var second=Object.Instantiate(chapter.Lessons[0]);assets.Add(second);
            Set(second,"id",p=>p.stringValue="fixture.second-guided");Set(second,"title",p=>p.stringValue="Second guided lesson");
            Set(chapter,"lessons",p=>{p.arraySize=2;p.GetArrayElementAtIndex(0).objectReferenceValue=chapter.Lessons[0];p.GetArrayElementAtIndex(1).objectReferenceValue=second;});
            Set(course,"chapters",p=>p.GetArrayElementAtIndex(0).objectReferenceValue=chapter);yield return RestartBootstrap(course);yield return Open();
            ReachInput();var old=Lesson;var oldSimulation=Session;Assert.That(bootstrap.Navigation.OpenLesson(second.Id),Is.True);yield return Ready();
            Assert.That(oldSimulation.State.IsClosed,Is.True);Assert.That(old.Next(),Is.False);Assert.That(Lesson.Presentation.LessonId,Is.EqualTo(second.Id));
            Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Hud.BoundLesson,Is.SameAs(Lesson));Assert.That(Root.Q<DoubleField>("input-0").enabledSelf,Is.False);
        }
        [UnityTest] public IEnumerator DisabledKeyboardCommandsCannotBypassTheLessonAndResetIsDeterministic()
        {
            yield return Open();keyboard=InputSystem.AddDevice<Keyboard>();var result=Session.State.Result;
            yield return Press(Key.D);yield return Press(Key.RightArrow);yield return Press(Key.P);yield return Press(Key.Digit2);
            Assert.That(Session.State.Result,Is.SameAs(result));Assert.That(Session.State.Sample.Timestep,Is.Zero);Assert.That(Session.State.IsPlaying,Is.False);Assert.That(Layers.IsVisible("PhasePlane"),Is.False);
            ReachInput();yield return Press(Key.D);Assert.That(Session.State.Sample.AppliedControl[0],Is.EqualTo(.5), "focus="+Root.focusController.focusedElement+", editing="+Hud.HasEditingFocus+", action="+bootstrap.ActiveHost.GetComponent<LessonInputRouter>().RuntimeActions.FindAction("IncreaseControl").enabled);yield return Press(Key.R);
            Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Session.State.Sample.AppliedControl[0],Is.Zero);Assert.That(Session.State.Comparison,Is.Null);
        }
        [UnityTest] public IEnumerator DisableEnableHudRebindsWithoutCreditingAutomaticSelection()
        {
            yield return Open();Hud.enabled=false;Session.Execute(SimulationCommand.Seek(1));Hud.enabled=true;
            Assert.That(Hud.DisplayedState,Is.SameAs(Session.State));Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Submit("term-state.current");Submit("answer-angle-speed");Next();Assert.That(Lesson.Presentation.StepIndex,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator GenericLayerToggleKeepsItsAdditionalRootsTogetherWithoutChangingSimulation()
        {
            yield return Open();Lesson.Explore();var result=Session.State.Result;
            int index=Layers.Ids.ToList().IndexOf("next");var data=new SerializedObject(Layers);
            var entry=data.FindProperty("layers").GetArrayElementAtIndex(index);
            var roots=entry.FindPropertyRelative("additionalRoots");Assert.That(roots.arraySize,Is.GreaterThan(0));
            Layers.Toggle(index);Assert.That(Layers.IsVisible("next"),Is.False);
            for(int i=0;i<roots.arraySize;i++)Assert.That(((GameObject)roots.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf,Is.False);
            Layers.Toggle(index);Assert.That(Layers.IsVisible("next"),Is.True);
            for(int i=0;i<roots.arraySize;i++)Assert.That(((GameObject)roots.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf,Is.True);
            Assert.That(Session.State.Result,Is.SameAs(result));
        }
        [UnityTest] public IEnumerator IndexOnlyGuidedLessonUsesGenericFormulaAndF07CostsWithoutInventingSeconds()
        {
            yield return Open();var definition=Object.Instantiate(bootstrap.Navigation.ActiveLesson);assets.Add(definition);
            var generic=AssetDatabase.LoadAssetAtPath<FormulaDefinition>(A+"Data/Formulas/DiscreteDynamics.asset");var costFormula=definition.Formulas[2];
            Set(definition,"formulas",p=>{p.arraySize=2;p.GetArrayElementAtIndex(0).objectReferenceValue=generic;p.GetArrayElementAtIndex(1).objectReferenceValue=costFormula;});
            JsonUtility.FromJsonOverwrite("{\"id\":\"fixture.index-lesson\",\"steps\":[{\"id\":\"transition\",\"instruction\":\"Observe 2+3.\",\"enabledControlIds\":[\"step\"],\"conditions\":[{\"evaluatorId\":\"action\",\"targetId\":\"time.advanced\"}]},{\"id\":\"terminal\",\"instruction\":\"Read cost.\",\"showCosts\":true,\"conditions\":[{\"evaluatorId\":\"terminal\"}]}]}",definition);
            Set(definition,"steps",p=>{p.GetArrayElementAtIndex(0).FindPropertyRelative("formulaFocus").objectReferenceValue=generic;p.GetArrayElementAtIndex(1).FindPropertyRelative("formulaFocus").objectReferenceValue=costFormula;});
            var cost=new QuadraticTrackingCost(new NumericVector(0),new NumericVector(0),new NumericVector(2),new NumericVector(2),new NumericVector(2),new[]{"1"},new[]{"1"});
            using(var simulation=new SimulationSession(new SimulationConfiguration("fixture.index-cost",new IndexOnlyConfiguration(),new NumericVector(2),new[]{new NumericVector(3)},cost:cost)))
            using(var lesson=new LessonSession(definition,simulation))
            {
                Hud.Bind(definition,simulation,null,lesson);Assert.That(Hud.Formula.Resolutions[SimulationMetricIds.NextState].Value.Vector[0],Is.EqualTo(5));
                Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.PhysicalTime].Validity,Is.EqualTo(MetricValidity.NotApplicable));
                lesson.Execute(SimulationCommand.StepForward());lesson.Next();Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TotalCost].Value.Scalar,Is.EqualTo(38));
                Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TerminalCost].Value.Scalar,Is.EqualTo(25));Assert.That(Hud.Formula.Resolutions[SimulationMetricIds.AppliedInput].Validity,Is.EqualTo(MetricValidity.NotApplicable));
                Assert.That(simulation.State.Sample.DiscretizationStepSeconds,Is.Null);lesson.Next();Assert.That(lesson.Presentation.IsCompleted,Is.True);Hud.Bind(null,null,null);
            }
        }
        private IEnumerator Press(Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
    }
}
#endif
