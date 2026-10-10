using System;
using System.Collections.Generic;

namespace OptCon.Core
{
    /// <summary>Retains the immutable SI parameters and Euler step that produced a revision.</summary>
    public sealed class PendulumConfiguration : ModelConfiguration
    {
        private readonly PendulumDynamics dynamics;
        public PendulumParameters Physics => dynamics.Parameters;
        public override ModelDescriptor Descriptor => dynamics.Descriptor;
        public override IReadOnlyList<ModelParameterValue> Parameters { get; }

        public PendulumConfiguration(PendulumParameters parameters, double stepSeconds)
        {
            dynamics = new PendulumDynamics(parameters, stepSeconds);
            Parameters = Array.AsReadOnly(new[] {
                new ModelParameterValue("model.mass", parameters.Mass, "kg"),
                new ModelParameterValue("model.length", parameters.Length, "m"),
                new ModelParameterValue("model.gravity", parameters.Gravity, "m/s²") });
        }

        public override IDiscreteDynamics CreateDynamics() => dynamics;
        public override IOutputFunction CreateOutputFunction() => new StateComponentOutput(2, 1, 0);
        public override ModelConfiguration WithParameter(string target, double value)
        {
            double mass = Physics.Mass, length = Physics.Length, gravity = Physics.Gravity;
            switch (target)
            {
                case "model.mass": mass = value; break;
                case "model.length": length = value; break;
                case "model.gravity": gravity = value; break;
                default: throw new ArgumentException("Unknown pendulum parameter: " + target, nameof(target));
            }
            return new PendulumConfiguration(new PendulumParameters(mass, length, gravity), dynamics.StepSeconds);
        }

        public override ModelConfiguration WithPhysicalStep(double? seconds)
        {
            if (!seconds.HasValue) throw new ArgumentException("The Euler pendulum requires a physical step in seconds.", nameof(seconds));
            return new PendulumConfiguration(Physics, seconds.Value);
        }
    }
}
