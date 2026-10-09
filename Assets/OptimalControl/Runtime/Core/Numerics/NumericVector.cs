using System;
using System.Collections;
using System.Collections.Generic;

namespace OptCon.Core
{
    /// <summary>Finite double components, copied on entry. Empty vectors support models without inputs.</summary>
    public sealed class NumericVector : IReadOnlyList<double>
    {
        private readonly double[] values;
        public int Count => values.Length;
        public double this[int index] => values[index];

        public NumericVector(params double[] components)
        {
            if (components == null) throw new ArgumentNullException(nameof(components));
            values = (double[])components.Clone();
            foreach (double value in values)
                NumericValidation.RequireFinite(value, nameof(components));
        }

        public double[] ToArray() => (double[])values.Clone();
        public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class NumericValidation
    {
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public static void RequireFinite(double value, string parameterName)
        {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(parameterName, "A finite value is required.");
        }

        public static void RequirePositive(double value, string parameterName)
        {
            RequireFinite(value, parameterName);
            if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, "A strictly positive value is required.");
        }

        public static void RequireDimension(NumericVector vector, int dimension, string parameterName)
        {
            if (vector == null) throw new ArgumentNullException(parameterName);
            if (vector.Count != dimension) throw new ArgumentException($"Expected {dimension} components, got {vector.Count}.", parameterName);
        }
    }
}
