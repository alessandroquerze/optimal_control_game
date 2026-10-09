using System;

namespace OptCon.Core
{
    /// <summary>
    /// Explicit Euler applied to the continuous pendulum (source I p.33, method I p.27).
    /// State [angle, angular velocity], input [torque]. Both equations use the OLD state.
    /// Angles remain unwrapped; no damping, saturation, collision or Unity physics is added.
    /// </summary>
    public sealed class PendulumDynamics : IDiscreteDynamics
    {
        public PendulumParameters Parameters { get; }
        public double StepSeconds { get; }
        public ModelDescriptor Descriptor { get; }

        public PendulumDynamics(PendulumParameters parameters, double stepSeconds)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            NumericValidation.RequirePositive(stepSeconds, nameof(stepSeconds));
            StepSeconds = stepSeconds;
            Descriptor = new ModelDescriptor("pendulum.explicit-euler", "1", new ModelDimensions(2, 1, 1),
                new[] { "rad", "rad/s" }, new[] { "N·m" }, new[] { "rad" }, stepSeconds);
        }

        public NumericVector Evaluate(int timestep, NumericVector state, NumericVector control)
        {
            if (timestep < 0) throw new ArgumentOutOfRangeException(nameof(timestep));
            NumericValidation.RequireDimension(state, Descriptor.Dimensions.State, nameof(state));
            NumericValidation.RequireDimension(control, Descriptor.Dimensions.Control, nameof(control));
            double angle = state[0];
            double velocity = state[1];
            double acceleration = -Parameters.GravityOverLength * Math.Sin(angle) + control[0] / Parameters.MomentOfInertia;
            double nextAngle = angle + StepSeconds * velocity;
            double nextVelocity = velocity + StepSeconds * acceleration;
            if (!NumericValidation.IsFinite(acceleration) || !NumericValidation.IsFinite(nextAngle) || !NumericValidation.IsFinite(nextVelocity))
                throw new ArithmeticException($"Pendulum transition at t={timestep} produced a non-finite result.");
            return new NumericVector(nextAngle, nextVelocity);
        }
    }
}
