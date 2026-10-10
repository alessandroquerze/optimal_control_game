using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Application
{
    /// <summary>Educational orchestration only. The simulation remains the sole mathematical owner.</summary>
    public sealed class LessonSession : ILessonRuntime, IDisposable
    {
        private readonly SimulationSession simulation;
        private readonly LessonPlan plan;
        private readonly IReadOnlyList<IReadOnlyList<ILessonConditionEvaluator>> evaluators;
        private readonly HashSet<string> facts = new HashSet<string>(StringComparer.Ordinal);
        private int index;
        private long generation;
        private bool free, completed, busy, dispatching, closed;
        private string answer, feedback;
        public LessonPresentationState Presentation { get; private set; }
        public event Action<LessonPresentationState> Changed;
        public LessonSession(LessonDefinition definition, SimulationSession simulation, LessonConditionRegistry registry = null, IReadOnlyList<string> availableLayers = null)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation)); plan = new LessonPlan(definition);
            var parameterTargets = simulation.State.Result.Configuration.Model.Parameters.Concat(simulation.State.Result.Configuration.Cost?.Parameters ?? Array.Empty<OptCon.Core.ModelParameterValue>()).Select(p => p.Target).ToArray();
            foreach (var step in plan.Steps)
            {
                if (step.EditableParameters.Any(target => !parameterTargets.Contains(target))) throw new ArgumentException("Step requests an unknown parameter target.");
                if (availableLayers != null && step.VisibleLayers.Any(layer => !availableLayers.Contains(layer))) throw new ArgumentException("Step requests an unknown visual layer.");
            }
            registry = registry ?? new LessonConditionRegistry();
            evaluators = Array.AsReadOnly(plan.Steps.Select(s => (IReadOnlyList<ILessonConditionEvaluator>)Array.AsReadOnly(s.Conditions.Select(c => registry.Resolve(c.EvaluatorId)).ToArray())).ToArray());
            free = plan.Steps.Count == 0;
            // Initialization occurs before UI/input binding and before navigation Ready.
            if (!free) Enter(false);
            else Refresh();
            simulation.Changed += OnSimulation;
        }
        private bool Blocked => closed || busy || dispatching;
        private void OnSimulation(SessionState state) { if (!busy && !closed) Publish(); }
        private void Enter(bool restorePreset)
        {
            facts.Clear(); answer = feedback = null; generation++;
            if (!simulation.Execute(SimulationCommand.Pause())) throw new InvalidOperationException("Cannot pause lesson session.");
            if (restorePreset)
            {
                if (!simulation.Execute(SimulationCommand.Reset())) throw new InvalidOperationException("Cannot restore lesson preset.");
                simulation.Execute(SimulationCommand.ClearComparison());
            }
            foreach (var command in plan.Steps[index].EntryCommands)
                if (!simulation.Execute(command)) throw new InvalidOperationException("Lesson entry command failed: " + command.Kind + ". " + simulation.State.Error);
            Refresh();
        }
        private void Refresh()
        {
            var step = plan.Steps.Count == 0 ? null : plan.Steps[index];
            var context = new LessonConditionContext(simulation.State, facts, answer);
            bool[] met = step == null ? Array.Empty<bool>() : step.Conditions.Select((c, i) => evaluators[index][i].IsMet(c, context)).ToArray();
            Presentation = new LessonPresentationState(plan.LessonId, step, index, plan.Steps.Count, generation, free,
                completed, !free && !completed && met.All(v => v), feedback, met);
        }
        private void Publish()
        {
            Refresh(); dispatching = true;
            try { Changed?.Invoke(Presentation); } finally { dispatching = false; }
        }
        private bool Transition(Action action)
        {
            if (Blocked) return false;
            busy = true;
            try { action(); } finally { busy = false; }
            Publish(); return true;
        }
        public bool Next()
        {
            if (Blocked || !Presentation.CanAdvance) return false;
            return Transition(() => { if (index + 1 < plan.Steps.Count) { index++; Enter(false); } else { completed = true; simulation.Execute(SimulationCommand.Pause()); } });
        }
        public bool Previous()
        {
            if (Blocked || free || index == 0) return false;
            // Back starts the previous activity from its authored preset, rather than retaining future actions.
            return Transition(() => { completed = false; index--; Enter(true); });
        }
        public bool Restart() => Transition(() =>
        {
            completed = false; index = 0; free = plan.Steps.Count == 0;
            if (!free) Enter(true);
            else { facts.Clear(); answer = feedback = null; generation++; simulation.Execute(SimulationCommand.Reset()); simulation.Execute(SimulationCommand.ClearComparison()); }
        });
        public bool Explore() => Transition(() => { free = true; generation++; simulation.Execute(SimulationCommand.Pause()); });
        public bool Answer(string answerId)
        {
            if (Blocked || free || completed) return false;
            var option = plan.Steps[index].Answers.FirstOrDefault(a => a.Id == answerId);
            if (option == null) return false;
            return Transition(() => { answer = option.Id; feedback = option.Feedback; });
        }
        public void RecordTermSelection(string formulaId, string termId)
        {
            if (Blocked || free || completed) return;
            var step = plan.Steps[index];
            if ((!string.IsNullOrEmpty(step.FormulaId) && formulaId != step.FormulaId) || (step.VisibleTerms.Count > 0 && !step.VisibleTerms.Contains(termId))) return;
            Transition(() => facts.Add("term:" + termId));
        }
        public bool Execute(SimulationCommand command)
        {
            if (Blocked || !Presentation.Allows(command)) return false;
            if (command.Kind == SimulationCommandKind.Reset)
            {
                if (!free) return Restart();
                return Transition(() => { generation++; facts.Clear(); answer = feedback = null; simulation.Execute(SimulationCommand.Reset()); simulation.Execute(SimulationCommand.ClearComparison()); });
            }
            bool success = false;
            Transition(() =>
            {
                var before = simulation.State;
                success = simulation.Execute(command);
                if (!success || free) return;
                var after = simulation.State;
                if (command.Kind == SimulationCommandKind.SetInput && !before.Result.Configuration.Controls[command.Index].SequenceEqual(after.Result.Configuration.Controls[command.Index])) facts.Add("input.changed");
                if (command.Kind == SimulationCommandKind.SetParameter && before.Result.RevisionId != after.Result.RevisionId)
                {
                    var oldValue = before.Result.Configuration.Model.Parameters.Concat(before.Result.Configuration.Cost?.Parameters ?? Array.Empty<OptCon.Core.ModelParameterValue>()).FirstOrDefault(p => p.Target == command.Target);
                    if (oldValue != null && oldValue.Value != command.Value) facts.Add("parameter.changed:" + command.Target);
                }
                if (command.Kind == SimulationCommandKind.StepForward && after.Sample.Timestep > before.Sample.Timestep) facts.Add("time.advanced");
                if (command.Kind == SimulationCommandKind.CaptureComparison) facts.Add("comparison.saved");
            });
            return success;
        }
        public void Dispose()
        {
            if (closed) return; closed = true; simulation.Changed -= OnSimulation; Changed = null; facts.Clear();
        }
    }
}
