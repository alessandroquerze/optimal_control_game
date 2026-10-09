#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class VisualizationTests
    {
        private AppBootstrap bootstrap;
        private Keyboard keyboard;
        private ExperimentHost Host => bootstrap.ActiveHost;
        private SimulationSession Session => Host.Session;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Assets/OptimalControl/Scenes/Bootstrap.unity", LoadSceneMode.Single);
            yield return UntilReady();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            foreach (var root in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null;
            foreach (var path in new[] { "Assets/OptimalControl/Scenes/MainMenu.unity", "Assets/OptimalControl/Scenes/LessonWorkspace.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        private IEnumerator UntilReady()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            do
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                yield return null; bootstrap = Object.FindFirstObjectByType<AppBootstrap>();
            } while (bootstrap == null || bootstrap.Navigation == null || !bootstrap.Navigation.IsReady);
        }
        private IEnumerator Open()
        {
            Assert.That(bootstrap.Navigation.OpenLesson(bootstrap.Navigation.Course.Chapters[0].Lessons[0].Id), Is.True);
            yield return UntilReady();
        }

        [UnityTest]
        public IEnumerator VisualConsumersAndDedicatedInputAreBoundBeforeReady()
        {
            var manager = (CourseManager)bootstrap.Navigation;
            bool observed = false;
            manager.LifecycleChanged += update =>
            {
                if (update.Phase != NavigationPhase.Ready || update.Transition.Destination == null) return;
                observed = true;
                var view = Host.GetComponent<ExperimentVisualization>();
                Assert.That(view.BoundSession, Is.SameAs(update.Session));
                Assert.That(view.DisplayedState, Is.SameAs(update.Session.State));
                Assert.That(Host.GetComponent<LessonInputRouter>().BoundSession, Is.SameAs(update.Session));
                Assert.That(Host.GetComponent<LessonInputRouter>().RuntimeActions.enabled, Is.True);
            };
            yield return Open(); Assert.That(observed, Is.True);
        }

        [UnityTest]
        public IEnumerator EveryTrajectoryPointAndTwoMarkersUseTheSameSavedResultAfterCommands()
        {
            yield return Open();
            var a = Session.State.Result;
            Assert.That(Session.Execute(SimulationCommand.SetInput(0, new NumericVector(3), InputEditScope.Tail)), Is.True);
            Assert.That(Session.Execute(SimulationCommand.Seek(1)), Is.True);
            var b = Session.State.Result;
            foreach (var plot in Host.GetComponentsInChildren<StateTrajectoryView>(true))
            {
                Assert.That(plot.Trajectory.Source, Is.SameAs(b.History));
                var line = plot.Trajectory.GetComponent<LineRenderer>();
                Assert.That(line.positionCount, Is.EqualTo(b.History.Horizon + 1));
                for (int t = 0; t <= b.History.Horizon; t++)
                    Assert.That(line.GetPosition(t), Is.EqualTo(plot.Projection.Project(b.History, t)));
                Assert.That(plot.Current.transform.localPosition, Is.EqualTo(plot.Projection.Project(b.History, 1)));
                Assert.That(plot.Next.transform.localPosition, Is.EqualTo(plot.Projection.Project(b.History, 2)));
                Assert.That(plot.Current.SampleIndex, Is.EqualTo(1)); Assert.That(plot.Next.SampleIndex, Is.EqualTo(2));
                Assert.That(plot.Current.RevisionId, Is.EqualTo(b.RevisionId)); Assert.That(plot.Next.RevisionId, Is.EqualTo(b.RevisionId));
                Assert.That(plot.Current.transform.localPosition, Is.Not.EqualTo(plot.Next.transform.localPosition));
            }
            Assert.That(a.History.Controls[0][0], Is.Zero);
            Assert.That(b.History.Controls[0][0], Is.EqualTo(3));
            Session.Execute(SimulationCommand.Seek(0));
            Assert.That(Session.State.Result, Is.SameAs(b));
            var time = Host.GetComponentsInChildren<StateTrajectoryView>().Single(p => p.Projection.Z.coordinate == ProjectionCoordinate.PhysicalTime);
            Assert.That(time.Projection.Project(b.History, 1).z, Is.EqualTo(0.028f));
            Assert.That(time.GetComponentsInChildren<TextMesh>().Any(l => l.text.Contains("tempo fisico, NON posizione") && l.text.Contains("[s]")), Is.True);
        }

        [UnityTest]
        public IEnumerator HiddenLayersNeverChangeSimulationAndHighlightUsesResolvedIndex()
        {
            yield return Open();
            var layers = Host.GetComponent<LayerController>();
            var result = Session.State.Result;
            for (int i = 0; i < 3; i++) { layers.Toggle(i); Assert.That(layers.IsVisible(i), Is.False); }
            Assert.That(Session.State.Result, Is.SameAs(result)); Assert.That(Session.State.Sample.Timestep, Is.Zero);
            Session.Execute(SimulationCommand.Seek(2));
            Host.GetComponent<HighlightRegistry>().Select("state.next", 3);
            var targets = Host.GetComponentsInChildren<HighlightTarget>(true).Where(t => t.SemanticRole == "state.next").ToArray();
            Assert.That(targets.Length, Is.EqualTo(3));
            Assert.That(targets.All(t => t.SampleIndex == 3 && t.IsHighlighted && !t.gameObject.activeInHierarchy), Is.True);
            for (int i = 0; i < 3; i++) layers.Toggle(i);
            Assert.That(targets.All(t => t.gameObject.activeInHierarchy), Is.True);
            Assert.That(Session.State.Result, Is.SameAs(result));
        }

        [UnityTest]
        public IEnumerator PhysicalPoseUsesResultConfigurationAndTerminalHasNoGhostOrTorque()
        {
            yield return Open();
            var body = Host.GetComponentInChildren<AngularStateView>();
            var a = Session.State.Result;
            Assert.That(Session.Execute(SimulationCommand.SetParameter("model.length", 2)), Is.True);
            var b = Session.State.Result;
            Assert.That(body.Current.transform.Find("Bob").localPosition.y, Is.EqualTo(-3));
            Assert.That(((PendulumConfiguration)a.Configuration.Model).Physics.Length, Is.EqualTo(1));
            Assert.That(((PendulumConfiguration)b.Configuration.Model).Physics.Length, Is.EqualTo(2));
            Assert.That(Session.Execute(SimulationCommand.SetParameter("model.length", 0)), Is.False);
            Assert.That(Host.GetComponent<ExperimentVisualization>().DisplayedState.Result, Is.SameAs(b));
            Assert.That(body.Current.transform.Find("Bob").localPosition.y, Is.EqualTo(-3));
            Session.Execute(SimulationCommand.Seek(b.History.Horizon));
            Assert.That(body.Next.gameObject.activeSelf, Is.False);
            foreach (var plot in Host.GetComponentsInChildren<StateTrajectoryView>()) Assert.That(plot.Next.SampleIndex, Is.Null);
            var torque = Host.GetComponentInChildren<CurvedTorqueRenderer>();
            Assert.That(torque.SampleIndex, Is.Null); Assert.That(torque.GetComponent<LineRenderer>().positionCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RealKeyboardChangesInputPlaybackStepsAndResetWithoutMutatingAssets()
        {
            yield return Open(); keyboard = InputSystem.AddDevice<Keyboard>();
            var asset = bootstrap.Navigation.ActiveLesson.Experiment;
            string before = UnityEditor.EditorJsonUtility.ToJson(asset);
            yield return Press(Key.D);
            Assert.That(Session.State.Result.History.Controls.All(u => u[0] == 0.5), Is.True);
            var torque = Host.GetComponentInChildren<CurvedTorqueRenderer>().GetComponent<LineRenderer>();
            Assert.That(torque.positionCount, Is.GreaterThan(0));
            Vector3 positiveEnd = torque.GetPosition(24);
            yield return Press(Key.A); yield return Press(Key.A);
            Assert.That(Session.State.Sample.AppliedControl[0], Is.EqualTo(-0.5));
            Assert.That(torque.GetPosition(24), Is.Not.EqualTo(positiveEnd));
            yield return Press(Key.RightArrow); Assert.That(Session.State.Sample.Timestep, Is.EqualTo(1));
            yield return Press(Key.LeftArrow); Assert.That(Session.State.Sample.Timestep, Is.Zero);
            yield return Press(Key.P); Assert.That(Session.State.IsPlaying, Is.True);
            yield return Press(Key.P); Assert.That(Session.State.IsPlaying, Is.False);
            yield return Press(Key.Z); Assert.That(Session.State.Sample.AppliedControl[0], Is.Zero);
            yield return Press(Key.R); Assert.That(Session.State.Result.History.Controls.All(u => u[0] == 0), Is.True);
            yield return Press(Key.Digit2); Assert.That(Host.GetComponent<LayerController>().IsVisible(1), Is.False);
            yield return Press(Key.Digit1); Assert.That(Host.GetComponent<LayerController>().IsVisible(0), Is.False);
            yield return Press(Key.Digit3); Assert.That(Host.GetComponent<LayerController>().IsVisible(2), Is.False);
            Assert.That(UnityEditor.EditorJsonUtility.ToJson(asset), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator ClosingAndReopeningReleasesVisualSubscriptionsAndClonedActions()
        {
            yield return Open();
            var view = Host.GetComponent<ExperimentVisualization>();
            var session = Session;
            view.Bind(session); view.Bind(session); view.enabled = false;
            var before = view.DisplayedState;
            session.Execute(SimulationCommand.Seek(2)); Assert.That(view.DisplayedState, Is.SameAs(before));
            view.enabled = true; Assert.That(view.DisplayedState, Is.SameAs(session.State));
            var actions = Host.GetComponent<LessonInputRouter>().RuntimeActions;
            var oldState = session.State;
            Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True);
            yield return UntilReady(); yield return null;
            Assert.That(session.State.IsClosed, Is.True); Assert.That(actions == null, Is.True);
            Assert.That(Object.FindObjectsByType<ExperimentVisualization>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<LessonInputRouter>(FindObjectsSortMode.None), Is.Empty);
            yield return Open();
            Assert.That(Session, Is.Not.SameAs(session));
            Assert.That(Host.GetComponent<ExperimentVisualization>().DisplayedState, Is.SameAs(Session.State));
            Assert.That(oldState.Result.History.Controls[0][0], Is.Zero);
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
        }
    }
}
#endif
