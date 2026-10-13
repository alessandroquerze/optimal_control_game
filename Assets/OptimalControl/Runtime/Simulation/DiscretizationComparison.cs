using System;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Comparison of independently sampled grids; equality means the same frozen physical laws/objective.</summary>
    public sealed class DiscretizationComparison
    {
        public SimulationResult A { get; }
        public SimulationResult B { get; }
        public bool SamePhysicalProblem { get; }
        public double? TotalDifference { get; }
        public NumericVector TerminalStateDifference { get; }
        public DiscretizationComparison(SimulationResult a, SimulationResult b)
        {
            A = a; B = b;
            var sa = a.Configuration.ContinuousSampling; var sb = b.Configuration.ContinuousSampling;
            if (sa == null || sb == null) throw new ArgumentException("Comparison requires physical sampling laws.");
            var ca = (ContinuousTrackingCost)a.Configuration.Cost; var cb = (ContinuousTrackingCost)b.Configuration.Cost;
            SamePhysicalProblem = a.Configuration.ExperimentId == b.Configuration.ExperimentId && sa.DurationSeconds == sb.DurationSeconds &&
                ReferenceEquals(sa.StateReference, sb.StateReference) && ReferenceEquals(sa.InputReference, sb.InputReference) && ReferenceEquals(sa.ControlLaw, sb.ControlLaw) &&
                a.Configuration.Model.Descriptor.Id == b.Configuration.Model.Descriptor.Id && a.Configuration.Model.Descriptor.Version == b.Configuration.Model.Descriptor.Version &&
                a.Configuration.Model.Parameters.Select(p => (p.Target, p.Value, p.Unit)).SequenceEqual(b.Configuration.Model.Parameters.Select(p => (p.Target, p.Value, p.Unit))) &&
                a.Configuration.InitialState.SequenceEqual(b.Configuration.InitialState) && ca.StateWeights.SequenceEqual(cb.StateWeights) &&
                ca.InputWeights.SequenceEqual(cb.InputWeights) && ca.TerminalWeights.SequenceEqual(cb.TerminalWeights) && ReferenceEquals(a.Configuration.Constraints, b.Configuration.Constraints);
            double difference = b.Costs.Total - a.Costs.Total;
            TotalDifference = NumericValidation.IsFinite(difference) ? difference : (double?)null;
            var terminal = b.History.States[b.History.Horizon].Select((v, i) => v - a.History.States[a.History.Horizon][i]).ToArray();
            TerminalStateDifference = terminal.All(NumericValidation.IsFinite) ? new NumericVector(terminal) : null;
        }
    }
}
