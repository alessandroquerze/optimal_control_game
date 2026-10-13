using System;
using System.Collections.Generic;
using OptCon.Core;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Content
{
    /// <summary>Editorial transfer setup, composed once through the ordinary dynamic provider/host.</summary>
    [CreateAssetMenu(menuName = "Optimal Control/Models/Pendulum Transfer (Continuous Functional)")]
    public sealed class PendulumTransferModelDefinition : PendulumModelDefinition
    {
        [SerializeField] private double durationSeconds = 2;
        [SerializeField] private double firstAngle = 0, secondAngle = .5;
        [SerializeField] private double[] stepChoices = { .1, .05, .02 };
        protected override SimulationConfiguration ComposeConfiguration(ExperimentDefinition experiment, ModelConfiguration model, NumericVector initialState,
            IReadOnlyList<NumericVector> controls, NumericVector referenceState)
        {
            if (!(model is PendulumConfiguration pendulum) || !(experiment.Cost is ContinuousTrackingCostDefinition functional))
                throw new ArgumentException("Transfer provider requires a pendulum and continuous functional.");
            var grid = new PhysicalTimeGrid(durationSeconds, pendulum.Descriptor.DiscretizationStepSeconds.Value, experiment.DiscreteTime.Horizon);
            var knots = new[] { 0.0, durationSeconds / 2 };
            var physics = pendulum.Physics;
            var states = new PhysicalStepProfile(durationSeconds, knots, new[] { physics.EquilibriumState(firstAngle), physics.EquilibriumState(secondAngle) }, model.Descriptor.StateUnits);
            var inputs = new PhysicalStepProfile(durationSeconds, knots, new[] { physics.EquilibriumInput(firstAngle), physics.EquilibriumInput(secondAngle) }, model.Descriptor.ControlUnits);
            var law = new PhysicalStepProfile(durationSeconds, new[] { 0.0 }, new[] { controls[0] }, model.Descriptor.ControlUnits);
            var sampling = new ContinuousSampling(states, inputs, law, stepChoices);
            return sampling.Compose(experiment.Id, model, initialState, functional.Freeze(sampling.SampleReference(grid)), experiment.Constraints?.Freeze());
        }
    }
}
