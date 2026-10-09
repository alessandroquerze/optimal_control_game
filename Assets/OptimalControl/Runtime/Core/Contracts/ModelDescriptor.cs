using System;
using System.Collections.Generic;

namespace OptCon.Core
{
    /// <summary>Immutable model identity, component units and optional physical discretization step.</summary>
    public sealed class ModelDescriptor
    {
        public string Id { get; }
        public string Version { get; }
        public ModelDimensions Dimensions { get; }
        public IReadOnlyList<string> StateUnits { get; }
        public IReadOnlyList<string> ControlUnits { get; }
        public IReadOnlyList<string> OutputUnits { get; }
        public double? DiscretizationStepSeconds { get; }

        public ModelDescriptor(string id, string version, ModelDimensions dimensions,
            IReadOnlyList<string> stateUnits, IReadOnlyList<string> controlUnits,
            IReadOnlyList<string> outputUnits, double? discretizationStepSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Model ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Model version is required.", nameof(version));
            if (dimensions.State <= 0) throw new ArgumentException("State dimension must be positive.", nameof(dimensions));
            if (discretizationStepSeconds.HasValue)
                NumericValidation.RequirePositive(discretizationStepSeconds.Value, nameof(discretizationStepSeconds));
            Id = id;
            Version = version;
            Dimensions = dimensions;
            StateUnits = CopyUnits(stateUnits, dimensions.State, nameof(stateUnits));
            ControlUnits = CopyUnits(controlUnits, dimensions.Control, nameof(controlUnits));
            OutputUnits = CopyUnits(outputUnits, dimensions.Output, nameof(outputUnits));
            DiscretizationStepSeconds = discretizationStepSeconds;
        }

        private static IReadOnlyList<string> CopyUnits(IReadOnlyList<string> units, int count, string name)
        {
            if (units == null) throw new ArgumentNullException(name);
            if (units.Count != count) throw new ArgumentException("Units must match model dimensions.", name);
            var copy = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrWhiteSpace(units[i])) throw new ArgumentException("Each component needs a unit (use 1 for dimensionless values).", name);
                copy[i] = units[i];
            }
            return Array.AsReadOnly(copy);
        }
    }
}
