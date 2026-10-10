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
    public sealed class ProgressionRuntimeTests
    {
        private const string A = "Assets/OptimalControl/";
        private readonly List<Object> owned = new List<Object>();
        private AppBootstrap bootstrap;
        private CourseDefinition course;
        private LessonDefinition first, second;
        private MemoryProgressStore store;
        private CourseRuntimeServices services;
        private LessonHudPresenter Hud => Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root => Hud.GetComponent<UIDocument>().rootVisualElement;
        private SimulationSession Sim => bootstrap.ActiveHost.Session;
        private LessonSession Lesson => bootstrap.ActiveHost.Lesson;
        private T Clone<T>(T value) where T : Object { var clone = Object.Instantiate(value); owned.Add(clone); return clone; }
        private static void Set(Object asset, string field, Action<SerializedProperty> change) { var so = new SerializedObject(asset); change(so.FindProperty(field)); so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Refs(Object asset, string field, params Object[] values) => Set(asset, field, p => { p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; });
        [UnitySetUp] public IEnumerator Setup()
        {
            store = new MemoryProgressStore(); services = new CourseRuntimeServices();
            course = Clone(AssetDatabase.LoadAssetAtPath<CourseDefinition>(A + "Data/Courses/PrototypeCourse.asset"));
            var chapter = Clone(course.Chapters[0]); first = Clone(chapter.Lessons[0]); second = Clone(first);
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"one\",\"instruction\":\"Observe a state.\"}]}", first);
            JsonUtility.FromJsonOverwrite("{\"id\":\"fixture.cost\",\"title\":\"Interpret the criterion\",\"steps\":[{\"id\":\"other\",\"instruction\":\"Explain a criterion.\"}],\"prerequisiteLessonIds\":[\"prototype.pendulum\"]}", second);
            Refs(chapter, "lessons", first, second); Refs(course, "chapters", chapter); yield return Restart();
        }
        private IEnumerator Restart()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            void Configure(Scene scene, LoadSceneMode mode)
            {
                if (scene.path != A + "Scenes/Bootstrap.unity") return;
                var app = Object.FindFirstObjectByType<AppBootstrap>(); Set(app, "course", p => p.objectReferenceValue = course); app.Configure(services, store);
            }
            SceneManager.sceneLoaded += Configure;
            try { yield return SceneManager.LoadSceneAsync(A + "Scenes/Bootstrap.unity", LoadSceneMode.Single); } finally { SceneManager.sceneLoaded -= Configure; }
            yield return Ready();
        }
        private IEnumerator Ready()
        {
            float deadline = Time.realtimeSinceStartup + 20;
            do { yield return null; Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); bootstrap = Object.FindFirstObjectByType<AppBootstrap>(); }
            while (bootstrap == null || bootstrap.Navigation == null || !bootstrap.Navigation.IsReady);
        }
        private IEnumerator Open(string id = null) { Assert.That(bootstrap.Navigation.OpenLesson(id ?? first.Id), Is.True); yield return Ready(); }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            foreach (var path in new[] { A + "Scenes/MainMenu.unity", A + "Scenes/LessonWorkspace.unity" })
            { var scene = SceneManager.GetSceneByPath(path); if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
            foreach (var value in owned) Object.Destroy(value); owned.Clear(); yield return null;
        }
        [UnityTest] public IEnumerator CompletionMenuReopenAndApplicationRestartPreserveOnlyCompletedIdentity()
        {
            Assert.That(bootstrap.Progress.GetStatus(second.Id).MissingPrerequisiteIds, Is.EqualTo(new[] { first.Id }));
            yield return Open(); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
            var oldLesson = Lesson; Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True); yield return Ready();
            var menu = Object.FindFirstObjectByType<CourseMenuPresenter>().GetComponent<UIDocument>().rootVisualElement;
            Assert.That(menu.Q<Label>("progress-" + first.Id).text, Does.Contain("salvato")); Assert.That(bootstrap.Progress.GetStatus(second.Id).IsCompleted, Is.False);
            yield return Open(); Assert.That(Lesson.Presentation.IsCompleted, Is.False); Assert.That(Lesson, Is.Not.SameAs(oldLesson)); Assert.That(oldLesson.Next(), Is.False);
            Assert.That(bootstrap.Progress.GetStatus(first.Id).IsCompleted, Is.True); yield return Restart();
            Assert.That(bootstrap.Progress.GetStatus(first.Id).IsCompleted, Is.True); Assert.That(bootstrap.Progress.GetStatus(second.Id).MissingPrerequisiteIds, Is.Empty);
            yield return Open(second.Id); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator AF08_07_TwoDifferentLessonsUseNeutralCompletionWithoutPrototypeConcepts()
        {
            foreach (string id in new[] { first.Id, second.Id })
            {
                yield return Open(id); Assert.That(Lesson.Next(), Is.True);
                Assert.That(Root.Q<Label>("lesson-step-title").text, Is.EqualTo("Lezione completata"));
                string text = Root.Q<Label>("lesson-instruction").text; Assert.That(text, Does.Contain("questa lezione"));
                Assert.That(text, Does.Not.Contain("prototipo")); Assert.That(text, Does.Not.Contain("traiettoria"));
                Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True); yield return Ready();
            }
        }
        [UnityTest] public IEnumerator NavigationDuringFinalPublicationNeverSavesOldDestination()
        {
            yield return Open(); var old = Lesson; var sim = Sim;
            old.Changed += p => { if (p.IsCompleted) Assert.That(bootstrap.Navigation.OpenLesson(second.Id), Is.True); };
            Assert.DoesNotThrow(() => Assert.That(old.Next(), Is.True)); yield return Ready();
            Assert.That(bootstrap.Navigation.ActiveLesson.Id, Is.EqualTo(second.Id)); Assert.That(store.SaveCount, Is.Zero); Assert.That(sim.State.IsClosed, Is.True);
            Assert.That(bootstrap.Progress.GetStatus(second.Id).MissingPrerequisiteIds, Is.EqualTo(new[] { first.Id }));
        }
        [UnityTest] public IEnumerator NavigationFromLateCompletionObserverCancelsEarlierQueuedCommit()
        {
            yield return Open(); Lesson.CompletionValidated += _ => Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True);
            Assert.That(Lesson.Next(), Is.True); yield return Ready(); Assert.That(store.SaveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator PresentationNonRepresentableBlocksPersistentCompletionAndResetRecovers()
        {
            yield return Open(); Assert.That(Sim.Execute(SimulationCommand.SetInput(0, new NumericVector(1e40), InputEditScope.Tail)), Is.True);
            Assert.That(bootstrap.ActiveHost.GetComponent<ExperimentVisualization>().Status.Availability, Is.EqualTo(VisualizationAvailability.NonRepresentable));
            Assert.That(Hud.DisplayedState.Result, Is.SameAs(Sim.State.Result)); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(Lesson.Restart(), Is.True); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator FailedEnterRuntimeCannotSaveAndRestartRecovers()
        {
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"one\",\"instruction\":\"One.\"},{\"id\":\"two\",\"instruction\":\"Terminal.\",\"entryActions\":[{\"kind\":4,\"index\":50}]}]}", first);
            yield return Open(); Sim.Execute(SimulationCommand.SetHorizon(1)); Assert.That(Lesson.Next(), Is.False);
            Assert.That(Lesson.Presentation.Error, Is.Not.Null); Assert.That(Lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(Lesson.Restart(), Is.True); Assert.That(Lesson.Next(), Is.True); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator AF08_04_HudKeyboardAndInspectorPolicyHandleFailedShortRequestedDomain()
        {
            yield return Open(); Lesson.Explore(); Sim.Execute(SimulationCommand.Seek(50)); var valid = Sim.State.Result;
            Sim.Execute(SimulationCommand.SetParameter("cost.target.0", 1e308)); Sim.Execute(SimulationCommand.SetHorizon(1));
            Assert.That(Root.Q<DoubleField>("input-0").enabledSelf, Is.False);
            Assert.DoesNotThrow(() => Root.Q<DoubleField>("input-0").value = 3);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Root.Q<DoubleField>("input-0").Blur();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            Assert.That(Sim.State.TryGetEditableInput(50, out var editable), Is.False, "Inspector uses this same guarded read."); Assert.That(editable, Is.Null);
            Assert.That(Sim.State.Sample.Timestep, Is.EqualTo(50)); Assert.That(Sim.State.Result, Is.SameAs(valid)); Assert.That(Sim.State.RequestedConfiguration.Horizon, Is.EqualTo(1));
            Assert.That(Root.Q<Label>("parameter-status").text, Does.Contain("Reset")); Assert.That(Lesson.Next(), Is.False);
            Assert.That(Lesson.Execute(SimulationCommand.Reset()), Is.True); Assert.That(Sim.State.Error, Is.Null); Assert.That(Root.Q<DoubleField>("input-0").enabledSelf, Is.True);
            Assert.That(Sim.State.Sample.DiscretizationStepSeconds, Is.EqualTo(.02));
        }
        private sealed class ExtraMetric : IRuntimeMetricSource
        {
            private readonly SessionMetricSource defaults = new SessionMetricSource();
            public bool TryGet(SimulationResult result, SimulationSnapshot sample, string id, out RuntimeMetric metric)
            {
                if (id == "fixture.extra") { metric = RuntimeMetric.Number(id, 7, "1", sample.Timestep); return true; }
                return defaults.TryGet(result, sample, id, out metric);
            }
        }
        private sealed class ExtraCondition : ILessonConditionEvaluator
        { public bool IsMet(LessonConditionPlan c, LessonConditionContext x) => x.Session.Sample.Timestep == 1; }
        [UnityTest] public IEnumerator AF08_06_ComposedCustomMetricAndConditionReachActualHudAndLesson()
        {
            var formula = Clone(first.Formulas[0]);
            JsonUtility.FromJsonOverwrite("{\"id\":\"fixture.formula\",\"terms\":[{\"id\":\"extra\",\"symbol\":\"v\",\"label\":\"Custom metric\",\"meaning\":\"Provider value\",\"rows\":1,\"columns\":1,\"componentUnits\":[\"1\"],\"value\":{\"metricId\":\"fixture.extra\",\"kind\":0,\"scope\":0,\"component\":-1,\"numericFormat\":\"G6\"}}]}", formula);
            Refs(first, "formulas", formula);
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"custom\",\"instruction\":\"Custom condition\",\"enabledControlIds\":[\"step\"],\"conditions\":[{\"evaluatorId\":\"fixture.condition\"}]}]}", first);
            var conditions = new LessonConditionRegistry(); conditions.Register("fixture.condition", new ExtraCondition());
            services = new CourseRuntimeServices(new RuntimeMetricResolver(new ExtraMetric()), conditions); yield return Restart(); yield return Open();
            Assert.That(Hud.Formula.Resolutions["extra"].Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(Hud.Formula.Resolutions["extra"].Value.Scalar, Is.EqualTo(7));
            Assert.That(Lesson.Next(), Is.False); Assert.That(Lesson.Execute(SimulationCommand.StepForward()), Is.True); Assert.That(Lesson.Next(), Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1)); Assert.That(Hud.Formula.DisplayedResult, Is.SameAs(bootstrap.ActiveHost.GetComponent<ExperimentVisualization>().DisplayedState.Result));
        }
    }
}
#endif
