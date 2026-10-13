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
    public sealed class ProductionContinuousTransferRuntimeTests
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
        private ContinuousFunctionalView Functional=>View.GetComponentInChildren<ContinuousFunctionalView>(true);
        private static void Click(Button b){Assert.That(b,Is.Not.Null);Assert.That(b.enabledInHierarchy,Is.True,b.name);using var e=NavigationSubmitEvent.GetPooled();e.target=b;b.SendEvent(e);}
        private IEnumerator Ready(){float end=Time.realtimeSinceStartup+25;do{yield return null;app=Object.FindFirstObjectByType<AppBootstrap>();Assert.That(Time.realtimeSinceStartup,Is.LessThan(end),app?.Navigation?.StatusMessage);}while(app?.Navigation==null||!app.Navigation.IsReady);yield return null;}
        private IEnumerator RestartApp()
        {
            foreach(var b in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.Destroy(b.gameObject);yield return null;
            void Configure(Scene s,LoadSceneMode m){if(s.path==A+"Scenes/Bootstrap.unity")Object.FindFirstObjectByType<AppBootstrap>().Configure(store:store);}
            SceneManager.sceneLoaded+=Configure;try{yield return SceneManager.LoadSceneAsync(A+"Scenes/Bootstrap.unity",LoadSceneMode.Single);}finally{SceneManager.sceneLoaded-=Configure;}yield return Ready();
        }
        [UnitySetUp] public IEnumerator Setup(){ProgressStoreIsolation.UseSavedCourse=true;store=new MemoryProgressStore();yield return RestartApp();}
        [UnityTearDown] public IEnumerator Cleanup(){ProgressStoreIsolation.UseSavedCourse=false;foreach(var b in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.Destroy(b.gameObject);yield return null;foreach(string p in new[]{A+"Scenes/MainMenu.unity",A+"Scenes/LessonWorkspace.unity"}){var s=SceneManager.GetSceneByPath(p);if(s.IsValid()&&s.isLoaded)yield return SceneManager.UnloadSceneAsync(s);}}
        private IEnumerator Open(){Click(Menu.Query<Button>().ToList().Single(b=>b.userData as string=="C1.L6"));yield return Ready();}
        private void Select(string id)=>Click(UI.Q<Button>("term-"+id));
        private void Act()
        {
            switch(Lesson.Presentation.Step.Id)
            {
                case "continuous":Select("duration");Select("terminal");break;
                case "dynamics":Select("time.index");Select("time.physical");Select("time.step");break;
                case "equilibria":UI.Q<SliderInt>("timeline-seek").value=10;Select("reference.state");Select("reference.input");break;
                case "desired":Select("desired-h");break;
                case "density":Select("density");Select("cell");break;
                case "weights":UI.Q<DoubleField>("parameter-cost.q.0").value=2;break;
                case "cumulative":UI.Q<SliderInt>("timeline-seek").value=20;Select("cumulative");Select("terminal");break;
                case "manual":UI.Q<DropdownField>("input-scope").index=1;UI.Q<DoubleField>("input-0").value=1;Select("state.next");break;
                case "refinement":UI.Q<SliderInt>("discretization-choice").value=1;Select("cost-difference");break;
                case "accuracy":UI.Q<DoubleField>("physical-step").value=.05;Select("oracle-error");break;
                case "definitions":Select("cell");break;
                case "terminal":UI.Q<SliderInt>("timeline-seek").value=20;Select("input.applied");Select("density");Select("cell");break;
                default:Assert.Fail("Uncovered F15 activity");break;
            }
        }
        private void Next(){Act();Click(UI.Q<Button>("answer-"+Lesson.Presentation.Step.Id+"-ok"));Click(UI.Q<Button>("lesson-next"));}
        private void Reach(string id){while(Lesson.Presentation.Step.Id!=id)Next();}
        [UnityTest] public IEnumerator SavedBootstrapExposesF15AndAllConsumersShareFrozenPublication()
        {
            Assert.That(Menu.Q<Label>("progress-C1.L6").text,Does.Contain("C1.L5"));yield return Open();
            Assert.That(app.Navigation.ActiveLesson.Id,Is.EqualTo("C1.L6"));Assert.That(Sim.State.Result.Configuration.Horizon,Is.EqualTo(20));
            Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));Assert.That(Functional.DisplayedState,Is.SameAs(Sim.State));
            Assert.That(Functional.Rectangles.positionCount,Is.EqualTo(100));Assert.That(Functional.InputSamples.positionCount,Is.EqualTo(40));
            Assert.That(Hud.Formula.Resolutions["duration"].Value.Scalar,Is.EqualTo(2));Assert.That(Hud.Formula.Resolutions["density"].Value.Units.Single(),Is.EqualTo("1/s"));
            foreach(var p in View.GetComponentsInChildren<StateTrajectoryView>(true))Assert.That(p.Trajectory.Source,Is.SameAs(Sim.State.Result.History));
            Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator CompleteTwelveStepsPersistsReopensAndRestartsApplication()
        {
            yield return Open();while(!Lesson.Presentation.IsCompleted)Next();Assert.That(store.SaveCount,Is.EqualTo(1));Assert.That(app.Progress.GetStatus("C1.L6").IsCompleted,Is.True);Assert.That(app.Progress.GetStatus("C1.L5").IsCompleted,Is.False);
            Click(Menu.Q<Button>("return-to-menu"));yield return Ready();yield return Open();Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("continuous"));Click(UI.Q<Button>("lesson-restart"));Assert.That(store.SaveCount,Is.EqualTo(1));
            yield return RestartApp();Assert.That(app.Progress.GetStatus("C1.L6").IsCompleted,Is.True);Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo("C1.L6"));
        }
        [UnityTest] public IEnumerator RefinementResamplesPhysicalReferenceAndKeepsABGridsAndHudAligned()
        {
            yield return Open();Reach("refinement");var a=Sim.State.Result;Act();var b=Sim.State.Result;
            Assert.That(Sim.State.DiscretizationComparison.SamePhysicalProblem,Is.True);Assert.That(Sim.State.Comparison.Result,Is.SameAs(a));
            Assert.That(a.Configuration.Horizon,Is.EqualTo(20));Assert.That(b.Configuration.Horizon,Is.EqualTo(40));Assert.That(a.Reference.Reference.States[10][0],Is.EqualTo(.5));Assert.That(b.Reference.Reference.States[20][0],Is.EqualTo(.5));
            Assert.That(Functional.Rectangles.positionCount,Is.EqualTo(200));Assert.That(Functional.DisplayedState,Is.SameAs(Hud.DisplayedState));
            Assert.That(View.GetComponentsInChildren<ComparisonTrajectoryRenderer>(true).Single().gameObject.activeInHierarchy,Is.True);
            Assert.That(UI.Q<Label>("physical-grid-status").text,Does.Contain("N=40"));Assert.That(UI.Q<Label>("comparison-status").text,Does.Contain("Stesso problema fisico: sì"));
            Assert.That(UI.Q<Label>("comparison-status").text,Does.Not.Contain("N/A"));
            Assert.That(Hud.Formula.Resolutions["cost-difference"].Value.Scalar,Is.EqualTo(b.Costs.Total-a.Costs.Total));
            Click(UI.Q<Button>("lesson-explore"));UI.Q<SliderInt>("timeline-seek").value=20;Assert.That(Hud.Formula.Resolutions["density"].SampleIndex,Is.EqualTo(20));Assert.That(Functional.DisplayedState.Sample.PhysicalTimeSeconds,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator WrongOutcomeAnswerPreviousResetAndRestartNeverAccredit()
        {
            yield return Open();Reach("accuracy");Click(UI.Q<Button>("answer-accuracy-ok"));Select("oracle-error");UI.Q<DoubleField>("physical-step").value=.2;Assert.That(Lesson.Next(),Is.False);
            Act();Click(UI.Q<Button>("answer-wrong"));Assert.That(Lesson.Next(),Is.False);Click(UI.Q<Button>("answer-accuracy-ok"));Click(UI.Q<Button>("lesson-next"));Click(UI.Q<Button>("lesson-previous"));
            Assert.That(Sim.State.Result.Accuracy.TerminalError[0],Is.EqualTo(-.1).Within(1e-12));Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Act();Assert.That(Sim.State.Result.Accuracy.TerminalError[0],Is.EqualTo(-.05).Within(1e-12));Click(UI.Q<Button>("reset"));Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Click(UI.Q<Button>("lesson-restart"));Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator DensitySelectionHighlightsSavedCellsAndTerminalNeverInventsRunningData()
        {
            yield return Open();Reach("density");Select("density");var target=View.GetComponentsInChildren<HighlightTarget>(true).Single(t=>t.SemanticRole=="cost.density");Assert.That(target.IsHighlighted,Is.True);Assert.That(target.RevisionId,Is.EqualTo(Sim.State.Result.RevisionId));
            Assert.That(Hud.Formula.Resolutions["cell"].Value.Scalar,Is.EqualTo(Sim.State.Result.Costs.Stages[10].Total));
            Reach("terminal");Act();foreach(string id in new[]{"input.applied","density","cell"})Assert.That(Hud.Formula.Resolutions[id].Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Sim.State.Sample.NextState,Is.Null);Assert.That(target.SampleIndex,Is.Null);Assert.That(UI.Q<Label>("cost-live").text,Does.Contain("N/A"));Assert.That(Hud.DisplayedState,Is.SameAs(Functional.DisplayedState));
        }
        [UnityTest] public IEnumerator FailedRequestAndNonRepresentablePreserveResultAndRecover()
        {
            yield return Open();Click(UI.Q<Button>("lesson-explore"));UI.Q<DoubleField>("initial-state-0").value=1e200;var a=Sim.State.Result;
            UI.Q<DoubleField>("physical-step").value=2;Assert.That(Sim.State.Error,Is.Not.Null);Assert.That(Sim.State.Result,Is.SameAs(a));Assert.That(UI.Q<DoubleField>("input-0").enabledInHierarchy,Is.False);Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));
            Click(UI.Q<Button>("reset"));UI.Q<DoubleField>("initial-state-0").value=1e40;Assert.That(Sim.State.Error,Is.Null);Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.NonRepresentable));Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));
            Click(UI.Q<Button>("reset"));Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.Available));Click(UI.Q<Button>("lesson-restart"));Assert.That(store.Load(),Is.Null);
        }
    }
}
#endif
