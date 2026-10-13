using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>One frozen evaluation of one history under its own immutable objective. No evaluation in consumers.</summary>
    public sealed class CostEvaluation
    {
        public CostConfiguration Configuration { get; }
        public TrajectoryHistory History { get; }
        public long RevisionId => History.RevisionId;
        public IReadOnlyList<CostContribution> Stages { get; }
        public IReadOnlyList<CostContribution> RunningDensities { get; }
        public IReadOnlyList<double> Cumulative { get; }
        public CostContribution Terminal { get; }
        public double Total { get; }
        internal CostEvaluation(CostConfiguration configuration, TrajectoryHistory history)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            History = history ?? throw new ArgumentNullException(nameof(history)); configuration.Validate(history.Model, history.Horizon);
            var stages = new CostContribution[history.Horizon]; var cumulative = new double[history.Horizon + 1];
            bool continuous = configuration.Semantics == CostSemantics.ContinuousFunctionalLeftRectangle;
            if (continuous && !history.Model.DiscretizationStepSeconds.HasValue) throw new ArgumentException("Quadrature requires physical seconds, never delta=1 by default.");
            var densities = continuous ? new CostContribution[history.Horizon] : null;
            for (int t = 0; t < stages.Length; t++)
            {
                stages[t] = (configuration.EvaluateStage(t, history.States[t], history.Controls[t]) ?? throw new InvalidOperationException("Cost model returned no stage contribution.")).At(CostRole.Stage, t, history.RevisionId);
                if (continuous)
                {
                    densities[t] = stages[t];
                    stages[t] = LeftRectangleQuadrature.IntegrateCell(densities[t], history.Model.DiscretizationStepSeconds.Value, configuration.CostUnit);
                }
                cumulative[t + 1] = cumulative[t] + stages[t].Total;
                NumericValidation.RequireFinite(cumulative[t + 1], "accumulated stage cost");
            }
            Terminal = (configuration.EvaluateTerminal(history.Horizon, history.States[history.Horizon]) ?? throw new InvalidOperationException("Cost model returned no terminal contribution.")).At(CostRole.Terminal, history.Horizon, history.RevisionId);
            Total = cumulative[history.Horizon] + Terminal.Total; NumericValidation.RequireFinite(Total, "total cost");
            Stages = Array.AsReadOnly(stages); Cumulative = Array.AsReadOnly(cumulative);
            RunningDensities = densities == null ? null : Array.AsReadOnly(densities);
        }
        public CostContribution StageAt(int sample)
        {
            if (sample < 0 || sample > History.Horizon) throw new ArgumentOutOfRangeException(nameof(sample));
            return sample == History.Horizon ? null : Stages[sample];
        }
    }
}
