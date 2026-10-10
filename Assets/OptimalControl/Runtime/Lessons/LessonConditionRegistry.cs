using System;
using System.Collections.Generic;
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
        public LessonConditionContext(IExperimentSnapshot session, IReadOnlyCollection<string> facts, string answer)
        { Snapshot = session; this.facts = facts; AnswerId = answer; }
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
        public LessonConditionRegistry()
        {
            Add("action", (c, x) => x.HasFact(c.TargetId));
            Add("term", (c, x) => x.HasFact("term:" + c.TargetId));
            Add("answer", (c, x) => x.AnswerId == c.ExpectedAnswerId);
            // Context selection is engagement, not an assessment of correctness. Other steps assess comprehension.
            Add("answer.selected", (c, x) => !string.IsNullOrEmpty(x.AnswerId));
            Add("terminal", (c, x) => x.Session != null && x.Session.Sample.IsTerminal, ExperimentCapabilities.Trajectory);
            Add("comparison.changed", (c, x) => x.HasFact("comparison.saved") && x.HasFact("input.changed") && x.Session != null && x.Session.Comparison != null &&
                !ReferenceEquals(x.Session.Comparison.Result, x.Session.Result), ExperimentCapabilities.Trajectory);
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
