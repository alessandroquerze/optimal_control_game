using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class CatalogValidatorTests
    {
        private const string CoursePath = "Assets/OptimalControl/Data/Courses/PrototypeCourse.asset";
        private readonly List<ContentDefinition> owned = new List<ContentDefinition>();
        private CourseDefinition course;
        private ChapterDefinition chapter;
        private LessonDefinition lesson;
        private ExperimentDefinition experiment;
        private FormulaDefinition formula;
        private SourceDocumentDefinition[] sources;
        private string fixtureFolder;
        private string sourceFixtureFolder;

        [SetUp]
        public void SetUp()
        {
            var original = AssetDatabase.LoadAssetAtPath<CourseDefinition>(CoursePath);
            Assert.That(original, Is.Not.Null, "Prototype assets must be imported before running F01 tests.");
            course = Clone(original);
            chapter = Clone(original.Chapters[0]);
            lesson = Clone(chapter.Lessons[0]);
            experiment = Clone(lesson.Experiment);
            formula = Clone(lesson.Formulas[0]);
            sources = original.Sources.Select(Clone).ToArray();
            References(course, "sources", sources);
            References(course, "chapters", chapter);
            References(chapter, "lessons", lesson);
            Reference(lesson, "experiment", experiment);
            References(lesson, "formulas", formula);
            // These legacy fixtures replace the experiment/formula graph; they test catalog contracts, not the production guided sequence.
            Edit(lesson, "steps", p => p.arraySize = 0);
            foreach (var asset in new ContentDefinition[] { chapter, lesson, experiment, formula }) RewireSources(asset);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(fixtureFolder)) AssetDatabase.DeleteAsset(fixtureFolder);
            if (!string.IsNullOrEmpty(sourceFixtureFolder))
            {
                string docsRoot = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Docs") + Path.DirectorySeparatorChar;
                string resolved = Path.GetFullPath(sourceFixtureFolder);
                if (!resolved.StartsWith(docsRoot, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("__OptConF01Tests_", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected PDF fixture cleanup target.");
                Directory.Delete(resolved, true);
            }
            foreach (var asset in owned)
                if (asset != null && !EditorUtility.IsPersistent(asset)) Object.DestroyImmediate(asset);
            owned.Clear();
            fixtureFolder = null;
            sourceFixtureFolder = null;
        }

        [Test]
        public void PrototypeCatalogAndExecutionConfigurationAreValid()
        {
            var original = AssetDatabase.LoadAssetAtPath<CourseDefinition>(CoursePath);
            AssertValid(CatalogValidator.ValidateCatalog(original));
            var execution = CatalogValidator.ValidateExecutionConfiguration(original);
            AssertValid(execution);
            Assert.That(original.IsPrototype, Is.True);
            Assert.That(original.Chapters.SelectMany(c => c.Lessons).Single().Id, Is.EqualTo("prototype.pendulum"));
            Assert.That(original.Sources.Select(s => s.PhysicalPageCount), Is.EquivalentTo(new[] { 36, 81, 20 }));
            Assert.That(original.Sources.Select(s => s.DocumentDate), Is.EquivalentTo(new[] { "2026-09-14", "2026-09-14", "2026-10-08" }));
        }

        [Test]
        public void DuplicateIdsAcrossAssetTypesAreRejected()
        {
            Edit(chapter, "id", p => p.stringValue = course.Id);
            HasIssue("DuplicateId");
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("chapter/lesson")]
        public void InvalidStableIdsAreRejected(string id)
        {
            Edit(lesson, "id", p => p.stringValue = id);
            HasIssue("InvalidId");
        }

        [Test]
        public void UnknownPrerequisiteIsRejected()
        {
            Strings(lesson, "prerequisiteLessonIds", "future.lesson.missing");
            HasIssue("MissingPrerequisite");
        }

        [Test]
        public void PrerequisiteCycleIsRejected()
        {
            var next = Clone(lesson);
            Edit(next, "id", p => p.stringValue = "future.lesson");
            Strings(lesson, "prerequisiteLessonIds", next.Id);
            Strings(next, "prerequisiteLessonIds", lesson.Id);
            References(chapter, "lessons", lesson, next);
            HasIssue("PrerequisiteCycle");
        }

        [Test]
        public void UnregisteredSourceIsRejectedEvenWithTheSameSourceId()
        {
            var unregistered = Clone(sources[0]);
            Edit(lesson, "sources.Array.data[0].document", p => p.objectReferenceValue = unregistered);
            HasIssue("UnknownSource");
        }

        [TestCase(0, 1)]
        [TestCase(2, 1)]
        [TestCase(1, 37)]
        public void PhysicalPageBoundsIncludeTheCover(int first, int last)
        {
            Edit(lesson, "sources.Array.data[0].firstPhysicalPage", p => p.intValue = first);
            Edit(lesson, "sources.Array.data[0].lastPhysicalPage", p => p.intValue = last);
            HasIssue("SourcePageRange");
        }

        [Test]
        public void SourceBytesMustMatchTheRegisteredVersion()
        {
            Edit(sources[0], "sha256", p => p.stringValue = new string('0', 64));
            Assert.That(CatalogValidator.ValidateCatalog(course).Issues.Any(i => i.Code == "SourceHashMismatch"), Is.True);
        }

        [Test]
        public void MissingPdfIsRejected()
        {
            Edit(sources[0], "projectRelativePath", p => p.stringValue = "Docs/CourseSources/missing.pdf");
            Assert.That(CatalogValidator.ValidateCatalog(course).Issues.Any(i => i.Code == "MissingSourceFile"), Is.True);
        }

        [TestCase("../outside.pdf")]
        [TestCase("Docs/../Assets/outside.pdf")]
        [TestCase("C:/outside.pdf")]
        public void SourcePathMustStayInsideDocs(string path)
        {
            Edit(sources[0], "projectRelativePath", p => p.stringValue = path);
            HasIssue("SourcePath");
        }

        [Test]
        public void MissingWorkspaceSceneIsRejected()
        {
            Edit(lesson, "workspace.assetPath", p => p.stringValue = "Assets/Scenes/Unknown.unity");
            HasIssue("MissingWorkspaceScene");
        }

        [Test]
        public void DisabledWorkspaceSceneIsRejected()
        {
            var original = EditorBuildSettings.scenes;
            try
            {
                EditorBuildSettings.scenes = original.Select(s => new EditorBuildSettingsScene(s.path, s.path != lesson.Workspace.AssetPath && s.enabled)).ToArray();
                HasIssue("WorkspaceNotInBuild");
            }
            finally { EditorBuildSettings.scenes = original; }
        }

        [Test]
        public void InvalidParameterRangeAndNonFiniteDefaultAreRejected()
        {
            Edit(experiment, "parameters.Array.data[0].defaultValue", p => p.doubleValue = double.NaN);
            HasIssue("ParameterRange");
        }

        [Test]
        public void InitialStateMustMatchTheDeclaredDimensions()
        {
            Edit(experiment, "discreteTime.initialState", p => p.arraySize = 1);
            HasIssue("PresetVector");
        }

        [Test]
        public void DescriptorCannotPretendToBeANumericalValue()
        {
            Edit(formula, "terms.Array.data[2].rows", p => p.intValue = 1);
            HasIssue("DescriptorShape");
        }

        [Test]
        public void ValueAndHighlightMustUseTheSameScope()
        {
            Edit(formula, "terms.Array.data[3].highlight.scope", p => p.enumValueIndex = (int)FormulaScope.Terminal);
            HasIssue("BindingScope");
        }

        [Test]
        public void RuntimeOverridesCannotModifyAuthoredPresetArrays()
        {
            var state = experiment.DiscreteTime.CopyInitialState();
            var input = experiment.DiscreteTime.CopyDefaultControl();
            var target = experiment.DiscreteTime.CopyReferenceState();
            state[0] = 999;
            input[0] = 999;
            target[0] = 999;
            Assert.That(experiment.DiscreteTime.InitialState[0], Is.EqualTo(0.35));
            Assert.That(experiment.DiscreteTime.DefaultControl[0], Is.Zero);
            Assert.That(experiment.DiscreteTime.ReferenceState[0], Is.Zero);
        }

        [Test]
        public void AnotherChapterAndSourceNeedOnlyNewDataAndListEntries()
        {
            var newSource = Clone(sources[0]);
            Edit(newSource, "id", p => p.stringValue = "future.source.notes");
            Edit(newSource, "title", p => p.stringValue = "Additional PDF fixture");
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            sourceFixtureFolder = Path.Combine(projectRoot, "Docs", "__OptConF01Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sourceFixtureFolder);
            File.Copy(Path.Combine(projectRoot, sources[0].ProjectRelativePath), Path.Combine(sourceFixtureFolder, "FutureNotes.pdf"));
            Edit(newSource, "projectRelativePath", p => p.stringValue = "Docs/" + Path.GetFileName(sourceFixtureFolder) + "/FutureNotes.pdf");
            var newChapter = Clone(chapter);
            var newLesson = Clone(lesson);
            Edit(newChapter, "id", p => p.stringValue = "future.chapter");
            Edit(newLesson, "id", p => p.stringValue = "future.lesson");
            Edit(newChapter, "sources.Array.data[0].document", p => p.objectReferenceValue = newSource);
            Edit(newLesson, "sources.Array.data[0].document", p => p.objectReferenceValue = newSource);
            References(newChapter, "lessons", newLesson);
            References(course, "sources", sources.Concat(new[] { newSource }).ToArray());
            References(course, "chapters", chapter, newChapter);
            AssertValid(CatalogValidator.ValidateCatalog(course));
            string[] ids = course.Chapters.Select(c => c.Id).ToArray();
            References(course, "chapters", newChapter, chapter);
            AssertValid(CatalogValidator.ValidateCatalog(course));
            Assert.That(course.Chapters.Select(c => c.Id), Is.EquivalentTo(ids));
            Assert.That(newLesson.Experiment, Is.SameAs(lesson.Experiment), "Shared experiment does not need a duplicate definition.");
        }

        [Test]
        public void AStaticExperimentDoesNotNeedAnArtificialTimeline()
        {
            Edit(experiment, "usesDiscreteTime", p => p.boolValue = false);
            Edit(experiment, "stateDimension", p => p.intValue = 0);
            Edit(experiment, "controlDimension", p => p.intValue = 0);
            Edit(experiment, "outputDimension", p => p.intValue = 0);
            Strings(experiment, "stateUnits");
            Strings(experiment, "controlUnits");
            Strings(experiment, "outputUnits");
            Edit(experiment, "discreteTime.horizon", p => p.intValue = 0);
            Edit(experiment, "discreteTime.stepSeconds", p => p.doubleValue = 0);
            AssertValid(CatalogValidator.ValidateCatalog(course, verifySourceFiles: false));
        }

        [Test]
        public void IndexOnlyDiscreteCatalogProducesFiveWithoutPhysicalStepOrTime()
        {
            ConfigureIndexOnlyScalarExperiment();
            AssertValid(CatalogValidator.ValidateCatalog(course));
            var preset = experiment.DiscreteTime;
            Assert.That(experiment.UsesDiscreteTime, Is.True);
            Assert.That(preset.HasPhysicalTimeStep, Is.False);
            Assert.That(preset.StepSeconds, Is.Null);
            var controls = Enumerable.Repeat(new NumericVector(preset.CopyDefaultControl()), preset.Horizon).ToArray();
            var history = RolloutEngine.Rollout(new AdditionDynamics(preset.StepSeconds),
                new NumericVector(preset.CopyInitialState()), controls, experiment.Id);
            Assert.That(history.Horizon, Is.EqualTo(1));
            Assert.That(history.States[1][0], Is.EqualTo(5));
            for (int t = 0; t <= history.Horizon; t++)
            {
                Assert.That(history.GetSnapshot(t).DiscretizationStepSeconds, Is.Null);
                Assert.That(history.GetSnapshot(t).PhysicalTimeSeconds, Is.Null);
            }
        }

        [TestCase(0)]
        [TestCase(-0.02)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void AnAbsentPhysicalStepDoesNotExposeOrValidateItsInactivePayload(double inactiveValue)
        {
            ConfigureIndexOnlyScalarExperiment();
            Edit(experiment, "discreteTime.stepSeconds", p => p.doubleValue = inactiveValue);
            AssertValid(CatalogValidator.ValidateCatalog(course, verifySourceFiles: false));
            Assert.That(experiment.DiscreteTime.StepSeconds, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void IndexOnlyDynamicsStillRequirePositiveHorizon(int horizon)
        {
            ConfigureIndexOnlyScalarExperiment();
            Edit(experiment, "discreteTime.horizon", p => p.intValue = horizon);
            HasIssue("TimePreset");
        }

        [TestCase("initialState")]
        [TestCase("defaultControl")]
        public void IndexOnlyDynamicsStillValidateVectorDimensions(string field)
        {
            ConfigureIndexOnlyScalarExperiment();
            Edit(experiment, "discreteTime." + field, p => p.arraySize = 0);
            HasIssue("PresetVector");
        }

        [TestCase("initialState", double.NaN)]
        [TestCase("defaultControl", double.PositiveInfinity)]
        public void IndexOnlyDynamicsStillRejectNonFiniteVectors(string field, double value)
        {
            ConfigureIndexOnlyScalarExperiment();
            Edit(experiment, "discreteTime." + field + ".Array.data[0]", p => p.doubleValue = value);
            HasIssue("PresetVector");
        }

        [Test]
        public void IndexOnlyDynamicsStillRequirePositiveStateDimension()
        {
            ConfigureIndexOnlyScalarExperiment();
            Edit(experiment, "stateDimension", p => p.intValue = 0);
            HasIssue("ModelDimensions");
        }

        [TestCase(0)]
        [TestCase(-0.02)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void ADeclaredPhysicalStepMustBePositiveAndFinite(double value)
        {
            Edit(experiment, "discreteTime.hasPhysicalTimeStep", p => p.boolValue = true);
            Edit(experiment, "discreteTime.stepSeconds", p => p.doubleValue = value);
            HasIssue("TimePreset");
        }

        [TestCase(false, 0.02)]
        [TestCase(true, 0)]
        [TestCase(true, -0.02)]
        [TestCase(true, double.NaN)]
        [TestCase(true, double.PositiveInfinity)]
        [TestCase(true, double.NegativeInfinity)]
        public void EulerPendulumRejectsMissingOrInvalidPhysicalStep(bool present, double value)
        {
            Edit(experiment, "discreteTime.hasPhysicalTimeStep", p => p.boolValue = present);
            Edit(experiment, "discreteTime.stepSeconds", p => p.doubleValue = value);
            Assert.That(() => PendulumRolloutDiagnostic.Generate(experiment), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ExistingPendulumPresetPreservesPhysicalMeaningAndEveryRolloutSample()
        {
            var original = AssetDatabase.LoadAssetAtPath<CourseDefinition>(CoursePath).Chapters[0].Lessons[0].Experiment;
            string before = EditorJsonUtility.ToJson(original);
            Assert.That(original.DiscreteTime.HasPhysicalTimeStep, Is.True);
            Assert.That(original.DiscreteTime.StepSeconds, Is.EqualTo(0.02));
            var actual = PendulumRolloutDiagnostic.Generate(original);
            var expected = RolloutEngine.Rollout(new PendulumDynamics(new PendulumParameters(1, 1, 9.81), 0.02),
                new NumericVector(0.35, 0), Enumerable.Repeat(new NumericVector(0), 100).ToArray(), original.Id);
            Assert.That(actual.Horizon, Is.EqualTo(100));
            Assert.That(actual.GetSnapshot(100).PhysicalTimeSeconds, Is.EqualTo(2));
            for (int t = 0; t <= actual.Horizon; t++)
                Assert.That(actual.States[t].ToArray(), Is.EqualTo(expected.States[t].ToArray()), "Preserved pendulum sample " + t);
            Assert.That(EditorJsonUtility.ToJson(original), Is.EqualTo(before));
        }

        [TestCase(false, 0.02)]
        [TestCase(true, 0)]
        [TestCase(true, -0.02)]
        [TestCase(true, double.NaN)]
        [TestCase(true, double.PositiveInfinity)]
        [TestCase(true, double.NegativeInfinity)]
        public void ConcreteProviderRejectsMissingOrInvalidPhysicalStep(bool present, double value)
        {
            Edit(experiment, "discreteTime.hasPhysicalTimeStep", p => p.boolValue = present);
            Edit(experiment, "discreteTime.stepSeconds", p => p.doubleValue = value);
            Assert.That(() => experiment.Model.CreateConfiguration(experiment), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void LegacyPresetWithoutPresenceFlagImportsAsPhysicalTime()
        {
            fixtureFolder = "Assets/__OptConF01Tests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(fixtureFolder));
            string originalPath = AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<CourseDefinition>(CoursePath).Chapters[0].Lessons[0].Experiment);
            string legacyYaml = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(originalPath),
                @"(?m)^\s*hasPhysicalTimeStep:.*\r?\n", string.Empty);
            string fixturePath = fixtureFolder + "/LegacyPreset.asset";
            File.WriteAllText(fixturePath, legacyYaml);
            AssetDatabase.ImportAsset(fixturePath, ImportAssetOptions.ForceSynchronousImport);
            var legacy = AssetDatabase.LoadAssetAtPath<ExperimentDefinition>(fixturePath);
            Assert.That(legacy, Is.Not.Null);
            Assert.That(legacy.DiscreteTime.HasPhysicalTimeStep, Is.True);
            Assert.That(legacy.DiscreteTime.StepSeconds, Is.EqualTo(0.02));
            Assert.That(PendulumRolloutDiagnostic.Generate(legacy).GetSnapshot(100).PhysicalTimeSeconds, Is.EqualTo(2));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NativeAssetRoundTripPreservesGraphUnicodeDoubleDefaultsAndStableIds(bool physicalTime)
        {
            if (!physicalTime) ConfigureIndexOnlyScalarExperiment();
            fixtureFolder = "Assets/__OptConF01Tests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(fixtureFolder));
            string savedCoursePath = null;
            // Persist dependencies before their owners so Unity writes asset GUIDs,
            // rather than references to objects that are still transient.
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                string path = fixtureFolder + "/" + i + ".asset";
                AssetDatabase.CreateAsset(owned[i], path);
                if (owned[i] == course) savedCoursePath = path;
            }
            AssetDatabase.SaveAssets();
            string stableId = course.Id;
            string movedPath = fixtureFolder + "/RenamedCourse.asset";
            Assert.That(AssetDatabase.MoveAsset(savedCoursePath, movedPath), Is.Empty);
            AssetDatabase.SaveAssets();
            foreach (var asset in owned) Resources.UnloadAsset(asset);
            AssetDatabase.ImportAsset(movedPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<CourseDefinition>(movedPath);
            Assert.That(reloaded.Id, Is.EqualTo(stableId));
            var reloadedLesson = reloaded.Chapters[0].Lessons[0];
            Assert.That(reloadedLesson.Experiment.DiscreteTime.HasPhysicalTimeStep, Is.EqualTo(physicalTime));
            Assert.That(reloadedLesson.Experiment.DiscreteTime.StepSeconds, Is.EqualTo(physicalTime ? (double?)0.02 : null));
            Assert.That(reloadedLesson.Experiment.DiscreteTime.InitialState[0], Is.EqualTo(physicalTime ? 0.35 : 2));
            Assert.That(reloadedLesson.Formulas[0].Terms[0].Symbol, Is.EqualTo("x_t"));
            Assert.That(reloadedLesson.Formulas[0].Terms[0].Meaning, Does.Contain(physicalTime ? "θ" : "scalare"));
            Assert.That(reloadedLesson.Experiment.StateUnits, Is.EqualTo(physicalTime ? new[] { "rad", "rad/s" } : new[] { "1" }));
            Assert.That(reloadedLesson.Sources[0].Document, Is.SameAs(reloaded.Sources[0]));
            AssertValid(CatalogValidator.ValidateCatalog(reloaded));
        }

        [Test]
        public void AssemblyBoundariesKeepCorePureAndContentIndependentOfRuntimeConsumers()
        {
            Assert.That(typeof(ModelDimensions).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity", StringComparison.Ordinal)), Is.False);
            var forbidden = new[] { "UnityEditor", "OptCon.Application", "OptCon.Presentation" };
            Assert.That(typeof(CourseDefinition).Assembly.GetReferencedAssemblies().Any(a => forbidden.Any(f => a.Name.StartsWith(f, StringComparison.Ordinal))), Is.False);
        }

        private T Clone<T>(T asset) where T : ContentDefinition
        {
            var copy = Object.Instantiate(asset);
            owned.Add(copy);
            return copy;
        }

        private void ConfigureIndexOnlyScalarExperiment()
        {
            Edit(experiment, "discreteTime.hasPhysicalTimeStep", p => p.boolValue = false);
            Edit(experiment, "discreteTime.stepSeconds", p => p.doubleValue = 0);
            Edit(experiment, "discreteTime.horizon", p => p.intValue = 1);
            Edit(experiment, "stateDimension", p => p.intValue = 1);
            Edit(experiment, "outputDimension", p => p.intValue = 0);
            Strings(experiment, "stateUnits", "1");
            Strings(experiment, "controlUnits", "1");
            Strings(experiment, "outputUnits");
            Edit(experiment, "discreteTime.initialState", p => { p.arraySize = 1; p.GetArrayElementAtIndex(0).doubleValue = 2; });
            Edit(experiment, "discreteTime.defaultControl.Array.data[0]", p => p.doubleValue = 3);
            Edit(experiment, "discreteTime.referenceState", p => p.arraySize = 0);
            Edit(experiment, "parameters", p => p.arraySize = 0);
            Edit(formula, "assumptions", p => p.stringValue = "Dinamica scalare discreta senza tempo fisico: x_next = x + u.");
            foreach (int term in new[] { 0, 1, 3 })
            {
                Edit(formula, "terms.Array.data[" + term + "].rows", p => p.intValue = 1);
                Strings(formula, "terms.Array.data[" + term + "].componentUnits", "1");
                Edit(formula, "terms.Array.data[" + term + "].meaning", p => p.stringValue = "Valore scalare adimensionale.");
            }
        }

        // Fixture only: integration of authored optional time with the existing pure rollout API.
        private sealed class AdditionDynamics : IDiscreteDynamics
        {
            public ModelDescriptor Descriptor { get; }
            public AdditionDynamics(double? physicalStep) => Descriptor = new ModelDescriptor("fixture.addition", "1",
                new ModelDimensions(1, 1, 0), new[] { "1" }, new[] { "1" }, Array.Empty<string>(), physicalStep);
            public NumericVector Evaluate(int t, NumericVector x, NumericVector u) => new NumericVector(x[0] + u[0]);
        }

        private void RewireSources(ContentDefinition owner)
        {
            var serialized = new SerializedObject(owner);
            var references = serialized.FindProperty("sources");
            for (int i = 0; i < references.arraySize; i++)
            {
                var document = references.GetArrayElementAtIndex(i).FindPropertyRelative("document");
                var original = (SourceDocumentDefinition)document.objectReferenceValue;
                document.objectReferenceValue = sources.Single(s => s.Id == original.Id);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void HasIssue(string code) => Assert.That(CatalogValidator.ValidateCatalog(course, verifySourceFiles: false).Issues.Any(i => i.Code == code), Is.True, code);
        private static void AssertValid(CatalogValidationResult result) => Assert.That(result.IsValid, Is.True, string.Join("\n", result.Issues.Select(i => i.ToString())));

        private static void Edit(Object owner, string path, Action<SerializedProperty> change)
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(path);
            Assert.That(property, Is.Not.Null, path);
            change(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Reference(Object owner, string path, Object value) => Edit(owner, path, p => p.objectReferenceValue = value);
        private static void References(Object owner, string path, params Object[] values) => Edit(owner, path, p =>
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });
        private static void Strings(Object owner, string path, params string[] values) => Edit(owner, path, p =>
        {
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
        });
    }
}
