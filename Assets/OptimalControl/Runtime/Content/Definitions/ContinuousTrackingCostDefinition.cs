using System;
using System.Linq;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Continuous Tracking Functional")]
    public sealed class ContinuousTrackingCostDefinition : CostDefinition
    {
        [SerializeField] private double[] stateWeights = Array.Empty<double>(), inputWeights = Array.Empty<double>(), terminalWeights = Array.Empty<double>();
        [SerializeField] private string[] stateWeightUnits = Array.Empty<string>(), inputWeightUnits = Array.Empty<string>(), terminalWeightUnits = Array.Empty<string>();
        public ContinuousTrackingCost Freeze(SampledReference reference)
        {
            var cost = new ContinuousTrackingCost(reference, new NumericVector(stateWeights), new NumericVector(inputWeights), new NumericVector(terminalWeights));
            void Check(string prefix, string[] units)
            {
                if (!cost.Parameters.Where(p => p.Target.StartsWith(prefix, StringComparison.Ordinal)).Select(p => p.Unit).SequenceEqual(units))
                    throw new ArgumentException("Continuous functional weight units differ from provider: " + prefix);
            }
            Check("cost.q.", stateWeightUnits); Check("cost.r.", inputWeightUnits); Check("cost.qt.", terminalWeightUnits);
            return cost;
        }
        public override CostConfiguration CreateConfiguration(ExperimentDefinition experiment) =>
            throw new ArgumentException("A continuous functional must be composed with its physical reference/grid by its experiment provider.");
    }
}
