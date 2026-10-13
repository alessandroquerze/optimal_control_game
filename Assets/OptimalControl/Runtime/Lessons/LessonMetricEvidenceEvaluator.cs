using System;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Application
{
    /// <summary>Activity-owned immutable publication, captured after entry actions, never from a global store.</summary>
    public sealed class LessonActivityBaseline
    {
        public string StepId { get; }
        public long Generation { get; }
        public IExperimentSnapshot Snapshot { get; }
        public object Lifetime { get; }
        internal LessonActivityBaseline(string stepId, long generation, IExperimentSnapshot snapshot, object lifetime)
        { StepId = stepId; Generation = generation; Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot)); Lifetime = lifetime; }
    }

    /// <summary>Compares saved scalar metrics; does not evaluate dynamics, Jacobians, reference or cost.</summary>
    public sealed class LessonMetricEvidenceEvaluator : ILessonConditionEvaluator
    {
        private readonly RuntimeMetricResolver metrics;
        public LessonMetricEvidenceEvaluator(RuntimeMetricResolver metrics) => this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        private MetricResolution Resolve(LessonMetricEvidencePlan evidence, IExperimentSnapshot snapshot) =>
            metrics.Resolve(snapshot, evidence.MetricId, FormulaValueKind.Scalar, evidence.Scope, evidence.Component,
                1, 1, new[] { evidence.Unit });
        public void ValidateBinding(LessonMetricEvidencePlan evidence, IExperimentSnapshot snapshot)
        {
            if (evidence == null) throw new ArgumentException("Missing metric evidence descriptor.");
            if (!(snapshot is SessionState trajectory) || evidence.SampleIndex > trajectory.Result.History.Horizon)
                throw new ArgumentException("Evidence sample is outside the trajectory domain.");
            if (evidence.Scope == FormulaScope.NextTimestep && evidence.SampleIndex == trajectory.Result.History.Horizon)
                throw new ArgumentException("Next-state evidence requires an existing transition; no next state at T.");
            var binding = Resolve(evidence, trajectory.Select(evidence.SampleIndex));
            if (binding.Validity == MetricValidity.Invalid) throw new ArgumentException("Invalid evidence binding: " + binding.Diagnostic);
            if (evidence.RequireSameNominal && (snapshot.Capabilities & ExperimentCapabilities.Linearization) == 0)
                throw new ArgumentException("Nominal evidence requires linearization capability.");
            if (evidence.RequireSameProblem && trajectory.Result.Configuration.Cost == null)
                throw new ArgumentException("Fixed-problem evidence requires a cost configuration.");
            if (evidence.RequireSamePhysicalProblem && trajectory.Result.Configuration.ContinuousSampling == null)
                throw new ArgumentException("Physical-problem evidence requires continuous sampling laws.");
        }
        public bool IsMet(LessonConditionPlan condition, LessonConditionContext context)
        {
            var evidence = condition.Evidence;
            var baseline = context.Baseline;
            if (evidence == null || baseline == null || !ReferenceEquals(baseline.Lifetime, context.ActivityLifetime) ||
                baseline.StepId != context.StepId || baseline.Generation != context.Generation ||
                context.Snapshot.IsClosed || context.Snapshot.Error != null || baseline.Snapshot.IsClosed || baseline.Snapshot.Error != null ||
                !(context.Snapshot is SessionState current) || !(baseline.Snapshot is SessionState before) ||
                current.Sample.Timestep != evidence.SampleIndex || before.Sample.Timestep != evidence.SampleIndex) return false;
            if (evidence.RequireSameNominal && (current.Linearization == null || before.Linearization == null ||
                !ReferenceEquals(current.Linearization.Nominal, before.Linearization.Nominal))) return false;
            if (evidence.RequireSameProblem && !SameProblem(before.Result.Configuration, current.Result.Configuration)) return false;
            if (evidence.RequireSamePhysicalProblem && (current.DiscretizationComparison == null ||
                !ReferenceEquals(current.DiscretizationComparison.A, before.Result) || !current.DiscretizationComparison.SamePhysicalProblem)) return false;
            var original = Resolve(evidence, before);
            var live = Resolve(evidence, current);
            if (original.Validity != MetricValidity.Available || live.Validity != MetricValidity.Available || original.SampleIndex != live.SampleIndex) return false;
            double a = original.Value.Scalar.Value, b = live.Value.Scalar.Value, tolerance = evidence.Tolerance;
            switch (evidence.Comparison)
            {
                case LessonMetricComparison.Unchanged: return Math.Abs(b - a) <= tolerance;
                case LessonMetricComparison.Changed: return Math.Abs(b - a) > tolerance;
                case LessonMetricComparison.EqualTo: return Math.Abs(b - evidence.ExpectedValue) <= tolerance;
                case LessonMetricComparison.GreaterMagnitude: return Math.Abs(b) - Math.Abs(a) > tolerance;
                case LessonMetricComparison.LessThanBaseline: return a - b > tolerance;
                case LessonMetricComparison.GreaterThanBaseline: return b - a > tolerance;
                default: return false;
            }
        }
        private static bool SameProblem(SimulationConfiguration a, SimulationConfiguration b) =>
            a.ExperimentId == b.ExperimentId && a.Horizon == b.Horizon && a.InitialState.SequenceEqual(b.InitialState) &&
            ReferenceEquals(a.Model, b.Model) && ReferenceEquals(a.Cost, b.Cost) &&
            ReferenceEquals(a.Constraints, b.Constraints) && ReferenceEquals(a.ReferenceState, b.ReferenceState);
    }
}
