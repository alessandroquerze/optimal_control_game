using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;
using SessionState = OptCon.Simulation.SessionState;

namespace OptCon.Tests
{
    public sealed class LessonEvidenceRemediationTests
    {
        private const string A = "Assets/OptimalControl/";
        private CourseDefinition Course => AssetDatabase.LoadAssetAtPath<CourseDefinition>(A + "Data/Courses/OptimalControlCourse.asset");
        private LessonDefinition Definition(string id) => Course.Chapters.Single().Lessons.Single(l => l.Id == id);
        private static void SelectAndAnswer(LessonSession lesson)
        {
            var step = lesson.Presentation.Step;
            foreach (var term in step.VisibleTerms) lesson.RecordTermSelection(step.FormulaId, term);
            var answer = step.Conditions.FirstOrDefault(c => c.EvaluatorId == "answer")?.ExpectedAnswerId ?? step.Answers.FirstOrDefault()?.Id;
            if (answer != null) Assert.That(lesson.Answer(answer), Is.True);
        }
        private static void CorrectAction(LessonSession lesson)
        {
            switch (lesson.Presentation.Step.Id)
            {
                case "state": lesson.Execute(SimulationCommand.SetInitialState(new NumericVector(.35, .5))); break;
                case "intervene": lesson.Execute(SimulationCommand.SetInput(0, new NumericVector(1))); break;
                case "transition": case "time": case "spaces": case "trajectory": case "equilibrium": case "compare-input": lesson.Execute(SimulationCommand.StepForward()); break;
                case "balance": lesson.Execute(SimulationCommand.SetInput(0, new NumericVector(9.81), InputEditScope.Tail)); break;
                case "nominal": lesson.Execute(SimulationCommand.CaptureComparison()); break;
                case "differences": lesson.Execute(SimulationCommand.SetInputPerturbation(0, 0, 1)); break;
                case "initial-perturbed": lesson.Execute(SimulationCommand.SetInitialPerturbation(0, .1)); lesson.Execute(SimulationCommand.StepForward()); break;
                case "amplitude": lesson.Execute(SimulationCommand.SetInitialPerturbation(0, .8)); break;
                case "weight": lesson.Execute(SimulationCommand.SetParameter("cost.q.0", 4)); break;
                case "effort": lesson.Execute(SimulationCommand.SetParameter("cost.r.0", .4)); break;
                case "terminal-weight": lesson.Execute(SimulationCommand.SetParameter("cost.qt.0", 10)); break;
                case "path-limit": lesson.Execute(SimulationCommand.SetParameter("constraint.torque.limit", .5)); break;
                case "cost-not-feasibility": lesson.Execute(SimulationCommand.SetInput(0, new NumericVector(.75), InputEditScope.Tail)); break;
            }
        }
        private static void Reach(LessonSession lesson, string stepId)
        {
            while (lesson.Presentation.Step.Id != stepId)
            { CorrectAction(lesson); SelectAndAnswer(lesson); Assert.That(lesson.Next(), Is.True, lesson.Presentation.Step.Id); }
        }
        private void CheckAdvance(LessonSession lesson, SimulationSession simulation, bool expected)
        {
            var store = new MemoryProgressStore(); var progress = new ProgressionService(Course, store);
            using var binding = new LessonProgressBinding(lesson, simulation, progress, () => true);
            SelectAndAnswer(lesson);
            Assert.That(lesson.Presentation.CanAdvance, Is.EqualTo(expected));
            Assert.That(lesson.Next(), Is.EqualTo(expected));
            Assert.That(store.Load(), Is.Null, "An intermediate or rejected step must not persist completion.");
        }
        [TestCase(.7, 0, false)] [TestCase(.35, 0, false)] [TestCase(.7, .5, false)] [TestCase(.35, .5, true)]
        public void L1RequiresSamePoseAndDifferentVelocity(double theta, double omega, bool expected)
        {
            using var s = Definition("C1.L1").Experiment.Model.CreateSession(Definition("C1.L1").Experiment);
            using var l = new LessonSession(Definition("C1.L1"), s); Reach(l, "state");
            var baseline = l.ActivityBaseline;
            l.Execute(SimulationCommand.SetInitialState(new NumericVector(theta, omega)));
            Assert.That(((SessionState)baseline.Snapshot).Result.Configuration.InitialState.ToArray(), Is.EqualTo(new[] { .35, 0d }));
            CheckAdvance(l, s, expected);
        }
        [TestCase(.05, 1, false)] [TestCase(.1, 1, false)] [TestCase(.8, 2, false)] [TestCase(.8, 1, true)]
        public void L2RequiresGreaterSavedErrorAtAuthoredSampleOnSameNominal(double amplitude, int sample, bool expected)
        {
            using var s = Definition("C1.L2").Experiment.Model.CreateSession(Definition("C1.L2").Experiment);
            using var l = new LessonSession(Definition("C1.L2"), s); Reach(l, "amplitude");
            var baseline = (SessionState)l.ActivityBaseline.Snapshot;
            Assert.That(baseline.Linearization.Errors[1][1], Is.EqualTo(.00003268365389231713).Within(1e-15));
            l.Execute(SimulationCommand.SetInitialPerturbation(0, amplitude)); l.Execute(SimulationCommand.Seek(sample));
            Assert.That(s.State.Linearization.Nominal, Is.SameAs(baseline.Linearization.Nominal));
            if (amplitude == .05) Assert.That(s.State.Linearization.Errors[1][1], Is.EqualTo(.000004086989092912108).Within(1e-15));
            CheckAdvance(l, s, expected);
        }
        [Test] public void L2RecapturedNominalCannotReinterpretTheActivityBaseline()
        {
            using var s = Definition("C1.L2").Experiment.Model.CreateSession(Definition("C1.L2").Experiment);
            using var l = new LessonSession(Definition("C1.L2"), s); Reach(l, "amplitude");
            s.Execute(SimulationCommand.CaptureComparison()); l.Execute(SimulationCommand.SetInitialPerturbation(0, .8));
            CheckAdvance(l, s, false);
        }
        [TestCase(4, false, true)] [TestCase(4, true, false)] [TestCase(3, false, false)] [TestCase(2, false, false)]
        public void L3WeightRequiresFinalAuthoredValueAndRejectsUndo(double value, bool undo, bool expected)
        {
            using var s = Definition("C1.L3").Experiment.Model.CreateSession(Definition("C1.L3").Experiment);
            using var l = new LessonSession(Definition("C1.L3"), s); Reach(l, "weight");
            l.Execute(SimulationCommand.SetParameter("cost.q.0", value));
            if (undo) l.Execute(SimulationCommand.SetParameter("cost.q.0", 2));
            CheckAdvance(l, s, expected);
        }
        [TestCase(2, false, false)] [TestCase(0, false, false)] [TestCase(.75, false, true)] [TestCase(.75, true, false)]
        public void L3RequiresCheaperDifferentRolloutAndRemainingViolationUnderFixedProblem(double input, bool undo, bool expected)
        {
            using var s = Definition("C1.L3").Experiment.Model.CreateSession(Definition("C1.L3").Experiment);
            using var l = new LessonSession(Definition("C1.L3"), s); Reach(l, "cost-not-feasibility");
            var a = (SessionState)l.ActivityBaseline.Snapshot;
            Assert.That(s.State.Comparison.Result, Is.SameAs(a.Result));
            Assert.That(a.Result.Costs.Total, Is.EqualTo(5.294120943466966).Within(1e-12));
            l.Execute(SimulationCommand.SetInput(0, new NumericVector(input), InputEditScope.Tail));
            if (undo) l.Execute(SimulationCommand.SetInput(0, new NumericVector(1), InputEditScope.Tail));
            var b = s.State.Result;
            Assert.That(b.Configuration.Model, Is.SameAs(a.Result.Configuration.Model));
            Assert.That(b.Configuration.Cost, Is.SameAs(a.Result.Configuration.Cost));
            Assert.That(b.Configuration.Reference, Is.SameAs(a.Result.Configuration.Reference));
            Assert.That(b.Configuration.Constraints, Is.SameAs(a.Result.Configuration.Constraints));
            if (!undo && input == .75) { Assert.That(b.Costs.Total, Is.EqualTo(4.655611848439829).Within(1e-12)); Assert.That(b.Constraints.PathResiduals[0][0], Is.EqualTo(.25)); }
            if (input == 2) Assert.That(b.Costs.Total, Is.GreaterThan(a.Result.Costs.Total));
            if (input == 0) { Assert.That(b.Costs.Total, Is.LessThan(a.Result.Costs.Total)); Assert.That(b.Constraints.PathResiduals.All(r => r.All(g => g <= 0)), Is.True); }
            CheckAdvance(l, s, expected);
        }
        [TestCase("cost.q.0", 4)] [TestCase("cost.q.0", 0)] [TestCase("cost.reference.state.0.0", .1)] [TestCase("constraint.torque.limit", .25)]
        public void L3ChangingTheProblemCannotSatisfyFixedCriterionEvidence(string parameter, double value)
        {
            using var s = Definition("C1.L3").Experiment.Model.CreateSession(Definition("C1.L3").Experiment);
            using var l = new LessonSession(Definition("C1.L3"), s); Reach(l, "cost-not-feasibility");
            Assert.That(l.Execute(SimulationCommand.SetParameter(parameter, value)), Is.False, "Guided editing exposes only input here.");
            // Even a non-UI caller cannot reinterpret the frozen baseline under a different problem.
            Assert.That(s.Execute(SimulationCommand.SetParameter(parameter, value)), Is.True);
            l.Execute(SimulationCommand.SetInput(0, new NumericVector(.75), InputEditScope.Tail));
            CheckAdvance(l, s, false);
        }
        [TestCase("C1.L1", "state")] [TestCase("C1.L2", "amplitude")] [TestCase("C1.L3", "weight")] [TestCase("C1.L3", "cost-not-feasibility")]
        public void PreviousRestartResetAndReopenReplaceBaselinesWithoutInheritedCredit(string id, string step)
        {
            var definition = Definition(id); LessonActivityBaseline old;
            using (var s = definition.Experiment.Model.CreateSession(definition.Experiment))
            using (var l = new LessonSession(definition, s))
            {
                Reach(l, step); old = l.ActivityBaseline; CorrectAction(l); SelectAndAnswer(l); Assert.That(l.Next(), Is.True);
                Assert.That(l.Previous(), Is.True); Assert.That(l.Presentation.Step.Id, Is.EqualTo(step));
                Assert.That(l.ActivityBaseline, Is.Not.SameAs(old)); Assert.That(l.ActivityBaseline.Generation, Is.GreaterThan(old.Generation));
                SelectAndAnswer(l); Assert.That(l.Presentation.CanAdvance, Is.False);
                CorrectAction(l); SelectAndAnswer(l); Assert.That(l.Presentation.CanAdvance, Is.True);
                Assert.That(l.Restart(), Is.True); Assert.That(l.Presentation.StepIndex, Is.Zero);
                Assert.That(l.Execute(SimulationCommand.Reset()), Is.True); Assert.That(l.Presentation.StepIndex, Is.Zero);
            }
            using var reopened = definition.Experiment.Model.CreateSession(definition.Experiment);
            using var fresh = new LessonSession(definition, reopened); Reach(fresh, step); SelectAndAnswer(fresh);
            Assert.That(fresh.ActivityBaseline, Is.Not.SameAs(old)); Assert.That(fresh.Presentation.CanAdvance, Is.False);
        }
        [Test] public void EvidenceRejectsStaleGenerationAndWrongActivityAndKeepsRetainedRevision()
        {
            using var s = Definition("C1.L3").Experiment.Model.CreateSession(Definition("C1.L3").Experiment);
            using var l = new LessonSession(Definition("C1.L3"), s); Reach(l, "weight"); var baseline = l.ActivityBaseline;
            l.Execute(SimulationCommand.SetParameter("cost.q.0", 4)); var condition = l.Presentation.Step.Conditions.Single(c => c.Evidence != null);
            var evaluator = new LessonMetricEvidenceEvaluator(new RuntimeMetricResolver());
            Assert.That(evaluator.IsMet(condition, new LessonConditionContext(s.State, Array.Empty<string>(), null, baseline, "weight", baseline.Generation, baseline.Lifetime)), Is.True);
            Assert.That(evaluator.IsMet(condition, new LessonConditionContext(s.State, Array.Empty<string>(), null, baseline, "weight", baseline.Generation + 1, baseline.Lifetime)), Is.False);
            Assert.That(evaluator.IsMet(condition, new LessonConditionContext(s.State, Array.Empty<string>(), null, baseline, "other", baseline.Generation, baseline.Lifetime)), Is.False);
            Assert.That(evaluator.IsMet(condition, new LessonConditionContext(s.State, Array.Empty<string>(), null, baseline, "weight", baseline.Generation, new object())), Is.False);
            Assert.That(baseline.Snapshot.Result.Configuration.Parameters.Single(p => p.Target == "cost.q.0").Value, Is.EqualTo(2));
        }
        [Test] public void ProductionAnswerOrderVariesDeterministicallyAndExpectedIdsArePreserved()
        {
            foreach (var definition in Course.Chapters.SelectMany(c => c.Lessons))
            {
                var questions = definition.Steps.Where(s => s.Conditions.Any(c => c.EvaluatorId == "answer")).ToArray();
                var positions = questions.Select(s => s.Answers.ToList().FindIndex(a => a.Id == s.Conditions.Single(c => c.EvaluatorId == "answer").ExpectedAnswerId)).ToArray();
                Assert.That(positions.All(p => p >= 0), Is.True); Assert.That(positions, Does.Contain(0)); Assert.That(positions.Any(p => p > 0), Is.True, definition.Id);
            }
        }
        [Test] public void EarlierAssessmentVersionsRemainStoredButCannotCreditCorrectedProductionLessons()
        {
            var store = new MemoryProgressStore();
            store.Save(new ProgressDocument(new[] { "C1.L1", "C1.L2", "C1.L3", "C1.L4" }.Select(id => new CompletionRecord("optimal-control", "1", id, "1", 1))));
            var progress = new ProgressionService(Course, store);
            foreach (var id in new[] { "C1.L1", "C1.L2", "C1.L3" })
            { Assert.That(Definition(id).ContentVersion, Is.EqualTo("2")); Assert.That(progress.GetStatus(id).IsCompleted, Is.False); }
            Assert.That(progress.GetStatus("C1.L4").IsCompleted, Is.True);
            Assert.That(store.Load().Completions.Count, Is.EqualTo(4)); Assert.That(store.SaveCount, Is.EqualTo(1));
        }
        [Test] public void FailedRequestCannotReusePositiveEvidenceAndResetInvalidatesItsBaseline()
        {
            using var s = Definition("C1.L3").Experiment.Model.CreateSession(Definition("C1.L3").Experiment);
            using var l = new LessonSession(Definition("C1.L3"), s); Reach(l, "weight"); var baseline = l.ActivityBaseline;
            CorrectAction(l); SelectAndAnswer(l); Assert.That(l.Presentation.CanAdvance, Is.True);
            s.Execute(SimulationCommand.SetParameter("cost.r.0", 0)); Assert.That(s.State.Error, Is.Not.Null);
            CheckAdvance(l, s, false); Assert.That(l.ActivityBaseline, Is.SameAs(baseline));
            Assert.That(l.Execute(SimulationCommand.Reset()), Is.True); Assert.That(s.State.Error, Is.Null);
            Assert.That(l.ActivityBaseline, Is.Null); Assert.That(l.Presentation.StepIndex, Is.Zero);
        }
        private sealed class EvidenceSource : IRuntimeMetricSource
        {
            private readonly SessionMetricSource fallback = new SessionMetricSource();
            public bool TryGet(SimulationResult result, SimulationSnapshot sample, string id, out RuntimeMetric metric)
            {
                if (id != "fixture.velocity") return fallback.TryGet(result, sample, id, out metric);
                metric = RuntimeMetric.Number(id, sample.CurrentState[1], "rad/s", sample.Timestep); return true;
            }
        }
        [Test] public void ComposedMetricResolverIsAlsoUsedByEvidenceValidationAndRuntime()
        {
            var course = UnityEngine.Object.Instantiate(Course); var chapter = UnityEngine.Object.Instantiate(Course.Chapters.Single());
            var lesson = UnityEngine.Object.Instantiate(Definition("C1.L1"));
            try
            {
                var c = new SerializedObject(course); c.FindProperty("chapters").GetArrayElementAtIndex(0).objectReferenceValue = chapter; c.ApplyModifiedPropertiesWithoutUndo();
                var ch = new SerializedObject(chapter); ch.FindProperty("lessons").GetArrayElementAtIndex(0).objectReferenceValue = lesson; ch.ApplyModifiedPropertiesWithoutUndo();
                var l = new SerializedObject(lesson); var conditions = l.FindProperty("steps").GetArrayElementAtIndex(2).FindPropertyRelative("conditions");
                var evidence = conditions.GetArrayElementAtIndex(conditions.arraySize - 1).FindPropertyRelative("evidence");
                evidence.FindPropertyRelative("metricId").stringValue = "fixture.velocity"; evidence.FindPropertyRelative("component").intValue = -1; l.ApplyModifiedPropertiesWithoutUndo();
                var services = new CourseRuntimeServices(new RuntimeMetricResolver(new EvidenceSource()));
                Assert.That(CatalogValidator.ValidateExecutionConfiguration(course, null, false, services).IsValid, Is.True);
                Assert.That(CatalogValidator.ValidateExecutionConfiguration(course, verifySourceFiles: false).IsValid, Is.False);
                using var s = lesson.Experiment.Model.CreateSession(lesson.Experiment);
                using var runtime = new LessonSession(lesson, s, services.Conditions); Reach(runtime, "state");
                runtime.Execute(SimulationCommand.SetInitialState(new NumericVector(.35, .5))); SelectAndAnswer(runtime);
                Assert.That(runtime.Presentation.CanAdvance, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(lesson); UnityEngine.Object.DestroyImmediate(chapter); UnityEngine.Object.DestroyImmediate(course); }
        }
        [TestCase("component", "7")] [TestCase("component", "-1")] [TestCase("unit", "kg")] [TestCase("unit", "")] [TestCase("metricId", "unknown.metric")]
        [TestCase("sampleIndex", "101")] [TestCase("tolerance", "-1")]
        public void CatalogRejectsInvalidEvidenceBinding(string field, string value)
        {
            var course = UnityEngine.Object.Instantiate(Course); var chapter = UnityEngine.Object.Instantiate(Course.Chapters.Single());
            var lesson = UnityEngine.Object.Instantiate(Definition("C1.L1"));
            try
            {
                var c = new SerializedObject(course); c.FindProperty("chapters").GetArrayElementAtIndex(0).objectReferenceValue = chapter; c.ApplyModifiedPropertiesWithoutUndo();
                var ch = new SerializedObject(chapter); ch.FindProperty("lessons").GetArrayElementAtIndex(0).objectReferenceValue = lesson; ch.ApplyModifiedPropertiesWithoutUndo();
                var l = new SerializedObject(lesson); var conditions = l.FindProperty("steps").GetArrayElementAtIndex(2).FindPropertyRelative("conditions");
                var evidence = conditions.GetArrayElementAtIndex(conditions.arraySize - 1).FindPropertyRelative("evidence").FindPropertyRelative(field);
                if (field == "component" || field == "sampleIndex") evidence.intValue = int.Parse(value);
                else if (field == "tolerance") evidence.doubleValue = double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                else evidence.stringValue = value;
                l.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(CatalogValidator.ValidateExecutionConfiguration(course, verifySourceFiles: false).IsValid, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(lesson); UnityEngine.Object.DestroyImmediate(chapter); UnityEngine.Object.DestroyImmediate(course); }
        }
    }
}
