using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Content
{
    /// <summary>Per-asset factory, invoked by the common composition root. No chapter/experiment-ID dispatch.</summary>
    public abstract class ExperimentProviderDefinition : ContentDefinition
    {
        public abstract ExperimentCapabilities Capabilities { get; }
        public abstract IExperimentSession CreateExperimentSession(ExperimentDefinition experiment);
        protected static void ValidateParameterMetadata(ExperimentDefinition experiment, IReadOnlyList<ModelParameterValue> actualParameters)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in experiment.Parameters)
            {
                if (parameter == null || !seen.Add(parameter.ModelTarget ?? string.Empty)) throw new ArgumentException("Parameter targets must be present and unique.");
                var actual = actualParameters.SingleOrDefault(p => p.Target == parameter.ModelTarget);
                if (actual == null) throw new ArgumentException("Unknown provider parameter target: " + parameter.ModelTarget);
                bool kindMatches = (parameter.ValueKind == ParameterValueKind.Real && actual.Kind == ModelParameterKind.Real)
                    || (parameter.ValueKind == ParameterValueKind.Integer && actual.Kind == ModelParameterKind.Integer)
                    || (parameter.ValueKind == ParameterValueKind.Boolean && actual.Kind == ModelParameterKind.Boolean);
                if (!kindMatches || !string.Equals(parameter.Unit, actual.Unit, StringComparison.Ordinal))
                    throw new ArgumentException("Provider parameter metadata mismatch: " + parameter.ModelTarget + ". Expected " + actual.Kind + " [" + actual.Unit + "].");
            }
        }
    }
}
