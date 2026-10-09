using System;
using System.Collections.Generic;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Models/Pendulum (Explicit Euler)")]
    public sealed class PendulumModelDefinition : ModelDefinition
    {
        public override ModelDimensions Dimensions => new ModelDimensions(2, 1, 1);
        protected override ModelConfiguration CreateModel(double? physicalStep, IReadOnlyDictionary<string, double> parameters)
        {
            if (!physicalStep.HasValue) throw new ArgumentException("The Euler pendulum requires a physical step in seconds.");
            if (parameters.Count != 3 || !parameters.TryGetValue("model.mass", out double mass)
                || !parameters.TryGetValue("model.length", out double length) || !parameters.TryGetValue("model.gravity", out double gravity))
                throw new ArgumentException("The pendulum requires exactly mass, length and gravity parameters.");
            return new PendulumConfiguration(new PendulumParameters(mass, length, gravity), physicalStep.Value);
        }
    }
}
