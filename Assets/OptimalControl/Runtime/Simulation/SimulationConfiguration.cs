using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    public enum InputEditScope { Single, Tail }

    /// <summary>Frozen mathematical inputs for one candidate/result. No Unity assets or presentation clock.</summary>
    public sealed class SimulationConfiguration
    {
        public string ExperimentId { get; }
        public ModelConfiguration Model { get; }
        public NumericVector InitialState { get; }
        public IReadOnlyList<NumericVector> Controls { get; }
        public NumericVector ReferenceState { get; }
        public int Horizon => Controls.Count;

        public SimulationConfiguration(string experimentId, ModelConfiguration model, NumericVector initialState,
            IReadOnlyList<NumericVector> controls, NumericVector referenceState = null)
        {
            if (string.IsNullOrWhiteSpace(experimentId)) throw new ArgumentException("Experiment ID is required.", nameof(experimentId));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            NumericValidation.RequireDimension(initialState, model.Descriptor.Dimensions.State, nameof(initialState));
            if (controls == null || controls.Count < 1 || controls.Count == int.MaxValue) throw new ArgumentException("A positive representable horizon is required.", nameof(controls));
            var copy = new NumericVector[controls.Count];
            for (int t = 0; t < copy.Length; t++)
            {
                NumericValidation.RequireDimension(controls[t], model.Descriptor.Dimensions.Control, nameof(controls));
                copy[t] = controls[t];
            }
            if (referenceState != null) NumericValidation.RequireDimension(referenceState, model.Descriptor.Dimensions.State, nameof(referenceState));
            ExperimentId = experimentId; InitialState = initialState; Controls = Array.AsReadOnly(copy); ReferenceState = referenceState;
        }

        public SimulationConfiguration WithModel(ModelConfiguration model) => new SimulationConfiguration(ExperimentId, model, InitialState, Controls, ReferenceState);
        public SimulationConfiguration WithInitialState(NumericVector state) => new SimulationConfiguration(ExperimentId, Model, state, Controls, ReferenceState);
        public SimulationConfiguration WithInput(int timestep, NumericVector input, InputEditScope scope)
        {
            if (timestep < 0 || timestep >= Horizon) throw new ArgumentOutOfRangeException(nameof(timestep));
            if (scope != InputEditScope.Single && scope != InputEditScope.Tail) throw new ArgumentOutOfRangeException(nameof(scope));
            NumericValidation.RequireDimension(input, Model.Descriptor.Dimensions.Control, nameof(input));
            var copy = new NumericVector[Horizon];
            for (int t = 0; t < Horizon; t++) copy[t] = t == timestep || (scope == InputEditScope.Tail && t > timestep) ? input : Controls[t];
            return new SimulationConfiguration(ExperimentId, Model, InitialState, copy, ReferenceState);
        }
    }

    /// <summary>Frozen authoring limits; mathematical invariants are also checked by ModelConfiguration.</summary>
    public sealed class ParameterRule
    {
        public string Target { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public bool IntegerOnly { get; }
        public ParameterRule(string target, double minimum, double maximum, bool integerOnly = false)
        {
            if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Parameter target is required.");
            NumericValidation.RequireFinite(minimum, nameof(minimum)); NumericValidation.RequireFinite(maximum, nameof(maximum));
            if (minimum > maximum) throw new ArgumentException("Parameter range is reversed.");
            Target = target; Minimum = minimum; Maximum = maximum; IntegerOnly = integerOnly;
        }
        public void Validate(double value)
        {
            NumericValidation.RequireFinite(value, nameof(value));
            if (value < Minimum || value > Maximum || (IntegerOnly && value != Math.Truncate(value)))
                throw new ArgumentOutOfRangeException(nameof(value), "Value violates the authored range/type for " + Target);
        }
    }
}
