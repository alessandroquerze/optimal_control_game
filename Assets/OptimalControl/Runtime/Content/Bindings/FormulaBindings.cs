using System;
using UnityEngine;

namespace OptCon.Content
{
    public enum FormulaValueKind { Scalar, Vector, Matrix, ModelDescriptor }
    public enum FormulaScope { CurrentTimestep, NextTimestep, Terminal, CurrentIteration }

    /// <summary>Describes a lookup only; it never evaluates an expression or offsets a sample.</summary>
    [Serializable]
    public sealed class ValueBinding
    {
        [SerializeField] private string metricId;
        [SerializeField] private FormulaValueKind kind;
        [SerializeField] private FormulaScope scope;
        [SerializeField, Tooltip("-1 means the complete value, otherwise a zero-based component.")]
        private int component = -1;
        [SerializeField] private string numericFormat = "G6";

        public string MetricId => metricId;
        public FormulaValueKind Kind => kind;
        public FormulaScope Scope => scope;
        public int Component => component;
        public string NumericFormat => numericFormat;
    }

    [Serializable]
    public sealed class HighlightBinding
    {
        [SerializeField] private string semanticRole;
        [SerializeField] private FormulaScope scope;
        public string SemanticRole => semanticRole;
        public FormulaScope Scope => scope;
    }
}
