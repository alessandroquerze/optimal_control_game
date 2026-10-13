using System;
using System.Collections.Generic;
using OptCon.Core;
using UnityEngine;
namespace OptCon.Content
{
    [CreateAssetMenu(menuName="Optimal Control/Models/Planar Drone (Forward Euler)")]
    public sealed class PlanarDroneModelDefinition : ModelDefinition
    {
        public override ModelDimensions Dimensions => new ModelDimensions(6,2,2);
        protected override ModelConfiguration CreateModel(double? physicalStep, IReadOnlyDictionary<string,double> parameters)
        {
            if(!physicalStep.HasValue) throw new ArgumentException("Euler drone requires a physical step in seconds.");
            if(parameters.Count!=4 || !parameters.TryGetValue("model.mass",out double mass) || !parameters.TryGetValue("model.arm-length",out double length)
                || !parameters.TryGetValue("model.gravity",out double gravity) || !parameters.TryGetValue("model.moment-of-inertia",out double inertia))
                throw new ArgumentException("Drone requires mass, arm length, gravity and moment of inertia.");
            return new PlanarDroneConfiguration(mass,length,gravity,inertia,physicalStep.Value);
        }
    }
}
