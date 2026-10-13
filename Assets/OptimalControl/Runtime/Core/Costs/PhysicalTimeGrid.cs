using System;

namespace OptCon.Core
{
    /// <summary>Uniform physical grid: N transitions, N+1 states and H=N*delta seconds.</summary>
    public sealed class PhysicalTimeGrid
    {
        public double DurationSeconds { get; }
        public double StepSeconds { get; }
        public int Transitions { get; }
        public PhysicalTimeGrid(double durationSeconds, double stepSeconds, int transitions)
        {
            NumericValidation.RequirePositive(durationSeconds, nameof(durationSeconds));
            NumericValidation.RequirePositive(stepSeconds, nameof(stepSeconds));
            if (transitions < 1 || transitions > 10000) throw new ArgumentOutOfRangeException(nameof(transitions));
            double duration = transitions * stepSeconds;
            if (!NumericValidation.IsFinite(duration) || Math.Abs(duration - durationSeconds) > 1e-10 * durationSeconds)
                throw new ArgumentException("Physical horizon mismatch: H must equal N*delta.");
            DurationSeconds = durationSeconds; StepSeconds = stepSeconds; Transitions = transitions;
        }
        public static PhysicalTimeGrid ForDuration(double duration, double step)
        {
            NumericValidation.RequirePositive(duration, nameof(duration)); NumericValidation.RequirePositive(step, nameof(step));
            double count = Math.Round(duration / step);
            if (!NumericValidation.IsFinite(count) || count < 1 || count > 10000) throw new ArgumentException("Unrepresentable physical grid.");
            return new PhysicalTimeGrid(duration, step, (int)count);
        }
        public double TimeAt(int sample)
        {
            if (sample < 0 || sample > Transitions) throw new ArgumentOutOfRangeException(nameof(sample));
            return sample == Transitions ? DurationSeconds : sample * StepSeconds;
        }
        public bool Matches(PhysicalTimeGrid other) => other != null && DurationSeconds == other.DurationSeconds &&
            StepSeconds == other.StepSeconds && Transitions == other.Transitions;
    }
}
