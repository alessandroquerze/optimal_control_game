using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    public sealed class ObjectiveConfiguration : IExperimentConfiguration
    {
        public string ExperimentId { get; }
        public NumericVector Decision { get; }
        public DiagonalQuadraticObjective Objective { get; }
        public IReadOnlyList<ModelParameterValue> Parameters { get; }
        public ObjectiveConfiguration(string experimentId, NumericVector decision, NumericVector weights)
        {
            if (string.IsNullOrWhiteSpace(experimentId)) throw new ArgumentException("Experiment ID is required.");
            ExperimentId = experimentId; Objective = new DiagonalQuadraticObjective(weights);
            NumericValidation.RequireDimension(decision, Objective.Dimension, nameof(decision)); Decision = decision;
            Parameters = Array.AsReadOnly(Enumerable.Range(0, decision.Count).Select(i => new ModelParameterValue("decision." + i, decision[i], "1"))
                .Concat(Enumerable.Range(0, weights.Count).Select(i => new ModelParameterValue("objective.weight." + i, weights[i], "1"))).ToArray());
        }
        public ObjectiveConfiguration WithParameter(string target, double value)
        {
            NumericValidation.RequireFinite(value, nameof(value));
            var parameter = Parameters.FirstOrDefault(p => p.Target == target);
            if (parameter == null) throw new ArgumentException("Unknown objective parameter: " + target);
            var decision = Decision.ToArray(); var weights = Objective.Weights.ToArray();
            int offset = Parameters.ToList().IndexOf(parameter);
            if (offset < decision.Length) decision[offset] = value; else weights[offset - decision.Length] = value;
            return new ObjectiveConfiguration(ExperimentId, new NumericVector(decision), new NumericVector(weights));
        }
    }
    public sealed class ObjectiveResult : IExperimentResult
    {
        public ObjectiveConfiguration Configuration { get; }
        IExperimentConfiguration IExperimentResult.Configuration => Configuration;
        public long RevisionId { get; }
        public double Value { get; }
        internal ObjectiveResult(ObjectiveConfiguration configuration, long revision)
        { Configuration = configuration; RevisionId = revision; Value = configuration.Objective.Value(configuration.Decision); }
    }
    public sealed class ObjectiveSnapshot : IExperimentSnapshot
    {
        public ObjectiveResult Result { get; }
        IExperimentResult IExperimentSnapshot.Result => Result;
        public ObjectiveConfiguration RequestedConfiguration { get; }
        public ExperimentCapabilities Capabilities => ExperimentCapabilities.Parameters | ExperimentCapabilities.Objective;
        public long AttemptRevisionId { get; }
        public string Error { get; }
        public bool IsClosed { get; }
        internal ObjectiveSnapshot(ObjectiveResult result, ObjectiveConfiguration requested, long attempt, string error, bool closed)
        { Result = result; RequestedConfiguration = requested; AttemptRevisionId = attempt; Error = error; IsClosed = closed; }
    }
    /// <summary>Value-only static session. No history, cursor, input, timestep, physical time or iteration.</summary>
    public sealed class ObjectiveSession : IExperimentSession
    {
        private readonly ObjectiveConfiguration preset;
        private readonly IReadOnlyList<ParameterRule> rules;
        private ObjectiveConfiguration requested;
        private ObjectiveResult result;
        private long attempt;
        private string error;
        private bool closed, dispatching;
        private event Action<IExperimentSnapshot> changed;
        public ObjectiveSnapshot State { get; private set; }
        public IExperimentSnapshot Current => State;
        public ExperimentCapabilities Capabilities => State.Capabilities;
        public SessionNotificationScope Notifications { get; } = new SessionNotificationScope();
        public SessionPublicationException LastPublicationFailure { get; private set; }
        public ObjectiveSession(ObjectiveConfiguration preset, IReadOnlyList<ParameterRule> rules = null)
        {
            this.preset = requested = preset ?? throw new ArgumentNullException(nameof(preset));
            this.rules = Array.AsReadOnly((rules ?? Array.Empty<ParameterRule>()).ToArray());
            if (this.rules.Any(r => r == null) || this.rules.Select(r => r.Target).Distinct().Count() != this.rules.Count) throw new ArgumentException("Unique parameter rules required.");
            result = new ObjectiveResult(preset, 0); Refresh();
        }
        public bool Supports(SimulationCommandKind command) => command == SimulationCommandKind.Reset || command == SimulationCommandKind.SetParameter;
        public IDisposable Subscribe(Action<IExperimentSnapshot> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (closed) throw new ObjectDisposedException(nameof(ObjectiveSession));
            changed += observer; return new ExperimentSubscription(() => changed -= observer);
        }
        public bool Execute(SimulationCommand command)
        {
            if (closed || dispatching || command == null || !Supports(command.Kind)) return false;
            bool success; attempt = checked(attempt + 1);
            try
            {
                if (command.Kind == SimulationCommandKind.Reset) requested = preset;
                else
                {
                    rules.FirstOrDefault(r => r.Target == command.Target)?.Validate(command.Value);
                    requested = requested.WithParameter(command.Target, command.Value);
                }
                var complete = new ObjectiveResult(requested, attempt);
                result = complete; error = null; success = true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is ArithmeticException || exception is InvalidOperationException)
            { error = exception.Message; success = false; }
            Publish(); return success;
        }
        private void Refresh() => State = new ObjectiveSnapshot(result, requested, attempt, error, closed);
        private void Publish()
        {
            using (Notifications.Enter())
            {
                Refresh(); var publication = State; dispatching = true; LastPublicationFailure = null;
                var failures = new List<Exception>(); var consumers = new List<string>();
                try
                {
                    var callbacks = changed?.GetInvocationList();
                    if (callbacks != null) foreach (Action<IExperimentSnapshot> callback in callbacks)
                    {
                        if (closed) break;
                        try { callback(publication); }
                        catch (Exception exception) { failures.Add(exception); consumers.Add(callback.Method.DeclaringType?.FullName + "." + callback.Method.Name); }
                    }
                }
                finally { dispatching = false; }
                if (failures.Count != 0) { LastPublicationFailure = new SessionPublicationException(publication, consumers, failures); throw LastPublicationFailure; }
            }
        }
        public void Dispose() { if (closed) return; closed = true; changed = null; Refresh(); }
    }
}
