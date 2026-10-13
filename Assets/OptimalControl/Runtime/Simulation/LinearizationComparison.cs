using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Frozen local model about a dynamically valid nominal rollout. Error is true minus linear
    /// prediction, NOT a dynamics residual. Both source results/configurations remain owned by this revision pair.</summary>
    public sealed class LinearizationComparison
    {
        public SimulationResult Nominal { get; }
        public SimulationResult Perturbed { get; }
        public IReadOnlyList<NumericMatrix> A { get; }
        public IReadOnlyList<NumericMatrix> B { get; }
        public IReadOnlyList<NumericVector> DeltaInputs { get; }
        public IReadOnlyList<NumericVector> TrueDeltaStates { get; }
        public IReadOnlyList<NumericVector> LinearDeltaStates { get; }
        public IReadOnlyList<NumericVector> PredictedStates { get; }
        public IReadOnlyList<NumericVector> Errors { get; }
        public int Horizon => Nominal.History.Horizon;
        public bool HasFixedInitialState { get; }
        public LinearizationComparison(SimulationResult nominal, SimulationResult perturbed)
        {
            Nominal = nominal ?? throw new ArgumentNullException(nameof(nominal));
            Perturbed = perturbed ?? throw new ArgumentNullException(nameof(perturbed));
            // Conservative: changing physics requires capturing a new nominal, never reinterpreting the old one.
            if (!ReferenceEquals(nominal.Configuration.Model, perturbed.Configuration.Model) ||
                nominal.Configuration.ExperimentId != perturbed.Configuration.ExperimentId || nominal.History.Horizon != perturbed.History.Horizon)
                throw new ArgumentException("Nominal and perturbed results require the same frozen model and horizon. Capture a new nominal.");
            var dynamics = nominal.DifferentiableDynamics ?? throw new ArgumentException("The model has no differentiable dynamics capability.");
            int n = nominal.History.Model.Dimensions.State, m = nominal.History.Model.Dimensions.Control, T = Horizon;
            var a = new NumericMatrix[T]; var b = new NumericMatrix[T]; var du = new NumericVector[T];
            var truth = new NumericVector[T + 1]; var linear = new NumericVector[T + 1];
            var predictions = new NumericVector[T + 1]; var errors = new NumericVector[T + 1];
            linear[0] = Difference(perturbed.History.States[0], nominal.History.States[0]);
            bool fixedInitial = true; foreach (double value in linear[0]) if (value != 0) fixedInitial = false;
            HasFixedInitialState = fixedInitial;
            for (int t = 0; t <= T; t++)
            {
                truth[t] = Difference(perturbed.History.States[t], nominal.History.States[t]);
                predictions[t] = Sum(nominal.History.States[t], linear[t]);
                errors[t] = Difference(perturbed.History.States[t], predictions[t]);
                if (t == T) break; // T states of transition data, T+1 states; no fabricated u_T or Jacobian at T.
                a[t] = dynamics.StateJacobian(t, nominal.History.States[t], nominal.History.Controls[t]);
                b[t] = dynamics.InputJacobian(t, nominal.History.States[t], nominal.History.Controls[t]);
                if (a[t].Rows != n || a[t].Columns != n || b[t].Rows != n || b[t].Columns != m)
                    throw new ArgumentException("Jacobian shape differs from the model dimensions.");
                du[t] = Difference(perturbed.History.Controls[t], nominal.History.Controls[t]);
                linear[t + 1] = Sum(a[t].Multiply(linear[t]), b[t].Multiply(du[t]));
            }
            A = Array.AsReadOnly(a); B = Array.AsReadOnly(b); DeltaInputs = Array.AsReadOnly(du);
            TrueDeltaStates = Array.AsReadOnly(truth); LinearDeltaStates = Array.AsReadOnly(linear);
            PredictedStates = Array.AsReadOnly(predictions); Errors = Array.AsReadOnly(errors);
        }
        private static NumericVector Sum(NumericVector a, NumericVector b) => Combine(a, b, 1);
        private static NumericVector Difference(NumericVector a, NumericVector b) => Combine(a, b, -1);
        private static NumericVector Combine(NumericVector a, NumericVector b, double sign)
        {
            NumericValidation.RequireDimension(b, a.Count, nameof(b));
            var values = new double[a.Count]; for (int i = 0; i < values.Length; i++) values[i] = a[i] + sign * b[i];
            return new NumericVector(values);
        }
        /// <summary>Absolute edits relative to the frozen nominal. No additive drift from repeated slider events.</summary>
        public SimulationConfiguration WithPerturbation(bool initial, int timestep, int component, double amplitude)
        {
            NumericValidation.RequireFinite(amplitude, nameof(amplitude));
            var current = Perturbed.Configuration;
            var baseline = initial ? Nominal.Configuration.InitialState :
                timestep >= 0 && timestep < Horizon ? Nominal.Configuration.Controls[timestep] : throw new ArgumentOutOfRangeException(nameof(timestep));
            var values = (initial ? current.InitialState : current.Controls[timestep]).ToArray();
            if (component < 0 || component >= values.Length) throw new ArgumentOutOfRangeException(nameof(component));
            values[component] = baseline[component] + amplitude;
            return initial ? current.WithInitialState(new NumericVector(values)) : current.WithInput(timestep, new NumericVector(values), InputEditScope.Single);
        }
    }
}
