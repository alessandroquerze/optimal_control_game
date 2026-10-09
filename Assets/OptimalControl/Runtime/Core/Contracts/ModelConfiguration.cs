using System;
using System.Collections.Generic;

namespace OptCon.Core
{
    /// <summary>Pure, immutable mathematical configuration. Implementations must freeze all their data.</summary>
    public abstract class ModelConfiguration
    {
        public abstract ModelDescriptor Descriptor { get; }
        public abstract IReadOnlyList<ModelParameterValue> Parameters { get; }
        public abstract IDiscreteDynamics CreateDynamics();
        public abstract ModelConfiguration WithParameter(string target, double value);
        public abstract ModelConfiguration WithPhysicalStep(double? seconds);
    }

    public sealed class ModelParameterValue
    {
        public string Target { get; }
        public double Value { get; }
        public string Unit { get; }
        public ModelParameterValue(string target, double value, string unit)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Parameter target and unit are required.");
            NumericValidation.RequireFinite(value, nameof(value));
            Target = target; Value = value; Unit = unit;
        }
    }
}
