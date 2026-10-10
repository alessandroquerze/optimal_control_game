using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Presentation
{
    /// <summary>Translates editorial terms into the central pure metric contract.</summary>
    public sealed class FormulaBindingResolver
    {
        private readonly RuntimeMetricResolver metrics;
        public FormulaBindingResolver(RuntimeMetricResolver metrics) => this.metrics = metrics ?? throw new System.ArgumentNullException(nameof(metrics));
        public MetricResolution Resolve(FormulaTermDefinition term, SimulationResult result, int cursor)
        {
            return metrics.Resolve(term, result, cursor);
        }
    }
}
