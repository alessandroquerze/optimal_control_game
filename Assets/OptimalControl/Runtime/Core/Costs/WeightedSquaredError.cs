namespace OptCon.Core
{
    /// <summary>Single implementation of diagonal weighted squared distance; convention is explicit.</summary>
    internal static class WeightedSquaredError
    {
        public static double Evaluate(NumericVector value, NumericVector reference, NumericVector weights, double factor)
        {
            NumericValidation.RequireDimension(value, reference.Count, nameof(value));
            NumericValidation.RequireDimension(weights, reference.Count, nameof(weights));
            double sum = 0;
            for (int i = 0; i < value.Count; i++)
            {
                if (weights[i] == 0) continue;
                double error = value[i] - reference[i];
                sum += factor * weights[i] * error * error;
                NumericValidation.RequireFinite(sum, "quadratic cost");
            }
            return sum;
        }
    }
}
