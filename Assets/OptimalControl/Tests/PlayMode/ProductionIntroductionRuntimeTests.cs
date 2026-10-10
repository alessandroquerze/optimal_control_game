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
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    /// <summary>Uses the saved production Bootstrap. No course/lesson/experiment replacement.</summary>
    public sealed class ProductionIntroductionRuntimeTests
    {
        private const string A = "Assets/OptimalControl/";
        private AppBootstrap bootstrap;
        private MemoryProgressStore store;
        private LessonHudPresenter Hud => Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root => Hud.GetComponent<UIDocument>().rootVisualElement;
        private VisualElement Menu => Object.FindFirstObjectByType<CourseMenuPresenter>().GetComponent<UIDocument>().rootVisualElement;
        private SimulationSession Sim => bootstrap.ActiveHost.Session;
        private LessonSession Lesson => bootstrap.ActiveHost.Lesson;
        private ExperimentVisualization View => bootstrap.ActiveHost.GetComponent<ExperimentVisualization>();
        private static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null); Assert.That(button.enabledInHierarchy, Is.True, button.name);
            using var submit = NavigationSubmitEvent.GetPooled(); submit.target = button; button.SendEvent(submit);
        }
        [UnitySetUp] public IEnumerator Setup()
        { ProgressStoreIsolation.UseSavedCourse = true; store = new MemoryProgressStore(); yield return Restart(); }
        private IEnumerator Restart()
        {
            foreach (var app in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(app.gameObject);
            yield return null;
            void Configure(Scene scene, LoadSceneMode mode)
            {
                if (scene.path == A + "Scenes/Bootstrap.unity") Object.FindFirstObjectByType<AppBootstrap>().Configure(store: store);
            }
            SceneManager.sceneLoaded += Configure;
            try { yield return SceneManager.LoadSceneAsync(A + "Scenes/Bootstrap.unity", LoadSceneMode.Single); }
            finally { SceneManager.sceneLoaded -= Configure; }
            yield return Ready();
        }
        private IEnumerator Ready()
        {
            float limit = Time.realtimeSinceStartup + 20;
            do
            {
                yield return null; bootstrap = Object.FindFirstObjectByType<AppBootstrap>();
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(limit), bootstrap?.Navigation?.StatusMessage);
                if (bootstrap?.Navigation is CourseManager manager) Assert.That(manager.State?.Phase, Is.Not.EqualTo(NavigationPhase.Failed), manager.StatusMessage);
            } while (bootstrap?.Navigation == null || !bootstrap.Navigation.IsReady);
            yield return null;
        }
        private IEnumerator Open()
        {
            var button = Menu.Query<Button>().ToList().Single(b => b.userData as string == "C1.L1"); Click(button); yield return Ready();
            Assert.That(bootstrap.Navigation.ActiveLesson.Id, Is.EqualTo("C1.L1"));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ProgressStoreIsolation.UseSavedCourse = false;
            foreach (var app in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(app.gameObject);
            yield return null;
            foreach (var path in new[] { A + "Scenes/MainMenu.unity", A + "Scenes/LessonWorkspace.unity" })
            { var s = SceneManager.GetSceneByPath(path); if (s.IsValid() && s.isLoaded) yield return SceneManager.UnloadSceneAsync(s); }
        }
        private void AnswerNext(string answer)
        { Click(Root.Q<Button>("answer-" + answer)); Click(Root.Q<Button>("lesson-next")); }
        private void Select(string term) => Click(Root.Q<Button>("term-" + term));
        private void StateStep()
        { AnswerNext("robotics"); AnswerNext("both"); Assert.That(Lesson.Presentation.Step.Id, Is.EqualTo("state")); }
        private void PredictionStep()
        {
            StateStep(); Select("state.current"); Root.Q<DoubleField>("initial-state-1").value = .5; AnswerNext("full");
            Select("output.current"); AnswerNext("angle"); Select("derivative"); AnswerNext("rate");
            Assert.That(Lesson.Presentation.Step.Id, Is.EqualTo("predict"));
        }
        private void Complete()
        {
            PredictionStep(); AnswerNext("velocity"); Select("input.applied"); Root.Q<DoubleField>("input-0").value = 1; AnswerNext("old");
            Select("state.next"); Click(Root.Q<Button>("step-next")); AnswerNext("saved");
            Select("time.physical"); Click(Root.Q<Button>("step-next")); AnswerNext("index");
            Select("state.current"); Click(Root.Q<Button>("step-next")); AnswerNext("time");
            Root.Q<SliderInt>("timeline-seek").value = 100; Select("input.applied"); AnswerNext("terminal");
        }
        [UnityTest] public IEnumerator SavedBootstrapNormalCatalogOpensProductionAndPublishesSameRevision()
        {
            Assert.That(bootstrap.Navigation.Course.Id, Is.EqualTo("optimal-control"));
            Assert.That(bootstrap.Navigation.Course.IsPrototype, Is.False);
            Assert.That(Menu.Q<Label>("catalog-kind").text, Is.EqualTo("CORSO"));
            Assert.That(Menu.Query<Button>().ToList().Count(b => b.userData is string), Is.EqualTo(1));
            Assert.That(bootstrap.Navigation.OpenLesson("prototype.pendulum"), Is.False);
            Assert.That(bootstrap.Navigation.OpenLesson("fixture.f10.static"), Is.False);
            yield return Open();
            Assert.That(Hud.DisplayedState, Is.SameAs(View.DisplayedState)); Assert.That(Hud.DisplayedState.Result.Outputs, Is.Not.Null);
            Assert.That(Hud.Formula.Resolutions["output.current"].Value.Scalar, Is.EqualTo(.35));
            Assert.That(Hud.Formula.Resolutions.Values.All(r => ReferenceEquals(r.Result, Sim.State.Result)), Is.True);
            Assert.That(Root.Q<Button>("lesson-next").enabledInHierarchy, Is.False);
        }
        [UnityTest] public IEnumerator InitialStateEditorShowsWhyPositionIsNotTheWholeStateAndOutputStaysAngle()
        {
            yield return Open(); StateStep(); var a = Sim.State.Result; Select("state.current");
            var field = Root.Q<DoubleField>("initial-state-1"); Assert.That(field.enabledInHierarchy, Is.True); field.value = .5;
            var b = Sim.State.Result; Assert.That(b.Configuration.InitialState[1], Is.EqualTo(.5));
            Assert.That(b.Outputs.Samples[0][0], Is.EqualTo(a.Outputs.Samples[0][0]));
            Assert.That(b.History.States[1][0], Is.EqualTo(.36).Within(1e-12)); Assert.That(a.History.States[1][0], Is.EqualTo(.35));
            Click(Root.Q<Button>("answer-pose")); Assert.That(Lesson.Next(), Is.False); AnswerNext("full");
            Select("output.current"); Assert.That(Hud.Formula.Resolutions["output.current"].Value.Scalar, Is.EqualTo(.35));
            Assert.That(Hud.DisplayedState, Is.SameAs(View.DisplayedState));
        }
        [UnityTest] public IEnumerator PredictionShowsConcreteEquationBeforeAnswerAndCannotSkipOrEditInput()
        {
            yield return Open(); PredictionStep();
            Assert.That(Root.Q<Label>("lesson-formula-expression").text, Is.EqualTo(Hud.Formula.Formula.Expression));
            Assert.That(Root.Q<Label>("lesson-formula-expression").text, Does.Contain("sin"));
            Assert.That(Root.Q<DoubleField>("input-0").enabledInHierarchy, Is.False);
            Assert.That(Sim.State.Result.Configuration.InitialState.ToArray(), Is.EqualTo(new[] { 0d, 0d }));
            Click(Root.Q<Button>("answer-angle")); Assert.That(Lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
            AnswerNext("velocity"); Select("input.applied"); Root.Q<DoubleField>("input-0").value = 1;
            Assert.That(Sim.State.Sample.NextState.ToArray(), Is.EqualTo(new[] { 0d, .02d }).Within(1e-12));
            Assert.That(Hud.Formula.Resolutions["omega.next"].Value.Scalar, Is.EqualTo(.02));
        }
        [UnityTest] public IEnumerator ElevenStepUiCompletionTerminalMenuReopenRestartAndPersistedIdentity()
        {
            yield return Open(); Complete(); Assert.That(Lesson.Presentation.IsCompleted, Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(Sim.State.Sample.Timestep, Is.EqualTo(100));
            Assert.That(Hud.Formula.Resolutions["input.applied"].Validity, Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Hud.Formula.Resolutions["state.next"].Validity, Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Hud.Formula.Resolutions["output.current"].Validity, Is.EqualTo(MetricValidity.Available));
            Assert.That(Hud.Formula.Resolutions["states"].Value.Scalar, Is.EqualTo(101));
            var record = store.Load().Completions.Single(); Assert.That(record.CourseId, Is.EqualTo("optimal-control")); Assert.That(record.LessonId, Is.EqualTo("C1.L1"));
            var oldSession = Sim; var oldLesson = Lesson; Click(Menu.Q<Button>("return-to-menu")); yield return Ready();
            Assert.That(oldSession.State.IsClosed, Is.True); Assert.That(oldLesson.Next(), Is.False);
            Assert.That(Menu.Q<Label>("progress-C1.L1").text, Does.Contain("salvato"));
            yield return Open(); Assert.That(Lesson.Presentation.Step.Id, Is.EqualTo("context")); Assert.That(Lesson.Presentation.IsCompleted, Is.False);
            yield return Restart(); Assert.That(bootstrap.Progress.GetStatus("C1.L1").IsCompleted, Is.True);
            yield return Open(); Complete(); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ProductionFailureKeepsOriginalOutputAndRestartRecoversWithoutCompletion()
        {
            yield return Open(); Click(Root.Q<Button>("lesson-explore")); var a = Sim.State.Result;
            Assert.That(Lesson.Execute(SimulationCommand.SetParameter("model.mass", -1)), Is.False);
            Assert.That(Sim.State.Result, Is.SameAs(a)); Assert.That(Hud.Formula.Resolutions["output.current"].Result, Is.SameAs(a));
            Assert.That(Lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
            Click(Root.Q<Button>("lesson-restart")); Assert.That(Sim.State.Error, Is.Null); Assert.That(Lesson.Presentation.Step.Id, Is.EqualTo("context"));
            Assert.That(Sim.State.Sample.DiscretizationStepSeconds, Is.EqualTo(.02));
        }
        [UnityTest] public IEnumerator IncompleteProductionSessionAndFreeExplorationNeverPersistCompletion()
        {
            yield return Open(); StateStep(); Assert.That(store.SaveCount, Is.Zero);
            Click(Root.Q<Button>("lesson-explore")); Root.Q<DoubleField>("input-0").value = 1;
            Assert.That(Lesson.Next(), Is.False); Click(Menu.Q<Button>("return-to-menu")); yield return Ready();
            Assert.That(bootstrap.Progress.GetStatus("C1.L1").IsCompleted, Is.False); yield return Open();
            Assert.That(Lesson.Presentation.Step.Id, Is.EqualTo("context")); Assert.That(store.SaveCount, Is.Zero);
        }
    }
}
#endif
