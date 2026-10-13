using System;
using System.Collections.Generic;
namespace OptCon.Core
{
    /// <summary>Planar drone I pp.26–27, SI. J denotes moment of inertia.</summary>
    public sealed class PlanarDroneConfiguration : ModelConfiguration
    {
        public double Mass { get; }
        public double ArmLength { get; }
        public double Gravity { get; }
        public double MomentOfInertia { get; }
        public double StepSeconds { get; }
        public override ModelDescriptor Descriptor { get; }
        public override IReadOnlyList<ModelParameterValue> Parameters { get; }
        public PlanarDroneConfiguration(double mass, double armLength, double gravity, double momentOfInertia, double stepSeconds)
        {
            NumericValidation.RequirePositive(mass, nameof(mass)); NumericValidation.RequirePositive(armLength, nameof(armLength));
            NumericValidation.RequirePositive(momentOfInertia, nameof(momentOfInertia)); NumericValidation.RequirePositive(stepSeconds, nameof(stepSeconds));
            NumericValidation.RequireFinite(gravity, nameof(gravity)); if (gravity < 0) throw new ArgumentOutOfRangeException(nameof(gravity));
            Mass=mass; ArmLength=armLength; Gravity=gravity; MomentOfInertia=momentOfInertia; StepSeconds=stepSeconds;
            Descriptor = new ModelDescriptor("drone.planar.explicit-euler", "1", new ModelDimensions(6,2,2),
                new[] { "m", "m/s", "m", "m/s", "rad", "rad/s" }, new[] { "N", "N" }, new[] { "m", "m" }, stepSeconds);
            Parameters = Array.AsReadOnly(new[] { new ModelParameterValue("model.mass",mass,"kg"), new ModelParameterValue("model.arm-length",armLength,"m"),
                new ModelParameterValue("model.gravity",gravity,"m/s²"), new ModelParameterValue("model.moment-of-inertia",momentOfInertia,"kg·m²") });
        }
        public override IDiscreteDynamics CreateDynamics() => new PlanarDroneDynamics(this);
        public override IOutputFunction CreateOutputFunction() => new StateComponentsOutput(6,2,0,2);
        public override ModelConfiguration WithPhysicalStep(double? seconds) => seconds.HasValue
            ? new PlanarDroneConfiguration(Mass,ArmLength,Gravity,MomentOfInertia,seconds.Value) : throw new ArgumentException("Euler drone requires a physical step.");
        public override ModelConfiguration WithParameter(string target, double value)
        {
            switch(target) {
                case "model.mass": return new PlanarDroneConfiguration(value,ArmLength,Gravity,MomentOfInertia,StepSeconds);
                case "model.arm-length": return new PlanarDroneConfiguration(Mass,value,Gravity,MomentOfInertia,StepSeconds);
                case "model.gravity": return new PlanarDroneConfiguration(Mass,ArmLength,value,MomentOfInertia,StepSeconds);
                case "model.moment-of-inertia": return new PlanarDroneConfiguration(Mass,ArmLength,Gravity,value,StepSeconds);
                default: throw new ArgumentException("Unknown drone parameter: "+target); }
        }
    }
    /// <summary>One authoritative Forward Euler transition. All rows use the old state.</summary>
    public sealed class PlanarDroneDynamics : IDiscreteDynamics
    {
        public PlanarDroneConfiguration Configuration { get; }
        public ModelDescriptor Descriptor => Configuration.Descriptor;
        public PlanarDroneDynamics(PlanarDroneConfiguration configuration) => Configuration=configuration ?? throw new ArgumentNullException(nameof(configuration));
        public NumericVector Evaluate(int timestep, NumericVector state, NumericVector control)
        {
            if(timestep<0) throw new ArgumentOutOfRangeException(nameof(timestep));
            NumericValidation.RequireDimension(state,6,nameof(state)); NumericValidation.RequireDimension(control,2,nameof(control));
            var p=Configuration; double d=p.StepSeconds, thrust=control[0]+control[1];
            return new NumericVector(state[0]+d*state[1], state[1]-d*thrust/p.Mass*Math.Sin(state[4]),
                state[2]+d*state[3], state[3]+d*(thrust/p.Mass*Math.Cos(state[4])-p.Gravity),
                state[4]+d*state[5], state[5]+d*p.ArmLength/(2*p.MomentOfInertia)*(control[0]-control[1]));
        }
    }
}
