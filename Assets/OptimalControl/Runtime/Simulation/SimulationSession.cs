using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Single owner of requested configuration, complete results, revisions and playback cursor.</summary>
    public sealed class SimulationSession : ICommandSink, IDisposable
    {
        private readonly SimulationConfiguration preset;
        private readonly Dictionary<string, ParameterRule> rules = new Dictionary<string, ParameterRule>(StringComparer.Ordinal);
        private SimulationConfiguration requested;
        private SimulationResult result;
        private ComparisonSnapshot comparison;
        private int cursor;
        private long attempt;
        private bool playing, dispatching, closed;
        private double rate, accumulator;
        private string error;
        public SessionState State { get; private set; }
        public event Action<SessionState> Changed;
        public SessionNotificationScope Notifications { get; } = new SessionNotificationScope();
        public SessionPublicationException LastPublicationFailure { get; private set; }

        public SimulationSession(SimulationConfiguration preset, IReadOnlyList<ParameterRule> parameterRules = null, double samplesPerSecond = 10)
        {
            this.preset = preset ?? throw new ArgumentNullException(nameof(preset));
            NumericValidation.RequirePositive(samplesPerSecond, nameof(samplesPerSecond));
            rate = samplesPerSecond;
            if (parameterRules != null) foreach (var rule in parameterRules)
            {
                if (rule == null || rules.ContainsKey(rule.Target)) throw new ArgumentException("Parameter rules must have unique targets.", nameof(parameterRules));
                rules.Add(rule.Target, rule);
            }
            requested = preset;
            result = Calculate(preset, 0);
            Refresh();
        }

        public bool Execute(SimulationCommand command)
        {
            if (closed || dispatching || command == null) return false;
            switch (command.Kind)
            {
                case SimulationCommandKind.Reset: return Revise(() => preset, true);
                case SimulationCommandKind.SetHorizon: return Revise(() => requested.WithHorizon(command.Index));
                case SimulationCommandKind.SetInput: return Revise(() => requested.WithInput(command.Index, command.Vector, command.Scope));
                case SimulationCommandKind.SetParameter: return Revise(() =>
                {
                    if (rules.TryGetValue(command.Target ?? string.Empty, out var rule)) rule.Validate(command.Value);
                    return requested.Cost != null && requested.Cost.Parameters.Any(p => p.Target == command.Target)
                        ? requested.WithCost(requested.Cost.WithParameter(command.Target, command.Value))
                        : requested.WithModel(requested.Model.WithParameter(command.Target, command.Value));
                });
                case SimulationCommandKind.SetInitialState: return Revise(() => requested.WithInitialState(command.Vector));
                case SimulationCommandKind.SetPhysicalStep: return Revise(() => requested.WithModel(requested.Model.WithPhysicalStep(command.PhysicalStep)));
            }
            try
            {
                switch (command.Kind)
                {
                    case SimulationCommandKind.CaptureComparison: comparison = new ComparisonSnapshot(result); break;
                    case SimulationCommandKind.ClearComparison: comparison = null; break;
                    case SimulationCommandKind.Play: playing = cursor < result.History.Horizon && error == null; break;
                    case SimulationCommandKind.Pause: playing = false; accumulator = 0; break;
                    case SimulationCommandKind.StepForward: Select(Math.Min(cursor + 1, result.History.Horizon)); break;
                    case SimulationCommandKind.StepBackward: Select(Math.Max(cursor - 1, 0)); break;
                    case SimulationCommandKind.Seek:
                        if (command.Index < 0 || command.Index > result.History.Horizon) throw new ArgumentOutOfRangeException(nameof(command.Index));
                        Select(command.Index); break;
                    case SimulationCommandKind.SetPlaybackRate:
                        NumericValidation.RequirePositive(command.Value, nameof(command.Value)); rate = command.Value; accumulator = 0; break;
                    default: return false;
                }
            }
            catch (ArgumentException) { return false; }
            Publish(); return true;
        }

        private bool Revise(Func<SimulationConfiguration> candidate, bool reset = false)
        {
            attempt = checked(attempt + 1);
            playing = false; accumulator = 0;
            bool success;
            try
            {
                requested = candidate();
                var complete = CalculateCandidate(requested, attempt);
                result = complete; error = null;
                cursor = reset ? 0 : Math.Min(cursor, result.History.Horizon);
                success = true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is ArithmeticException || exception is InvalidOperationException || exception is RolloutException)
            {
                // Keep both history AND configuration of the last successful revision together.
                error = exception.Message; success = false;
            }
            Publish(); return success;
        }

        private static SimulationResult Calculate(SimulationConfiguration configuration, long revision) =>
            new SimulationResult(configuration, RolloutEngine.Rollout(configuration.Model.CreateDynamics(),
                configuration.InitialState, configuration.Controls, configuration.ExperimentId, revision));

        private SimulationResult CalculateCandidate(SimulationConfiguration configuration, long revision)
        {
            var old = result.Configuration;
            if (!ReferenceEquals(configuration.Model, old.Model) || !ReferenceEquals(configuration.InitialState, old.InitialState) || configuration.Horizon != old.Horizon)
                return Calculate(configuration, revision);
            int first = 0;
            while (first < configuration.Horizon && ReferenceEquals(configuration.Controls[first], old.Controls[first])) first++;
            var dynamics = configuration.Model.CreateDynamics();
            if (!ReferenceEquals(dynamics.Descriptor, result.History.Model))
                return new SimulationResult(configuration, RolloutEngine.Rollout(dynamics,
                    configuration.InitialState, configuration.Controls, configuration.ExperimentId, revision));
            return new SimulationResult(configuration, RolloutEngine.RecalculateFrom(result.History,
                dynamics, configuration.Controls, first, revision));
        }

        private void Select(int index) { cursor = index; playing = false; accumulator = 0; }

        /// <summary>Presentation seconds only. Advancing/rewinding never evaluates the dynamics.</summary>
        public void AdvancePlayback(double elapsedSeconds)
        {
            if (closed || dispatching) return;
            NumericValidation.RequireFinite(elapsedSeconds, nameof(elapsedSeconds));
            if (elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (!playing) return;
            double pending = accumulator + elapsedSeconds * rate;
            if (!NumericValidation.IsFinite(pending)) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            // Tolerance is only for presentation-clock roundoff, never for model values.
            double whole = Math.Floor(pending + 1e-9);
            if (whole < 1) { accumulator = pending; return; }
            int advance = (int)Math.Min(whole, result.History.Horizon - cursor);
            cursor += advance; accumulator = Math.Max(0, pending - whole);
            if (cursor == result.History.Horizon) { playing = false; accumulator = 0; }
            Publish();
        }

        private void Refresh() => State = new SessionState(result, requested, cursor, attempt, error, playing, rate, closed, comparison);
        private void Publish()
        {
            using (Notifications.Enter())
            {
                Refresh(); var publication = State; dispatching = true;
                LastPublicationFailure = null;
                var failures = new List<Exception>(); var consumers = new List<string>();
                try
                {
                    var callbacks = Changed?.GetInvocationList();
                    if (callbacks != null) foreach (Action<SessionState> callback in callbacks)
                    {
                        if (closed) break;
                        try { callback(publication); }
                        catch (Exception exception)
                        {
                            failures.Add(exception);
                            consumers.Add(callback.Method.DeclaringType?.FullName + "." + callback.Method.Name);
                        }
                    }
                }
                finally { dispatching = false; }
                if (failures.Count != 0)
                {
                    LastPublicationFailure = new SessionPublicationException(publication, consumers, failures);
                    throw LastPublicationFailure;
                }
            }
        }

        public void Dispose()
        {
            if (closed) return;
            comparison = null; closed = true; playing = false; accumulator = 0; Changed = null; Refresh();
        }
    }
}
