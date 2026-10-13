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
        public override SampledReference Reference { get; }
        private readonly CostTerm stateTerm = new CostTerm("tracking", "Tracking", "ℓˣ", "Deviazione dello stato dal riferimento", "1", 0);
        private readonly CostTerm inputTerm = new CostTerm("effort", "Effort", "ℓᵘ", "Deviazione del controllo dal riferimento", "1", 0);

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
        public QuadraticTrackingCost(SampledReference reference, NumericVector stateWeights, NumericVector inputWeights,
            NumericVector terminalWeights, CostTerm stateContribution = null, CostTerm inputContribution = null)
            : this((reference ?? throw new ArgumentNullException(nameof(reference))).States[0], reference.Inputs[0],
                stateWeights, inputWeights, terminalWeights, reference.StateUnits, reference.InputUnits)
        {
            Reference = reference;
            stateTerm = stateContribution ?? stateTerm; inputTerm = inputContribution ?? inputTerm;
            if (stateTerm.Id == inputTerm.Id || stateTerm.Unit != CostUnit || inputTerm.Unit != CostUnit)
                throw new ArgumentException("Quadratic contribution IDs must be distinct and units must match CostUnit.");
            var parameters = Parameters.Where(p => !p.Target.StartsWith("cost.target.", StringComparison.Ordinal) && !p.Target.StartsWith("cost.inputTarget.", StringComparison.Ordinal)).ToList();
            for (int t = 0; t <= reference.Horizon; t++) Add(parameters, "cost.reference.state." + t + ".", reference.States[t], StateUnits, false);
            for (int t = 0; t < reference.Horizon; t++) Add(parameters, "cost.reference.input." + t + ".", reference.Inputs[t], InputUnits, false);
            Parameters = parameters.AsReadOnly();
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
            if (Reference != null)
            {
                foreach (var domain in new[] { "state", "input" })
                {
                    string prefix = "cost.reference." + domain + ".";
                    if (!target.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    string[] parts = target.Substring(prefix.Length).Split('.');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int t) || !int.TryParse(parts[1], out int i)) throw new ArgumentException("Invalid reference parameter.");
                    return new QuadraticTrackingCost(Reference.WithComponent(domain == "state", t, i, value), StateWeights, InputWeights, TerminalWeights, stateTerm, inputTerm);
                }
            }
            var vectors = new[] { StateWeights, InputWeights, TerminalWeights, ReferenceState, ReferenceInput };
            var prefixes = new[] { "cost.q.", "cost.r.", "cost.qt.", "cost.target.", "cost.inputTarget." };
            for (int k = 0; k < prefixes.Length; k++) for (int i = 0; i < vectors[k].Count; i++) if (target == prefixes[k] + i)
            {
                var copy = vectors[k].ToArray(); copy[i] = value; vectors[k] = new NumericVector(copy);
                if (Reference != null)
                {
                    if (k > 2) throw new ArgumentException("A sampled reference requires an explicit sample target.");
                    return new QuadraticTrackingCost(Reference, vectors[0], vectors[1], vectors[2], stateTerm, inputTerm);
                }
                return new QuadraticTrackingCost(vectors[3], vectors[4], vectors[0], vectors[1], vectors[2], StateUnits, InputUnits);
            }
            throw new ArgumentException("Unknown cost parameter: " + target);
        }
        // The only productive implementation of the weighted quadratic expression.
        private static double WeightedError(NumericVector value, NumericVector reference, NumericVector weights)
        {
            return WeightedSquaredError.Evaluate(value, reference, weights, .5);
        }
        public override CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input)
        {
            if (timestep < 0) throw new ArgumentOutOfRangeException(nameof(timestep));
            if (Reference != null && timestep >= Reference.Horizon) throw new ArgumentOutOfRangeException(nameof(timestep), "No stage cost at T.");
            return new CostContribution(new[] { stateTerm.WithValue(WeightedError(state, StateReferenceAt(timestep), StateWeights)),
                inputTerm.WithValue(WeightedError(input, Reference == null ? ReferenceInput : Reference.Inputs[timestep], InputWeights)) });
        }
        public override CostContribution EvaluateTerminal(NumericVector state) => EvaluateTerminal(Reference?.Horizon ?? 0, state);
        public override CostContribution EvaluateTerminal(int terminalIndex, NumericVector state)
        {
            if (Reference != null && terminalIndex != Reference.Horizon) throw new ArgumentException("Terminal cost requires reference at T.");
            var term = stateTerm.WithValue(WeightedError(state, StateReferenceAt(terminalIndex), TerminalWeights));
            return new CostContribution(Reference == null ? new[] { term, inputTerm.WithValue(0) } : new[] { term });
        }
    }
}
