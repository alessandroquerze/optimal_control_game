using System;

namespace OptCon.Core
{
    /// <summary>Point-mass pendulum parameters in kg, m and m/s². Zero gravity is allowed.</summary>
    public sealed class PendulumParameters
    {
        public double Mass { get; }
        public double Length { get; }
        public double Gravity { get; }
        public double MomentOfInertia { get; }
        internal double GravityOverLength { get; }

        public PendulumParameters(double mass, double length, double gravity)
        {
            NumericValidation.RequirePositive(mass, nameof(mass));
            NumericValidation.RequirePositive(length, nameof(length));
            NumericValidation.RequireFinite(gravity, nameof(gravity));
            if (gravity < 0) throw new ArgumentOutOfRangeException(nameof(gravity));
            double inertia = mass * length * length;
            NumericValidation.RequirePositive(inertia, "mass * length²");
            double gravityOverLength = gravity / length;
            NumericValidation.RequireFinite(gravityOverLength, "gravity / length");
            if (gravity > 0 && gravityOverLength == 0)
                throw new ArgumentOutOfRangeException(nameof(gravity), "Gravity coefficient underflows double precision.");
            Mass = mass;
            Length = length;
            Gravity = gravity;
            MomentOfInertia = inertia;
            GravityOverLength = gravityOverLength;
        }
    }
}
