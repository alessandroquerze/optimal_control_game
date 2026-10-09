using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Tests.Fixtures
{
    /// <summary>Deterministic test-only index-based model. Not registered in the production course.</summary>
    public sealed class IndexOnlyConfiguration : ModelConfiguration, IDiscreteDynamics
    {
        public override ModelDescriptor Descriptor { get; }
        public override IReadOnlyList<ModelParameterValue> Parameters => Array.Empty<ModelParameterValue>();
        public IndexOnlyConfiguration(int dimension = 1) => Descriptor = new ModelDescriptor("fixture.addition", "1", new ModelDimensions(dimension, dimension, 0),
            Enumerable.Repeat("1", dimension).ToArray(), Enumerable.Repeat("1", dimension).ToArray(), Array.Empty<string>());
        public override IDiscreteDynamics CreateDynamics() => this;
        public NumericVector Evaluate(int timestep, NumericVector state, NumericVector control) => new NumericVector(state.Select((x, i) => x + control[i]).ToArray());
        public override ModelConfiguration WithParameter(string target, double value) => throw new ArgumentException("No parameters in addition fixture.");
        public override ModelConfiguration WithPhysicalStep(double? seconds) => throw new ArgumentException("Index-only fixture.");
        public static SimulationSession CreateSession(int dimension = 1, int horizon = 1) => new SimulationSession(new SimulationConfiguration("fixture.index-only",
            new IndexOnlyConfiguration(dimension), new NumericVector(Enumerable.Repeat(2d, dimension).ToArray()),
            Enumerable.Range(0, horizon).Select(_ => new NumericVector(Enumerable.Repeat(3d, dimension).ToArray())).ToArray()));
    }
}
