using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Core
{
    /// <summary>Frozen finite piecewise-constant curve in seconds. Right-continuous at every knot.</summary>
    public sealed class PhysicalStepProfile
    {
        public double DurationSeconds { get; }
        public IReadOnlyList<double> Knots { get; }
        public IReadOnlyList<NumericVector> Values { get; }
        public IReadOnlyList<string> Units { get; }
        public PhysicalStepProfile(double duration, IEnumerable<double> knots, IEnumerable<NumericVector> values, IEnumerable<string> units)
        {
            NumericValidation.RequirePositive(duration, nameof(duration));
            var times = knots?.ToArray() ?? throw new ArgumentNullException(nameof(knots));
            var vectors = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
            var labels = units?.ToArray() ?? throw new ArgumentNullException(nameof(units));
            if (times.Length < 1 || times[0] != 0 || times.Length != vectors.Length || labels.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Profile requires a zero knot, values and component units.");
            for (int i = 0; i < times.Length; i++)
            {
                NumericValidation.RequireFinite(times[i], nameof(knots));
                if (times[i] < 0 || times[i] >= duration || (i > 0 && times[i] <= times[i - 1])) throw new ArgumentException("Profile knots must increase in [0,H).");
                NumericValidation.RequireDimension(vectors[i], labels.Length, nameof(values));
            }
            DurationSeconds = duration; Knots = Array.AsReadOnly(times); Values = Array.AsReadOnly(vectors); Units = Array.AsReadOnly(labels);
        }
        public NumericVector At(double seconds)
        {
            NumericValidation.RequireFinite(seconds, nameof(seconds));
            if (seconds < 0 || seconds > DurationSeconds) throw new ArgumentOutOfRangeException(nameof(seconds));
            int i = 0; while (i + 1 < Knots.Count && seconds >= Knots[i + 1]) i++;
            return Values[i];
        }
        public PhysicalStepProfile WithInterval(double start, double end, NumericVector value)
        {
            NumericValidation.RequireDimension(value, Units.Count, nameof(value));
            if (start < 0 || start >= end || end > DurationSeconds || !NumericValidation.IsFinite(start) || !NumericValidation.IsFinite(end))
                throw new ArgumentException("Input edit requires a physical interval in [0,H].");
            var knots = Knots.Where(t => t < start || t >= end).Concat(new[] { start });
            if (end < DurationSeconds) knots = knots.Concat(new[] { end });
            var times = knots.Distinct().OrderBy(t => t).ToArray();
            return new PhysicalStepProfile(DurationSeconds, times, times.Select(t => t >= start && t < end ? value : At(t)), Units);
        }
    }
}
