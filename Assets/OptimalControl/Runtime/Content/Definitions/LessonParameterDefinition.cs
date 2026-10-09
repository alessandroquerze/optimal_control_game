using System;
using UnityEngine;

namespace OptCon.Content
{
    public enum ParameterValueKind { Real, Integer, Boolean }
    public enum ParameterRecalculation { FullRollout, FutureRollout, Diagnostics, Presentation }

    /// <summary>Embedded editorial descriptor; a session will own mutable overrides.</summary>
    [Serializable]
    public sealed class LessonParameterDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ParameterValueKind valueKind;
        [SerializeField] private string unit;
        [SerializeField] private double defaultValue;
        [SerializeField] private double minimum;
        [SerializeField] private double maximum;
        [SerializeField] private double step;
        [SerializeField] private string modelTarget;
        [SerializeField] private ParameterRecalculation recalculation;

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public ParameterValueKind ValueKind => valueKind;
        public string Unit => unit;
        public double DefaultValue => defaultValue;
        public double Minimum => minimum;
        public double Maximum => maximum;
        public double Step => step;
        public string ModelTarget => modelTarget;
        public ParameterRecalculation Recalculation => recalculation;
    }
}
