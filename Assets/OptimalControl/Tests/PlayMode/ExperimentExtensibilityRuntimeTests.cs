#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class ExperimentExtensibilityRuntimeTests
    {
        private const string A = "Assets/OptimalControl/", F = A + "Tests/Fixtures/F10/";
        private readonly List<Object> owned = new List<Object>();
        private AppBootstrap bootstrap;
        private CourseDefinition course;
        private LessonDefinition definition;
        private MemoryProgressStore store;
        private CourseRuntimeServices services;
        private LessonHudPresenter Hud => Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root => Hud.GetComponent<UIDocument>().rootVisualElement;
        private ObjectiveSession Static => (ObjectiveSession)bootstrap.ActiveHost.Experiment;
        private LessonSession Lesson => bootstrap.ActiveHost.Lesson;
        private MetricPointVisualization View => bootstrap.ActiveHost.GetComponent<MetricPointVisualization>();
        private T Clone<T>(T value) where T : Object { var clone = Object.Instantiate(value); owned.Add(clone); return clone; }
        private static void Set(Object asset, string field, Action<SerializedProperty> action) { var so = new SerializedObject(asset); action(so.FindProperty(field)); so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Refs(Object asset, string field, params Object[] values) => Set(asset, field, p => { p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; });
        [UnitySetUp] public IEnumerator Setup()
        {
            store = new MemoryProgressStore(); services = new CourseRuntimeServices();
            course = Clone(AssetDatabase.LoadAssetAtPath<CourseDefinition>(F + "FixtureCourse.asset"));
            var chapter = Clone(course.Chapters[1]); definition = Clone(chapter.Lessons[0]); Refs(chapter, "lessons", definition); Refs(course, "chapters", course.Chapters[0], chapter);
            yield return Restart();
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
            do
            {
                yield return null; bootstrap = Object.FindFirstObjectByType<AppBootstrap>();
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), bootstrap?.Navigation?.StatusMessage);
                if (bootstrap?.Navigation is CourseManager manager) Assert.That(manager.State?.Phase, Is.Not.EqualTo(NavigationPhase.Failed), manager.StatusMessage);
            }
            while (bootstrap == null || bootstrap.Navigation == null || !bootstrap.Navigation.IsReady);
        }
        private IEnumerator Open(string id = "fixture.f10.static") { Assert.That(bootstrap.Navigation.OpenLesson(id), Is.True); yield return Ready(); }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            foreach (var path in new[] { A + "Scenes/MainMenu.unity", A + "Scenes/LessonWorkspace.unity" })
            { var s = SceneManager.GetSceneByPath(path); if (s.IsValid() && s.isLoaded) yield return SceneManager.UnloadSceneAsync(s); }
            foreach (var value in owned) Object.Destroy(value); owned.Clear(); yield return null;
        }
        [UnityTest] public IEnumerator CatalogToReadyPublishesBoundCommonStaticSessionWithoutFakeTime()
        {
            var events = new List<NavigationPhase>();
            ((CourseManager)bootstrap.Navigation).LifecycleChanged += update =>
            {
                events.Add(update.Phase);
                if (update.Phase != NavigationPhase.Ready || update.Transition.Destination == null) return;
                Assert.That(update.Transition.Destination.Id, Is.EqualTo(definition.Id));
                Assert.That(bootstrap.ActiveHost.Experiment, Is.Not.Null); Assert.That(Hud.BoundExperiment, Is.SameAs(bootstrap.ActiveHost.Experiment));
                Assert.That(View.BoundExperiment, Is.SameAs(Hud.BoundExperiment)); Assert.That(Hud.DisplayedSnapshot, Is.SameAs(View.DisplayedSnapshot));
            };
            yield return Open();
            Assert.That(events, Is.EqualTo(new[] { NavigationPhase.Requested, NavigationPhase.SceneLoaded, NavigationPhase.Composing, NavigationPhase.Binding, NavigationPhase.Ready }));
            Assert.That(bootstrap.ActiveHost.Session, Is.Null); Assert.That(Static.Current, Is.InstanceOf<ObjectiveSnapshot>());
            Assert.That(Hud.Formula.Cursor, Is.Null); Assert.That(Root.Q<VisualElement>("timeline-seek").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(Hud.Formula.Resolutions["value"].Value.Scalar, Is.EqualTo(9)); Assert.That(Hud.Formula.Resolutions["value"].SampleIndex, Is.Null);
            Assert.That(View.Point.transform.localPosition, Is.EqualTo(new Vector3(1, 2, 0))); Assert.That(View.Ordinate.transform.localPosition.y, Is.EqualTo(2.25f));
        }
        [UnityTest] public IEnumerator HudEditCreatesNewRevisionSharedByFormulaVisualsAndKeepsAImmutable()
        {
            yield return Open(); var a = Static.Current; var field = Root.Q<DoubleField>("parameter-decision.0");
            Assert.That(field.enabledInHierarchy, Is.True); field.value = 2;
            var b = Static.Current; Assert.That(b.Result.RevisionId, Is.GreaterThan(a.Result.RevisionId));
            Assert.That(Hud.DisplayedSnapshot, Is.SameAs(b)); Assert.That(View.DisplayedSnapshot, Is.SameAs(b));
            Assert.That(Hud.Formula.Resolutions.Values.All(r => ReferenceEquals(r.Result, b.Result)), Is.True);
            Assert.That(Hud.Formula.Resolutions["value"].Value.Scalar, Is.EqualTo(12)); Assert.That(View.Point.transform.localPosition, Is.EqualTo(new Vector3(2, 2, 0)));
            Assert.That(((ObjectiveResult)a.Result).Value, Is.EqualTo(9)); Assert.That(((ObjectiveResult)a.Result).Configuration.Decision[0], Is.EqualTo(1));
            Hud.Formula.Render(a); Assert.That(Hud.Formula.Resolutions["value"].Value.Scalar, Is.EqualTo(9)); Hud.Formula.Render(b);
            Hud.Formula.SelectTerm("value"); Assert.That(View.Ordinate.GetComponent<HighlightTarget>().IsHighlighted, Is.True);
            Assert.That(View.Ordinate.SampleIndex, Is.Null);
            Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(bootstrap.Progress.GetStatus(definition.Id).IsCompleted, Is.True); Assert.That(bootstrap.Progress.GetStatus("prototype.pendulum").IsCompleted, Is.False);
            Lesson.Restart(); Root.Q<DoubleField>("parameter-decision.0").value = 2; Hud.Formula.SelectTerm("value"); Lesson.Next(); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ExitReopenAndApplicationRestartDisposeOldSessionAndKeepDistinctProgress()
        {
            yield return Open(); var old = Static; var oldHud = Hud.Formula; var oldLesson = Lesson;
            Root.Q<DoubleField>("parameter-decision.0").value = 2; Hud.Formula.SelectTerm("value"); Assert.That(Lesson.Next(), Is.True);
            Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True); yield return Ready();
            Assert.That(old.Current.IsClosed, Is.True); Assert.That(oldLesson.Next(), Is.False); Assert.That(oldHud.DisplayedSnapshot, Is.Null);
            yield return Open(); Assert.That(Static, Is.Not.SameAs(old)); Assert.That(Static.State.Result.Value, Is.EqualTo(9)); Assert.That(Lesson.Presentation.IsCompleted, Is.False);
            yield return Restart(); Assert.That(bootstrap.Progress.GetStatus(definition.Id).IsCompleted, Is.True); Assert.That(bootstrap.Progress.GetStatus("prototype.pendulum").IsCompleted, Is.False);
        }
        [UnityTest] public IEnumerator NavigationDuringStaticPublicationDefersUntilAllConsumersAndClosesLifetime()
        {
            yield return Open(); var old = Static; bool observed = false;
            old.Subscribe(snapshot => { Assert.That(bootstrap.Navigation.OpenLesson("prototype.pendulum"), Is.True); Assert.That(old.Current.IsClosed, Is.False); });
            old.Subscribe(snapshot => { observed = true; Assert.That(Hud.DisplayedSnapshot, Is.SameAs(snapshot)); Assert.That(old.Current.IsClosed, Is.False); });
            Assert.That(Lesson.Execute(SimulationCommand.SetParameter("decision.0", 2)), Is.True); Assert.That(observed, Is.True);
            yield return Ready(); Assert.That(old.Current.IsClosed, Is.True); Assert.That(bootstrap.ActiveHost.Session, Is.Not.Null); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(bootstrap.ActiveHost.Session.State.Sample.DiscretizationStepSeconds, Is.EqualTo(.02));
        }
        [UnityTest] public IEnumerator SharedHudSupportsStructuredMatrixIntegralAndCompactLayouts()
        {
            yield return Open();
            foreach (var formula in definition.Formulas)
            {
                Hud.Formula.SetFormula(formula.Id); Assert.That(Root.Query<Button>(className: "math-term").ToList(), Is.Not.Empty);
                Assert.That(Hud.Formula.Resolutions.Values.All(r => r.SampleIndex == null), Is.True);
                if (formula.Id.Contains("integral") || formula.Id.Contains("compact")) Assert.That(Hud.Formula.Resolutions.Values.All(r => r.Validity == MetricValidity.NotApplicable), Is.True);
            }
            Assert.That(Static.State.Result.Value, Is.EqualTo(9));
        }
        [UnityTest] public IEnumerator StaticFailedRequestKeepsLastValidSnapshotAndResetRecovers()
        {
            yield return Open(); var a = Static.State.Result;
            Assert.That(Lesson.Execute(SimulationCommand.SetParameter("decision.0", double.NaN)), Is.False);
            Assert.That(Static.State.Result, Is.SameAs(a)); Assert.That(Static.State.Error, Is.Not.Null); Assert.That(Lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(Hud.Formula.Resolutions["value"].Value.Scalar, Is.EqualTo(9)); Assert.That(View.Status.Source.Result, Is.SameAs(a));
            Assert.That(Lesson.Restart(), Is.True); Assert.That(Static.State.Error, Is.Null);
        }
        [UnityTest] public IEnumerator StaticNonRepresentableStillPublishesMathAndCannotAwardProgressUntilRecovery()
        {
            var experiment = Clone(definition.Experiment); Set(definition, "experiment", p => p.objectReferenceValue = experiment);
            Set(experiment, "parameters", p => p.GetArrayElementAtIndex(0).FindPropertyRelative("maximum").doubleValue = 1e100);
            yield return Open();
            Assert.That(Lesson.Execute(SimulationCommand.SetParameter("decision.0", 1e40)), Is.True);
            Assert.That(View.Status.Availability, Is.EqualTo(VisualizationAvailability.NonRepresentable)); Assert.That(Static.State.Error, Is.Null);
            Assert.That(Hud.DisplayedSnapshot, Is.SameAs(Static.Current)); Assert.That(Hud.Formula.Resolutions["value"].Validity, Is.EqualTo(MetricValidity.Available));
            Hud.Formula.SelectTerm("value"); Assert.That(View.Ordinate.GetComponent<HighlightTarget>().IsHighlighted, Is.False);
            Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(Lesson.Restart(), Is.True); Assert.That(View.Status.Availability, Is.EqualTo(VisualizationAvailability.Available));
            Root.Q<DoubleField>("parameter-decision.0").value = 2; Hud.Formula.SelectTerm("value"); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator StaticFailedEnterCannotCompleteAndRestartRecovers()
        {
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"one\",\"instruction\":\"Observe\"},{\"id\":\"two\",\"instruction\":\"Impossible entry\",\"entryActions\":[{\"kind\":7,\"target\":\"decision.0\",\"value\":100}]}]}", definition);
            yield return Open(); var id = Lesson.Presentation.Step.Id;
            Assert.That(Lesson.Next(), Is.False); Assert.That(Lesson.Presentation.Step.Id, Is.EqualTo(id)); Assert.That(Lesson.Presentation.Error, Is.Not.Null);
            Assert.That(Lesson.Presentation.IsCompleted, Is.False); Assert.That(Lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(Lesson.Restart(), Is.True); Assert.That(Lesson.Presentation.Error, Is.Null); Assert.That(Static.State.Result.Value, Is.EqualTo(9));
        }
        [UnityTest] public IEnumerator StaticNavigationFromCompletionObserverCancelsPersistentReceipt()
        {
            yield return Open(); var old = Static;
            Root.Q<DoubleField>("parameter-decision.0").value = 2; Hud.Formula.SelectTerm("value");
            Lesson.CompletionValidated += _ => Assert.That(bootstrap.Navigation.ReturnToMenu(), Is.True);
            Assert.That(Lesson.Next(), Is.True); yield return Ready(); Assert.That(old.Current.IsClosed, Is.True); Assert.That(store.SaveCount, Is.Zero);
        }
        private sealed class CustomMetric : IExperimentMetricSource
        {
            public bool TryGet(IExperimentSnapshot snapshot, string id, out RuntimeMetric metric)
            { metric = id == "fixture.custom" ? RuntimeMetric.Number(id, 17, "1", null, scope: FormulaScope.Snapshot) : null; return metric != null; }
        }
        private sealed class CustomCondition : ILessonConditionEvaluator
        { public bool IsMet(LessonConditionPlan condition, LessonConditionContext context) => context.Snapshot.Result is ObjectiveResult result && result.Value == 12; }
        [UnityTest] public IEnumerator ComposedCustomStaticMetricAndConditionReachActualFormulaHudAndRuntime()
        {
            var formula = Clone(definition.Formulas[0]);
            JsonUtility.FromJsonOverwrite("{\"id\":\"fixture.custom.formula\",\"layout\":{\"nodes\":[]},\"terms\":[{\"id\":\"custom\",\"symbol\":\"c\",\"label\":\"Custom\",\"meaning\":\"Provider value\",\"rows\":1,\"columns\":1,\"componentUnits\":[\"1\"],\"value\":{\"metricId\":\"fixture.custom\",\"kind\":0,\"scope\":4,\"component\":-1}}]}", formula);
            Refs(definition, "formulas", formula);
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"custom\",\"instruction\":\"Custom static provider\",\"enabledControlIds\":[\"parameter\"],\"editableParameterTargets\":[\"decision.0\"],\"conditions\":[{\"evaluatorId\":\"fixture.condition\"}]}]}", definition);
            Set(definition, "steps", p => { var step = p.GetArrayElementAtIndex(0); step.FindPropertyRelative("formulaFocus").objectReferenceValue = formula; step.FindPropertyRelative("selectedTermId").stringValue = "custom"; step.FindPropertyRelative("visibleTermIds").arraySize = 0; });
            var conditions = new LessonConditionRegistry(); conditions.Register("fixture.condition", new CustomCondition(), ExperimentCapabilities.Objective);
            services = new CourseRuntimeServices(new RuntimeMetricResolver(experimentSources: new IExperimentMetricSource[] { new CustomMetric(), new ObjectiveMetricSource() }), conditions);
            yield return Restart(); yield return Open();
            Assert.That(Hud.Formula.Resolutions["custom"].Value.Scalar, Is.EqualTo(17)); Assert.That(Hud.Formula.Resolutions["custom"].Result, Is.SameAs(View.DisplayedSnapshot.Result));
            Assert.That(Lesson.Next(), Is.False); Assert.That(Lesson.Execute(SimulationCommand.SetParameter("decision.0", 2)), Is.True); Assert.That(Lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
    }
}
#endif
