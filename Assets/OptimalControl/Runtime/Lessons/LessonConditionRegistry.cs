using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Application
{
    public sealed class LessonConditionContext
    {
        private readonly IReadOnlyCollection<string> facts;
        public IExperimentSnapshot Snapshot { get; }
        public SessionState Session => Snapshot as SessionState;
        public string AnswerId { get; }
        public LessonActivityBaseline Baseline { get; }
        public string StepId { get; }
        public long Generation { get; }
        public object ActivityLifetime { get; }
        public LessonConditionContext(IExperimentSnapshot session, IReadOnlyCollection<string> facts, string answer,
            LessonActivityBaseline baseline = null, string stepId = null, long generation = 0, object activityLifetime = null)
        { Snapshot = session; this.facts = facts; AnswerId = answer; Baseline = baseline; StepId = stepId; Generation = generation; ActivityLifetime = activityLifetime; }
        public bool HasFact(string fact) { foreach (var value in facts) if (value == fact) return true; return false; }
    }
    public interface ILessonConditionEvaluator
    {
        bool IsMet(LessonConditionPlan condition, LessonConditionContext context);
    }
    /// <summary>Provider registry; adding a condition does not add a chapter/lesson switch.</summary>
    public sealed class LessonConditionRegistry
    {
        private sealed class PredicateEvaluator : ILessonConditionEvaluator
        {
            private readonly Func<LessonConditionPlan, LessonConditionContext, bool> predicate;
            public PredicateEvaluator(Func<LessonConditionPlan, LessonConditionContext, bool> predicate) => this.predicate = predicate;
            public bool IsMet(LessonConditionPlan condition, LessonConditionContext context) => predicate(condition, context);
        }
        private readonly Dictionary<string, ExperimentCapabilities> requirements = new Dictionary<string, ExperimentCapabilities>(StringComparer.Ordinal);
        private readonly Dictionary<string, ILessonConditionEvaluator> evaluators = new Dictionary<string, ILessonConditionEvaluator>(StringComparer.Ordinal);
        private readonly LessonMetricEvidenceEvaluator metricEvidence;
        public LessonConditionRegistry(RuntimeMetricResolver metrics = null)
        {
            metricEvidence = new LessonMetricEvidenceEvaluator(metrics ?? new RuntimeMetricResolver());
            Register("metric.evidence", metricEvidence, ExperimentCapabilities.Trajectory);
            Add("action", (c, x) => x.HasFact(c.TargetId));
            Add("term", (c, x) => x.HasFact("term:" + c.TargetId));
            Add("answer", (c, x) => x.AnswerId == c.ExpectedAnswerId);
            // Context selection is engagement, not an assessment of correctness. Other steps assess comprehension.
            Add("answer.selected", (c, x) => !string.IsNullOrEmpty(x.AnswerId));
            Add("terminal", (c, x) => x.Session != null && x.Session.Sample.IsTerminal, ExperimentCapabilities.Trajectory);
            Add("trajectory.constant", (c, x) => x.Session?.Result.IsConstantStateInput == true, ExperimentCapabilities.Trajectory);
            Add("constraint.feasible", (c, x) => x.Session?.Result.Constraints?.IsFeasible == true, ExperimentCapabilities.Trajectory);
            Add("constraint.path-violated", (c, x) => x.Session?.Result.Constraints != null && x.Session.Result.Constraints.PathResiduals.Any(row => row.Any(v => v > x.Session.Result.Constraints.Configuration.Tolerance)), ExperimentCapabilities.Trajectory);
            Add("reference.not-feasible", (c, x) => x.Session?.Result.Constraints?.DesiredDynamicsResiduals != null && !x.Session.Result.Constraints.DesiredSatisfiesDynamics, ExperimentCapabilities.Trajectory);
            Add("comparison.changed", (c, x) => x.HasFact("comparison.saved") && x.HasFact("input.changed") && x.Session != null && x.Session.Comparison != null &&
                !ReferenceEquals(x.Session.Comparison.Result, x.Session.Result), ExperimentCapabilities.Trajectory);
            Add("linearization.available", (c, x) => x.Session?.Linearization != null, ExperimentCapabilities.Linearization);
            Add("linearization.initial-fixed", (c, x) => x.Session?.Linearization?.HasFixedInitialState == true, ExperimentCapabilities.Linearization);
            Add("linearization.initial-perturbed", (c, x) => x.Session?.Linearization != null && !x.Session.Linearization.HasFixedInitialState, ExperimentCapabilities.Linearization);
            Add("linearization.error-nonzero", (c, x) => x.Session?.Linearization != null && x.Session.Linearization.Errors[x.Session.Sample.Timestep].Any(v => v != 0), ExperimentCapabilities.Linearization);
        }
        public void ValidateBinding(LessonConditionPlan condition, IExperimentSnapshot snapshot)
        {
            if (condition.EvaluatorId == "metric.evidence") metricEvidence.ValidateBinding(condition.Evidence, snapshot);
        }
        private void Add(string id, Func<LessonConditionPlan, LessonConditionContext, bool> predicate, ExperimentCapabilities required = ExperimentCapabilities.None) => Register(id, new PredicateEvaluator(predicate), required);
        public void Register(string id, ILessonConditionEvaluator evaluator, ExperimentCapabilities required = ExperimentCapabilities.None)
        { if (string.IsNullOrWhiteSpace(id) || evaluator == null || evaluators.ContainsKey(id)) throw new ArgumentException("Invalid/duplicate condition provider."); evaluators.Add(id, evaluator); requirements.Add(id, required); }
        public ILessonConditionEvaluator Resolve(string id, ExperimentCapabilities offered)
        {
            var evaluator = Resolve(id);
            if ((offered & requirements[id]) != requirements[id]) throw new ArgumentException("Condition requires unavailable capabilities: " + id);
            return evaluator;
        }
        public ILessonConditionEvaluator Resolve(string id) => evaluators.TryGetValue(id, out var evaluator) ? evaluator : throw new ArgumentException("Unknown lesson condition evaluator: " + id);
    }
}
