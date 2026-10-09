using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>One immutable rollout: T+1 states and T applied inputs. No cursor or playback state.</summary>
    public sealed class TrajectoryHistory
    {
        public string ExperimentId { get; }
        public long RevisionId { get; }
        public ModelDescriptor Model { get; }
        public int Horizon => Controls.Count;
        public IReadOnlyList<NumericVector> States { get; }
        public IReadOnlyList<NumericVector> Controls { get; }
        internal VectorMetricDescriptor CurrentStateMetric { get; }
        internal VectorMetricDescriptor InputMetric { get; }
        internal VectorMetricDescriptor NextStateMetric { get; }

        internal TrajectoryHistory(string experimentId, long revisionId, ModelDescriptor model,
            NumericVector[] states, NumericVector[] controls)
        {
            ExperimentId = experimentId;
            RevisionId = revisionId;
            Model = model;
            States = Array.AsReadOnly((NumericVector[])states.Clone());
            Controls = Array.AsReadOnly((NumericVector[])controls.Clone());
            CurrentStateMetric = new VectorMetricDescriptor(SimulationMetricIds.CurrentState, model.StateUnits);
            InputMetric = new VectorMetricDescriptor(SimulationMetricIds.AppliedInput, model.ControlUnits);
            NextStateMetric = new VectorMetricDescriptor(SimulationMetricIds.NextState, model.StateUnits);
        }

        public SimulationSnapshot GetSnapshot(int timestep)
        {
            ValidateTimestep(timestep);
            return new SimulationSnapshot(this, timestep);
        }

        public double? PhysicalTimeAt(int timestep)
        {
            ValidateTimestep(timestep);
            return Model.DiscretizationStepSeconds.HasValue ? timestep * Model.DiscretizationStepSeconds.Value : (double?)null;
        }

        private void ValidateTimestep(int timestep)
        {
            if (timestep < 0 || timestep > Horizon) throw new ArgumentOutOfRangeException(nameof(timestep));
        }
    }
}
