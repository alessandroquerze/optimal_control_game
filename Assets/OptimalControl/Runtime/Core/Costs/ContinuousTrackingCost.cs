using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Core
{
    /// <summary>Integral running density and terminal objective, WITHOUT the half factor (source I p.36).
    /// EvaluateStage returns a density, never an integral contribution. Quadrature belongs to Simulation.</summary>
    public sealed class ContinuousTrackingCost : CostConfiguration
    {
        public override string Id => "cost.continuous-tracking.left-rectangle.v1";
        public override string CostUnit => "1";
        public override string RunningUnit => "1/s";
        public override CostSemantics Semantics => CostSemantics.ContinuousFunctionalLeftRectangle;
        public PhysicalTimeGrid Grid => Reference.PhysicalGrid;
        public override SampledReference Reference { get; }
        public override NumericVector ReferenceState => Reference.States[0];
        public NumericVector StateWeights { get; }
        public NumericVector InputWeights { get; }
        public NumericVector TerminalWeights { get; }
        public override IReadOnlyList<ModelParameterValue> Parameters { get; }
        public ContinuousTrackingCost(SampledReference reference, NumericVector q, NumericVector r, NumericVector qt)
        {
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            if (reference.PhysicalGrid == null) throw new ArgumentException("Continuous quadrature requires an explicit physical reference grid.");
            NumericValidation.RequireDimension(q, reference.StateUnits.Count, nameof(q));
            NumericValidation.RequireDimension(qt, q.Count, nameof(qt)); NumericValidation.RequireDimension(r, reference.InputUnits.Count, nameof(r));
            if (q.Concat(qt).Any(v => v < 0)) throw new ArgumentException("Q and QT must be nonnegative.");
            foreach (var value in r) NumericValidation.RequirePositive(value, nameof(r));
            StateWeights = q; InputWeights = r; TerminalWeights = qt;
            var parameters = new List<ModelParameterValue>();
            Add(parameters, "cost.q.", q, reference.StateUnits, true);
            Add(parameters, "cost.r.", r, reference.InputUnits, true);
            Add(parameters, "cost.qt.", qt, reference.StateUnits, false);
            Parameters = parameters.AsReadOnly();
        }
        private static void Add(List<ModelParameterValue> list, string prefix, NumericVector values, IReadOnlyList<string> units, bool running)
        {
            for (int i = 0; i < values.Count; i++) list.Add(new ModelParameterValue(prefix + i, values[i],
                (units[i] == "1" ? "1" : "1/(" + units[i] + ")²") + (running ? "/s" : "")));
        }
        public override void Validate(ModelDescriptor model)
        {
            if (!model.DiscretizationStepSeconds.HasValue) throw new ArgumentException("Index-only dynamics cannot evaluate a continuous functional.");
            Reference.Validate(model, Grid.Transitions);
        }
        public override CostConfiguration WithParameter(string target, double value)
        {
            NumericValidation.RequireFinite(value, nameof(value));
            var vectors = new[] { StateWeights, InputWeights, TerminalWeights };
            var prefixes = new[] { "cost.q.", "cost.r.", "cost.qt." };
            for (int k = 0; k < vectors.Length; k++) for (int i = 0; i < vectors[k].Count; i++) if (target == prefixes[k] + i)
            {
                var copy = vectors[k].ToArray(); copy[i] = value; vectors[k] = new NumericVector(copy);
                return new ContinuousTrackingCost(Reference, vectors[0], vectors[1], vectors[2]);
            }
            throw new ArgumentException("Unknown continuous-cost parameter: " + target);
        }
        public ContinuousTrackingCost OnReference(SampledReference reference) => new ContinuousTrackingCost(reference, StateWeights, InputWeights, TerminalWeights);
        public override CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input)
        {
            if (timestep < 0 || timestep >= Grid.Transitions) throw new ArgumentOutOfRangeException(nameof(timestep), "No running density at N.");
            return new CostContribution(new[] {
                new CostTerm("state-deviation", "Densità stato", "Lˣ", "Scarto quadratico di stato per secondo", RunningUnit,
                    WeightedSquaredError.Evaluate(state, Reference.States[timestep], StateWeights, 1)),
                new CostTerm("control-deviation", "Densità controllo", "Lᵘ", "Scarto quadratico di controllo per secondo", RunningUnit,
                    WeightedSquaredError.Evaluate(input, Reference.Inputs[timestep], InputWeights, 1)) });
        }
        public override CostContribution EvaluateTerminal(NumericVector state) => EvaluateTerminal(Grid.Transitions, state);
        public override CostContribution EvaluateTerminal(int terminalIndex, NumericVector state)
        {
            if (terminalIndex != Grid.Transitions) throw new ArgumentException("Terminal objective requires x_N.");
            return new CostContribution(new[] { new CostTerm("state-deviation", "Costo terminale", "Φ", "Esito finale, escluso dalla quadratura running", CostUnit,
                WeightedSquaredError.Evaluate(state, Reference.States[terminalIndex], TerminalWeights, 1)) });
        }
    }
}
