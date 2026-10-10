using System;

namespace OptCon.Simulation
{
    /// <summary>Retains the whole immutable result A, including its own objective. No mutable asset or consumer copies.</summary>
    public sealed class ComparisonSnapshot
    {
        public SimulationResult Result { get; }
        public ComparisonSnapshot(SimulationResult result) => Result = result ?? throw new ArgumentNullException(nameof(result));
    }
}
