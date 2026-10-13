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
    public sealed class ProductionLinearizationRuntimeTests
    {
        const string A="Assets/OptimalControl/";
        private AppBootstrap bootstrap;
        private MemoryProgressStore store;
        private LessonHudPresenter Hud=>Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root=>Hud.GetComponent<UIDocument>().rootVisualElement;
        private VisualElement Menu=>Object.FindFirstObjectByType<CourseMenuPresenter>().GetComponent<UIDocument>().rootVisualElement;
        private SimulationSession Sim=>bootstrap.ActiveHost.Session;
        private LessonSession Lesson=>bootstrap.ActiveHost.Lesson;
        private ExperimentVisualization View=>bootstrap.ActiveHost.GetComponent<ExperimentVisualization>();
        private static void Click(Button button)
        {
            Assert.That(button,Is.Not.Null); Assert.That(button.enabledInHierarchy,Is.True,button.name);
            using var submit=NavigationSubmitEvent.GetPooled(); submit.target=button; button.SendEvent(submit);
        }
        private IEnumerator Ready()
        {
            float limit=Time.realtimeSinceStartup+20;
            do { yield return null; bootstrap=Object.FindFirstObjectByType<AppBootstrap>(); Assert.That(Time.realtimeSinceStartup,Is.LessThan(limit),bootstrap?.Navigation?.StatusMessage); }
            while(bootstrap?.Navigation==null || !bootstrap.Navigation.IsReady);
            yield return null;
        }
        private IEnumerator RestartApp()
        {
            foreach(var app in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Object.Destroy(app.gameObject);
            yield return null;
            void Configure(Scene scene,LoadSceneMode mode)
            { if(scene.path==A+"Scenes/Bootstrap.unity") Object.FindFirstObjectByType<AppBootstrap>().Configure(store:store); }
            SceneManager.sceneLoaded+=Configure;
            try { yield return SceneManager.LoadSceneAsync(A+"Scenes/Bootstrap.unity",LoadSceneMode.Single); }
            finally { SceneManager.sceneLoaded-=Configure; }
            yield return Ready();
        }
        [UnitySetUp] public IEnumerator Setup()
        { ProgressStoreIsolation.UseSavedCourse=true; store=new MemoryProgressStore(); yield return RestartApp(); }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ProgressStoreIsolation.UseSavedCourse=false;
            foreach(var app in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Object.Destroy(app.gameObject);
            yield return null;
            foreach(string path in new[]{A+"Scenes/MainMenu.unity",A+"Scenes/LessonWorkspace.unity"})
            { var scene=SceneManager.GetSceneByPath(path); if(scene.IsValid()&&scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
        }
        private IEnumerator Open(string id="C1.L2")
        { Click(Menu.Query<Button>().ToList().Single(b=>b.userData as string==id)); yield return Ready(); Assert.That(bootstrap.Navigation.ActiveLesson.Id,Is.EqualTo(id)); }
        private void Select(string term)=>Click(Root.Q<Button>("term-"+term));
        private void Next(string answer) { Click(Root.Q<Button>("answer-"+answer)); Click(Root.Q<Button>("lesson-next")); }
        private void ToNominal()
        {
            Select("input.applied"); Click(Root.Q<Button>("step-next")); Next("pair");
            Select("state.current"); Click(Root.Q<Button>("step-next")); Next("constant");
            Root.Q<DropdownField>("input-scope").index=1; Root.Q<DoubleField>("input-0").value=9.81; Next("balance");
        }
        private void ToInitial()
        {
            ToNominal(); Click(Root.Q<Button>("nominal-capture")); Select("nominal.state"); Next("valid");
            Root.Q<DoubleField>("perturb-input-0").value=1; Select("delta.input"); Next("zero");
            Select("A10"); Select("B10"); Next("nominal"); Select("delta.next"); Next("first");
            Click(Root.Q<Button>("step-next")); Select("error"); Next("first-only");
        }
        private void Complete()
        {
            ToInitial(); Root.Q<DoubleField>("perturb-initial-0").value=.1; Click(Root.Q<Button>("step-next")); Select("error"); Next("separate");
            Root.Q<DoubleField>("perturb-initial-0").value=.8; Next("local"); Select("error"); Next("approximation");
            Root.Q<SliderInt>("timeline-seek").value=100; Select("delta.input"); Next("terminal");
        }
        private void CompleteIntroduction()
        {
            Next("robotics"); Next("both"); Select("state.current"); Root.Q<DoubleField>("initial-state-1").value=.5; Next("full");
            Select("output.current"); Next("angle"); Select("derivative"); Next("rate"); Next("velocity");
            Select("input.applied"); Root.Q<DoubleField>("input-0").value=1; Next("old");
            Select("state.next"); Click(Root.Q<Button>("step-next")); Next("saved");
            Select("time.physical"); Click(Root.Q<Button>("step-next")); Next("index");
            Select("state.current"); Click(Root.Q<Button>("step-next")); Next("time");
            Root.Q<SliderInt>("timeline-seek").value=100; Select("input.applied"); Next("terminal");
        }
        [UnityTest] public IEnumerator IntroductionEvidenceRejectsWrongComponentAndBothChangedThenCompletesNormalPath()
        {
            yield return Open("C1.L1");Next("robotics");Next("both");Select("state.current");Click(Root.Q<Button>("answer-full"));
            Root.Q<DoubleField>("initial-state-0").value=.7;
            Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(Lesson.Next(),Is.False);
            Root.Q<DoubleField>("initial-state-1").value=.5;Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Root.Q<DoubleField>("initial-state-0").value=.35;Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Root.Q<DoubleField>("initial-state-1").value=0;Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(store.Load(),Is.Null);
            Click(Menu.Q<Button>("return-to-menu"));yield return Ready();yield return Open("C1.L1");CompleteIntroduction();
            Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo("C1.L1"));
        }
        [UnityTest] public IEnumerator AmplitudeEvidenceRejectsSmallerErrorAndDifferentSampleThenCompletesOnSameNominal()
        {
            yield return Open();ToInitial();Root.Q<DoubleField>("perturb-initial-0").value=.1;
            Click(Root.Q<Button>("step-next"));Select("error");Next("separate");var baseline=(SessionState)Lesson.ActivityBaseline.Snapshot;
            Click(Root.Q<Button>("answer-local"));Root.Q<DoubleField>("perturb-initial-0").value=.05;
            Assert.That(Sim.State.Linearization.Errors[1][1],Is.LessThan(baseline.Linearization.Errors[1][1]));
            Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(Lesson.Next(),Is.False);Assert.That(store.Load(),Is.Null);
            Root.Q<DoubleField>("perturb-initial-0").value=.8;Root.Q<SliderInt>("timeline-seek").value=2;
            Assert.That(Lesson.Presentation.CanAdvance,Is.False);Root.Q<SliderInt>("timeline-seek").value=1;
            Assert.That(Sim.State.Linearization.Nominal,Is.SameAs(baseline.Linearization.Nominal));Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Next("local");Select("error");Next("approximation");Root.Q<SliderInt>("timeline-seek").value=100;Select("delta.input");Next("terminal");
            Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo("C1.L2"));
        }
        [UnityTest] public IEnumerator NormalBootstrapShowsProductionLessonAndMissingPrerequisiteWithoutPromotingPrototype()
        {
            Assert.That(bootstrap.Navigation.Course.Id,Is.EqualTo("optimal-control"));
            Assert.That(Menu.Q<Label>("progress-C1.L2").text,Does.Contain("C1.L1"));
            Assert.That(bootstrap.Navigation.OpenLesson("prototype.pendulum"),Is.False); Assert.That(bootstrap.Navigation.OpenLesson("fixture.f10.static"),Is.False);
            yield return Open("C1.L1"); Click(Root.Q<Button>("lesson-explore"));
            Assert.That(Root.Q<VisualElement>("linearization-controls").style.display.value,Is.EqualTo(DisplayStyle.None));
            Click(Menu.Q<Button>("return-to-menu")); yield return Ready();
            yield return Open(); Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("trajectory"));
            Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState)); Assert.That(Sim.Capabilities.HasFlag(ExperimentCapabilities.Linearization),Is.True);
            Assert.That(bootstrap.Progress.GetStatus("C1.L2").IsCompleted,Is.False);
        }
        [UnityTest] public IEnumerator CompleteL1ThenWholeF12ThroughRealUIStoresIndependentProgressAndReopensFresh()
        {
            yield return Open("C1.L1"); CompleteIntroduction(); Click(Menu.Q<Button>("return-to-menu")); yield return Ready();
            Assert.That(Menu.Q<Label>("progress-C1.L2").text,Does.Not.Contain("C1.L1"));
            yield return Open(); Complete(); Assert.That(Lesson.Presentation.IsCompleted,Is.True);
            Assert.That(store.Load().Completions.Select(c=>c.LessonId),Is.EquivalentTo(new[]{"C1.L1","C1.L2"}));
            var old=Sim; Click(Menu.Q<Button>("return-to-menu")); yield return Ready(); Assert.That(old.State.IsClosed,Is.True);
            Assert.That(Menu.Q<Label>("progress-C1.L2").text,Does.Contain("Completamento salvato")); yield return Open();
            Assert.That(Sim,Is.Not.SameAs(old)); Assert.That(Sim.State.Comparison,Is.Null); Assert.That(Lesson.Presentation.StepIndex,Is.Zero);
            Assert.That(Lesson.Presentation.IsCompleted,Is.False); Assert.That(store.SaveCount,Is.EqualTo(2));
            yield return RestartApp(); Assert.That(bootstrap.Progress.GetStatus("C1.L2").IsCompleted,Is.True);
        }
        [UnityTest] public IEnumerator HUDAndVisualsUseSameFrozenLinearizationAcrossSeekAndTwoPerturbations()
        {
            yield return Open(); ToInitial(); Root.Q<DoubleField>("perturb-initial-0").value=.1; Click(Root.Q<Button>("step-next"));
            var a=Sim.State; var linearView=View.GetComponentInChildren<LinearizationComparisonView>(true);
            Assert.That(linearView.DisplayedState,Is.SameAs(a)); Assert.That(Hud.DisplayedState,Is.SameAs(a));
            Assert.That(Hud.Formula.Resolutions["error"].Value.Vector[1],Is.EqualTo(.00003268365389231713).Within(1e-13));
            var predicted=View.GetComponentsInChildren<TrajectoryRenderer>(true).Single(v=>v.name=="LinearPrediction");
            var line=predicted.GetComponent<LineRenderer>(); Assert.That(line.positionCount,Is.EqualTo(101));
            Assert.That(line.GetPosition(1).x,Is.EqualTo(.4f).Within(1e-6)); Assert.That(line.GetPosition(1).y,Is.EqualTo(-.01962f).Within(1e-6));
            Select("error"); Assert.That(View.GetComponentsInChildren<HighlightTarget>(true).Single(t=>t.SemanticRole=="linearization.error").IsHighlighted,Is.True);
            Next("separate"); var amplitudeStart=Sim.State;
            Assert.That(amplitudeStart.Result.Configuration.InitialState[0],Is.EqualTo(.1));
            Root.Q<DoubleField>("perturb-initial-0").value=.8; var b=Sim.State;
            Assert.That(Hud.Formula.Resolutions["error"].Value.Vector[1],Is.EqualTo(.01621473496551364).Within(1e-13));
            Assert.That(a.Linearization.Perturbed,Is.SameAs(a.Result)); Assert.That(a.Result.Configuration.InitialState[0],Is.EqualTo(.1));
            Assert.That(linearView.DisplayedComparison,Is.SameAs(b.Linearization)); Assert.That(b.Linearization.Nominal,Is.SameAs(amplitudeStart.Linearization.Nominal));
            Root.Q<SliderInt>("timeline-seek").value=2;
            Assert.That(Hud.Formula.Resolutions["error"].SampleIndex,Is.EqualTo(2)); Assert.That(Sim.State.Linearization,Is.SameAs(b.Linearization));
        }
        [UnityTest] public IEnumerator WrongAnswersNoopPreviousRestartAndLeavingIncompleteNeverSaveCompletion()
        {
            yield return Open(); Click(Root.Q<Button>("answer-curve")); Assert.That(Lesson.Next(),Is.False); Assert.That(store.Load(),Is.Null);
            ToInitial(); Click(Root.Q<Button>("step-next")); Select("error"); Click(Root.Q<Button>("answer-separate")); Assert.That(Lesson.Next(),Is.False);
            Root.Q<DoubleField>("perturb-initial-0").value=.1; Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Click(Root.Q<Button>("lesson-previous")); Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Click(Root.Q<Button>("lesson-restart")); Assert.That(Lesson.Presentation.StepIndex,Is.Zero); Assert.That(Sim.State.Comparison,Is.Null);
            var old=Sim; Click(Menu.Q<Button>("return-to-menu")); yield return Ready(); Assert.That(old.State.IsClosed,Is.True); Assert.That(store.Load(),Is.Null);
            yield return Open(); Assert.That(Lesson.Presentation.StepIndex,Is.Zero); Assert.That(Sim.State.Linearization,Is.Null);
        }
        [UnityTest] public IEnumerator FailedShortRequestedConfigurationDisablesPerturbationEditorsAndResetRecovers()
        {
            yield return Open(); ToInitial(); Click(Root.Q<Button>("lesson-explore")); Root.Q<SliderInt>("timeline-seek").value=50; var valid=Sim.State;
            Assert.That(Sim.Execute(SimulationCommand.SetInitialState(new NumericVector(double.MaxValue,double.MaxValue))),Is.False);
            Assert.That(Sim.Execute(SimulationCommand.SetHorizon(1)),Is.False);
            Assert.That(Sim.State.Result,Is.SameAs(valid.Result)); Assert.That(Sim.State.Sample.Timestep,Is.EqualTo(50)); Assert.That(Sim.State.RequestedConfiguration.Horizon,Is.EqualTo(1));
            Assert.That(Root.Q<DoubleField>("perturb-input-0").enabledInHierarchy,Is.False); Assert.That(Root.Q<DoubleField>("perturb-initial-0").enabledInHierarchy,Is.False);
            Assert.That(Sim.Execute(SimulationCommand.SetInputPerturbation(50,0,1)),Is.False);
            Assert.That(Sim.Execute(SimulationCommand.SetInitialPerturbation(0,.1)),Is.False);
            Click(Root.Q<Button>("reset")); Assert.That(Sim.State.Error,Is.Null); Assert.That(Sim.State.Result.History.Horizon,Is.EqualTo(100)); Assert.That(Sim.State.Sample.Timestep,Is.Zero);
            Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.Available)); Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator NonRepresentableComparisonKeepsMathematicsSharedAndResetRestoresPresentation()
        {
            yield return Open(); ToInitial(); Click(Root.Q<Button>("lesson-explore"));
            Assert.That(Lesson.Execute(SimulationCommand.SetInitialPerturbation(0,1e40)),Is.True);
            Assert.That(Sim.State.Error,Is.Null); Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.NonRepresentable));
            Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState)); Assert.That(Hud.DisplayedState.Linearization,Is.SameAs(Sim.State.Linearization));
            Assert.That(View.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),Is.True); Assert.That(store.Load(),Is.Null);
            Click(Root.Q<Button>("reset")); Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.Available)); Assert.That(Sim.State.Linearization,Is.Null);
        }
        [UnityTest] public IEnumerator TerminalComparisonHasNoFabricatedInputOrJacobianAndPreservesPhysicalTime()
        {
            yield return Open(); ToInitial(); Click(Root.Q<Button>("lesson-explore")); Click(Root.Q<Button>("nominal-capture"));
            Root.Q<SliderInt>("timeline-seek").value=100; Hud.Formula.SetFormula("c1.linearized-dynamics"); Hud.Formula.Render(Sim.State);
            foreach(string id in new[]{"delta.input","delta.next","A10","B10"}) Assert.That(Hud.Formula.Resolutions[id].Validity,Is.EqualTo(MetricValidity.NotApplicable),id);
            foreach(string id in new[]{"prediction","error"}) { Assert.That(Hud.Formula.Resolutions[id].Validity,Is.EqualTo(MetricValidity.Available)); Assert.That(Hud.Formula.Resolutions[id].SampleIndex,Is.EqualTo(100)); }
            Assert.That(Sim.State.Sample.DiscretizationStepSeconds,Is.EqualTo(.02)); Assert.That(Sim.State.Sample.PhysicalTimeSeconds,Is.EqualTo(2));
            Assert.That(Root.Q<DoubleField>("perturb-input-0").enabledInHierarchy,Is.False);
            Assert.That(Root.Q<Label>("linearization-status").text,Does.Contain("Δu_T: N/A"));
            Assert.That(View.GetComponentsInChildren<HighlightTarget>(true).Single(t=>t.SemanticRole=="linearization.input").SampleIndex,Is.Null);
        }
        [UnityTest] public IEnumerator PerturbationSliderOwnsArrowsAndRejectedEditRestoresPublishedValues()
        {
            yield return Open(); ToInitial();
            var slider=Root.Q<Slider>("perturb-initial-0-slider"); slider.Focus(); yield return null;
            Assert.That(Hud.BlocksLessonShortcut("StepForward"),Is.True);
            Assert.That(Hud.BlocksLessonShortcut("StepBackward"),Is.True);
            slider.value=.25f;
            Assert.That(Sim.State.Result.Configuration.InitialState[0],Is.EqualTo(.25));
            var valid=Sim.State; Root.Q<DoubleField>("perturb-initial-0").value=double.NaN;
            Assert.That(Sim.State,Is.SameAs(valid));
            Assert.That(Root.Q<DoubleField>("perturb-initial-0").value,Is.EqualTo(.25));
            Assert.That(Root.Q<Label>("linearization-status").text,Does.Contain("Modifica rifiutata"));
            Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator PreviousReconstructsNominalAndSmallPerturbationBeforeRepeatingAmplitude()
        {
            yield return Open(); ToInitial(); Root.Q<DoubleField>("perturb-initial-0").value=.1;
            Click(Root.Q<Button>("step-next")); Select("error"); Next("separate");
            Root.Q<DoubleField>("perturb-initial-0").value=.8; var retained=Sim.State; Next("local");
            Click(Root.Q<Button>("lesson-previous"));
            Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("amplitude")); Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Assert.That(Sim.State.Linearization,Is.Not.Null); Assert.That(Sim.State.Sample.Timestep,Is.EqualTo(1));
            Assert.That(Root.Q<DoubleField>("perturb-initial-0").value,Is.EqualTo(.1));
            Assert.That(Hud.Formula.Resolutions["error"].Value.Vector[1],Is.EqualTo(.00003268365389231713).Within(1e-13));
            Assert.That(retained.Result.Configuration.InitialState[0],Is.EqualTo(.8));
            Root.Q<DoubleField>("perturb-initial-0").value=.8; Next("local");
            Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("error-meaning")); Assert.That(store.Load(),Is.Null);
        }
    }
}
#endif
