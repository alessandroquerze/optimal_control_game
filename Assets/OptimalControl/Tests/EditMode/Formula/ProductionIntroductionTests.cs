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

namespace OptCon.Tests
{
    public sealed class ProductionIntroductionTests
    {
        private const string A = "Assets/OptimalControl/";
        private CourseDefinition Course => AssetDatabase.LoadAssetAtPath<CourseDefinition>(A + "Data/Courses/OptimalControlCourse.asset");
        private LessonDefinition Definition => Course.Chapters.Single().Lessons.Single(l => l.Id == "C1.L1");
        private SimulationSession Open() => Definition.Experiment.Model.CreateSession(Definition.Experiment);
        private readonly RuntimeMetricResolver metrics = new RuntimeMetricResolver();
        private static void Select(LessonSession lesson, string term) => lesson.RecordTermSelection(lesson.Presentation.Step.FormulaId, term);
        private static void Advance(LessonSession lesson, string answer)
        { Assert.That(lesson.Answer(answer), Is.True); Assert.That(lesson.Next(), Is.True, lesson.Presentation.Step.Id); }
        // Actions below are student actions, never direct assignments to the educational state.
        private static void Complete(LessonSession lesson)
        {
            Advance(lesson, "robotics"); Advance(lesson, "both");
            Select(lesson, "state.current");
            Assert.That(lesson.Execute(SimulationCommand.SetInitialState(new NumericVector(.35, .5))), Is.True);
            Advance(lesson, "full");
            Select(lesson, "output.current"); Advance(lesson, "angle");
            Select(lesson, "derivative"); Advance(lesson, "rate");
            Advance(lesson, "velocity");
            Select(lesson, "input.applied");
            Assert.That(lesson.Execute(SimulationCommand.SetInput(0, new NumericVector(1))), Is.True);
            Advance(lesson, "old");
            Select(lesson, "state.next"); lesson.Execute(SimulationCommand.StepForward()); Advance(lesson, "saved");
            Select(lesson, "time.physical"); lesson.Execute(SimulationCommand.StepForward()); Advance(lesson, "index");
            Select(lesson, "state.current"); lesson.Execute(SimulationCommand.StepForward()); Advance(lesson, "time");
            lesson.Execute(SimulationCommand.Seek(100)); Select(lesson, "input.applied"); Advance(lesson, "terminal");
        }
        [Test] public void ProductionGraphAndSavedBootstrapExposeOnlyIndependentC1L1()
        {
            Assert.That(Course.Id, Is.EqualTo("optimal-control")); Assert.That(Course.IsPrototype, Is.False);
            Assert.That(Course.Chapters.Single().Id, Is.EqualTo("C1")); Assert.That(Definition.Id, Is.EqualTo("C1.L1"));
            Assert.That(Definition.ContentVersion, Is.EqualTo("2")); Assert.That(Definition.PrerequisiteLessonIds, Is.Empty);
            Assert.That(Definition.Experiment.Id, Is.EqualTo("c1.introduction.pendulum")); Assert.That(Definition.Experiment.Cost, Is.Null);
            Assert.That(Definition.Experiment, Is.Not.SameAs(AssetDatabase.LoadAssetAtPath<ExperimentDefinition>(A + "Data/Experiments/PendulumPrototype.asset")));
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Course));
            Assert.That(File.ReadAllText(A + "Scenes/Bootstrap.unity"), Does.Contain("course: {fileID: 11400000, guid: " + guid));
            Assert.That(Definition.Steps.Select(s => s.Id), Is.EqualTo(new[] { "context", "purpose", "state", "output", "continuous", "predict", "intervene", "transition", "time", "spaces", "horizon" }));
        }
        [Test] public void ProductionCatalogAndAllRuntimeBindingsPassExecutionValidation()
        {
            var result = CatalogValidator.ValidateExecutionConfiguration(Course, verifySourceFiles: true);
            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Issues));
        }
        [Test] public void ConcreteEulerIsDisclosedBeforePredictionWithoutInputEditingOrCostLayers()
        {
            var prediction = Definition.Steps.Single(s => s.Id == "predict");
            Assert.That(prediction.FormulaFocus.Id, Is.EqualTo("dynamics.pendulum.explicit-euler"));
            Assert.That(prediction.FormulaFocus.Expression, Does.Contain("sin"));
            Assert.That(prediction.VisibleTermIds, Does.Contain("theta.next")); Assert.That(prediction.VisibleTermIds, Does.Contain("omega.next"));
            Assert.That(prediction.EnabledControlIds, Does.Not.Contain(LessonControls.Input));
            Assert.That(Definition.Steps.All(s => !s.ShowCosts && !s.VisibleLayerIds.Contains("cost") && s.EditableParameterTargets.Count == 0), Is.True);
            Assert.That(Definition.Steps.Single(s => s.Id == "continuous").FormulaFocus.Id, Is.EqualTo("c1.continuous-system"));
        }
        [Test] public void PendulumEulerKnownAnswerAndFirstClassOutputDistinguishStateInputOutput()
        {
            using var s = Open(); s.Execute(SimulationCommand.SetInitialState(new NumericVector(0, 0)));
            s.Execute(SimulationCommand.SetInput(0, new NumericVector(1)));
            var r = s.State.Result;
            Assert.That(r.History.States[1].ToArray(), Is.EqualTo(new[] { 0d, .02d }).Within(1e-12));
            Assert.That(r.Outputs.Samples[0].ToArray(), Is.EqualTo(new[] { 0d }));
            Assert.That(r.Outputs.Samples[1].ToArray(), Is.EqualTo(new[] { 0d }));
            s.Execute(SimulationCommand.StepForward()); s.Execute(SimulationCommand.StepForward());
            Assert.That(s.State.Result.History.States[2][0], Is.EqualTo(.0004).Within(1e-12));
            Assert.That(s.State.Result.Outputs.Samples[2][0], Is.EqualTo(.0004).Within(1e-12));
        }
        [Test] public void SameAngleDifferentVelocityHasSameOutputAndDifferentFuture()
        {
            using var s = Open(); var a = s.State.Result;
            s.Execute(SimulationCommand.SetInitialState(new NumericVector(.35, .5))); var b = s.State.Result;
            Assert.That(a.Outputs.Samples[0][0], Is.EqualTo(b.Outputs.Samples[0][0]));
            Assert.That(a.History.States[1][0], Is.Not.EqualTo(b.History.States[1][0]));
            Assert.That(a.Outputs.Samples[1][0], Is.EqualTo(.35)); Assert.That(b.Outputs.Samples[1][0], Is.EqualTo(.36).Within(1e-12));
        }
        [TestCase(-1, FormulaValueKind.Vector, MetricValidity.Available)]
        [TestCase(0, FormulaValueKind.Scalar, MetricValidity.Available)]
        [TestCase(-1, FormulaValueKind.Scalar, MetricValidity.Invalid)]
        [TestCase(1, FormulaValueKind.Scalar, MetricValidity.Invalid)]
        public void OutputTypingIsStrictEvenWhenVectorHasOneComponent(int component, FormulaValueKind kind, MetricValidity validity)
        {
            using var s = Open(); var r = metrics.Resolve(s.State.Result, 0, RuntimeMetricIds.Output, kind, component: component);
            Assert.That(r.Validity, Is.EqualTo(validity));
            if (validity == MetricValidity.Invalid) Assert.That(r.Diagnostic, Is.Not.Empty);
            else { Assert.That(r.SampleIndex, Is.Zero); Assert.That(r.Value.Units, Is.EqualTo(new[] { "rad" })); Assert.That(r.Result, Is.SameAs(s.State.Result)); }
        }
        [Test] public void OutputChecksUnitsScopeAndSampleIndexAndRemainsAvailableAtTerminal()
        {
            using var s = Open(); var r = s.State.Result;
            Assert.That(metrics.Resolve(r, 5, RuntimeMetricIds.Output, FormulaValueKind.Scalar, component: 0, expectedUnits: new[] { "kg" }).Validity, Is.EqualTo(MetricValidity.Invalid));
            Assert.That(metrics.Resolve(r, 5, RuntimeMetricIds.Output, FormulaValueKind.Vector, FormulaScope.NextTimestep).Validity, Is.EqualTo(MetricValidity.Invalid));
            var y = metrics.Resolve(r, 5, RuntimeMetricIds.Output, FormulaValueKind.Scalar, component: 0);
            Assert.That(y.SampleIndex, Is.EqualTo(5)); Assert.That(y.Value.Scalar, Is.EqualTo(r.History.States[5][0]));
            var terminal = metrics.Resolve(r, 0, RuntimeMetricIds.Output, FormulaValueKind.Vector, FormulaScope.Terminal);
            Assert.That(terminal.SampleIndex, Is.EqualTo(100)); Assert.That(terminal.Value.Vector, Is.SameAs(r.Outputs.Samples[100]));
            Assert.That(metrics.Resolve(r, 100, SimulationMetricIds.AppliedInput, FormulaValueKind.Vector).Validity, Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(metrics.Resolve(r, 100, SimulationMetricIds.NextState, FormulaValueKind.Vector, FormulaScope.NextTimestep).Validity, Is.EqualTo(MetricValidity.NotApplicable));
        }
        [Test] public void OutputAndCountsStayWithResultAAfterRevisionBAndFailedRequest()
        {
            using var s = Open(); var a = s.State.Result; var y = a.Outputs.Samples[10];
            s.Execute(SimulationCommand.SetInitialState(new NumericVector(.7, 1))); var b = s.State.Result;
            s.Execute(SimulationCommand.SetParameter("model.mass", -1));
            Assert.That(s.State.Error, Is.Not.Null); Assert.That(s.State.Result, Is.SameAs(b));
            Assert.That(a.Outputs.Samples[10], Is.SameAs(y)); Assert.That(a.Configuration.InitialState[0], Is.EqualTo(.35));
            Assert.That(b.Configuration.InitialState[0], Is.EqualTo(.7)); Assert.That(a.Outputs.Samples[10][0], Is.Not.EqualTo(b.Outputs.Samples[10][0]));
            Assert.That(metrics.Resolve(a, 0, RuntimeMetricIds.Horizon, FormulaValueKind.Scalar, FormulaScope.Snapshot).Value.Scalar, Is.EqualTo(100));
            Assert.That(metrics.Resolve(a, 0, RuntimeMetricIds.StateCount, FormulaValueKind.Scalar, FormulaScope.Snapshot).Value.Scalar, Is.EqualTo(101));
            var count = metrics.Resolve(a, 0, RuntimeMetricIds.InputCount, FormulaValueKind.Scalar, FormulaScope.Snapshot);
            Assert.That(count.SampleIndex, Is.Null); Assert.That(count.Value.Scalar, Is.EqualTo(100));
            Assert.That(s.Execute(SimulationCommand.Reset()), Is.True); Assert.That(s.State.Error, Is.Null);
        }
        [Test] public void OutputDoesNotInventPhysicalTimeForIndexOnlyModel()
        {
            using var s = new SimulationSession(new SimulationConfiguration("fixture.output", new AdditionOutput(false), new NumericVector(2), new[] { new NumericVector(3) }));
            Assert.That(s.State.Result.History.States[1][0], Is.EqualTo(5)); Assert.That(s.State.Result.Outputs.Samples[1][0], Is.EqualTo(5));
            Assert.That(metrics.Resolve(s.State.Result, 1, RuntimeMetricIds.Index, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(1));
            foreach (var id in new[] { RuntimeMetricIds.PhysicalStep, RuntimeMetricIds.PhysicalTime })
                Assert.That(metrics.Resolve(s.State.Result, 1, id, FormulaValueKind.Scalar).Validity, Is.EqualTo(MetricValidity.NotApplicable));
            using var noOutput = IndexOnlyConfiguration.CreateSession();
            Assert.That(noOutput.State.Result.Outputs, Is.Null);
            Assert.That(metrics.Resolve(noOutput.State.Result, 0, RuntimeMetricIds.Output, FormulaValueKind.Vector).Validity, Is.EqualTo(MetricValidity.NotApplicable));
        }
        [Test] public void InputDependentOutputUsesActualInputAndIsUnavailableAtTerminal()
        {
            using var s = new SimulationSession(new SimulationConfiguration("fixture.output", new AdditionOutput(true), new NumericVector(2), new[] { new NumericVector(3) }));
            Assert.That(s.State.Result.Outputs.Samples[0][0], Is.EqualTo(8)); Assert.That(s.State.Result.Outputs.Samples[1], Is.Null);
            var unavailable = metrics.Resolve(s.State.Result, 1, RuntimeMetricIds.Output, FormulaValueKind.Scalar, component: 0);
            Assert.That(unavailable.Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(unavailable.Diagnostic, Does.Contain("u_T")); Assert.That(unavailable.SampleIndex, Is.Null);
        }
        [Test] public void FailedOutputEvaluationCannotPublishPartialRevisionAndResetRecovers()
        {
            using var s = new SimulationSession(new SimulationConfiguration("fixture.output", new AdditionOutput(true), new NumericVector(2), new[] { new NumericVector(3) }));
            var a = s.State.Result;
            Assert.That(s.Execute(SimulationCommand.SetParameter("break", 1)), Is.False);
            Assert.That(s.State.Result, Is.SameAs(a)); Assert.That(s.State.Error, Does.Contain("Output evaluation"));
            Assert.That(a.Outputs.Samples[0][0], Is.EqualTo(8)); Assert.That(s.Execute(SimulationCommand.Reset()), Is.True);
        }
        [Test] public void PositionProjectionRejectsWrongDimensionAndInvalidComponent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StateComponentOutput(2, 1, 2));
            var output = new StateComponentOutput(2, 1, 0);
            Assert.Throws<ArgumentException>(() => output.Evaluate(0, new NumericVector(2), null));
            Assert.That(output.Evaluate(0, new NumericVector(2, 7), new NumericVector(100))[0], Is.EqualTo(2));
        }
        [Test] public void WrongAnswersNoOpAndUnselectedTermsCannotAdvanceProductionLesson()
        {
            using var s = Open(); using var lesson = new LessonSession(Definition, s);
            Assert.That(lesson.Next(), Is.False); Advance(lesson, "aircraft");
            lesson.Answer("only-output"); Assert.That(lesson.Next(), Is.False); Advance(lesson, "both");
            lesson.Answer("full"); lesson.Execute(SimulationCommand.SetInitialState(s.State.Result.Configuration.InitialState));
            Assert.That(lesson.Next(), Is.False); Select(lesson, "state.current"); Assert.That(lesson.Next(), Is.False);
            Assert.That(lesson.Execute(SimulationCommand.SetInput(0, new NumericVector(1))), Is.False);
            lesson.Execute(SimulationCommand.SetInitialState(new NumericVector(.35, .5))); Assert.That(lesson.Next(), Is.True);
            Select(lesson, "output.current"); lesson.Answer("state"); Assert.That(lesson.Next(), Is.False);
        }
        [Test] public void FullProductionCompletionPersistsOnlyCorrectIdentityAndRestartIsIdempotent()
        {
            var store = new MemoryProgressStore();
            store.Save(new ProgressDocument(new[] { new CompletionRecord("prototype", "1", "prototype.pendulum", "1", 1), new CompletionRecord("fixture.f10.course", "1", "fixture.f10.static", "1", 1) }));
            var progress = new ProgressionService(Course, store); Assert.That(progress.GetStatus("C1.L1").IsCompleted, Is.False);
            using var s = Open(); using var lesson = new LessonSession(Definition, s);
            using var binding = new LessonProgressBinding(lesson, s, progress, () => true);
            Complete(lesson); Assert.That(lesson.Presentation.IsCompleted, Is.True); Assert.That(store.SaveCount, Is.EqualTo(2));
            var record = store.Load().Completions.Single(c => c.CourseId == Course.Id);
            Assert.That(record.LessonId, Is.EqualTo("C1.L1")); Assert.That(record.LessonVersion, Is.EqualTo("2")); Assert.That(record.LessonSchemaVersion, Is.EqualTo(1));
            Assert.That(new ProgressionService(Course, store).GetStatus("C1.L1").IsCompleted, Is.True);
            lesson.Restart(); Complete(lesson); Assert.That(store.SaveCount, Is.EqualTo(2));
        }
        [Test] public void NavigationGuardCancelsProductionCompletionWithoutLosingMathResult()
        {
            var store = new MemoryProgressStore(); var progress = new ProgressionService(Course, store);
            using var s = Open(); using var lesson = new LessonSession(Definition, s);
            using var binding = new LessonProgressBinding(lesson, s, progress, () => false);
            Complete(lesson); Assert.That(lesson.Presentation.IsCompleted, Is.True); Assert.That(store.SaveCount, Is.Zero);
            Assert.That(s.State.Sample.IsTerminal, Is.True); Assert.That(s.State.Result.Outputs.Samples[100], Is.Not.Null);
        }
        [Test] public void PreviousRestartAndDisposalCannotCarryAnswerOrSelectionCreditAcrossGenerations()
        {
            using var s = Open(); using var lesson = new LessonSession(Definition, s);
            Advance(lesson, "drones"); Advance(lesson, "both"); Select(lesson, "state.current");
            lesson.Execute(SimulationCommand.SetInitialState(new NumericVector(.35, .5))); lesson.Answer("full");
            Assert.That(lesson.Presentation.CanAdvance, Is.True); Assert.That(lesson.Previous(), Is.True);
            Assert.That(lesson.Presentation.Step.Id, Is.EqualTo("purpose")); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Advance(lesson, "both"); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Assert.That(lesson.Restart(), Is.True); Assert.That(lesson.Presentation.Step.Id, Is.EqualTo("context"));
            Assert.That(s.State.Result.Configuration.InitialState.ToArray(), Is.EqualTo(new[] { .35, 0d }));
            Assert.That(lesson.Presentation.CanAdvance, Is.False); lesson.Dispose(); Assert.That(lesson.Next(), Is.False);
        }
        [Test] public void OutputSamplesAreReadOnlyAndSharedLookupsNeverReplaceThem()
        {
            using var s = Open(); var r = s.State.Result; var samples = r.Outputs.Samples;
            Assert.Throws<NotSupportedException>(() => ((IList<NumericVector>)samples)[0] = new NumericVector(99));
            for (int i = 0; i <= r.History.Horizon; i++)
            {
                s.Execute(SimulationCommand.Seek(i));
                Assert.That(metrics.Resolve(r, i, RuntimeMetricIds.Output, FormulaValueKind.Vector).Value.Vector, Is.SameAs(samples[i]));
                Assert.That(s.State.Result, Is.SameAs(r));
            }
        }
        private sealed class AdditionOutput : ModelConfiguration, IDiscreteDynamics
        {
            private readonly bool requiresInput, broken;
            public AdditionOutput(bool requiresInput, bool broken = false) { this.requiresInput = requiresInput; this.broken = broken; }
            public override ModelDescriptor Descriptor { get; } = new ModelDescriptor("fixture.output.addition", "1", new ModelDimensions(1, 1, 1), new[] { "1" }, new[] { "1" }, new[] { "1" });
            public override IReadOnlyList<ModelParameterValue> Parameters => Array.Empty<ModelParameterValue>();
            public override IDiscreteDynamics CreateDynamics() => this;
            public NumericVector Evaluate(int t, NumericVector x, NumericVector u) => new NumericVector(x[0] + u[0]);
            public override ModelConfiguration WithParameter(string id, double value) => new AdditionOutput(requiresInput, value != 0);
            public override ModelConfiguration WithPhysicalStep(double? value) => throw new ArgumentException("Index only");
            public override IOutputFunction CreateOutputFunction() => new Output(requiresInput, broken);
            private sealed class Output : IOutputFunction
            {
                public int Dimension => 1;
                public bool RequiresControl { get; }
                private readonly bool broken;
                public Output(bool requiresControl, bool broken) { RequiresControl = requiresControl; this.broken = broken; }
                public NumericVector Evaluate(int t, NumericVector x, NumericVector u)
                { if (broken) throw new ArithmeticException("Output oracle failure"); return new NumericVector(x[0] + (RequiresControl ? 2 * u[0] : 0)); }
            }
        }
    }
}
