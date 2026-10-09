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
    public sealed class CourseNavigationTests
    {
        private const string A = "Assets/OptimalControl/";
        private const string BootstrapPath = A + "Scenes/Bootstrap.unity";
        private const string MenuPath = A + "Scenes/MainMenu.unity";
        private const string WorkspacePath = A + "Scenes/LessonWorkspace.unity";
        private readonly List<ScriptableObject> owned = new List<ScriptableObject>();
        private AppBootstrap bootstrap;
        private Mouse testMouse;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Restart();
            Assert.That(bootstrap.Navigation.ActiveLesson, Is.Null);
            AssertContentScene(MenuPath);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (testMouse != null && testMouse.added) InputSystem.RemoveDevice(testMouse);
            testMouse = null;
            foreach (var root in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(root.gameObject);
            yield return null;
            foreach (string path in new[] { MenuPath, WorkspacePath })
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
            foreach (var asset in owned) Object.Destroy(asset);
            owned.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MenuWorkspaceMenuTwiceUsesAssetTitlesObjectivesAndOneScene()
        {
            var navigation = bootstrap.Navigation;
            var lesson = navigation.Course.Chapters[0].Lessons[0];
            string catalogBefore = EditorJsonUtility.ToJson(lesson);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                var button = Root().Query<Button>(className: "lesson-button").ToList().Single();
                Assert.That(button.text, Is.EqualTo(lesson.Title));
                Assert.That(button.userData, Is.EqualTo(lesson.Id));
                Submit(button);
                yield return WaitForNavigation();
                Assert.That(navigation.ActiveLesson, Is.SameAs(lesson));
                AssertContentScene(lesson.Workspace.AssetPath);
                Assert.That(Root().Q<Label>("lesson-title").text, Is.EqualTo(lesson.Title));
                Assert.That(Root().Q<Label>("lesson-objective").text, Is.EqualTo(lesson.Objective));
                Submit(Root().Q<Button>("return-to-menu"));
                yield return WaitForNavigation();
                Assert.That(navigation.ActiveLesson, Is.Null);
                AssertContentScene(MenuPath);
            }
            Assert.That(EditorJsonUtility.ToJson(lesson), Is.EqualTo(catalogBefore), "Navigation must not author completion or mutate lesson data.");
        }

        [UnityTest]
        public IEnumerator MissingSceneAndUnknownIdKeepCurrentMenuUsable()
        {
            var course = ExtendedCourse(out var unavailable);
            Edit(unavailable, "workspace.assetPath", p => p.stringValue = "Assets/Scenes/Unavailable.unity");
            yield return Restart(course);
            var navigation = bootstrap.Navigation;
            Assert.That(navigation.OpenLesson("unknown.lesson"), Is.False);
            Assert.That(navigation.StatusMessage, Does.Contain("non è presente"));
            Assert.That(navigation.OpenLesson(unavailable.Id), Is.False);
            Assert.That(navigation.StatusMessage, Does.Contain("Build Settings"));
            Assert.That(Root().Q<Label>("navigation-status").text, Is.EqualTo(navigation.StatusMessage));
            Assert.That(navigation.ActiveLesson, Is.Null);
            Assert.That(navigation.IsTransitioning, Is.False);
            AssertContentScene(MenuPath);
            Assert.That(navigation.OpenLesson(course.Chapters[0].Lessons[1].Id), Is.True);
            yield return WaitForNavigation();
            AssertContentScene(WorkspacePath);
        }

        [UnityTest]
        public IEnumerator AdditionalCatalogEntryPreservesOrderAndOpensWithoutCodeChanges()
        {
            var course = ExtendedCourse(out var second);
            yield return Restart(course);
            var buttons = Root().Query<Button>(className: "lesson-button").ToList();
            Assert.That(buttons.Select(b => (string)b.userData), Is.EqualTo(course.Chapters[0].Lessons.Select(l => l.Id)));
            Assert.That(buttons[0].text, Is.EqualTo(second.Title));
            Submit(buttons[0]);
            yield return WaitForNavigation();
            Assert.That(bootstrap.Navigation.ActiveLesson, Is.SameAs(second));
            Assert.That(Root().Q<Label>("lesson-objective").text, Is.EqualTo(second.Objective));
            AssertContentScene(second.Workspace.AssetPath);
        }

        [UnityTest]
        public IEnumerator SharedLessonAcrossChaptersUsesOneStableIdentity()
        {
            var original = bootstrap.Navigation.Course;
            var course = Object.Instantiate(original);
            var chapter = Object.Instantiate(original.Chapters[0]);
            owned.AddRange(new ScriptableObject[] { course, chapter });
            Edit(chapter, "id", p => p.stringValue = "future.chapter.shared");
            Edit(course, "chapters", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = original.Chapters[0];
                p.GetArrayElementAtIndex(1).objectReferenceValue = chapter;
            });
            yield return Restart(course);
            var buttons = Root().Query<Button>(className: "lesson-button").ToList();
            Assert.That(buttons.Count, Is.EqualTo(2));
            Submit(buttons[1]);
            yield return WaitForNavigation();
            Assert.That(bootstrap.Navigation.ActiveLesson, Is.SameAs(original.Chapters[0].Lessons[0]));
            AssertContentScene(WorkspacePath);
        }

        [UnityTest]
        public IEnumerator ConcurrentRequestsCannotCreateTwoWorkspaces()
        {
            var navigation = bootstrap.Navigation;
            string id = navigation.Course.Chapters[0].Lessons[0].Id;
            Assert.That(navigation.OpenLesson(id), Is.True);
            Assert.That(navigation.IsTransitioning, Is.True);
            Assert.That(navigation.OpenLesson(id), Is.False);
            Assert.That(navigation.ReturnToMenu(), Is.False);
            yield return WaitForNavigation();
            AssertContentScene(WorkspacePath);
            Assert.That(navigation.ActiveLesson.Id, Is.EqualTo(id));
        }

        [UnityTest]
        public IEnumerator DuplicateBootstrapAndAdditiveReloadKeepOneApplicationRoot()
        {
            Object.Instantiate(bootstrap.gameObject);
            yield return null;
            yield return null;
            Assert.That(Object.FindObjectsByType<AppBootstrap>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var originalScene = SceneManager.GetSceneByPath(BootstrapPath);
            yield return SceneManager.LoadSceneAsync(BootstrapPath, LoadSceneMode.Additive);
            yield return null;
            Assert.That(Object.FindObjectsByType<AppBootstrap>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            AssertContentScene(MenuPath);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == BootstrapPath && scene.handle != originalScene.handle)
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator PresenterRebindingAndDisableEnableReleaseSubscriptions()
        {
            var presenter = Object.FindFirstObjectByType<CourseMenuPresenter>();
            var probe = new NavigationProbe(bootstrap.Navigation.Course);
            presenter.Bind(probe);
            presenter.Bind(probe);
            Assert.That(probe.ListenerCount, Is.EqualTo(1));
            for (int cycle = 0; cycle < 2; cycle++)
            {
                presenter.enabled = false;
                Assert.That(probe.ListenerCount, Is.Zero);
                presenter.enabled = true;
                Assert.That(probe.ListenerCount, Is.EqualTo(1));
            }
            Submit(presenter.GetComponent<UIDocument>().rootVisualElement.Q<Button>("return-to-menu"));
            Assert.That(probe.ReturnRequests, Is.EqualTo(1));
            presenter.Bind(bootstrap.Navigation);
            Assert.That(probe.ListenerCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InputSystemPointerOpensTheActualMenuButton()
        {
            var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            Rect previousPosition = gameView.position;
            try
            {
                gameView.position = new Rect(0, 0, 1280, 800);
                gameView.Focus();
                yield return null;
                yield return null;
                var root = Root();
                var button = root.Query<Button>(className: "lesson-button").ToList().Single();
                root.Q<ScrollView>("menu-panel").ScrollTo(button);
                yield return null;
                yield return null;
                Assert.That(root.resolvedStyle.width, Is.GreaterThan(0));
                var center = button.worldBound.center;
                var point = new Vector2(center.x * Screen.width / root.resolvedStyle.width,
                    Screen.height - center.y * Screen.height / root.resolvedStyle.height);
                testMouse = InputSystem.AddDevice<Mouse>();
                // Input actions and UI Toolkit process on separate player-loop stages.
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = point });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = point }
                    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = point });
                yield return null;
                yield return null;
                yield return WaitForNavigation();
                Assert.That(bootstrap.Navigation.ActiveLesson, Is.Not.Null);
                AssertContentScene(WorkspacePath);
            }
            finally { gameView.position = previousPosition; }
        }
        [Test]
        public void MathematicalCoreAndPresentationRespectAssemblyBoundaries()
        {
            Assert.That(typeof(ModelDimensions).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity", StringComparison.Ordinal)), Is.False);
            Assert.That(typeof(CourseMenuPresenter).Assembly.GetReferencedAssemblies().Any(a => a.Name == "OptCon.Application"), Is.False);
            Assert.That(typeof(ICourseNavigation).Assembly.GetReferencedAssemblies().Any(a => a.Name == "OptCon.Application" || a.Name == "OptCon.Presentation"), Is.False);
        }

        private IEnumerator Restart(CourseDefinition overrideCourse = null)
        {
            foreach (var root in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null;
            void Configure(Scene scene, LoadSceneMode mode)
            {
                if (scene.path != BootstrapPath || overrideCourse == null) return;
                var root = Object.FindFirstObjectByType<AppBootstrap>();
                Edit(root, "course", p => p.objectReferenceValue = overrideCourse);
            }
            SceneManager.sceneLoaded += Configure;
            try { yield return SceneManager.LoadSceneAsync(BootstrapPath, LoadSceneMode.Single); }
            finally { SceneManager.sceneLoaded -= Configure; }
            float deadline = Time.realtimeSinceStartup + 20;
            do
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Bootstrap navigation timed out.");
                yield return null;
                bootstrap = Object.FindFirstObjectByType<AppBootstrap>();
            } while (bootstrap == null || bootstrap.Navigation == null || bootstrap.Navigation.IsTransitioning);
            yield return null;
        }

        private IEnumerator WaitForNavigation()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (bootstrap.Navigation.IsTransitioning)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Scene transition timed out.");
                Assert.That(LoadedContentScenes().Count(), Is.LessThanOrEqualTo(1));
                yield return null;
            }
            yield return null;
        }

        private VisualElement Root() => Object.FindFirstObjectByType<CourseMenuPresenter>().GetComponent<UIDocument>().rootVisualElement;

        private static void Submit(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }

        private static IEnumerable<Scene> LoadedContentScenes() => Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt).Where(s => s.isLoaded && (s.path == MenuPath || s.path == WorkspacePath));

        private static void AssertContentScene(string path)
        {
            Assert.That(LoadedContentScenes().Select(s => s.path), Is.EqualTo(new[] { path }));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(path));
            Assert.That(Object.FindObjectsByType<CourseMenuPresenter>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        private CourseDefinition ExtendedCourse(out LessonDefinition second)
        {
            var original = bootstrap.Navigation.Course;
            var course = Object.Instantiate(original);
            var chapter = Object.Instantiate(original.Chapters[0]);
            second = Object.Instantiate(chapter.Lessons[0]);
            var additional = second;
            owned.AddRange(new ScriptableObject[] { course, chapter, second });
            Edit(second, "id", p => p.stringValue = "future.lesson.second");
            Edit(second, "title", p => p.stringValue = "Una seconda voce di catalogo");
            Edit(second, "objective", p => p.stringValue = "Verificare la navigazione di contenuti aggiuntivi.");
            Edit(chapter, "lessons", p => { p.arraySize = 2; p.GetArrayElementAtIndex(0).objectReferenceValue = additional; p.GetArrayElementAtIndex(1).objectReferenceValue = original.Chapters[0].Lessons[0]; });
            Edit(course, "chapters", p => p.GetArrayElementAtIndex(0).objectReferenceValue = chapter);
            return course;
        }

        private static void Edit(Object owner, string field, Action<SerializedProperty> change)
        {
            var serialized = new SerializedObject(owner);
            change(serialized.FindProperty(field));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class NavigationProbe : ICourseNavigation
        {
            public CourseDefinition Course { get; }
            public LessonDefinition ActiveLesson => Course.Chapters[0].Lessons[0];
            public bool IsTransitioning => false;
            public string StatusMessage => string.Empty;
            public int ListenerCount { get; private set; }
            public int ReturnRequests { get; private set; }
            public event Action Changed { add { ListenerCount++; } remove { ListenerCount--; } }
            public NavigationProbe(CourseDefinition course) => Course = course;
            public bool OpenLesson(string lessonId) => false;
            public bool ReturnToMenu() { ReturnRequests++; return true; }
        }
    }
}
#endif





