using System;
using System.Collections;
using OptCon.Content;
using OptCon.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OptCon.Application
{
    /// <summary>Persistent composition root for catalog navigation and additive lesson scenes.</summary>
    [RequireComponent(typeof(LessonLoader)), DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        private static AppBootstrap instance;
        [SerializeField] private CourseDefinition course;
        [SerializeField, Tooltip("Menu scene path, included and enabled in Build Settings.")]
        private string menuScenePath;
        private LessonLoader loader;
        public ICourseNavigation Navigation { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (instance != this) yield break;
            loader = GetComponent<LessonLoader>();
            try { Navigation = new CourseManager(course, loader, menuScenePath); }
            catch (ArgumentException exception)
            {
                Debug.LogError("Catalog initialization failed: " + exception.Message, this);
                yield break;
            }
            loader.SceneReady += BindScene;
            if (!Navigation.ReturnToMenu()) Debug.LogError(Navigation.StatusMessage, this);
        }

        private void BindScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var presenter in root.GetComponentsInChildren<CourseMenuPresenter>(true))
                    presenter.Bind(Navigation);
        }

        private void OnDestroy()
        {
            if (loader != null) loader.SceneReady -= BindScene;
            (Navigation as IDisposable)?.Dispose();
            if (instance == this) instance = null;
        }
    }
}
