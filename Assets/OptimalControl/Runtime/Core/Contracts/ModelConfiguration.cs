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

    public enum ModelParameterKind { Real, Integer, Boolean }

    public sealed class ModelParameterValue
    {
        public string Target { get; }
        public double Value { get; }
        public string Unit { get; }
        public ModelParameterKind Kind { get; }
        public ModelParameterValue(string target, double value, string unit, ModelParameterKind kind = ModelParameterKind.Real)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Parameter target and unit are required.");
            NumericValidation.RequireFinite(value, nameof(value));
            if (!Enum.IsDefined(typeof(ModelParameterKind), kind)) throw new ArgumentException("Unknown parameter kind.");
            if (kind == ModelParameterKind.Integer && value != Math.Truncate(value)) throw new ArgumentException("Integer parameter required.");
            if (kind == ModelParameterKind.Boolean && value != 0 && value != 1) throw new ArgumentException("Boolean parameter requires 0 or 1.");
            Target = target; Value = value; Unit = unit; Kind = kind;
        }
    }
}
