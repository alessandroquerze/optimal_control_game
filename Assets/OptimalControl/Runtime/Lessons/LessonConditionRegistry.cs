using System;
using System.Collections.Generic;
using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Application
{
    public sealed class LessonConditionContext
    {
        private readonly IReadOnlyCollection<string> facts;
        public SessionState Session { get; }
        public string AnswerId { get; }
        public LessonConditionContext(SessionState session, IReadOnlyCollection<string> facts, string answer)
        { Session = session; this.facts = facts; AnswerId = answer; }
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
        private readonly Dictionary<string, ILessonConditionEvaluator> evaluators = new Dictionary<string, ILessonConditionEvaluator>(StringComparer.Ordinal);
        public LessonConditionRegistry()
        {
            Add("action", (c, x) => x.HasFact(c.TargetId));
            Add("term", (c, x) => x.HasFact("term:" + c.TargetId));
            Add("answer", (c, x) => x.AnswerId == c.ExpectedAnswerId);
            Add("terminal", (c, x) => x.Session.Sample.IsTerminal);
            Add("comparison.changed", (c, x) => x.HasFact("comparison.saved") && x.HasFact("input.changed") && x.Session.Comparison != null &&
                !ReferenceEquals(x.Session.Comparison.Result, x.Session.Result));
        }
        private void Add(string id, Func<LessonConditionPlan, LessonConditionContext, bool> predicate) => Register(id, new PredicateEvaluator(predicate));
        public void Register(string id, ILessonConditionEvaluator evaluator)
        { if (string.IsNullOrWhiteSpace(id) || evaluator == null || evaluators.ContainsKey(id)) throw new ArgumentException("Invalid/duplicate condition provider."); evaluators.Add(id, evaluator); }
        public ILessonConditionEvaluator Resolve(string id) => evaluators.TryGetValue(id, out var evaluator) ? evaluator : throw new ArgumentException("Unknown lesson condition evaluator: " + id);
    }
}
