#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
namespace OptCon.Tests
{
    public sealed class ProductionDroneRuntimeTests
    {
        const string A="Assets/OptimalControl/";
        private AppBootstrap app;
        private MemoryProgressStore store;
        private LessonHudPresenter Hud=>Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement UI=>Hud.GetComponent<UIDocument>().rootVisualElement;
        private VisualElement Menu=>Object.FindFirstObjectByType<CourseMenuPresenter>().GetComponent<UIDocument>().rootVisualElement;
        private SimulationSession Sim=>app.ActiveHost.Session;
        private LessonSession Lesson=>app.ActiveHost.Lesson;
        private ExperimentVisualization View=>app.ActiveHost.GetComponent<ExperimentVisualization>();
        private static void Click(Button button){Assert.That(button,Is.Not.Null);Assert.That(button.enabledInHierarchy,Is.True,button.name);using var e=NavigationSubmitEvent.GetPooled();e.target=button;button.SendEvent(e);}
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+25;do{yield return null;app=Object.FindFirstObjectByType<AppBootstrap>();Assert.That(Time.realtimeSinceStartup,Is.LessThan(end),app?.Navigation?.StatusMessage);}while(app?.Navigation==null||!app.Navigation.IsReady);yield return null;}
        private IEnumerator RestartApp()
        {
            foreach(var b in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.Destroy(b.gameObject);yield return null;
            void Configure(Scene scene,LoadSceneMode mode){if(scene.path==A+"Scenes/Bootstrap.unity")Object.FindFirstObjectByType<AppBootstrap>().Configure(store:store);}
            SceneManager.sceneLoaded+=Configure;try{yield return SceneManager.LoadSceneAsync(A+"Scenes/Bootstrap.unity",LoadSceneMode.Single);}finally{SceneManager.sceneLoaded-=Configure;}yield return Ready();
        }
        [UnitySetUp] public IEnumerator Setup(){ProgressStoreIsolation.UseSavedCourse=true;store=new MemoryProgressStore();yield return RestartApp();}
        [UnityTearDown] public IEnumerator Cleanup(){ProgressStoreIsolation.UseSavedCourse=false;foreach(var b in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.Destroy(b.gameObject);yield return null;foreach(string p in new[]{A+"Scenes/MainMenu.unity",A+"Scenes/LessonWorkspace.unity"}){var s=SceneManager.GetSceneByPath(p);if(s.IsValid()&&s.isLoaded)yield return SceneManager.UnloadSceneAsync(s);}}
        private IEnumerator Open(){Click(Menu.Query<Button>().ToList().Single(b=>b.userData as string=="C1.L5"));yield return Ready();}
        private void Select(string id)=>Click(UI.Q<Button>("term-"+id));
        private void Input(double a,double b){UI.Q<DropdownField>("input-scope").index=1;UI.Q<DoubleField>("input-0").value=a;UI.Q<DoubleField>("input-1").value=b;}
        private void Act()
        {
            switch(Lesson.Presentation.Step.Id){
                case "state":Select("state.current");Select("input.applied");Select("output.current");break;
                case "same-position":UI.Q<DoubleField>("initial-state-3").value=.5;break;
                case "hover":Input(4.905,4.905);break;
                case "collective":Input(5.905,5.905);break;
                case "differential":Input(5.905,3.905);break;
                case "tilt":UI.Q<DoubleField>("initial-state-4").value=.2;break;
                case "trajectory":UI.Q<SliderInt>("timeline-seek").value=10;Select("output.current");break;
                case "reference":UI.Q<DoubleField>("parameter-cost.reference.state.10.0").value=.4;Select("desired-h");break;
                case "cost":UI.Q<DoubleField>("parameter-cost.q.3").value=2;break;
                case "input-limit":UI.Q<DropdownField>("input-scope").index=0;UI.Q<DoubleField>("input-0").value=9;break;
                case "altitude-limit":UI.Q<DoubleField>("initial-state-2").value=2.5;break;
                case "terminal":UI.Q<SliderInt>("timeline-seek").value=50;Select("input.applied");Select("state.next");Select("stage");break;
                default:Assert.Fail("Unknown activity");break;
            }
        }
        private void Next(){Act();Click(UI.Q<Button>("answer-"+Lesson.Presentation.Step.Id+"-ok"));Click(UI.Q<Button>("lesson-next"));}
        private void Reach(string id){while(Lesson.Presentation.Step.Id!=id)Next();}
        [UnityTest] public IEnumerator SavedBootstrapExposesDroneAndConsumersShareTheSamePublication()
        {
            Assert.That(Menu.Q<Label>("progress-C1.L5").text,Does.Contain("C1.L4"));yield return Open();
            Assert.That(app.Navigation.ActiveLesson.Id,Is.EqualTo("C1.L5"));Assert.That(Sim.State.Result.Configuration.Model.Descriptor.Dimensions.State,Is.EqualTo(6));
            Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));Assert.That(View.GetComponentInChildren<PlanarDroneBodyView>(true).DisplayedState,Is.SameAs(Sim.State));
            Assert.That(Hud.Formula.Resolutions["input.applied"].Value.Rows,Is.EqualTo(2));Assert.That(Hud.Formula.Resolutions["output.current"].Value.Rows,Is.EqualTo(2));
            foreach(var plot in View.GetComponentsInChildren<StateTrajectoryView>(true))Assert.That(plot.Trajectory.Source,Is.SameAs(Sim.State.Result.History));
            Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator FullTwelveStepPathPersistsReopensAndSurvivesAppRestart()
        {
            yield return Open();while(!Lesson.Presentation.IsCompleted)Next();Assert.That(store.SaveCount,Is.EqualTo(1));Assert.That(app.Progress.GetStatus("C1.L5").IsCompleted,Is.True);Assert.That(app.Progress.GetStatus("C1.L4").IsCompleted,Is.False);
            Click(Menu.Q<Button>("return-to-menu"));yield return Ready();yield return Open();Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("state"));Assert.That(app.Progress.GetStatus("C1.L5").IsCompleted,Is.True);
            Click(UI.Q<Button>("lesson-restart"));Assert.That(store.SaveCount,Is.EqualTo(1));yield return RestartApp();Assert.That(app.Progress.GetStatus("C1.L5").IsCompleted,Is.True);Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo("C1.L5"));
        }
        [UnityTest] public IEnumerator BothInputEditorsSelectExplicitVectorComponentsAndRejectWrongOutcomes()
        {
            yield return Open();Reach("collective");Click(UI.Q<Button>("answer-collective-ok"));Input(3.905,3.905);Assert.That(Lesson.Next(),Is.False);
            Input(5.905,4.905);Assert.That(Lesson.Next(),Is.False);Input(5.905,5.905);Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Assert.That(Sim.State.Result.History.States[1][3],Is.EqualTo(.04).Within(1e-12));Assert.That(Hud.Formula.Resolutions["u1"].ComponentSelection,Is.Zero);Assert.That(Hud.Formula.Resolutions["u2"].ComponentSelection,Is.EqualTo(1));
            Select("u2");Assert.That(View.GetComponentsInChildren<HighlightTarget>(true).Single(t=>t.SemanticRole=="input.thrust2").IsHighlighted,Is.True);
            Click(UI.Q<Button>("lesson-next"));Click(UI.Q<Button>("answer-differential-ok"));Input(3.905,5.905);Assert.That(Lesson.Next(),Is.False);Input(5.905,3.905);Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Assert.That(Hud.Formula.Resolutions["next5"].SampleIndex,Is.EqualTo(1));Assert.That(Hud.Formula.Resolutions["next5"].Value.Scalar,Is.EqualTo(.08).Within(1e-12));Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));
        }
        [UnityTest] public IEnumerator DisclosureShowsOnlyRelevantStateFieldAndPreviousResetDoNotAccredit()
        {
            yield return Open();Next();yield return null;
            Assert.That(UI.Q<DoubleField>("initial-state-3").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            foreach(int i in new[]{0,1,2,4,5})Assert.That(UI.Q<DoubleField>("initial-state-"+i).resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Act();Click(UI.Q<Button>("answer-wrong"));Assert.That(Lesson.Next(),Is.False);Click(UI.Q<Button>("answer-same-position-ok"));Click(UI.Q<Button>("lesson-next"));Click(UI.Q<Button>("lesson-previous"));
            Assert.That(Sim.State.Result.Configuration.InitialState[3],Is.Zero);Assert.That(Lesson.Presentation.CanAdvance,Is.False);Click(UI.Q<Button>("reset"));Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Click(UI.Q<Button>("lesson-restart"));Assert.That(store.Load(),Is.Null);
            var old=Sim;Click(Menu.Q<Button>("return-to-menu"));yield return Ready();Assert.That(old.State.IsClosed,Is.True);yield return Open();Assert.That(Sim,Is.Not.SameAs(old));Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator ReferenceCostAndConstraintViewsReadTheSameImmutableRevision()
        {
            yield return Open();Reach("reference");var a=Sim.State.Result;Act();var b=Sim.State.Result;
            Assert.That(a.Reference.Outputs[10][0],Is.EqualTo(.1));Assert.That(b.Reference.Outputs[10][0],Is.EqualTo(.4));Assert.That(Hud.Formula.Resolutions["reference.output"].Value.Vector,Is.EqualTo(new[]{.4,1}));
            Assert.That(View.GetComponentInChildren<ReferenceTrajectoryView>(true).DisplayedState,Is.SameAs(Sim.State));Next();Act();Assert.That(UI.Q<Label>("cost-live").text,Does.Contain("ℓ_tot"));Assert.That(Hud.Formula.Resolutions["inertia"].Value.Units.Single(),Is.EqualTo("kg·m²"));
            Next();Act();Assert.That(Sim.State.Sample.AppliedControl[0],Is.EqualTo(9));Assert.That(Hud.Formula.Resolutions["g0"].Value.Scalar,Is.EqualTo(1));
            foreach(var v in View.GetComponentsInChildren<ConstraintRegionVisualizer>(true))Assert.That(v.DisplayedState,Is.SameAs(Sim.State));Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));
        }
        [UnityTest] public IEnumerator TerminalKeepsPositionOutputButHasNoForceNextStageOrPathResidual()
        {
            yield return Open();Reach("terminal");Act();Assert.That(Sim.State.Sample.PhysicalTimeSeconds,Is.EqualTo(1));
            foreach(string id in new[]{"input.applied","state.next","stage","g0"})Assert.That(Hud.Formula.Resolutions[id].Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Hud.Formula.Resolutions["output.current"].Value.Vector,Is.EqualTo(new[]{0d,1d}));
            Assert.That(View.GetComponentsInChildren<StateTrajectoryView>(true).All(v=>!v.Next.SampleIndex.HasValue),Is.True);
            foreach(var h in View.GetComponentsInChildren<HighlightTarget>(true).Where(t=>t.SemanticRole.StartsWith("input.")))Assert.That(h.SampleIndex,Is.Null);
        }
        [UnityTest] public IEnumerator NonRepresentableAndFailedCostKeepPublicationAndRecoverWithoutProgress()
        {
            yield return Open();Click(UI.Q<Button>("lesson-explore"));UI.Q<DoubleField>("initial-state-0").value=1e40;
            Assert.That(Sim.State.Error,Is.Null);Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.NonRepresentable));Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));
            Click(UI.Q<Button>("reset"));Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.Available));var a=Sim.State.Result;
            UI.Q<DoubleField>("parameter-cost.r.0").value=0;Assert.That(Sim.State.Error,Is.Not.Null);Assert.That(Sim.State.Result,Is.SameAs(a));Assert.That(UI.Q<DoubleField>("input-0").enabledInHierarchy,Is.False);Assert.That(UI.Q<DoubleField>("input-1").enabledInHierarchy,Is.False);
            Click(UI.Q<Button>("reset"));Assert.That(Sim.State.Error,Is.Null);Assert.That(store.Load(),Is.Null);Click(UI.Q<Button>("lesson-restart"));Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("state"));
        }
    }
}
#endif
