using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Core
{
    /// <summary>Desired state/control curve, not a dynamically feasible rollout. T+1 states and T inputs.</summary>
    public sealed class SampledReference
    {
        public IReadOnlyList<NumericVector> States { get; }
        public IReadOnlyList<NumericVector> Inputs { get; }
        public IReadOnlyList<string> StateUnits { get; }
        public IReadOnlyList<string> InputUnits { get; }
        public int Horizon => Inputs.Count;
        public PhysicalTimeGrid PhysicalGrid { get; }
        public SampledReference(IReadOnlyList<NumericVector> states, IReadOnlyList<NumericVector> inputs,
            IReadOnlyList<string> stateUnits, IReadOnlyList<string> inputUnits, PhysicalTimeGrid physicalGrid = null)
        {
            if (inputs == null || inputs.Count < 1 || states == null || states.Count != inputs.Count + 1)
                throw new ArgumentException("Reference requires T inputs and T+1 states, including the terminal reference.");
            if (stateUnits == null || inputUnits == null || stateUnits.Count < 1 ||
                stateUnits.Concat(inputUnits).Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Reference component units are required.");
            foreach (var x in states) NumericValidation.RequireDimension(x, stateUnits.Count, "reference state");
            foreach (var u in inputs) NumericValidation.RequireDimension(u, inputUnits.Count, "reference input");
            States = Array.AsReadOnly(states.ToArray()); Inputs = Array.AsReadOnly(inputs.ToArray());
            StateUnits = Array.AsReadOnly(stateUnits.ToArray()); InputUnits = Array.AsReadOnly(inputUnits.ToArray());
            if (physicalGrid != null && physicalGrid.Transitions != Horizon) throw new ArgumentException("Reference grid/horizon mismatch.");
            PhysicalGrid = physicalGrid;
        }
        public void Validate(ModelDescriptor model, int horizon)
        {
            if (horizon != Horizon) throw new ArgumentException("Reference horizon differs from rollout horizon; no implicit resampling.");
            if (model == null || !StateUnits.SequenceEqual(model.StateUnits) || !InputUnits.SequenceEqual(model.ControlUnits))
                throw new ArgumentException("Reference shape/units differ from the model.");
            if (PhysicalGrid != null && model.DiscretizationStepSeconds != PhysicalGrid.StepSeconds)
                throw new ArgumentException("Reference physical grid differs from model grid.");
        }
        public SampledReference WithComponent(bool state, int sample, int component, double value)
        {
            NumericValidation.RequireFinite(value, nameof(value));
            var list = (state ? States : Inputs).ToArray();
            if (sample < 0 || sample >= list.Length || component < 0 || component >= list[sample].Count)
                throw new ArgumentOutOfRangeException(nameof(sample), "Reference sample/component outside its domain.");
            var copy = list[sample].ToArray(); copy[component] = value; list[sample] = new NumericVector(copy);
            return new SampledReference(state ? list : States, state ? Inputs : list, StateUnits, InputUnits, PhysicalGrid);
        }
    }
}
