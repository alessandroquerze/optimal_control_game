using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Saved h, g and r diagnostics. Bounds diagnose, they never alter applied inputs or solve a problem.</summary>
    public sealed class ConstraintEvaluation
    {
        public ConstraintConfiguration Configuration { get; }
        public TrajectoryHistory History { get; }
        public IReadOnlyList<NumericVector> DynamicsResiduals { get; }
        public IReadOnlyList<NumericVector> DesiredDynamicsResiduals { get; }
        public IReadOnlyList<IReadOnlyList<double>> PathResiduals { get; }
        public IReadOnlyList<double> EndpointResiduals { get; }
        public bool IsFeasible { get; }
        public bool DesiredSatisfiesDynamics { get; }
        internal ConstraintEvaluation(ConstraintConfiguration configuration, ModelConfiguration model, TrajectoryHistory history, SampledReference desired)
        {
            Configuration = configuration; History = history; configuration.Validate(history.Model);
            var dynamics = model.CreateDynamics(); int T = history.Horizon;
            var h = new NumericVector[T]; var hd = desired == null ? null : new NumericVector[T];
            var g = new IReadOnlyList<double>[T];
            for (int t = 0; t < T; t++)
            {
                h[t] = Residual(dynamics.Evaluate(t, history.States[t], history.Controls[t]), history.States[t + 1]);
                if (hd != null) hd[t] = Residual(dynamics.Evaluate(t, desired.States[t], desired.Inputs[t]), desired.States[t + 1]);
                g[t] = Array.AsReadOnly(configuration.Path.Select(b => b.Evaluate(history.States[t], history.Controls[t])).ToArray());
            }
            DynamicsResiduals = Array.AsReadOnly(h); DesiredDynamicsResiduals = hd == null ? null : Array.AsReadOnly(hd);
            PathResiduals = Array.AsReadOnly(g);
            EndpointResiduals = Array.AsReadOnly(configuration.Endpoints.Select(e => e.Evaluate(history.States[0], history.States[T])).ToArray());
            bool Zero(IEnumerable<NumericVector> values) => values.All(v => v.All(x => Math.Abs(x) <= configuration.Tolerance));
            IsFeasible = Zero(h) && g.All(row => row.All(x => x <= configuration.Tolerance)) && EndpointResiduals.All(x => Math.Abs(x) <= configuration.Tolerance);
            DesiredSatisfiesDynamics = hd != null && Zero(hd);
        }
        private static NumericVector Residual(NumericVector predicted, NumericVector next)
        {
            NumericValidation.RequireDimension(predicted, next.Count, "constraint dynamics result");
            var values = new double[next.Count]; for (int i = 0; i < values.Length; i++) values[i] = predicted[i] - next[i];
            return new NumericVector(values);
        }
    }
}
