using System;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    /// <summary>Editorial diagonal weights; converted once, never retained by a result.</summary>
    [CreateAssetMenu(menuName = "Optimal Control/Content/Quadratic Tracking Cost")]
    public sealed class QuadraticTrackingCostDefinition : CostDefinition
    {
        [SerializeField] private double[] stateWeights = Array.Empty<double>();
        [SerializeField] private double[] inputWeights = Array.Empty<double>();
        [SerializeField] private double[] terminalWeights = Array.Empty<double>();
        [SerializeField] private double[] referenceInput = Array.Empty<double>();
        public override CostConfiguration CreateConfiguration(ExperimentDefinition experiment)
        {
            if (experiment == null) throw new ArgumentNullException(nameof(experiment));
            return new QuadraticTrackingCost(new NumericVector(experiment.DiscreteTime.CopyReferenceState()),
                new NumericVector(referenceInput), new NumericVector(stateWeights), new NumericVector(inputWeights),
                new NumericVector(terminalWeights), experiment.StateUnits, experiment.ControlUnits);
        }
    }
}
