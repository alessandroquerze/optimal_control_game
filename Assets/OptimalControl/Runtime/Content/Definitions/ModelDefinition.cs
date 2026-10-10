using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Content
{
    /// <summary>Converts authored data once into pure immutable runtime configuration.</summary>
    public abstract class ModelDefinition : ContentDefinition
    {
        public abstract ModelDimensions Dimensions { get; }
        protected abstract ModelConfiguration CreateModel(double? physicalStep, IReadOnlyDictionary<string, double> parameters);

        public SimulationConfiguration CreateConfiguration(ExperimentDefinition experiment)
        {
            if (experiment == null || !experiment.UsesDiscreteTime || experiment.DiscreteTime == null)
                throw new ArgumentException("A discrete experiment preset is required.", nameof(experiment));
            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var parameter in experiment.Parameters)
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.ModelTarget) || values.ContainsKey(parameter.ModelTarget))
                    throw new ArgumentException("Parameter targets must be present and unique.");
                Rule(parameter).Validate(parameter.DefaultValue);
                if (parameter.Recalculation != ParameterRecalculation.FullRollout)
                    throw new ArgumentException("This dynamics provider requires FullRollout parameters.");
                values.Add(parameter.ModelTarget, parameter.DefaultValue);
            }
            var model = CreateModel(experiment.DiscreteTime.StepSeconds, values);
            foreach (var parameter in experiment.Parameters)
            {
                var actual = model.Parameters.SingleOrDefault(p => p.Target == parameter.ModelTarget);
                if (actual == null) throw new ArgumentException("Unknown provider parameter target: " + parameter.ModelTarget);
                bool kindMatches = (parameter.ValueKind == ParameterValueKind.Real && actual.Kind == ModelParameterKind.Real)
                    || (parameter.ValueKind == ParameterValueKind.Integer && actual.Kind == ModelParameterKind.Integer)
                    || (parameter.ValueKind == ParameterValueKind.Boolean && actual.Kind == ModelParameterKind.Boolean);
                if (!kindMatches || !string.Equals(parameter.Unit, actual.Unit, StringComparison.Ordinal))
                    throw new ArgumentException("Provider parameter metadata mismatch: " + parameter.ModelTarget + ". Expected " + actual.Kind + " [" + actual.Unit + "].");
            }
            var descriptor = model.Descriptor;
            if (experiment.StateDimension != descriptor.Dimensions.State || experiment.ControlDimension != descriptor.Dimensions.Control || experiment.OutputDimension != descriptor.Dimensions.Output
                || !experiment.StateUnits.SequenceEqual(descriptor.StateUnits) || !experiment.ControlUnits.SequenceEqual(descriptor.ControlUnits) || !experiment.OutputUnits.SequenceEqual(descriptor.OutputUnits))
                throw new ArgumentException("Experiment dimensions/units differ from the mathematical provider.");
            int horizon = experiment.DiscreteTime.Horizon;
            if (horizon < 1 || horizon == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(horizon));
            var input = new NumericVector(experiment.DiscreteTime.CopyDefaultControl());
            var controls = new NumericVector[horizon];
            for (int t = 0; t < horizon; t++) controls[t] = input;
            var reference = experiment.DiscreteTime.CopyReferenceState();
            return new SimulationConfiguration(experiment.Id, model, new NumericVector(experiment.DiscreteTime.CopyInitialState()),
                controls, reference.Length == 0 ? null : new NumericVector(reference), experiment.Cost?.CreateConfiguration(experiment));
        }

        public SimulationSession CreateSession(ExperimentDefinition experiment) =>
            new SimulationSession(CreateConfiguration(experiment), experiment.Parameters.Select(Rule).ToArray());

        private static ParameterRule Rule(LessonParameterDefinition parameter) => new ParameterRule(parameter.ModelTarget,
            parameter.Minimum, parameter.Maximum, parameter.ValueKind != ParameterValueKind.Real);
    }
}
