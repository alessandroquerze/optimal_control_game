using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Content;
using OptCon.Core;
using OptCon.Simulation;
using OptCon.Presentation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine.UIElements;

namespace OptCon.Tests
{
    public sealed class CostSystemTests
    {
        private static QuadraticTrackingCost Cost(double q = 2, double r = 2, double qt = 2) => new QuadraticTrackingCost(
            new NumericVector(0), new NumericVector(0), new NumericVector(q), new NumericVector(r), new NumericVector(qt), new[] { "1" }, new[] { "1" });
        private static SimulationSession Session(int horizon = 1, CostConfiguration cost = null) => new SimulationSession(new SimulationConfiguration("fixture.cost",
            new IndexOnlyConfiguration(), new NumericVector(2), Enumerable.Range(0, horizon).Select(_ => new NumericVector(3)).ToArray(), cost: cost ?? Cost()));
        [Test] public void KnownStageSeparatesTrackingAndEffort() { var c = Cost().EvaluateStage(0, new NumericVector(2), new NumericVector(3)); Assert.That(c.Tracking, Is.EqualTo(4)); Assert.That(c.Effort, Is.EqualTo(9)); Assert.That(c.Total, Is.EqualTo(13)); }
        [Test] public void KnownTerminalIsTwentyFive() { var c = Cost().EvaluateTerminal(new NumericVector(5)); Assert.That(c.Total, Is.EqualTo(25)); Assert.That(c.Effort, Is.Zero); }
        [Test] public void OneTransitionHasTotalThirtyEightAndOneTerminal() { using (var s = Session()) { var c = s.State.Result.Costs; Assert.That(c.Total, Is.EqualTo(38)); Assert.That(c.Stages.Count, Is.EqualTo(1)); Assert.That(c.History, Is.SameAs(s.State.Result.History)); Assert.That(c.Configuration, Is.SameAs(s.State.Result.Configuration.Cost)); } }
        [Test] public void EveryPrefixExcludesCurrentStageAndTerminal() { using (var s = Session(2)) { Assert.That(s.State.Result.Costs.Cumulative, Is.EqualTo(new[] { 0d, 13, 47 })); Assert.That(s.State.Result.Costs.Terminal.Total, Is.EqualTo(64)); Assert.That(s.State.Result.Costs.Total, Is.EqualTo(111)); } }
        [Test] public void TerminalNeverEvaluatesAnExtraInputOrStage() { using (var s = Session()) { s.Execute(SimulationCommand.Seek(1)); Assert.That(s.State.Sample.AppliedControl, Is.Null); Assert.That(s.State.Result.Costs.StageAt(1), Is.Null); var r = new RuntimeMetricResolver(); Assert.That(r.Resolve(s.State.Result, 1, RuntimeMetricIds.StageCost, FormulaValueKind.Scalar).Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(r.Resolve(s.State.Result, 0, RuntimeMetricIds.TerminalCost, FormulaValueKind.Scalar, FormulaScope.Terminal).SampleIndex, Is.EqualTo(1)); } }
        [Test] public void IndexOnlyCostsNeverInventPhysicalTime() { using (var s = Session()) { var r = new RuntimeMetricResolver(); foreach (int t in new[] { 0, 1 }) { Assert.That(r.Resolve(s.State.Result, t, RuntimeMetricIds.Index, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(t)); foreach (string id in new[] { RuntimeMetricIds.PhysicalTime, RuntimeMetricIds.PhysicalStep }) Assert.That(r.Resolve(s.State.Result, t, id, FormulaValueKind.Scalar).Validity, Is.EqualTo(MetricValidity.NotApplicable)); } Assert.That(s.State.Result.Costs.Total, Is.EqualTo(38)); } }
        [Test] public void PendulumKeepsPhysicalStepButCostIsPerSample() { var experiment = AssetDatabase.LoadAssetAtPath<ExperimentDefinition>("Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset"); using (var s = experiment.Model.CreateSession(experiment)) { Assert.That(s.State.Result.History.Model.DiscretizationStepSeconds, Is.EqualTo(.02)); Assert.That(s.State.Result.Costs.Stages[0].Total, Is.EqualTo(.6125).Within(1e-12)); Assert.That(s.State.Result.History.PhysicalTimeAt(100), Is.EqualTo(2)); Assert.That(s.State.Result.Costs.Configuration.Parameters.Single(p => p.Target == "cost.qt.0").Value, Is.EqualTo(20)); } }
        [Test] public void WeightEditRetainsAAndReusesAllSavedStates() { using (var s = Session(2)) { var a = s.State.Result; s.Execute(SimulationCommand.CaptureComparison()); Assert.That(s.Execute(SimulationCommand.SetParameter("cost.q.0", 4)), Is.True); var b = s.State.Result; Assert.That(s.State.Comparison.Result, Is.SameAs(a)); Assert.That(a.Costs.Total, Is.EqualTo(111)); Assert.That(b.Costs.Total, Is.EqualTo(140)); Assert.That(b.RevisionId, Is.EqualTo(1)); for (int t = 0; t <= 2; t++) Assert.That(b.History.States[t], Is.SameAs(a.History.States[t])); Assert.That(a.Configuration.Cost.Parameters.Single(p => p.Target == "cost.q.0").Value, Is.EqualTo(2)); } }
        [Test] public void TargetEditDoesNotChangeDynamicsAndUpdatesAuthoritativeReference() { using (var s = Session()) { var a = s.State.Result; s.Execute(SimulationCommand.SetParameter("cost.target.0", 2)); var b = s.State.Result; Assert.That(b.Configuration.ReferenceState, Is.SameAs(b.Configuration.Cost.ReferenceState)); Assert.That(b.Costs.Total, Is.EqualTo(18)); Assert.That(a.Costs.Total, Is.EqualTo(38)); Assert.That(b.History.States[1], Is.SameAs(a.History.States[1])); } }
        [Test] public void InvalidWeightAndOverflowKeepWholeLastValidResult() { using (var s = Session()) { var a = s.State.Result; Assert.That(s.Execute(SimulationCommand.SetParameter("cost.r.0", 0)), Is.False); Assert.That(s.State.Result, Is.SameAs(a)); Assert.That(s.Execute(SimulationCommand.SetParameter("cost.q.0", double.MaxValue)), Is.False); Assert.That(s.State.Error, Is.Not.Empty); Assert.That(s.State.Result, Is.SameAs(a)); Assert.That(s.State.Result.Costs.Configuration, Is.SameAs(a.Configuration.Cost)); } }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)] public void NonFiniteWeightsAreRejected(double value) => Assert.Throws<ArgumentOutOfRangeException>(() => Cost(value));
        [TestCase(-1)] [TestCase(0)] public void EffortWeightMustBeStrictlyPositive(double value) => Assert.That(() => Cost(r: value), Throws.InstanceOf<ArgumentException>());
        [Test] public void ZeroTrackingWeightsAndEquilibriumHaveKnownZeroCost() { var model = new IndexOnlyConfiguration(); using (var s = new SimulationSession(new SimulationConfiguration("zero", model, new NumericVector(0), new[] { new NumericVector(0) }, cost: Cost(0, 2, 0)))) Assert.That(s.State.Result.Costs.Total, Is.Zero); }
        [Test] public void CostsAndArraysAreImmutable() { var q = new[] { 2d }; var cost = Cost(); var vector = new NumericVector(q); q[0] = 999; Assert.That(vector[0], Is.EqualTo(2)); using (var s = Session(cost: cost)) { Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<double>)s.State.Result.Costs.Cumulative)[0] = 99); Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<CostContribution>)s.State.Result.Costs.Stages)[0] = new CostContribution(99, 0)); } }
        [Test] public void HorizonAndResetKeepComparisonObjectiveIndependent() { using (var s = Session()) { var a = s.State.Result; s.Execute(SimulationCommand.CaptureComparison()); Assert.That(s.Execute(SimulationCommand.SetHorizon(2)), Is.True); Assert.That(s.State.Result.Costs.Total, Is.EqualTo(111)); Assert.That(s.State.Result.Configuration.Controls[1][0], Is.EqualTo(3)); s.Execute(SimulationCommand.Reset()); Assert.That(s.State.Result.Costs.Total, Is.EqualTo(38)); Assert.That(s.State.Comparison.Result, Is.SameAs(a)); s.Execute(SimulationCommand.ClearComparison()); Assert.That(s.State.Comparison, Is.Null); } }
        [TestCase(0)] [TestCase(-1)] [TestCase(10001)] public void InvalidHorizonIsExplicitAndPreservesResult(int horizon) { using (var s = Session()) { var a = s.State.Result; Assert.That(s.Execute(SimulationCommand.SetHorizon(horizon)), Is.False); Assert.That(s.State.Result, Is.SameAs(a)); } }
        [Test] public void ResolverTypesUnitsIndicesAndTerminalScopeAreExplicit() { using (var s = Session(2)) { var r = new RuntimeMetricResolver(); Assert.That(r.Resolve(s.State.Result, 1, RuntimeMetricIds.StageCost, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(34)); Assert.That(r.Resolve(s.State.Result, 1, RuntimeMetricIds.CumulativeCost, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(13)); Assert.That(r.Resolve(s.State.Result, 1, RuntimeMetricIds.StageCost, FormulaValueKind.Scalar).SampleIndex, Is.EqualTo(1)); var terminal = r.Resolve(s.State.Result, 0, RuntimeMetricIds.TerminalCost, FormulaValueKind.Scalar, FormulaScope.Terminal); Assert.That(terminal.Value.Scalar, Is.EqualTo(64)); Assert.That(terminal.SampleIndex, Is.EqualTo(2)); Assert.That(terminal.Value.Units.Single(), Is.EqualTo("1")); Assert.That(r.Resolve(s.State.Result, 0, RuntimeMetricIds.StageCost, FormulaValueKind.Vector).Validity, Is.EqualTo(MetricValidity.Invalid)); Assert.That(r.Resolve(s.State.Result, 0, RuntimeMetricIds.TerminalCost, FormulaValueKind.Scalar).Validity, Is.EqualTo(MetricValidity.Invalid)); } }
        [Test] public void AdditiveFormulaHudReadsExactEvaluationAndRetainedA() { using (var s = Session()) { var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/OptimalControl/UI/LessonHud.uxml").CloneTree(); var formula = AssetDatabase.LoadAssetAtPath<FormulaDefinition>("Assets/OptimalControl/Data/Formulas/AdditiveCost.asset"); using (var presenter = new FormulaPresenter(tree, new[] { formula }, null)) { var a = s.State.Result; presenter.Render(a, 0); Assert.That(presenter.Resolutions[RuntimeMetricIds.TotalCost].Value.Scalar, Is.EqualTo(a.Costs.Total)); Assert.That(tree.Q<Button>("term-cost.total").Q<Label>(className: "term-value").text, Is.EqualTo("38 1")); s.Execute(SimulationCommand.SetParameter("cost.q.0", 4)); presenter.Render(a, 0); Assert.That(presenter.Resolutions[RuntimeMetricIds.TotalCost].Value.Scalar, Is.EqualTo(38)); presenter.Render(s.State.Result, 1); Assert.That(presenter.Resolutions[RuntimeMetricIds.StageCost].Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(presenter.Resolutions["state.terminal"].Value.Vector[0], Is.EqualTo(5)); } } }
        [Test] public void WeightChangeDoesNotEvaluateDynamics() { var model = new CountingModel(); using (var s = new SimulationSession(new SimulationConfiguration("count", model, new NumericVector(2), new[] { new NumericVector(3) }, cost: Cost()))) { Assert.That(model.Calls, Is.EqualTo(1)); s.Execute(SimulationCommand.SetParameter("cost.q.0", 4)); Assert.That(model.Calls, Is.EqualTo(1)); } }
        [Test] public void ObjectiveUnitsAndDimensionsMustMatchModel() { Assert.Throws<ArgumentException>(() => Cost().Validate(new IndexOnlyConfiguration(2).Descriptor)); Assert.Throws<ArgumentException>(() => new QuadraticTrackingCost(new NumericVector(0), new NumericVector(0), new NumericVector(-1), new NumericVector(1), new NumericVector(1), new[] { "1" }, new[] { "1" })); }
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NonFiniteWeightCommandsPreserveTheCompleteValidRevision(double value)
        {
            using (var session = Session())
            {
                var valid = session.State.Result;
                Assert.That(session.Execute(SimulationCommand.SetParameter("cost.q.0", value)), Is.False);
                Assert.That(session.State.Result, Is.SameAs(valid));
                Assert.That(session.State.Result.Costs.Configuration, Is.SameAs(valid.Configuration.Cost));
                Assert.That(session.State.Error, Is.Not.Empty);
            }
        }
        [Test]
        public void CostParameterMetricsKeepTheirRevisionAndPhysicalUnits()
        {
            var experiment = AssetDatabase.LoadAssetAtPath<ExperimentDefinition>("Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset");
            using (var session = experiment.Model.CreateSession(experiment))
            {
                var a = session.State.Result;
                session.Execute(SimulationCommand.SetParameter("cost.q.0", 30));
                var resolver = new RuntimeMetricResolver();
                var oldWeight = resolver.Resolve(a, 0, "parameter.cost.q.0", FormulaValueKind.Scalar);
                var newWeight = resolver.Resolve(session.State.Result, 0, "parameter.cost.q.0", FormulaValueKind.Scalar);
                Assert.That(oldWeight.Value.Scalar, Is.EqualTo(10));
                Assert.That(newWeight.Value.Scalar, Is.EqualTo(30));
                Assert.That(oldWeight.Value.Units.Single(), Is.EqualTo("1/(rad)²"));
                Assert.That(resolver.Resolve(a, 0, "parameter.cost.r.0", FormulaValueKind.Scalar).Value.Units.Single(), Is.EqualTo("1/(N·m)²"));
            }
        }
        [Test]
        public void NoCostProviderProducesExplicitUnavailableCostMetrics()
        {
            using (var session = IndexOnlyConfiguration.CreateSession())
            {
                Assert.That(session.State.Result.Costs, Is.Null);
                var resolution = new RuntimeMetricResolver().Resolve(session.State.Result, 0, RuntimeMetricIds.TotalCost, FormulaValueKind.Scalar);
                Assert.That(resolution.Validity, Is.EqualTo(MetricValidity.NotApplicable));
                Assert.That(resolution.Value.Scalar, Is.Null);
                Assert.That(resolution.Diagnostic, Does.Contain("Nessun modello di costo"));
            }
        }
        private sealed class CountingModel : ModelConfiguration, IDiscreteDynamics
        {
            private readonly IndexOnlyConfiguration inner = new IndexOnlyConfiguration(); public int Calls;
            public override ModelDescriptor Descriptor => inner.Descriptor;
            public override System.Collections.Generic.IReadOnlyList<ModelParameterValue> Parameters => inner.Parameters;
            public override IDiscreteDynamics CreateDynamics() => this;
            public NumericVector Evaluate(int t, NumericVector x, NumericVector u) { Calls++; return inner.Evaluate(t, x, u); }
            public override ModelConfiguration WithParameter(string target, double value) => throw new ArgumentException();
            public override ModelConfiguration WithPhysicalStep(double? seconds) => throw new ArgumentException();
        }
    }
}
