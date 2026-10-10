using System;

namespace OptCon.Simulation
{
    /// <summary>Atomic result: history and the exact immutable configuration of the same revision.</summary>
    public sealed class SimulationResult : IExperimentResult
    {
        public SimulationConfiguration Configuration { get; }
        IExperimentConfiguration IExperimentResult.Configuration => Configuration;
        public SessionState Select(int cursor) => new SessionState(this, Configuration, cursor, RevisionId, null, false, 10, false);
        public TrajectoryHistory History { get; }
        public long RevisionId => History.RevisionId;
        public CostEvaluation Costs { get; }
        public OutputEvaluation Outputs { get; }
        internal SimulationResult(SimulationConfiguration configuration, TrajectoryHistory history)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            History = history ?? throw new ArgumentNullException(nameof(history));
            Costs = configuration.Cost == null ? null : new CostEvaluation(configuration.Cost, history);
            var output = configuration.Model.CreateOutputFunction();
            Outputs = output == null ? null : new OutputEvaluation(output, history);
        }
    }

    /// <summary>A complete publication. Interpret Sample through Result.Configuration, never RequestedConfiguration.</summary>
    public sealed class SessionState : IExperimentSnapshot
    {
        public SimulationResult Result { get; }
        IExperimentResult IExperimentSnapshot.Result => Result;
        public ExperimentCapabilities Capabilities => ExperimentCapabilities.Parameters | ExperimentCapabilities.Trajectory;
        public SimulationSnapshot Sample { get; }
        public ComparisonSnapshot Comparison { get; }
        public SimulationConfiguration RequestedConfiguration { get; }
        public long AttemptRevisionId { get; }
        public string Error { get; }
        public bool IsPlaying { get; }
        public bool IsClosed { get; }
        public double SamplesPerSecond { get; }
        // A failed request is not an input-editing domain. Correct its parameters or reset first.
        public string InputEditingDiagnostic => IsClosed ? "Session closed." : Error != null
            ? "Input editing unavailable after a failed request. Correct the request or Reset. " + Error : null;
        public bool TryGetEditableInput(int index, out OptCon.Core.NumericVector input)
        {
            input = null;
            if (InputEditingDiagnostic != null || index < 0 || index >= RequestedConfiguration.Horizon) return false;
            input = RequestedConfiguration.Controls[index]; return true;
        }
        internal SessionState(SimulationResult result, SimulationConfiguration requested, int cursor,
            long attempt, string error, bool playing, double samplesPerSecond, bool closed, ComparisonSnapshot comparison = null)
        {
            Comparison = comparison; Result = result; Sample = result.History.GetSnapshot(cursor); RequestedConfiguration = requested;
            AttemptRevisionId = attempt; Error = error; IsPlaying = playing; SamplesPerSecond = samplesPerSecond; IsClosed = closed;
        }
    }
}
