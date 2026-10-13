using System;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    /// <summary>Editorial diagonal weights; converted once, never retained by a result.</summary>
    [CreateAssetMenu(menuName = "Optimal Control/Content/Quadratic Tracking Cost")]
    public sealed class QuadraticTrackingCostDefinition : CostDefinition
    {
        [SerializeField] private SampledReferenceDefinition sampledReference;
        [SerializeField] private CostTermDefinition stateContribution = new CostTermDefinition();
        [SerializeField] private CostTermDefinition inputContribution = new CostTermDefinition();
        public SampledReferenceDefinition SampledReference => sampledReference;
        [SerializeField] private double[] stateWeights = Array.Empty<double>();
        [SerializeField] private double[] inputWeights = Array.Empty<double>();
        [SerializeField] private double[] terminalWeights = Array.Empty<double>();
        [SerializeField] private double[] referenceInput = Array.Empty<double>();
        public override CostConfiguration CreateConfiguration(ExperimentDefinition experiment)
        {
            if (experiment == null) throw new ArgumentNullException(nameof(experiment));
            if (sampledReference != null) return new QuadraticTrackingCost(sampledReference.Freeze(), new NumericVector(stateWeights), new NumericVector(inputWeights),
                new NumericVector(terminalWeights), stateContribution.Freeze(), inputContribution.Freeze());
            return new QuadraticTrackingCost(new NumericVector(experiment.DiscreteTime.CopyReferenceState()),
                new NumericVector(referenceInput), new NumericVector(stateWeights), new NumericVector(inputWeights),
                new NumericVector(terminalWeights), experiment.StateUnits, experiment.ControlUnits);
        }
    }
    [Serializable]
    public sealed class CostTermDefinition
    {
        [SerializeField] private string id, label, symbol, description;
        [SerializeField] private string unit = "1";
        public CostTerm Freeze() => new CostTerm(id, label, symbol, description, unit, 0);
    }
}
