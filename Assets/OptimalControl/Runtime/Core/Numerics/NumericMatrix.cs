using System;

namespace OptCon.Core
{
    /// <summary>Immutable small dense matrix. Rows are outputs, columns are inputs; no inverse or solver.</summary>
    public sealed class NumericMatrix
    {
        private readonly double[] entries;
        public int Rows { get; }
        public int Columns { get; }
        public double this[int row, int column] => row >= 0 && row < Rows && column >= 0 && column < Columns
            ? entries[row * Columns + column] : throw new ArgumentOutOfRangeException();
        public NumericMatrix(int rows, int columns, params double[] values)
        {
            if (rows < 1 || columns < 1 || values == null || values.Length != checked(rows * columns))
                throw new ArgumentException("Matrix dimensions and entries must agree.");
            Rows = rows; Columns = columns; entries = (double[])values.Clone();
            foreach (var value in entries) NumericValidation.RequireFinite(value, nameof(values));
        }
        public NumericVector Multiply(NumericVector vector)
        {
            NumericValidation.RequireDimension(vector, Columns, nameof(vector));
            var value = new double[Rows];
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Columns; c++) value[r] += this[r, c] * vector[c];
            return new NumericVector(value);
        }
    }
}
