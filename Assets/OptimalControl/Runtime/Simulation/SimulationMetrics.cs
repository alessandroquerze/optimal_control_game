using System.Collections.Generic;

namespace OptCon.Simulation
{
    public enum MetricValidity { Available, NotApplicable, Invalid }

    public static class SimulationMetricIds
    {
        public const string CurrentState = "state.current";
        public const string AppliedInput = "input.applied";
        public const string NextState = "state.next";
    }

    /// <summary>Vector metric shape and per-component units, never a formatted UI formula.</summary>
    public sealed class VectorMetricDescriptor
    {
        public string Id { get; }
        public int Rows => ComponentUnits.Count;
        public int Columns => 1;
        public IReadOnlyList<string> ComponentUnits { get; }

        internal VectorMetricDescriptor(string id, IReadOnlyList<string> immutableUnits)
        {
            Id = id;
            ComponentUnits = immutableUnits;
        }
    }

    /// <summary>SampleIndex is already resolved. A consumer must not offset state.next a second time.</summary>
    public sealed class VectorMetric
    {
        public VectorMetricDescriptor Descriptor { get; }
        public MetricValidity Validity { get; }
        public int? SampleIndex { get; }
        public OptCon.Core.NumericVector Value { get; }

        internal VectorMetric(VectorMetricDescriptor descriptor, int? sampleIndex, OptCon.Core.NumericVector value)
        {
            Descriptor = descriptor;
            SampleIndex = sampleIndex;
            Value = value;
            Validity = value == null ? MetricValidity.NotApplicable : MetricValidity.Available;
        }
    }
}
