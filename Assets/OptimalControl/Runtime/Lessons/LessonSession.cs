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
        private readonly IExperimentSession simulation;
        private readonly IDisposable subscription;
        private readonly LessonPlan plan;
        private readonly IReadOnlyList<IReadOnlyList<ILessonConditionEvaluator>> evaluators;
        private readonly HashSet<string> facts = new HashSet<string>(StringComparer.Ordinal);
        private readonly object activityLifetime = new object();
        private int index;
        private long generation;
        private bool free, completed, busy, dispatching, closed;
        private string answer, feedback, entryError;
        private readonly string contentVersion;
        public LessonActivityBaseline ActivityBaseline { get; private set; }
        private readonly int schemaVersion;
        private sealed class EntryFailure : InvalidOperationException
        { public EntryFailure(string message) : base(message) { } }
        public LessonPresentationState Presentation { get; private set; }
        public event Action<LessonPresentationState> Changed;
        public event Action<LessonCompletion> CompletionValidated;
        public LessonSession(LessonDefinition definition, IExperimentSession simulation, LessonConditionRegistry registry = null, IReadOnlyList<string> availableLayers = null)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation)); plan = new LessonPlan(definition);
            contentVersion = definition.ContentVersion; schemaVersion = definition.SchemaVersion;
            var parameterTargets = simulation.Current.Result.Configuration.Parameters.Select(p => p.Target).ToArray();
            foreach (var step in plan.Steps)
            {
                LessonCapabilityRequirements.Validate(step, simulation);
                if (step.EditableParameters.Any(target => !parameterTargets.Contains(target))) throw new ArgumentException("Step requests an unknown parameter target.");
                if (availableLayers != null && step.VisibleLayers.Any(layer => !availableLayers.Contains(layer))) throw new ArgumentException("Step requests an unknown visual layer.");
            }
            registry = registry ?? new LessonConditionRegistry();
            foreach (var step in plan.Steps) foreach (var condition in step.Conditions) registry.ValidateBinding(condition, simulation.Current);
            evaluators = Array.AsReadOnly(plan.Steps.Select(s => (IReadOnlyList<ILessonConditionEvaluator>)Array.AsReadOnly(s.Conditions.Select(c => registry.Resolve(c.EvaluatorId, simulation.Capabilities)).ToArray())).ToArray());
            free = plan.Steps.Count == 0;
            // Initialization occurs before UI/input binding and before navigation Ready.
            if (!free) Enter(0, false);
            Refresh();
            subscription = simulation.Subscribe(OnSimulation);
        }
        private bool Blocked => closed || busy || dispatching;
        private void OnSimulation(IExperimentSnapshot state) { if (!busy && !closed) Publish(); }
        private void Enter(int target, bool restorePreset)
        {
            if (simulation.Supports(SimulationCommandKind.Pause)) RequireEntry(SimulationCommand.Pause(), target);
            if (restorePreset)
            {
                RequireEntry(SimulationCommand.Reset(), target);
                if (simulation.Supports(SimulationCommandKind.ClearComparison)) RequireEntry(SimulationCommand.ClearComparison(), target);
            }
            foreach (var command in plan.Steps[target].EntryCommands) RequireEntry(command, target);
            // Commit educational identity only after every required action succeeds.
            index = target; completed = free = false; entryError = null;
            facts.Clear(); answer = feedback = null; generation++;
            ActivityBaseline = plan.Steps[target].Conditions.Any(c => c.Evidence != null)
                ? new LessonActivityBaseline(plan.Steps[target].Id, generation, simulation.Current, activityLifetime) : null;
        }
        private void RequireEntry(SimulationCommand command, int target)
        {
            if (!simulation.Execute(command)) throw new EntryFailure("Step '" + plan.Steps[target].Id +
                "' entry failed: " + command.Kind + ". " + (simulation.Current.Error ?? "Command is outside the current simulation domain."));
        }
        private void Refresh()
        {
            var step = plan.Steps.Count == 0 ? null : plan.Steps[index];
            var context = new LessonConditionContext(simulation.Current, facts, answer, ActivityBaseline, step?.Id, generation, activityLifetime);
            bool[] met = step == null ? Array.Empty<bool>() : step.Conditions.Select((c, i) => evaluators[index][i].IsMet(c, context)).ToArray();
            Presentation = new LessonPresentationState(plan.LessonId, step, index, plan.Steps.Count, generation, free,
                completed, entryError == null && !free && !completed && met.All(v => v), feedback, met, entryError);
        }
        private void Publish()
        {
            using (simulation.Notifications.Enter())
            {
                Refresh(); dispatching = true;
                try { Changed?.Invoke(Presentation); } finally { dispatching = false; }
            }
        }
        private bool Transition(Action action)
        {
            if (Blocked) return false;
            using (simulation.Notifications.Enter())
            {
                bool success = true; Exception unexpected = null; busy = true; bool wasCompleted = completed;
                try { action(); }
                catch (EntryFailure exception)
                {
                    entryError = exception.Message; completed = false; success = false;
                    facts.Clear(); answer = feedback = null; generation++;
                    ActivityBaseline = null;
                }
                catch (Exception exception)
                {
                    // Programming/consumer failures remain observable to the caller; never award progress.
                    entryError = "Lesson operation failed: " + exception.Message; completed = false;
                    facts.Clear(); answer = feedback = null; generation++;
                    ActivityBaseline = null;
                    unexpected = exception;
                }
                finally { busy = false; Refresh(); }
                Publish();
                if (unexpected != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(unexpected).Throw();
                if (!wasCompleted && completed)
                {
                    var receipt = new LessonCompletion(this, plan.LessonId, contentVersion, schemaVersion, simulation.Current.Result, generation);
                    dispatching = true;
                    try { CompletionValidated?.Invoke(receipt); }
                    catch { receipt.Cancelled = true; throw; }
                    finally { dispatching = false; }
                }
                return success;
            }
        }
        public bool Next()
        {
            if (Blocked || !Presentation.CanAdvance || simulation.Current.IsClosed || simulation.Current.Error != null || simulation.LastPublicationFailure != null) return false;
            return Transition(() => { if (index + 1 < plan.Steps.Count) Enter(index + 1, false); else { if (simulation.Supports(SimulationCommandKind.Pause)) RequireEntry(SimulationCommand.Pause(), index); completed = true; } });
        }
        public bool Previous()
        {
            if (Blocked || free || index == 0) return false;
            // Back starts the previous activity from its authored preset, rather than retaining future actions.
            return Transition(() => Enter(index - 1, true));
        }
        public bool Restart() => Transition(() =>
        {
            if (plan.Steps.Count != 0) Enter(0, true);
            else ResetExploration();
        });
        private void ResetExploration()
        {
            if (!simulation.Execute(SimulationCommand.Reset()) || (simulation.Supports(SimulationCommandKind.ClearComparison) && !simulation.Execute(SimulationCommand.ClearComparison())))
                throw new EntryFailure("Cannot restore exploration preset. " + simulation.Current.Error);
            entryError = null; completed = false; facts.Clear(); answer = feedback = null; generation++;
            ActivityBaseline = null;
        }
        public bool Explore() => entryError == null && Transition(() => { free = true; generation++; ActivityBaseline = null; if (simulation.Supports(SimulationCommandKind.Pause)) simulation.Execute(SimulationCommand.Pause()); });
        public bool Answer(string answerId)
        {
            if (Blocked || entryError != null || free || completed) return false;
            var option = plan.Steps[index].Answers.FirstOrDefault(a => a.Id == answerId);
            if (option == null) return false;
            return Transition(() => { answer = option.Id; feedback = option.Feedback; });
        }
        public void RecordTermSelection(string formulaId, string termId)
        {
            if (Blocked || entryError != null || free || completed) return;
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
                return Transition(ResetExploration);
            }
            bool success = false;
            Transition(() =>
            {
                var before = simulation.Current;
                var beforeTrajectory = before as SessionState;
                success = simulation.Execute(command);
                if (!success || free) return;
                var after = simulation.Current;
                var afterTrajectory = after as SessionState;
                if ((command.Kind == SimulationCommandKind.SetInput || command.Kind == SimulationCommandKind.SetInputPerturbation) && beforeTrajectory != null && afterTrajectory != null && !beforeTrajectory.Result.Configuration.Controls[command.Index].SequenceEqual(afterTrajectory.Result.Configuration.Controls[command.Index])) facts.Add("input.changed");
                if (command.Kind == SimulationCommandKind.SetParameter && before.Result.RevisionId != after.Result.RevisionId)
                {
                    var oldValue = before.Result.Configuration.Parameters.FirstOrDefault(p => p.Target == command.Target);
                    if (oldValue != null && oldValue.Value != command.Value) facts.Add("parameter.changed:" + command.Target);
                }
                if (command.Kind == SimulationCommandKind.StepForward && beforeTrajectory != null && afterTrajectory != null && afterTrajectory.Sample.Timestep > beforeTrajectory.Sample.Timestep) facts.Add("time.advanced");
                if ((command.Kind == SimulationCommandKind.SetInitialState || command.Kind == SimulationCommandKind.SetInitialPerturbation) && beforeTrajectory != null && afterTrajectory != null &&
                    !beforeTrajectory.Result.Configuration.InitialState.SequenceEqual(afterTrajectory.Result.Configuration.InitialState)) facts.Add("initial.changed");
                if (command.Kind == SimulationCommandKind.CaptureComparison) facts.Add("comparison.saved");
            });
            return success;
        }
        public void Dispose()
        {
            if (closed) return; closed = true; subscription.Dispose(); Changed = null; CompletionValidated = null; facts.Clear(); ActivityBaseline = null;
        }
        internal bool IsCurrentCompletion(LessonCompletion receipt) => !closed && completed && !free && entryError == null
            && Presentation.IsCompleted && Presentation.Error == null && Presentation.ConditionsMet.All(v => v)
            && receipt.Generation == generation && ReferenceEquals(receipt.Result, simulation.Current.Result)
            && !simulation.Current.IsClosed && simulation.Current.Error == null && simulation.LastPublicationFailure == null;
    }
}
