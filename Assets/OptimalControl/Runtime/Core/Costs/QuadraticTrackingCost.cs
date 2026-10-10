using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Core
{
    /// <summary>Diagonal quadratic tracking, one half convention. Discrete cost per sample, NO physical-step factor.</summary>
    public sealed class QuadraticTrackingCost : CostConfiguration
    {
        public override string Id => "cost.quadratic-tracking.diagonal.v1";
        public override string CostUnit => "1";
        public override NumericVector ReferenceState { get; }
        public NumericVector ReferenceInput { get; }
        public NumericVector StateWeights { get; }
        public NumericVector InputWeights { get; }
        public NumericVector TerminalWeights { get; }
        public IReadOnlyList<string> StateUnits { get; }
        public IReadOnlyList<string> InputUnits { get; }
        public override IReadOnlyList<ModelParameterValue> Parameters { get; }

        public QuadraticTrackingCost(NumericVector referenceState, NumericVector referenceInput,
            NumericVector stateWeights, NumericVector inputWeights, NumericVector terminalWeights,
            IReadOnlyList<string> stateUnits, IReadOnlyList<string> inputUnits)
        {
            ReferenceState = referenceState ?? throw new ArgumentNullException(nameof(referenceState));
            ReferenceInput = referenceInput ?? throw new ArgumentNullException(nameof(referenceInput));
            NumericValidation.RequireDimension(stateWeights, referenceState.Count, nameof(stateWeights));
            NumericValidation.RequireDimension(terminalWeights, referenceState.Count, nameof(terminalWeights));
            NumericValidation.RequireDimension(inputWeights, referenceInput.Count, nameof(inputWeights));
            if (referenceState.Count < 1 || stateUnits == null || inputUnits == null || stateUnits.Count != referenceState.Count || inputUnits.Count != referenceInput.Count
                || stateUnits.Concat(inputUnits).Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Objective dimensions and component units are required.");
            foreach (double q in stateWeights.Concat(terminalWeights)) if (q < 0) throw new ArgumentException("Q and QT must be positive semidefinite.");
            foreach (double r in inputWeights) NumericValidation.RequirePositive(r, nameof(inputWeights));
            StateWeights = stateWeights; InputWeights = inputWeights; TerminalWeights = terminalWeights;
            StateUnits = Array.AsReadOnly(stateUnits.ToArray()); InputUnits = Array.AsReadOnly(inputUnits.ToArray());
            var parameters = new List<ModelParameterValue>();
            Add(parameters, "cost.q.", stateWeights, StateUnits, true); Add(parameters, "cost.r.", inputWeights, InputUnits, true);
            Add(parameters, "cost.qt.", terminalWeights, StateUnits, true); Add(parameters, "cost.target.", referenceState, StateUnits, false);
            Add(parameters, "cost.inputTarget.", referenceInput, InputUnits, false); Parameters = parameters.AsReadOnly();
        }
        private static void Add(List<ModelParameterValue> values, string prefix, NumericVector vector, IReadOnlyList<string> units, bool weight)
        {
            for (int i = 0; i < vector.Count; i++) values.Add(new ModelParameterValue(prefix + i, vector[i], weight && units[i] != "1" ? "1/(" + units[i] + ")²" : units[i]));
        }
        public override void Validate(ModelDescriptor model)
        {
            if (model == null || !model.StateUnits.SequenceEqual(StateUnits) || !model.ControlUnits.SequenceEqual(InputUnits)
                || model.Dimensions.State != ReferenceState.Count || model.Dimensions.Control != ReferenceInput.Count)
                throw new ArgumentException("Cost and dynamics dimensions/units differ.");
        }
        public override CostConfiguration WithParameter(string target, double value)
        {
            NumericValidation.RequireFinite(value, nameof(value));
            var vectors = new[] { StateWeights, InputWeights, TerminalWeights, ReferenceState, ReferenceInput };
            var prefixes = new[] { "cost.q.", "cost.r.", "cost.qt.", "cost.target.", "cost.inputTarget." };
            for (int k = 0; k < prefixes.Length; k++) for (int i = 0; i < vectors[k].Count; i++) if (target == prefixes[k] + i)
            {
                var copy = vectors[k].ToArray(); copy[i] = value; vectors[k] = new NumericVector(copy);
                return new QuadraticTrackingCost(vectors[3], vectors[4], vectors[0], vectors[1], vectors[2], StateUnits, InputUnits);
            }
            throw new ArgumentException("Unknown cost parameter: " + target);
        }
        // The only productive implementation of the weighted quadratic expression.
        private static double WeightedError(NumericVector value, NumericVector reference, NumericVector weights)
        {
            NumericValidation.RequireDimension(value, reference.Count, nameof(value));
            double sum = 0;
            for (int i = 0; i < value.Count; i++)
            {
                if (weights[i] == 0) continue;
                double error = value[i] - reference[i];
                sum += .5 * weights[i] * error * error;
                NumericValidation.RequireFinite(sum, "quadratic cost");
            }
            return sum;
        }
        public override CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input)
        {
            if (timestep < 0) throw new ArgumentOutOfRangeException(nameof(timestep));
            return new CostContribution(WeightedError(state, ReferenceState, StateWeights), WeightedError(input, ReferenceInput, InputWeights));
        }
        public override CostContribution EvaluateTerminal(NumericVector state) => new CostContribution(WeightedError(state, ReferenceState, TerminalWeights), 0);
    }
}
