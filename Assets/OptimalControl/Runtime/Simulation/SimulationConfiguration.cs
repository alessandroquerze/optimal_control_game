using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    public enum InputEditScope { Single, Tail }

    /// <summary>Frozen mathematical inputs for one candidate/result. No Unity assets or presentation clock.</summary>
    public sealed class SimulationConfiguration : IExperimentConfiguration
    {
        public string ExperimentId { get; }
        public ModelConfiguration Model { get; }
        public NumericVector InitialState { get; }
        public IReadOnlyList<NumericVector> Controls { get; }
        private readonly NumericVector referenceState;
        public CostConfiguration Cost { get; }
        public ConstraintConfiguration Constraints { get; }
        public ContinuousSampling ContinuousSampling { get; }
        public SampledReference Reference => Cost?.Reference;
        /// <summary>Legacy sample-zero view. Runtime consumers must use StateReferenceAt(sample).</summary>
        public NumericVector ReferenceState => Cost?.ReferenceState ?? referenceState;
        public NumericVector StateReferenceAt(int sample) => Cost == null ? referenceState : Cost.StateReferenceAt(sample);
        public int Horizon => Controls.Count;
        public IReadOnlyList<ModelParameterValue> Parameters { get; }

        public SimulationConfiguration(string experimentId, ModelConfiguration model, NumericVector initialState,
            IReadOnlyList<NumericVector> controls, NumericVector referenceState = null, CostConfiguration cost = null, ConstraintConfiguration constraints = null,
            ContinuousSampling continuousSampling = null)
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
            cost?.Validate(model.Descriptor, controls.Count); Cost = cost;
            constraints?.Validate(model.Descriptor); Constraints = constraints;
            continuousSampling?.Validate(model.Descriptor, controls, cost); ContinuousSampling = continuousSampling;
            Parameters = Array.AsReadOnly(model.Parameters.Concat(cost?.Parameters ?? Array.Empty<ModelParameterValue>()).Concat(constraints?.Parameters ?? Array.Empty<ModelParameterValue>()).ToArray());
            ExperimentId = experimentId; InitialState = initialState; Controls = Array.AsReadOnly(copy); this.referenceState = referenceState;
        }

        public SimulationConfiguration WithModel(ModelConfiguration model) => ContinuousSampling == null
            ? new SimulationConfiguration(ExperimentId, model, InitialState, Controls, referenceState, Cost, Constraints)
            : ContinuousSampling.Compose(ExperimentId, model, InitialState, (ContinuousTrackingCost)Cost, Constraints);
        public SimulationConfiguration WithInitialState(NumericVector state) => new SimulationConfiguration(ExperimentId, Model, state, Controls, referenceState, Cost, Constraints, ContinuousSampling);
        public SimulationConfiguration WithCost(CostConfiguration cost) => new SimulationConfiguration(ExperimentId, Model, InitialState, Controls, referenceState, cost, Constraints, ContinuousSampling);
        public SimulationConfiguration WithConstraints(ConstraintConfiguration constraints) => new SimulationConfiguration(ExperimentId, Model, InitialState, Controls, referenceState, Cost, constraints, ContinuousSampling);
        public SimulationConfiguration WithHorizon(int horizon)
        {
            if (ContinuousSampling != null) throw new ArgumentException("H is fixed: change physical delta to resample N, input and reference together.");
            if (horizon < 1 || horizon > 10000) throw new ArgumentOutOfRangeException(nameof(horizon), "Runtime horizon must be in [1,10000].");
            var controls = new NumericVector[horizon];
            for (int t = 0; t < horizon; t++) controls[t] = Controls[Math.Min(t, Horizon - 1)];
            return new SimulationConfiguration(ExperimentId, Model, InitialState, controls, referenceState, Cost, Constraints);
        }
        public SimulationConfiguration WithInput(int timestep, NumericVector input, InputEditScope scope)
        {
            if (timestep < 0 || timestep >= Horizon) throw new ArgumentOutOfRangeException(nameof(timestep));
            if (scope != InputEditScope.Single && scope != InputEditScope.Tail) throw new ArgumentOutOfRangeException(nameof(scope));
            NumericValidation.RequireDimension(input, Model.Descriptor.Dimensions.Control, nameof(input));
            if (ContinuousSampling != null)
                return ContinuousSampling.EditInput(((ContinuousTrackingCost)Cost).Grid, timestep, input, scope).Compose(ExperimentId, Model, InitialState, (ContinuousTrackingCost)Cost, Constraints);
            var copy = new NumericVector[Horizon];
            for (int t = 0; t < Horizon; t++) copy[t] = t == timestep || (scope == InputEditScope.Tail && t > timestep) ? input : Controls[t];
            return new SimulationConfiguration(ExperimentId, Model, InitialState, copy, referenceState, Cost, Constraints);
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
