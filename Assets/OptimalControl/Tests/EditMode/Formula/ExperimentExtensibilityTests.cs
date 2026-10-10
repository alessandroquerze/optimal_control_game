using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Tests
{
    public sealed class ExperimentExtensibilityTests
    {
        private const string F = "Assets/OptimalControl/Tests/Fixtures/F10/";
        private readonly RuntimeMetricResolver metrics = new RuntimeMetricResolver();
        private static ObjectiveConfiguration Configuration() => new ObjectiveConfiguration("fixture.static", new NumericVector(1, 2), new NumericVector(2, 4));
        private static T Asset<T>(string name) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(F + name + ".asset");

        [Test] public void ObjectiveOracleIsIndependentOfDynamicsAndTrajectoryCost()
        {
            IObjectiveFunction f = new DiagonalQuadraticObjective(new NumericVector(2, 4));
            Assert.That(f.Value(new NumericVector(1, 2)), Is.EqualTo(9)); // 1/2 * (2*1 + 4*4)
            Assert.That(f.Value(new NumericVector(2, 2)), Is.EqualTo(12));
            Assert.That(f.Value(new NumericVector(0, 0)), Is.Zero);
            Assert.Throws<ArgumentException>(() => f.Value(new NumericVector(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiagonalQuadraticObjective(new NumericVector(-1, 4)));
        }
        [Test] public void StaticRevisionsRetainOwnConfigurationSamplesAndFailedAttempt()
        {
            using (var s = new ObjectiveSession(Configuration()))
            {
                var a = s.State; Assert.That(a.Result.Value, Is.EqualTo(9));
                Assert.That(s.Execute(SimulationCommand.SetParameter("decision.0", 2)), Is.True);
                var b = s.State; Assert.That(b.Result.Value, Is.EqualTo(12));
                s.Execute(SimulationCommand.SetParameter("objective.weight.0", 6));
                Assert.That(s.State.Result.Value, Is.EqualTo(20));
                Assert.That(a.Result.Configuration.Decision[0], Is.EqualTo(1)); Assert.That(a.Result.Configuration.Objective.Weights[0], Is.EqualTo(2));
                Assert.That(metrics.Resolve(a, ObjectiveMetricSource.Value, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(9));
                Assert.That(metrics.Resolve(b, ObjectiveMetricSource.Value, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(12));
                var valid = s.State.Result;
                Assert.That(s.Execute(SimulationCommand.SetParameter("decision.0", 1e308)), Is.False);
                Assert.That(s.State.Result, Is.SameAs(valid)); Assert.That(s.State.Error, Is.Not.Null);
                Assert.That(valid.Configuration.Decision[0], Is.EqualTo(2));
                Assert.That(s.Execute(SimulationCommand.Reset()), Is.True); Assert.That(s.State.Result.Value, Is.EqualTo(9));
            }
        }
        [Test] public void CommonStaticContractDoesNotExposeTimelineOrIteration()
        {
            using (IExperimentSession s = new ObjectiveSession(Configuration()))
            {
                Assert.That(s.Current, Is.Not.InstanceOf<OptCon.Simulation.SessionState>());
                Assert.That(s.Current.Result, Is.Not.InstanceOf<SimulationResult>());
                Assert.That(s.Capabilities.HasFlag(ExperimentCapabilities.Trajectory), Is.False);
                foreach (var kind in new[] { SimulationCommandKind.Seek, SimulationCommandKind.Play, SimulationCommandKind.SetInput }) Assert.That(s.Supports(kind), Is.False);
                foreach (var id in new[] { RuntimeMetricIds.Index, RuntimeMetricIds.PhysicalStep, RuntimeMetricIds.PhysicalTime })
                {
                    var r = metrics.Resolve(s.Current, id, FormulaValueKind.Scalar);
                    Assert.That(r.Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(r.SampleIndex, Is.Null); Assert.That(r.Value.Scalar, Is.Null);
                }
                foreach (var property in typeof(IExperimentSnapshot).GetProperties())
                    Assert.That(new[] { "Cursor", "History", "Horizon", "Timestep", "Iteration", "Inputs" }, Does.Not.Contain(property.Name));
            }
        }
        [TestCase(1)] [TestCase(2)]
        public void SnapshotVectorsPreserveShapeUnitsAndExplicitComponentSelection(int dimension)
        {
            using (var s = new ObjectiveSession(new ObjectiveConfiguration("fixture", new NumericVector(Enumerable.Repeat(2d, dimension).ToArray()), new NumericVector(Enumerable.Repeat(3d, dimension).ToArray()))))
            {
                var v = metrics.Resolve(s.Current, ObjectiveMetricSource.Decision, FormulaValueKind.Vector, rows: dimension, columns: 1, expectedUnits: Enumerable.Repeat("1", dimension).ToArray());
                Assert.That(v.Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(v.Value.Scope, Is.EqualTo(FormulaScope.Snapshot)); Assert.That(v.SampleIndex, Is.Null);
                Assert.That(v.Value.Vector, Is.SameAs(s.State.Result.Configuration.Decision));
                Assert.That(metrics.Resolve(s.Current, ObjectiveMetricSource.Decision, FormulaValueKind.Scalar).Validity, Is.EqualTo(MetricValidity.Invalid));
                Assert.That(metrics.Resolve(s.Current, ObjectiveMetricSource.Decision, FormulaValueKind.Scalar, component: 0).Value.Scalar, Is.EqualTo(2));
                Assert.That(metrics.Resolve(s.Current, ObjectiveMetricSource.Decision, FormulaValueKind.Scalar, component: dimension).Diagnostic, Does.Contain("range"));
                Assert.That(metrics.Resolve(s.Current, ObjectiveMetricSource.Decision, FormulaValueKind.Vector, expectedUnits: new[] { "m" }).Validity, Is.EqualTo(MetricValidity.Invalid));
                Assert.That(metrics.Resolve(s.Current, ObjectiveMetricSource.Decision, FormulaValueKind.Vector, FormulaScope.CurrentTimestep).Validity, Is.EqualTo(MetricValidity.Invalid));
            }
        }
        [Test] public void StaticControlledDispatchContinuesConsumersAndUsesSameNotificationBoundary()
        {
            using (var s = new ObjectiveSession(Configuration()))
            {
                int delivered = 0; bool after = false; IExperimentSnapshot seen = null;
                s.Subscribe(_ => throw new InvalidOperationException("consumer failure"));
                s.Subscribe(x => { delivered++; seen = x; Assert.That(s.Execute(SimulationCommand.Reset()), Is.False); s.Notifications.Defer(() => after = true); Assert.That(after, Is.False); });
                var e = Assert.Throws<SessionPublicationException>(() => s.Execute(SimulationCommand.SetParameter("decision.0", 2)));
                Assert.That(delivered, Is.EqualTo(1)); Assert.That(seen, Is.SameAs(s.Current)); Assert.That(seen.Result, Is.SameAs(e.Publication.Result));
                Assert.That(s.State.Result.Value, Is.EqualTo(12)); Assert.That(s.LastPublicationFailure, Is.SameAs(e)); Assert.That(after, Is.True);
            }
        }
        [Test] public void StaticSubscriptionLifetimeAndCloseStopDispatch()
        {
            var s = new ObjectiveSession(Configuration()); int count = 0;
            var a = s.Subscribe(_ => { count++; s.Dispose(); }); s.Subscribe(_ => count++);
            s.Execute(SimulationCommand.Reset()); Assert.That(count, Is.EqualTo(1)); Assert.That(s.Current.IsClosed, Is.True);
            Assert.That(s.Execute(SimulationCommand.Reset()), Is.False); a.Dispose(); a.Dispose();
            Assert.Throws<ObjectDisposedException>(() => s.Subscribe(_ => { }));
        }
        [Test] public void FixtureCatalogIsExecutableAndRemainsSeparateFromProduction()
        {
            var c = Asset<CourseDefinition>("FixtureCourse");
            var result = CatalogValidator.ValidateExecutionConfiguration(c);
            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Issues)); Assert.That(c.IsPrototype, Is.True);
            Assert.That(c.Chapters.SelectMany(x => x.Lessons).Select(x => x.Id), Is.EquivalentTo(new[] { "prototype.pendulum", "fixture.f10.static" }));
        }
        [TestCase("usesDiscreteTime", "1")] [TestCase("stateDimension", "1")]
        public void StaticProviderRejectsFabricatedDynamics(string field, string value)
        {
            var e = UnityEngine.Object.Instantiate(Asset<ExperimentDefinition>("QuadraticExperiment"));
            try { JsonUtility.FromJsonOverwrite("{\"" + field + "\":" + value + "}", e); Assert.Throws<ArgumentException>(() => ((QuadraticObjectiveDefinition)e.Provider).CreateConfiguration(e)); }
            finally { UnityEngine.Object.DestroyImmediate(e); }
        }
        [Test] public void StaticProviderRejectsIncorrectUnitsAndUnknownTargets()
        {
            var e = UnityEngine.Object.Instantiate(Asset<ExperimentDefinition>("QuadraticExperiment"));
            try
            {
                var so = new SerializedObject(e); var p = so.FindProperty("parameters").GetArrayElementAtIndex(0);
                p.FindPropertyRelative("unit").stringValue = "m"; so.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentException>(() => ((QuadraticObjectiveDefinition)e.Provider).CreateConfiguration(e));
                p.FindPropertyRelative("unit").stringValue = "1"; p.FindPropertyRelative("modelTarget").stringValue = "unknown"; so.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<ArgumentException>(() => ((QuadraticObjectiveDefinition)e.Provider).CreateConfiguration(e));
            }
            finally { UnityEngine.Object.DestroyImmediate(e); }
        }
        [TestCase("QuadraticFormula")] [TestCase("MatrixLayout")] [TestCase("ContinuousIntegralLayout")] [TestCase("CompactVectorLayout")]
        public void StructuredLayoutsHaveValidatedShapeAndSelectableTerms(string name)
        {
            var formula = Asset<FormulaDefinition>(name); Assert.DoesNotThrow(() => formula.Layout.Validate(formula.Terms));
            using (IFormulaView view = new StructuredFormulaView())
            {
                view.Show(formula, _ => { }); Assert.That(view.Element.Query<Button>().ToList(), Is.Not.Empty);
                view.Select(formula.Terms[0].Id); view.Disclose(new[] { formula.Terms[0].Id });
                if (name == "MatrixLayout")
                {
                    var matrix = formula.Layout.Nodes.Single(x => x.Kind == FormulaLayoutKind.Matrix);
                    Assert.That(matrix.Rows, Is.EqualTo(2)); Assert.That(matrix.Columns, Is.EqualTo(2)); Assert.That(matrix.Children.Count, Is.EqualTo(4));
                    Assert.That(formula.Terms.All(x => x.Value.Kind == FormulaValueKind.Scalar), Is.True, "Matrix layout is not a flattened runtime Vector.");
                }
            }
        }
        [TestCase("{\"rootId\":\"m\",\"nodes\":[{\"id\":\"m\",\"kind\":7,\"rows\":2,\"columns\":2,\"children\":[\"x\"]},{\"id\":\"x\",\"kind\":0}]}")]
        [TestCase("{\"rootId\":\"r\",\"nodes\":[{\"id\":\"r\",\"kind\":2,\"children\":[\"r\"]}]}")]
        [TestCase("{\"rootId\":\"f\",\"nodes\":[{\"id\":\"f\",\"kind\":3,\"children\":[]}]}")]
        public void InvalidShapeCycleOrArityIsDiagnosed(string json)
        { Assert.Throws<ArgumentException>(() => JsonUtility.FromJson<FormulaLayout>(json).Validate(Array.Empty<FormulaTermDefinition>())); }

        [TestCase("timeline")] [TestCase("condition")] [TestCase("presentation")] [TestCase("binding")]
        [TestCase("units")] [TestCase("entry")] [TestCase("entryRange")]
        public void CatalogRejectsStaticCapabilityBindingUnitsAndEntryIncompatibilities(string fault)
        {
            var course = UnityEngine.Object.Instantiate(Asset<CourseDefinition>("FixtureCourse"));
            var chapter = UnityEngine.Object.Instantiate(course.Chapters[1]); var lesson = UnityEngine.Object.Instantiate(chapter.Lessons[0]);
            var experiment = UnityEngine.Object.Instantiate(lesson.Experiment); var formula = UnityEngine.Object.Instantiate(lesson.Formulas[0]);
            try
            {
                void Reference(UnityEngine.Object owner, string path, UnityEngine.Object value) { var so = new SerializedObject(owner); so.FindProperty(path).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
                Reference(course, "chapters.Array.data[1]", chapter); Reference(chapter, "lessons.Array.data[0]", lesson); Reference(lesson, "experiment", experiment);
                if (fault == "timeline") JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"bad\",\"instruction\":\"Bad capability\",\"enabledControlIds\":[\"seek\"]}]}", lesson);
                if (fault == "condition") JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"bad\",\"instruction\":\"Bad capability\",\"conditions\":[{\"evaluatorId\":\"terminal\"}]}]}", lesson);
                if (fault == "entry") JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"bad\",\"instruction\":\"Bad capability\",\"entryActions\":[{\"kind\":4,\"index\":0}]}]}", lesson);
                if (fault == "entryRange") JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"bad\",\"instruction\":\"Invalid parameter\",\"entryActions\":[{\"kind\":7,\"target\":\"decision.0\",\"value\":100}]}]}", lesson);
                if (fault == "presentation") Reference(experiment, "scenarioPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OptimalControl/Prefabs/PendulumScenario.prefab"));
                if (fault == "units") { var so = new SerializedObject(experiment); so.FindProperty("parameters.Array.data[0].unit").stringValue = "kg"; so.ApplyModifiedPropertiesWithoutUndo(); }
                if (fault == "binding")
                {
                    Reference(lesson, "formulas.Array.data[0]", formula); Reference(lesson, "steps.Array.data[0].formulaFocus", formula);
                    var so = new SerializedObject(formula); so.FindProperty("terms.Array.data[1].rows").intValue = 3; so.ApplyModifiedPropertiesWithoutUndo();
                }
                var result = CatalogValidator.ValidateExecutionConfiguration(course);
                Assert.That(result.IsValid, Is.False, fault); Assert.That(result.Issues.Any(x => x.Code == "ProviderResult" || x.Code == "PresentationCapability" || x.Code == "RuntimeBinding" || x.Code == "LessonEntryDomain"), Is.True, string.Join("\n", result.Issues));
            }
            finally { foreach (var o in new UnityEngine.Object[] { formula, experiment, lesson, chapter, course }) UnityEngine.Object.DestroyImmediate(o); }
        }
        private sealed class CustomSnapshotMetric : IExperimentMetricSource
        {
            public bool TryGet(IExperimentSnapshot snapshot, string id, out RuntimeMetric metric)
            { metric = id == "fixture.custom" ? RuntimeMetric.Number(id, 17, "1", null, scope: FormulaScope.Snapshot) : null; return metric != null; }
        }
        [Test] public void CustomSnapshotProviderUsesSameCompositionForValidationAndFormula()
        {
            var definition = UnityEngine.Object.Instantiate(Asset<LessonDefinition>("QuadraticLesson")); var formula = UnityEngine.Object.Instantiate(definition.Formulas[0]);
            var course = UnityEngine.Object.Instantiate(Asset<CourseDefinition>("FixtureCourse")); var chapter = UnityEngine.Object.Instantiate(course.Chapters[1]);
            try
            {
                JsonUtility.FromJsonOverwrite("{\"layout\":{\"nodes\":[]},\"terms\":[{\"id\":\"custom\",\"symbol\":\"c\",\"label\":\"Custom\",\"meaning\":\"Composed provider\",\"rows\":1,\"columns\":1,\"componentUnits\":[\"1\"],\"value\":{\"metricId\":\"fixture.custom\",\"kind\":0,\"scope\":4,\"component\":-1}}]}", formula);
                var data = new SerializedObject(definition); var array = data.FindProperty("formulas"); array.arraySize = 1; array.GetArrayElementAtIndex(0).objectReferenceValue = formula; data.FindProperty("steps").arraySize = 0; data.ApplyModifiedPropertiesWithoutUndo();
                data = new SerializedObject(chapter); data.FindProperty("lessons.Array.data[0]").objectReferenceValue = definition; data.ApplyModifiedPropertiesWithoutUndo();
                data = new SerializedObject(course); data.FindProperty("chapters.Array.data[1]").objectReferenceValue = chapter; data.ApplyModifiedPropertiesWithoutUndo();
                var services = new CourseRuntimeServices(new RuntimeMetricResolver(experimentSources: new IExperimentMetricSource[] { new CustomSnapshotMetric(), new ObjectiveMetricSource() }));
                var execution = CatalogValidator.ValidateExecutionConfiguration(course, null, true, services);
                Assert.That(execution.IsValid, Is.True, string.Join("\n", execution.Issues));
                using (var session = definition.Experiment.Provider.CreateExperimentSession(definition.Experiment))
                {
                    var report = new CatalogValidationResult(); BindingValidator.Validate(definition, session.Current, services.Metrics, report);
                    Assert.That(report.IsValid, Is.True, string.Join("\n", report.Issues));
                    Assert.That(services.Metrics.Resolve(formula.Terms[0], session.Current).Value.Scalar, Is.EqualTo(17));
                    var missing = new CatalogValidationResult(); BindingValidator.Validate(definition, session.Current, new RuntimeMetricResolver(), missing); Assert.That(missing.IsValid, Is.False);
                }
            }
            finally { foreach (var asset in new UnityEngine.Object[] { formula, definition, chapter, course }) UnityEngine.Object.DestroyImmediate(asset); }
        }
        [Test] public void ExistingPendulumAdaptsCommonSessionWithoutChangingTimelineOrPhysicalTime()
        {
            var e = AssetDatabase.LoadAssetAtPath<CourseDefinition>("Assets/OptimalControl/Data/Courses/PrototypeCourse.asset").Chapters[0].Lessons[0].Experiment;
            using (IExperimentSession session = e.Provider.CreateExperimentSession(e))
            {
                IExperimentSnapshot observed = null; using (session.Subscribe(x => observed = x))
                {
                    Assert.That(session.Execute(SimulationCommand.Seek(50)), Is.True); var state = (OptCon.Simulation.SessionState)observed;
                    Assert.That(observed, Is.SameAs(session.Current)); Assert.That(state.Sample.Timestep, Is.EqualTo(50));
                    Assert.That(state.Sample.PhysicalTimeSeconds, Is.EqualTo(1)); Assert.That(state.Sample.DiscretizationStepSeconds, Is.EqualTo(.02));
                    Assert.That(metrics.Resolve(observed, "state.current", FormulaValueKind.Vector, FormulaScope.CurrentTimestep).Value.Vector, Is.SameAs(state.Sample.CurrentState));
                    Assert.That(session.Execute(SimulationCommand.Seek(100)), Is.True);
                    Assert.That(metrics.Resolve(session.Current, "input.applied", FormulaValueKind.Scalar, FormulaScope.CurrentTimestep, component: 0).Validity, Is.EqualTo(MetricValidity.NotApplicable));
                }
            }
        }
        [Test] public void CommonPresentersAndNavigationHaveNoExperimentSpecificTypeOrIdentityBranches()
        {
            foreach (var file in new[] { "Runtime/UI/FormulaPresenter.cs", "Runtime/Course/CourseManager.cs", "Runtime/Visualization/TrajectoryRenderer.cs" })
            {
                var code = File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "OptimalControl", file));
                foreach (var model in new[] { "ObjectiveSession", "ObjectiveResult", "QuadraticObjective", "Pendulum", "fixture.f10", "prototype.pendulum" })
                    Assert.That(code, Does.Not.Contain(model), file + " must dispatch through providers/capabilities, never an experiment implementation or ID.");
            }
        }
    }
}
