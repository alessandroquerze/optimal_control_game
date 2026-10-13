using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Frozen desired outputs and deviation samples; the output map is evaluated here, never in a consumer.</summary>
    public sealed class ReferenceEvaluation
    {
        public SampledReference Reference { get; }
        public TrajectoryHistory History { get; }
        public IReadOnlyList<NumericVector> Outputs { get; }
        public IReadOnlyList<NumericVector> StateErrors { get; }
        internal ReferenceEvaluation(SampledReference reference, TrajectoryHistory history, IOutputFunction output)
        {
            Reference = reference; History = history; reference.Validate(history.Model, history.Horizon);
            var outputs = new NumericVector[history.Horizon + 1]; var errors = new NumericVector[outputs.Length];
            for (int t = 0; t < outputs.Length; t++)
            {
                var e = new double[history.Model.Dimensions.State];
                for (int i = 0; i < e.Length; i++) e[i] = history.States[t][i] - reference.States[t][i];
                errors[t] = new NumericVector(e);
                if (output == null || (t == history.Horizon && output.RequiresControl)) continue;
                outputs[t] = output.Evaluate(t, reference.States[t], t < history.Horizon ? reference.Inputs[t] : null);
                NumericValidation.RequireDimension(outputs[t], history.Model.Dimensions.Output, "desired output");
            }
            Outputs = Array.AsReadOnly(outputs); StateErrors = Array.AsReadOnly(errors);
        }
    }
}
