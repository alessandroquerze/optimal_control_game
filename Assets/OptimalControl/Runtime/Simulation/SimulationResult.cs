using System;

namespace OptCon.Simulation
{
    /// <summary>Atomic result: history and the exact immutable configuration of the same revision.</summary>
    public sealed class SimulationResult
    {
        public SimulationConfiguration Configuration { get; }
        public TrajectoryHistory History { get; }
        public long RevisionId => History.RevisionId;
        internal SimulationResult(SimulationConfiguration configuration, TrajectoryHistory history)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            History = history ?? throw new ArgumentNullException(nameof(history));
        }
    }

    /// <summary>A complete publication. Interpret Sample through Result.Configuration, never RequestedConfiguration.</summary>
    public sealed class SessionState
    {
        public SimulationResult Result { get; }
        public SimulationSnapshot Sample { get; }
        public SimulationConfiguration RequestedConfiguration { get; }
        public long AttemptRevisionId { get; }
        public string Error { get; }
        public bool IsPlaying { get; }
        public bool IsClosed { get; }
        public double SamplesPerSecond { get; }
        internal SessionState(SimulationResult result, SimulationConfiguration requested, int cursor,
            long attempt, string error, bool playing, double samplesPerSecond, bool closed)
        {
            Result = result; Sample = result.History.GetSnapshot(cursor); RequestedConfiguration = requested;
            AttemptRevisionId = attempt; Error = error; IsPlaying = playing; SamplesPerSecond = samplesPerSecond; IsClosed = closed;
        }
    }
}
