using System;
using System.Linq;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    [Serializable]
    public sealed class ReferenceSampleDefinition
    {
        [SerializeField] private double[] values = Array.Empty<double>();
        public NumericVector Freeze() => new NumericVector(values);
    }
    [CreateAssetMenu(menuName = "Optimal Control/Content/Sampled Reference")]
    public sealed class SampledReferenceDefinition : ContentDefinition
    {
        [SerializeField] private ReferenceSampleDefinition[] states = Array.Empty<ReferenceSampleDefinition>();
        [SerializeField] private ReferenceSampleDefinition[] inputs = Array.Empty<ReferenceSampleDefinition>();
        [SerializeField] private string[] stateUnits = Array.Empty<string>();
        [SerializeField] private string[] inputUnits = Array.Empty<string>();
        public SampledReference Freeze() => new SampledReference(states.Select(s => s?.Freeze()).ToArray(), inputs.Select(s => s?.Freeze()).ToArray(), stateUnits, inputUnits);
    }
}
