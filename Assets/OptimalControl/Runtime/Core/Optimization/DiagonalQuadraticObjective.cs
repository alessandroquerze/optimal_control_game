using System;

namespace OptCon.Core
{
    /// <summary>Value-only specialization of 1/2 z^T Q z with Q diagonal positive definite (O p.23).</summary>
    public sealed class DiagonalQuadraticObjective : IObjectiveFunction
    {
        public NumericVector Weights { get; }
        public int Dimension => Weights.Count;
        public DiagonalQuadraticObjective(NumericVector weights)
        {
            Weights = weights ?? throw new ArgumentNullException(nameof(weights));
            if (weights.Count == 0) throw new ArgumentException("A decision dimension is required.");
            foreach (double weight in weights) NumericValidation.RequirePositive(weight, nameof(weights));
        }
        public double Value(NumericVector decision)
        {
            NumericValidation.RequireDimension(decision, Dimension, nameof(decision));
            double value = 0;
            for (int i = 0; i < Dimension; i++) value += .5 * Weights[i] * decision[i] * decision[i];
            NumericValidation.RequireFinite(value, "objective value");
            return value;
        }
    }
}
