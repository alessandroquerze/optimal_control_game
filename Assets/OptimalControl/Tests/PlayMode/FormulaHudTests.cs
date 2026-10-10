#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
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
    public sealed class FormulaHudTests
    {
        private AppBootstrap bootstrap;
        private LessonHudPresenter Hud => Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root => Hud.GetComponent<UIDocument>().rootVisualElement;
        private SimulationSession Session => bootstrap.ActiveHost.Session;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null; yield return SceneManager.LoadSceneAsync("Assets/OptimalControl/Scenes/Bootstrap.unity", LoadSceneMode.Single);
            yield return Ready();
        }
        private IEnumerator Ready()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            do { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; bootstrap = Object.FindFirstObjectByType<AppBootstrap>(); }
            while (bootstrap == null || bootstrap.Navigation == null || !bootstrap.Navigation.IsReady);
        }
        private IEnumerator Open() { Assert.That(bootstrap.Navigation.OpenLesson(bootstrap.Navigation.Course.Chapters[0].Lessons[0].Id), Is.True); yield return Ready(); bootstrap.ActiveHost.Lesson.Explore(); }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            foreach (var path in new[] { "Assets/OptimalControl/Scenes/MainMenu.unity", "Assets/OptimalControl/Scenes/LessonWorkspace.unity" })
            { var scene = SceneManager.GetSceneByPath(path); if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
        }
        private void Submit(string name) { var button = Root.Q<Button>(name); using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); } }

        [UnityTest]
        public IEnumerator InputSystemPointerSelectsAnActualFormulaTerm()
        {
            yield return Open();
            var view = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")); var previous = view.position;
            Mouse mouse = null;
            try
            {
                view.position = new Rect(0, 0, 1440, 1000); view.Focus(); yield return null; yield return null;
                var button = Root.Q<Button>("term-state.next"); Root.Q<ScrollView>("lesson-panel").ScrollTo(button); yield return null; yield return null;
                var center = button.worldBound.center;
                var point = new Vector2(center.x * Screen.width / Root.resolvedStyle.width, Screen.height - center.y * Screen.height / Root.resolvedStyle.height);
                mouse = InputSystem.AddDevice<Mouse>();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left)); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                Assert.That(Root.Q<Label>("term-detail-title").text, Does.StartWith("x_(t+1)"), $"button={button.worldBound}, viewport={Root.Q<ScrollView>("lesson-panel").contentViewport.worldBound}, point={point}, screen={Screen.width}x{Screen.height}, root={Root.worldBound}");
                Assert.That(bootstrap.ActiveHost.GetComponentsInChildren<HighlightTarget>(true).Where(t => t.SemanticRole == "state.next").All(t => t.IsHighlighted && t.SampleIndex == 1), Is.True);
            }
            finally { if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse); view.position = previous; }
        }

        [UnityTest]
        public IEnumerator HudIsBoundAndRenderedBeforeReadyWithTheVisualResult()
        {
            bool observed = false;
            ((CourseManager)bootstrap.Navigation).LifecycleChanged += update =>
            {
                if (update.Phase != NavigationPhase.Ready || update.Transition.Destination == null) return;
                observed = true; Assert.That(Hud.BoundSession, Is.SameAs(update.Session));
                Assert.That(Hud.DisplayedState, Is.SameAs(update.Session.State));
                Assert.That(Hud.Formula.DisplayedResult, Is.SameAs(bootstrap.ActiveHost.GetComponent<ExperimentVisualization>().DisplayedState.Result));
                Assert.That(Root.Q<Label>("formula-expression").text, Is.EqualTo(update.Transition.Destination.Formulas[0].Expression));
            };
            yield return Open(); Assert.That(observed, Is.True);
        }
        [UnityTest]
        public IEnumerator SelectingTermsUsesSemanticRolesAndTheSameAbsoluteSample()
        {
            yield return Open(); Session.Execute(SimulationCommand.Seek(2));
            foreach (var pair in new[] { ("state.current", 2), ("input.applied", 2), ("state.next", 3), ("dynamics.model", 2) })
            {
                Submit("term-" + pair.Item1);
                var term = Hud.Formula.Formula.Terms.Single(t => t.Id == pair.Item1);
                var r = Hud.Formula.Resolutions[pair.Item1];
                Assert.That(r.SampleIndex, Is.EqualTo(pair.Item2));
                var targets = bootstrap.ActiveHost.GetComponentsInChildren<HighlightTarget>(true).Where(t => t.SemanticRole == term.Highlight.SemanticRole).ToArray();
                Assert.That(targets, Is.Not.Empty);
                Assert.That(targets.All(t => t.IsHighlighted && t.SampleIndex == pair.Item2 && t.RevisionId == r.Result.RevisionId), Is.True);
                Assert.That(Root.Q<Label>("term-detail-meaning").text, Is.EqualTo(term.Meaning));
                Assert.That(Root.Q<Label>("term-detail-world").text, Does.Contain(term.Highlight.SemanticRole));
            }
        }
        [UnityTest]
        public IEnumerator SeekAndUiCommandsKeepAllLiveValuesAndVisualsCoherent()
        {
            yield return Open(); var result = Session.State.Result;
            Root.Q<SliderInt>("timeline-seek").value = 20;
            Assert.That(Session.State.Sample.Timestep, Is.EqualTo(20));
            Assert.That(Hud.Formula.Resolutions["time.index"].Value.Scalar, Is.EqualTo(20));
            Assert.That(Hud.Formula.Resolutions["time.physical"].Value.Scalar, Is.EqualTo(.4));
            Assert.That(Hud.Formula.Resolutions["time.step"].Value.Scalar, Is.EqualTo(.02));
            Assert.That(Hud.Formula.Resolutions["state.current"].Value.Vector, Is.SameAs(result.History.States[20]));
            Assert.That(Hud.Formula.Resolutions["state.next"].Value.Vector, Is.SameAs(result.History.States[21]));
            foreach (var plot in bootstrap.ActiveHost.GetComponentsInChildren<StateTrajectoryView>()) Assert.That(plot.Trajectory.Source, Is.SameAs(Hud.Formula.DisplayedResult.History));
            Submit("step-next"); Assert.That(Session.State.Sample.Timestep, Is.EqualTo(21));
            Submit("step-back"); Assert.That(Session.State.Sample.Timestep, Is.EqualTo(20));
            Root.Q<DoubleField>("playback-rate").value = 20; Assert.That(Session.State.SamplesPerSecond, Is.EqualTo(20));
            Root.Q<DoubleField>("playback-rate").value = 0;
            Assert.That(Root.Q<DoubleField>("playback-rate").value, Is.EqualTo(20)); Assert.That(Root.Q<Label>("timeline-status").text, Does.Contain("Playback rifiutato"));
            Assert.That(Session.State.Result, Is.SameAs(result)); Assert.That(Session.State.Sample.DiscretizationStepSeconds, Is.EqualTo(.02));
            Submit("play-pause"); Assert.That(Session.State.IsPlaying, Is.True); Submit("play-pause"); Assert.That(Session.State.IsPlaying, Is.False);
            Submit("reset"); Assert.That(Session.State.Sample.Timestep, Is.Zero);
        }
        [UnityTest]
        public IEnumerator ParameterInputAndSpecializationReadOnlyTheirRevisionAndPreserveAssets()
        {
            yield return Open(); var lesson = bootstrap.Navigation.ActiveLesson;
            string before = EditorJsonUtility.ToJson(lesson.Experiment);
            Root.Q<DoubleField>("input-0").value = 3;
            Assert.That(Session.State.Sample.AppliedControl[0], Is.EqualTo(3));
            Assert.That(Session.State.Result.Configuration.Controls.All(u => u[0] == 3), Is.True);
            var a = Session.State.Result;
            var copy = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/OptimalControl/UI/LessonHud.uxml").CloneTree();
            using (var retained = new FormulaPresenter(copy, lesson.Formulas, null))
            {
                retained.Render(a, 0); Root.Q<DoubleField>("parameter-model.mass").value = 2; var b = Session.State.Result;
                Assert.That(b, Is.Not.SameAs(a)); Assert.That(Hud.Formula.DisplayedResult, Is.SameAs(b));
                retained.Render(a, 0);
                Assert.That(retained.Resolutions["dynamics.model"].Value.Model.Parameters.Single(p => p.Target == "model.mass").Value, Is.EqualTo(1));
                Assert.That(Hud.Formula.Resolutions["dynamics.model"].Value.Model.Parameters.Single(p => p.Target == "model.mass").Value, Is.EqualTo(2));
                Root.Q<DoubleField>("parameter-model.mass").value = 0;
                Assert.That(Session.State.Result, Is.SameAs(b)); Assert.That(Hud.Formula.DisplayedResult, Is.SameAs(b));
                Assert.That(Root.Q<Label>("timeline-status").text, Does.Contain("rifiutato"));
                var choice = Root.Q<DropdownField>("formula-choice"); choice.value = choice.choices[1];
                Assert.That(Hud.Formula.Resolutions["state.current"].Value.Scalar, Is.EqualTo(b.History.States[0][0]));
                Assert.That(Hud.Formula.Resolutions["omega.current"].Value.Units.Single(), Is.EqualTo("rad/s"));
                Assert.That(Hud.Formula.Resolutions["parameter.model.mass"].Value.Scalar, Is.EqualTo(2));
                Assert.That(retained.Resolutions["state.next"].Value.Vector, Is.SameAs(a.History.States[1]));
            }
            Assert.That(EditorJsonUtility.ToJson(lesson.Experiment), Is.EqualTo(before));
        }
        [UnityTest]
        public IEnumerator TerminalHasNoInputNextTransitionOrInventedValues()
        {
            yield return Open(); Root.Q<SliderInt>("timeline-seek").value = 100;
            foreach (var id in new[] { "input.applied", "state.next" })
            {
                var r = Hud.Formula.Resolutions[id]; Assert.That(r.Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(r.SampleIndex, Is.Null);
                Submit("term-" + id); Assert.That(Root.Q<Label>("term-detail-value").text, Does.StartWith("N/A"));
                Assert.That(bootstrap.ActiveHost.GetComponentsInChildren<HighlightTarget>(true).Any(t => t.IsHighlighted), Is.False);
            }
            Assert.That(Root.Q<VisualElement>("input-fields").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(Root.Q<Label>("input-unavailable").text, Does.Contain("N/A"));
            Assert.That(Root.Q<Button>("play-pause").enabledSelf, Is.False);
            Assert.That(Hud.Formula.Resolutions["time.physical"].Value.Scalar, Is.EqualTo(2));
            Assert.That(bootstrap.ActiveHost.GetComponentsInChildren<TransitionHighlightView>().All(v => v.GetComponent<HighlightTarget>().SampleIndex == null && v.GetComponent<LineRenderer>().positionCount == 0), Is.True);
        }
        [UnityTest]
        public IEnumerator IndexOnlyHudUsesTheSameGenericFormulaWithoutPhysicalTime()
        {
            yield return Open(); Session.Execute(SimulationCommand.Seek(100)); var lesson = Object.Instantiate(bootstrap.Navigation.ActiveLesson);
            var so = new SerializedObject(lesson); var formulas = so.FindProperty("formulas"); formulas.arraySize = 1;
            formulas.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<FormulaDefinition>("Assets/OptimalControl/Data/Formulas/DiscreteDynamics.asset"); so.ApplyModifiedPropertiesWithoutUndo();
            using (var fixture = IndexOnlyConfiguration.CreateSession())
            {
                try
                {
                    var initial = fixture.State;
                    Hud.Bind(lesson, fixture, null);
                    Assert.That(fixture.State, Is.SameAs(initial), "Changing the timeline range while binding must not issue seek commands.");
                    Assert.That(Hud.DisplayedState, Is.SameAs(fixture.State)); Assert.That(fixture.State.Sample.Timestep, Is.Zero);
                    Assert.That(Hud.Formula.Resolutions["state.current"].Value.Vector[0], Is.EqualTo(2));
                    Assert.That(Hud.Formula.Resolutions["input.applied"].Value.Vector[0], Is.EqualTo(3));
                    Assert.That(Hud.Formula.Resolutions["state.next"].Value.Vector[0], Is.EqualTo(5));
                    foreach (var id in new[] { "time.step", "time.physical" }) Assert.That(Hud.Formula.Resolutions[id].Validity, Is.EqualTo(MetricValidity.NotApplicable));
                    Root.Q<SliderInt>("timeline-seek").value = 1;
                    Assert.That(Hud.Formula.Resolutions["state.current"].Value.Vector[0], Is.EqualTo(5));
                    Assert.That(Hud.Formula.Resolutions["time.index"].Value.Scalar, Is.EqualTo(1));
                    Assert.That(Hud.Formula.Resolutions["state.next"].Validity, Is.EqualTo(MetricValidity.NotApplicable));
                }
                finally { Hud.Bind(null, null, null); Object.Destroy(lesson); }
            }
        }
        [UnityTest]
        public IEnumerator DisableRebindAndCloseReleaseSubscriptionsAndSelection()
        {
            yield return Open(); var hud = Hud; var session = Session; var formula = hud.Formula;
            hud.enabled = false; Assert.That(formula.DisplayedResult, Is.Null);
            session.Execute(SimulationCommand.Seek(4)); Assert.That(hud.DisplayedState, Is.Null);
            hud.enabled = true; Assert.That(hud.DisplayedState, Is.SameAs(session.State));
            hud.Bind(bootstrap.Navigation.ActiveLesson, session, bootstrap.ActiveHost.GetComponent<HighlightRegistry>());
            var oldFormula = hud.Formula;
            bootstrap.Navigation.ReturnToMenu(); yield return Ready(); yield return null;
            Assert.That(oldFormula.DisplayedResult, Is.Null); Assert.That(session.State.IsClosed, Is.True);
            yield return Open(); Assert.That(Hud.BoundSession, Is.Not.SameAs(session)); Assert.That(Hud.DisplayedState, Is.SameAs(Session.State));
        }
        [UnityTest]
        public IEnumerator EditingFocusPreventsLessonKeyboardShortcuts()
        {
            yield return Open(); Root.Q<Foldout>("parameter-panel").value = true; yield return null;
            Root.Q<DoubleField>("input-0").Focus(); yield return null; Assert.That(Hud.HasEditingFocus, Is.True);
            var result = Session.State.Result; var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                Assert.That(Session.State.Result, Is.SameAs(result));
                Root.Q<Button>("step-next").Focus(); Assert.That(Hud.HasEditingFocus, Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
    }
}
#endif
