using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class ProgressionTests
    {
        private const string A = "Assets/OptimalControl/";
        private readonly List<Object> owned = new List<Object>();
        private readonly List<IDisposable> disposable = new List<IDisposable>();
        private CourseDefinition course;
        private ChapterDefinition chapter;
        private LessonDefinition first, second;
        private string directory;
        private T Clone<T>(T value) where T : Object { var result = Object.Instantiate(value); owned.Add(result); return result; }
        private static void Set(Object asset, string field, Action<SerializedProperty> change)
        { var so = new SerializedObject(asset); change(so.FindProperty(field)); so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Refs(Object asset, string field, params Object[] values) => Set(asset, field, p =>
        { p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; });
        private static void Text(Object asset, string field, string value) => Set(asset, field, p => p.stringValue = value);
        [SetUp] public void Setup()
        {
            course = Clone(AssetDatabase.LoadAssetAtPath<CourseDefinition>(A + "Data/Courses/PrototypeCourse.asset"));
            chapter = Clone(course.Chapters[0]); first = Clone(chapter.Lessons[0]); second = Clone(first);
            Text(second, "id", "fixture.second");
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"verify\",\"instruction\":\"Verify.\"}]}", first);
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"verify\",\"instruction\":\"Another topic.\"}],\"prerequisiteLessonIds\":[\"prototype.pendulum\"]}", second);
            Refs(chapter, "lessons", first, second); Refs(course, "chapters", chapter);
        }
        [TearDown] public void Cleanup()
        {
            for (int i = disposable.Count - 1; i >= 0; i--) disposable[i].Dispose(); disposable.Clear();
            foreach (var asset in owned) Object.DestroyImmediate(asset); owned.Clear();
            if (directory != null)
            {
                Assert.That(Path.GetFullPath(directory), Does.StartWith(Path.GetFullPath(Path.GetTempPath())));
                Assert.That(Path.GetFileName(directory), Does.StartWith("OptCon-F09-Test-")); Directory.Delete(directory, true); directory = null;
            }
        }
        private LessonSession Open(LessonDefinition definition, ProgressionService progress, out SimulationSession simulation, Func<bool> guard = null)
        {
            simulation = IndexOnlyConfiguration.CreateSession(); disposable.Add(simulation);
            var lesson = new LessonSession(definition, simulation); disposable.Add(lesson);
            disposable.Add(new LessonProgressBinding(lesson, simulation, progress, guard ?? (() => true))); return lesson;
        }
        private void Complete(LessonDefinition definition, ProgressionService progress) { var lesson = Open(definition, progress, out _); Assert.That(lesson.Next(), Is.True); }
        [Test] public void NewProfileHasNoCompletionsAndMissingPrerequisitesRemainExplorable()
        {
            var store = new MemoryProgressStore(); var progress = new ProgressionService(course, store);
            Assert.That(progress.GetStatus(first.Id).IsCompleted, Is.False); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(progress.GetStatus(second.Id).MissingPrerequisiteIds, Is.EqualTo(new[] { first.Id })); Assert.That(progress.GetStatus(second.Id).IsAvailable, Is.True);
        }
        [Test] public void ValidCompletionIsSavedReloadedAndSatisfiesOnlyItsPrerequisite()
        {
            var store = new MemoryProgressStore(); var progress = new ProgressionService(course, store); Complete(first, progress);
            var reload = new ProgressionService(course, store);
            Assert.That(reload.GetStatus(first.Id).IsCompleted, Is.True); Assert.That(reload.GetStatus(second.Id).IsCompleted, Is.False);
            Assert.That(reload.GetStatus(second.Id).MissingPrerequisiteIds, Is.Empty); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [Test] public void PrototypeCompletionDoesNotPromoteProductionLessonOrAnotherCourse()
        {
            var store = new MemoryProgressStore(); Complete(first, new ProgressionService(course, store));
            Text(second, "id", "C1.L1"); Assert.That(new ProgressionService(course, store).GetStatus(second.Id).IsCompleted, Is.False);
            Text(course, "id", "production.course"); Assert.That(new ProgressionService(course, store).GetStatus(first.Id).IsCompleted, Is.False);
        }
        [Test] public void ReorderingLessonsAndChaptersOrRenamingDisplayPreservesProgress()
        {
            var store = new MemoryProgressStore(); Complete(first, new ProgressionService(course, store));
            Refs(chapter, "lessons", second); var another = Clone(chapter); Text(another, "id", "fixture.chapter"); Refs(another, "lessons", first);
            Refs(course, "chapters", another, chapter); Text(first, "title", "Renamed"); Text(course, "contentVersion", "new-catalog-order");
            Assert.That(new ProgressionService(course, store).GetStatus(first.Id).IsCompleted, Is.True);
        }
        [Test] public void NewLessonAddedAfterSavingStartsIncomplete()
        {
            Refs(chapter, "lessons", first); var store = new MemoryProgressStore(); Complete(first, new ProgressionService(course, store));
            Refs(chapter, "lessons", first, second); var progress = new ProgressionService(course, store);
            Assert.That(progress.GetStatus(first.Id).IsCompleted, Is.True); Assert.That(progress.GetStatus(second.Id).IsCompleted, Is.False);
        }
        [TestCase(false)] [TestCase(true)] public void LessonVersionOrSchemaChangeDoesNotCreditOldCompletion(bool schema)
        {
            var store = new MemoryProgressStore(); Complete(first, new ProgressionService(course, store));
            if (schema) Set(first, "schemaVersion", p => p.intValue++); else Text(first, "contentVersion", "2");
            Assert.That(new ProgressionService(course, store).GetStatus(first.Id).IsCompleted, Is.False);
        }
        [Test] public void RemovedAndUnknownIdsAreRetainedWithoutGrantingCredit()
        {
            var store = new MemoryProgressStore(); store.Save(new ProgressDocument(new[] { new CompletionRecord(course.Id, "1", "removed.lesson", "1", 1), new CompletionRecord("future.course", "1", first.Id, "1", 1) }));
            var progress = new ProgressionService(course, store); Assert.That(progress.GetStatus(first.Id).IsCompleted, Is.False); Complete(first, progress);
            Assert.That(store.Load().Completions.Count, Is.EqualTo(3)); Assert.That(store.Load().Completions.Any(r => r.LessonId == "removed.lesson"), Is.True);
        }
        [Test] public void RestartAndRepeatedCompletionDoNotDuplicateSavedRecord()
        {
            var store = new MemoryProgressStore(); var progress = new ProgressionService(course, store); var lesson = Open(first, progress, out _);
            Assert.That(lesson.Next(), Is.True); Assert.That(lesson.Restart(), Is.True); Assert.That(lesson.Next(), Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1)); Assert.That(store.Load().Completions.Count, Is.EqualTo(1));
        }
        [Test] public void UnsatisfiedConditionAndExplorationDoNotProduceProgress()
        {
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"one\",\"instruction\":\"Terminal.\",\"conditions\":[{\"evaluatorId\":\"terminal\"}]}]}", first);
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out _);
            Assert.That(lesson.Next(), Is.False); lesson.Explore(); Assert.That(lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
        }
        [Test] public void FailedEnterAndRepeatedNextNeverSave()
        {
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"one\",\"instruction\":\"One.\"},{\"id\":\"bad\",\"instruction\":\"Bad.\",\"entryActions\":[{\"kind\":4,\"index\":2}]}]}", first);
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out _);
            Assert.That(lesson.Next(), Is.False); Assert.That(lesson.Presentation.Error, Is.Not.Null); Assert.That(lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)] public void ClosedLessonOrSimulationCannotSave(bool closeSimulation)
        {
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out var sim);
            if (closeSimulation) sim.Dispose(); else lesson.Dispose(); Assert.That(lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
        }
        [Test] public void NavigationOrPresentationGuardRejectsCompletionAtBoundary()
        {
            bool active = true; var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out _, () => active);
            lesson.Changed += p => { if (p.IsCompleted) active = false; };
            Assert.That(lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.Zero);
        }
        [Test] public void FailedPresentationPublicationCannotProduceReceipt()
        {
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out _);
            lesson.Changed += p => { if (p.IsCompleted) throw new InvalidOperationException("presentation"); };
            Assert.Throws<InvalidOperationException>(() => lesson.Next()); Assert.That(store.SaveCount, Is.Zero);
        }
        [Test] public void FailingCompletionObserverCancelsQueuedCommit()
        {
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out _);
            lesson.CompletionValidated += _ => throw new InvalidOperationException("completion observer");
            Assert.Throws<InvalidOperationException>(() => lesson.Next()); Assert.That(store.SaveCount, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)] public void DisposalDuringCompletionDispatchCancelsQueuedCommit(bool closeSimulation)
        {
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out var simulation);
            lesson.CompletionValidated += _ => { if (closeSimulation) simulation.Dispose(); else lesson.Dispose(); };
            Assert.That(lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.Zero);
        }
        [Test] public void UnexpectedSimulationConsumerFailureCannotProduceProgress()
        {
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out var simulation);
            simulation.Changed += _ => throw new InvalidOperationException("presentation failed");
            Assert.Throws<SessionPublicationException>(() => simulation.Execute(SimulationCommand.Seek(1)));
            Assert.That(lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
        }
        [Test] public void AF08_04_CorrectingFailedCostUsesRequestedDomainAndThenRestoresInputEditing()
        {
            using (var session = first.Experiment.Model.CreateSession(first.Experiment))
            {
                session.Execute(SimulationCommand.Seek(50)); session.Execute(SimulationCommand.SetParameter("cost.target.0", 1e308)); session.Execute(SimulationCommand.SetHorizon(1));
                Assert.That(session.Execute(SimulationCommand.SetParameter("cost.target.0", 0)), Is.True);
                Assert.That(session.State.Sample.Timestep, Is.EqualTo(1)); Assert.That(session.State.Result.History.Horizon, Is.EqualTo(1));
                Assert.That(session.State.TryGetEditableInput(1, out _), Is.False); Assert.That(session.State.TryGetEditableInput(0, out _), Is.True);
                Assert.That(session.Execute(SimulationCommand.SetInput(0, new NumericVector(3))), Is.True); Assert.That(session.State.Result.Configuration.Controls[0][0], Is.EqualTo(3));
            }
        }
        [Test] public void FailedMathematicalRequestCannotCompleteUntilReset()
        {
            var store = new MemoryProgressStore(); var lesson = Open(first, new ProgressionService(course, store), out var sim);
            Assert.That(sim.Execute(SimulationCommand.SetParameter("missing", 3)), Is.False);
            Assert.That(lesson.Next(), Is.False); Assert.That(store.SaveCount, Is.Zero);
            lesson.Restart(); Assert.That(lesson.Next(), Is.True); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [Test] public void FutureSchemaIsReadOnlyAndNeverOverwritten()
        {
            var store = new MemoryProgressStore(); store.Save(new ProgressDocument(schemaVersion: 99));
            var progress = new ProgressionService(course, store); Complete(first, progress);
            Assert.That(progress.Diagnostic, Does.Contain("read-only")); Assert.That(store.SaveCount, Is.EqualTo(1)); Assert.That(store.Load().SchemaVersion, Is.EqualTo(99));
        }
        private string FilePath() { directory = Path.Combine(Path.GetTempPath(), "OptCon-F09-Test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); return Path.Combine(directory, "progress.json"); }
        [Test] public void FileStoreMissingSaveAndReloadRoundTrip()
        {
            var path = FilePath(); var store = new FileProgressStore(path); Assert.That(store.Load(), Is.Null);
            Complete(first, new ProgressionService(course, store));
            Assert.That(new ProgressionService(course, new FileProgressStore(path)).GetStatus(first.Id).IsCompleted, Is.True);
            Complete(second, new ProgressionService(course, store)); Assert.That(store.Load().Completions.Count, Is.EqualTo(2));
        }
        [TestCase("{\"schemaVersion\":99,\"futureField\":42}")] [TestCase("broken JSON")] [TestCase("{\"schemaVersion\":1}")]
        [TestCase("{\"schemaVersion\":0,\"completions\":[]}")] [TestCase("{\"schemaVersion\":1,\"completions\":[null]}")]
        public void FutureOrCorruptFileIsPreserved(string text)
        {
            var path = FilePath(); File.WriteAllText(path, text); var progress = new ProgressionService(course, new FileProgressStore(path));
            Complete(first, progress); Assert.That(progress.Diagnostic, Is.Not.Null); Assert.That(File.ReadAllText(path), Is.EqualTo(text));
        }
        [Test] public void ProgressDocumentRejectsDuplicateOrNullCompletionRecords()
        {
            var record = new CompletionRecord(course.Id, "1", first.Id, "1", 1);
            Assert.Throws<ArgumentException>(() => new ProgressDocument(new[] { record, record }));
            Assert.Throws<ArgumentException>(() => new ProgressDocument(new CompletionRecord[] { null }));
        }
        private sealed class FailingStore : IProgressStore
        { public ProgressDocument Load() => null; public void Save(ProgressDocument document) => throw new IOException("disk full"); }
        [Test] public void StorageFailureDoesNotClaimSavedProgress()
        {
            var progress = new ProgressionService(course, new FailingStore()); Complete(first, progress);
            Assert.That(progress.GetStatus(first.Id).IsCompleted, Is.False); Assert.That(progress.Diagnostic, Does.Contain("disk full"));
        }
        [TestCase("missing")] [TestCase("cycle")] [TestCase("duplicate")] public void InvalidPrerequisiteGraphIsRejectedByCatalogAndRuntime(string error)
        {
            string ids = error == "missing" ? "[\"missing\"]" : error == "cycle" ? "[\"fixture.second\"]" : "[\"fixture.second\",\"fixture.second\"]";
            JsonUtility.FromJsonOverwrite("{\"prerequisiteLessonIds\":" + ids + "}", first);
            Assert.That(CatalogValidator.ValidateCatalog(course).IsValid, Is.False); Assert.Throws<ArgumentException>(() => new ProgressionService(course, new MemoryProgressStore()));
        }
        [TestCase("id")] [TestCase("contentVersion")] public void MissingIdentityOrVersionCannotCreateProgress(string field)
        { Text(first, field, ""); Assert.Throws<ArgumentException>(() => new ProgressionService(course, new MemoryProgressStore())); Assert.That(CatalogValidator.ValidateCatalog(course).IsValid, Is.False); }
        [TestCase("kg", true)] [TestCase("m", false)] [TestCase("", false)] public void AF08_05_ProviderUnitMustMatch(string unit, bool valid)
        {
            var experiment = Clone(first.Experiment); Set(first, "experiment", p => p.objectReferenceValue = experiment);
            Set(second, "experiment", p => p.objectReferenceValue = experiment);
            Set(experiment, "parameters", p => p.GetArrayElementAtIndex(0).FindPropertyRelative("unit").stringValue = unit);
            var report = CourseContentValidator.Validate(course); Assert.That(report.IsValid, Is.EqualTo(valid), string.Join("\n", report.Issues));
            if (!valid) Assert.Throws<ArgumentException>(() => experiment.Model.CreateConfiguration(experiment));
        }
        [TestCase("unknown")] [TestCase("duplicate")] [TestCase("type")] public void AF08_05_ProviderTargetAndTypeAreValidated(string error)
        {
            var experiment = Clone(first.Experiment); Set(first, "experiment", p => p.objectReferenceValue = experiment);
            Set(second, "experiment", p => p.objectReferenceValue = experiment);
            Set(experiment, "parameters", p => {
                if (error == "type") p.GetArrayElementAtIndex(0).FindPropertyRelative("valueKind").enumValueIndex = 1;
                else p.GetArrayElementAtIndex(0).FindPropertyRelative("modelTarget").stringValue = error == "unknown" ? "missing" : p.GetArrayElementAtIndex(1).FindPropertyRelative("modelTarget").stringValue;
            });
            Assert.That(CourseContentValidator.Validate(course).IsValid, Is.False); Assert.Throws<ArgumentException>(() => experiment.Model.CreateConfiguration(experiment));
        }
        [TestCase(false)] [TestCase(true)] public void ExecutionValidatorRejectsUnknownStepLayerOrParameter(bool parameter)
        {
            JsonUtility.FromJsonOverwrite(parameter
                ? "{\"steps\":[{\"id\":\"one\",\"instruction\":\"One\",\"editableParameterTargets\":[\"missing\"]}]}"
                : "{\"steps\":[{\"id\":\"one\",\"instruction\":\"One\",\"visibleLayerIds\":[\"missing\"]}]}", first);
            var result = CourseContentValidator.Validate(course);
            Assert.That(result.Issues.Any(i => i.Code == (parameter ? "UnknownStepParameter" : "UnknownStepLayer")), Is.True);
        }
        [Test] public void StaticContentDoesNotClaimSupportByCurrentRuntimeHost()
        {
            var experiment = Clone(first.Experiment); Set(first, "experiment", p => p.objectReferenceValue = experiment); Set(second, "experiment", p => p.objectReferenceValue = experiment);
            Set(experiment, "usesDiscreteTime", p => p.boolValue = false);
            Assert.That(CourseContentValidator.Validate(course).Issues.Any(i => i.Code == "UnsupportedRuntimeCapability"), Is.True);
        }
        [Test] public void AF08_04_FailedShortRequestHasNoInputEditDomainAndResetRestoresIt()
        {
            using (var session = first.Experiment.Model.CreateSession(first.Experiment))
            {
                var result = session.State.Result; session.Execute(SimulationCommand.Seek(50));
                Assert.That(session.Execute(SimulationCommand.SetParameter("cost.target.0", 1e308)), Is.False);
                Assert.That(session.Execute(SimulationCommand.SetHorizon(1)), Is.False);
                Assert.That(session.State.RequestedConfiguration.Horizon, Is.EqualTo(1)); Assert.That(session.State.Sample.Timestep, Is.EqualTo(50));
                Assert.That(session.State.TryGetEditableInput(50, out var input), Is.False); Assert.That(input, Is.Null);
                Assert.DoesNotThrow(() => Assert.That(session.Execute(SimulationCommand.SetInput(50, new NumericVector(3), InputEditScope.Tail)), Is.False));
                Assert.That(session.State.Result, Is.SameAs(result)); Assert.That(session.State.InputEditingDiagnostic, Is.Not.Null);
                Assert.That(session.Execute(SimulationCommand.Reset()), Is.True); Assert.That(session.State.TryGetEditableInput(0, out input), Is.True); Assert.That(input[0], Is.Zero);
            }
        }
        private sealed class ExtraMetric : IRuntimeMetricSource
        {
            private readonly SessionMetricSource defaults = new SessionMetricSource();
            public bool TryGet(SimulationResult result, SimulationSnapshot sample, string id, out RuntimeMetric metric)
            { if (id == "fixture.extra") { metric = RuntimeMetric.Number(id, 7, "1", sample.Timestep); return true; } return defaults.TryGet(result, sample, id, out metric); }
        }
        private sealed class ExtraCondition : ILessonConditionEvaluator
        { public bool IsMet(LessonConditionPlan condition, LessonConditionContext context) => context.Session.Sample.Timestep == 1; }
        [Test] public void AF08_06_ValidatorUsesTheSameCustomProviderCompositionAsRuntime()
        {
            var formula = Clone(first.Formulas[0]); Refs(first, "formulas", formula);
            JsonUtility.FromJsonOverwrite("{\"id\":\"fixture.formula\",\"terms\":[{\"id\":\"extra\",\"symbol\":\"v\",\"label\":\"Custom metric\",\"meaning\":\"Provider value\",\"rows\":1,\"columns\":1,\"componentUnits\":[\"1\"],\"value\":{\"metricId\":\"fixture.extra\",\"kind\":0,\"scope\":0,\"component\":-1,\"numericFormat\":\"G6\"}}]}", formula);
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"custom\",\"instruction\":\"Custom condition\",\"conditions\":[{\"evaluatorId\":\"fixture.condition\"}]}]}", first);
            var conditions = new LessonConditionRegistry(); conditions.Register("fixture.condition", new ExtraCondition());
            var services = new CourseRuntimeServices(new RuntimeMetricResolver(new ExtraMetric()), conditions);
            var report = CourseContentValidator.Validate(course, services); Assert.That(report.IsValid, Is.True, string.Join("\n", report.Issues));
            Assert.That(CourseContentValidator.Validate(course).IsValid, Is.False, "Default providers must not silently accept custom metadata.");
            using (var simulation = first.Experiment.Model.CreateSession(first.Experiment)) using (var runtime = new LessonSession(first, simulation, services.Conditions))
            { Assert.That(runtime.Presentation.CanAdvance, Is.False); simulation.Execute(SimulationCommand.Seek(1)); Assert.That(runtime.Presentation.CanAdvance, Is.True); }
        }
    }
}
