using System.Collections.Generic;
using System.Collections.ObjectModel;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Immutable view of stored samples. Reading or rewinding never evaluates the model.</summary>
    public sealed class SimulationSnapshot
    {
        public TrajectoryHistory History { get; }
        public string ExperimentId => History.ExperimentId;
        public long RevisionId => History.RevisionId;
        public ModelDescriptor Model => History.Model;
        public int Timestep { get; }
        public int Horizon => History.Horizon;
        public double? DiscretizationStepSeconds => Model.DiscretizationStepSeconds;
        public double? PhysicalTimeSeconds => History.PhysicalTimeAt(Timestep);
        public bool IsTerminal => Timestep == Horizon;
        public NumericVector CurrentState => History.States[Timestep];
        public NumericVector AppliedControl => IsTerminal ? null : History.Controls[Timestep];
        public NumericVector NextState => IsTerminal ? null : History.States[Timestep + 1];
        public IReadOnlyDictionary<string, VectorMetric> Metrics { get; }

        internal SimulationSnapshot(TrajectoryHistory history, int timestep)
        {
            History = history;
            Timestep = timestep;
            Metrics = new ReadOnlyDictionary<string, VectorMetric>(new Dictionary<string, VectorMetric>
            {
                [SimulationMetricIds.CurrentState] = new VectorMetric(history.CurrentStateMetric, timestep, CurrentState),
                [SimulationMetricIds.AppliedInput] = new VectorMetric(history.InputMetric, IsTerminal ? (int?)null : timestep, AppliedControl),
                [SimulationMetricIds.NextState] = new VectorMetric(history.NextStateMetric, IsTerminal ? (int?)null : timestep + 1, NextState)
            });
        }
    }
}
