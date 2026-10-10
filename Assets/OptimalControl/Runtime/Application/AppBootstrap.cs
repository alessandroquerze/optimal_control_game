using System;
using System.Collections;
using OptCon.Content;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OptCon.Application
{
    /// <summary>Persistent composition root; binding is a direct lifecycle step, never an event subscription.</summary>
    [RequireComponent(typeof(LessonLoader)), DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        private static AppBootstrap instance;
        [SerializeField] private CourseDefinition course;
        [SerializeField, Tooltip("Menu scene path, included and enabled in Build Settings.")]
        private string menuScenePath;
        public ICourseNavigation Navigation { get; private set; }
        public ExperimentHost ActiveHost { get; private set; }
        public CourseRuntimeServices Services { get; private set; }
        public ProgressionService Progress { get; private set; }
        private IProgressStore progressStore;
        private LessonProgressBinding progressBinding;
        public void Configure(CourseRuntimeServices services = null, IProgressStore store = null)
        {
            if (Navigation != null) throw new InvalidOperationException("Configure providers/storage before Start.");
            if (services != null) Services = services;
            if (store != null) progressStore = store;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => instance = null;
        private void Awake()
        {
            if (instance != null && instance != this) { enabled = false; Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private IEnumerator Start()
        {
            if (instance != this) yield break;
            try
            {
                Services = Services ?? new CourseRuntimeServices();
                Progress = new ProgressionService(course, progressStore ?? new FileProgressStore(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "OptCon", "progress-v1.json")));
                Navigation = new CourseManager(course, GetComponent<LessonLoader>(), menuScenePath, Compose, BindScene, Release);
            }
            catch (ArgumentException exception) { Debug.LogError("Catalog initialization failed: " + exception.Message, this); yield break; }
            if (!Navigation.ReturnToMenu()) Debug.LogError(Navigation.StatusMessage, this);
        }

        private SimulationSession Compose(NavigationTransition transition, Scene scene)
        {
            if (transition.Destination == null) return null;
            var experiment = transition.Destination.Experiment;
            if (experiment == null || experiment.Model == null || experiment.ScenarioPrefab == null)
                throw new InvalidOperationException("Lesson requires a model provider and scenario prefab.");
            var session = experiment.Model.CreateSession(experiment);
            GameObject scenario = null;
            LessonSession lessonSession = null;
            try
            {
                scenario = Instantiate(experiment.ScenarioPrefab);
                SceneManager.MoveGameObjectToScene(scenario, scene);
                var host = scenario.GetComponent<ExperimentHost>();
                if (host == null) throw new InvalidOperationException("Scenario root requires an ExperimentHost.");
                lessonSession = new LessonSession(transition.Destination, session, Services.Conditions, availableLayers: scenario.GetComponent<LayerController>()?.Ids ?? Array.Empty<string>());
                host.Initialize(session, lessonSession); ActiveHost = host;
                return session;
            }
            catch
            {
                lessonSession?.Dispose(); session.Dispose();
                if (scenario != null) { scenario.SetActive(false); Destroy(scenario); }
                throw;
            }
        }

        private void BindScene(NavigationTransition transition, Scene scene, SimulationSession session)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var presenter in root.GetComponentsInChildren<CourseMenuPresenter>(true)) presenter.Bind(Navigation, Progress);
                foreach (var diagnostic in root.GetComponentsInChildren<SessionDiagnosticsPresenter>(true)) diagnostic.Bind(session);
                foreach (var hud in root.GetComponentsInChildren<LessonHudPresenter>(true))
                {
                    hud.Bind(transition.Destination, session, ActiveHost == null ? null : ActiveHost.GetComponent<HighlightRegistry>(), ActiveHost?.Lesson,
                        ActiveHost == null ? null : ActiveHost.GetComponent<ExperimentVisualization>(), Services.Metrics);
                    if (ActiveHost != null)
                        foreach (var input in ActiveHost.GetComponentsInChildren<LessonInputRouter>(true)) input.SetFocusGuard(hud.BlocksLessonShortcut);
                }
            }
            if (session != null && ActiveHost != null)
            {
                var host = ActiveHost;
                var view = host.GetComponent<ExperimentVisualization>();
                var huds = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(scene.GetRootGameObjects(), root => root.GetComponentsInChildren<LessonHudPresenter>(true)));
                progressBinding = new LessonProgressBinding(host.Lesson, session, Progress, () => Navigation.IsReady
                    && ReferenceEquals(ActiveHost, host) && ReferenceEquals(Navigation.ActiveLesson, transition.Destination)
                    && ((CourseManager)Navigation).State.Transition.Id == transition.Id
                    && view != null && view.Status != null && view.Status.Availability == VisualizationAvailability.Available
                    && ReferenceEquals(view.BoundSession, session) && huds.Length != 0
                    && ReferenceEquals(view.Status.Source.Result, session.State.Result)
                    && System.Linq.Enumerable.All(huds, hud => hud != null && ReferenceEquals(hud.BoundSession, session) && ReferenceEquals(hud.BoundLesson, host.Lesson)
                        && ReferenceEquals(hud.DisplayedState?.Result, session.State.Result)));
            }
        }
        private void Release()
        {
            progressBinding?.Dispose(); progressBinding = null;
            if (ActiveHost == null) return;
            var scene = ActiveHost.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var diagnostic in root.GetComponentsInChildren<SessionDiagnosticsPresenter>(true)) diagnostic.Bind(null);
                    foreach (var hud in root.GetComponentsInChildren<LessonHudPresenter>(true)) hud.Bind(null, null, null);
                }
            var scenario = ActiveHost.gameObject;
            ActiveHost.Close(); ActiveHost = null;
            scenario.SetActive(false); Destroy(scenario);
        }
        private void OnDestroy()
        {
            (Navigation as IDisposable)?.Dispose();
            Release();
            if (instance == this) instance = null;
        }
    }
}
