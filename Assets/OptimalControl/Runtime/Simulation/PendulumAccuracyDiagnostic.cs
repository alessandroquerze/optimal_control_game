using System;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Independent analytic oracle ONLY for zero gravity and constant torque. No nonlinear exact-solution claim.</summary>
    public sealed class PendulumAccuracyDiagnostic
    {
        public NumericVector ExactTerminal { get; }
        public NumericVector TerminalError { get; }
        private PendulumAccuracyDiagnostic(SimulationConfiguration config, TrajectoryHistory history, PendulumConfiguration model)
        {
            double h = config.ContinuousSampling.DurationSeconds, acceleration = config.Controls[0][0] / model.Physics.MomentOfInertia;
            ExactTerminal = new NumericVector(config.InitialState[0] + h * config.InitialState[1] + .5 * acceleration * h * h,
                config.InitialState[1] + acceleration * h);
            TerminalError = new NumericVector(history.States[history.Horizon].Select((v, i) => v - ExactTerminal[i]).ToArray());
        }
        internal static PendulumAccuracyDiagnostic TryCreate(SimulationConfiguration config, TrajectoryHistory history)
        {
            if (config.ContinuousSampling == null || !(config.Model is PendulumConfiguration model) || model.Physics.Gravity != 0 ||
                !config.ContinuousSampling.ControlLaw.Values.All(v => v.SequenceEqual(config.ContinuousSampling.ControlLaw.Values[0]))) return null;
            // An optional oracle must not invalidate an otherwise representable rollout.
            try { return new PendulumAccuracyDiagnostic(config, history, model); }
            catch (ArgumentException) { return null; }
            catch (ArithmeticException) { return null; }
        }
    }
}
