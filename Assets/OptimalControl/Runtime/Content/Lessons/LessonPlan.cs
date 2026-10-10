using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Simulation;

namespace OptCon.Content
{
    public sealed class LessonConditionPlan
    {
        public string EvaluatorId { get; }
        public string TargetId { get; }
        public string ExpectedAnswerId { get; }
        public LessonConditionPlan(LessonConditionDefinition definition)
        { EvaluatorId = definition.EvaluatorId; TargetId = definition.TargetId; ExpectedAnswerId = definition.ExpectedAnswerId; }
    }
    public sealed class LessonAnswerPlan
    {
        public string Id { get; }
        public string Text { get; }
        public string Feedback { get; }
        public LessonAnswerPlan(LessonAnswerDefinition definition)
        { Id = definition.Id; Text = definition.Text; Feedback = definition.Feedback; }
    }
    public sealed class LessonStepPlan
    {
        public string Id { get; }
        public string Title { get; }
        public string Objective { get; }
        public string Instruction { get; }
        public string Help { get; }
        public string FormulaId { get; }
        public string SelectedTermId { get; }
        public string Question { get; }
        public bool ShowCosts { get; }
        public IReadOnlyList<string> VisibleTerms { get; }
        public IReadOnlyList<string> EnabledControls { get; }
        public IReadOnlyList<string> EditableParameters { get; }
        public IReadOnlyList<string> VisibleLayers { get; }
        public IReadOnlyList<SimulationCommand> EntryCommands { get; }
        public IReadOnlyList<LessonConditionPlan> Conditions { get; }
        public IReadOnlyList<LessonAnswerPlan> Answers { get; }
        public LessonStepPlan(LessonStepDefinition definition, string defaultFormulaId = null)
        {
            Id = definition.Id; Title = definition.Title; Objective = definition.Objective; Instruction = definition.Instruction;
            Help = definition.Help; FormulaId = definition.FormulaFocus?.Id ?? defaultFormulaId; SelectedTermId = definition.SelectedTermId; Question = definition.Question;
            ShowCosts = definition.ShowCosts;
            VisibleTerms = Array.AsReadOnly(definition.VisibleTermIds.ToArray()); EnabledControls = Array.AsReadOnly(definition.EnabledControlIds.ToArray());
            EditableParameters = Array.AsReadOnly(definition.EditableParameterTargets.ToArray()); VisibleLayers = Array.AsReadOnly(definition.VisibleLayerIds.ToArray());
            EntryCommands = Array.AsReadOnly(definition.EntryActions.Select(a => a.CreateCommand()).ToArray());
            Conditions = Array.AsReadOnly(definition.Conditions.Select(c => new LessonConditionPlan(c)).ToArray());
            Answers = Array.AsReadOnly(definition.Answers.Select(a => new LessonAnswerPlan(a)).ToArray());
        }
    }
    /// <summary>Freezes editorial progression. No simulation samples or live values are copied here.</summary>
    public sealed class LessonPlan
    {
        public string LessonId { get; }
        public IReadOnlyList<LessonStepPlan> Steps { get; }
        public LessonPlan(LessonDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            LessonId = definition.Id;
            var steps = new List<LessonStepPlan>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in definition.Steps)
            {
                if (step == null || string.IsNullOrWhiteSpace(step.Id) || !ids.Add(step.Id) || string.IsNullOrWhiteSpace(step.Instruction))
                    throw new ArgumentException("Lesson steps require unique IDs and instructions.");
                if (step.FormulaFocus != null && !definition.Formulas.Contains(step.FormulaFocus)) throw new ArgumentException("Unknown step formula.");
                var formula = step.FormulaFocus ?? definition.Formulas.FirstOrDefault();
                foreach (string term in step.VisibleTermIds.Concat(string.IsNullOrEmpty(step.SelectedTermId) ? Array.Empty<string>() : new[] { step.SelectedTermId }))
                    if (formula == null || !formula.Terms.Any(t => t.Id == term)) throw new ArgumentException("Unknown step formula term: " + term);
                if (step.VisibleTermIds.Count > 0 && !string.IsNullOrEmpty(step.SelectedTermId) && !step.VisibleTermIds.Contains(step.SelectedTermId))
                    throw new ArgumentException("Selected term must be disclosed by the step.");
                if (step.EnabledControlIds.Any(c => !LessonControls.All.Contains(c))) throw new ArgumentException("Unknown lesson control ID.");
                if (step.Conditions.Any(c => c == null || string.IsNullOrWhiteSpace(c.EvaluatorId))) throw new ArgumentException("Missing lesson evaluator ID.");
                if (step.Answers.Any(a => a == null || string.IsNullOrWhiteSpace(a.Id) || string.IsNullOrWhiteSpace(a.Text)) || step.Answers.Select(a => a.Id).Distinct().Count() != step.Answers.Count)
                    throw new ArgumentException("Answer IDs/text must be present and unique.");
                if (step.Conditions.Any(c => c.EvaluatorId == "answer" && !step.Answers.Any(a => a.Id == c.ExpectedAnswerId))) throw new ArgumentException("Expected answer does not belong to the step.");
                steps.Add(new LessonStepPlan(step, definition.Formulas.FirstOrDefault()?.Id));
            }
            Steps = steps.AsReadOnly();
        }
    }
}
