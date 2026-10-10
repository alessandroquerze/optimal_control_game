using System;
using System.Collections.Generic;

namespace OptCon.Core
{
    public interface ICostModel
    {
        CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input);
        CostContribution EvaluateTerminal(NumericVector state);
    }

    /// <summary>Immutable objective contract. Weights include the units needed to produce CostUnit.</summary>
    public abstract class CostConfiguration : ICostModel
    {
        public abstract string Id { get; }
        public abstract string CostUnit { get; }
        public abstract NumericVector ReferenceState { get; }
        public abstract IReadOnlyList<ModelParameterValue> Parameters { get; }
        public abstract void Validate(ModelDescriptor model);
        public abstract CostConfiguration WithParameter(string target, double value);
        public abstract CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input);
        public abstract CostContribution EvaluateTerminal(NumericVector state);
    }

    public sealed class CostContribution
    {
        public double Tracking { get; }
        public double Effort { get; }
        public double Total { get; }
        public CostContribution(double tracking, double effort)
        {
            NumericValidation.RequireFinite(tracking, nameof(tracking));
            NumericValidation.RequireFinite(effort, nameof(effort));
            Tracking = tracking; Effort = effort; Total = tracking + effort;
            NumericValidation.RequireFinite(Total, nameof(Total));
        }
    }
}
