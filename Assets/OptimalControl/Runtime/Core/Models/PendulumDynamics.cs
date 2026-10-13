using System;

namespace OptCon.Core
{
    /// <summary>
    /// Explicit Euler applied to the continuous pendulum (source I p.33, method I p.27).
    /// State [angle, angular velocity], input [torque]. Both equations use the OLD state.
    /// Angles remain unwrapped; no damping, saturation, collision or Unity physics is added.
    /// </summary>
    public sealed class PendulumDynamics : IDifferentiableDynamics
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
            var derivative = EvaluateDerivative(state, control);
            double nextAngle = state[0] + StepSeconds * derivative[0];
            double nextVelocity = state[1] + StepSeconds * derivative[1];
            if (!NumericValidation.IsFinite(nextAngle) || !NumericValidation.IsFinite(nextVelocity))
                throw new ArithmeticException($"Pendulum transition at t={timestep} produced a non-finite result.");
            return new NumericVector(nextAngle, nextVelocity);
        }
        public NumericVector EvaluateDerivative(NumericVector state, NumericVector control)
        {
            ValidatePair(0, state, control);
            double acceleration = -Parameters.GravityOverLength * Math.Sin(state[0]) + control[0] / Parameters.MomentOfInertia;
            if (!NumericValidation.IsFinite(acceleration)) throw new ArithmeticException("Pendulum derivative produced a non-finite result.");
            return new NumericVector(state[1], acceleration);
        }

        private void ValidatePair(int timestep, NumericVector state, NumericVector control)
        {
            if (timestep < 0) throw new ArgumentOutOfRangeException(nameof(timestep));
            NumericValidation.RequireDimension(state, 2, nameof(state));
            NumericValidation.RequireDimension(control, 1, nameof(control));
        }
        public NumericMatrix StateJacobian(int timestep, NumericVector state, NumericVector control)
        {
            ValidatePair(timestep, state, control);
            return new NumericMatrix(2, 2, 1, StepSeconds,
                -StepSeconds * Parameters.GravityOverLength * Math.Cos(state[0]), 1);
        }
        public NumericMatrix InputJacobian(int timestep, NumericVector state, NumericVector control)
        {
            ValidatePair(timestep, state, control);
            return new NumericMatrix(2, 1, 0, StepSeconds / Parameters.MomentOfInertia);
        }
    }
}
