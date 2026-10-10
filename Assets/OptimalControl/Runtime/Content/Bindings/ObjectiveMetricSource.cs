using OptCon.Simulation;

namespace OptCon.Content
{
    public interface IExperimentMetricSource
    {
        bool TryGet(IExperimentSnapshot snapshot, string id, out RuntimeMetric metric);
    }
    /// <summary>Reads saved objective values and their own frozen configuration. Never evaluates an objective.</summary>
    public sealed class ObjectiveMetricSource : IExperimentMetricSource
    {
        public const string Decision = "decision.current", Weights = "objective.weights", Value = "objective.value";
        public bool TryGet(IExperimentSnapshot snapshot, string id, out RuntimeMetric metric)
        {
            metric = null;
            if (!(snapshot.Result is ObjectiveResult result)) return false;
            if (id == Decision || id == Weights)
            {
                var vector = id == Decision ? result.Configuration.Decision : result.Configuration.Objective.Weights;
                var units = new string[vector.Count]; for (int i = 0; i < units.Length; i++) units[i] = "1";
                metric = RuntimeMetric.FromVector(id, vector, units, null, FormulaScope.Snapshot);
            }
            else if (id == Value) metric = RuntimeMetric.Number(id, result.Value, "1", null, scope: FormulaScope.Snapshot);
            else if (id == RuntimeMetricIds.Index || id == RuntimeMetricIds.PhysicalTime || id == RuntimeMetricIds.PhysicalStep)
                metric = RuntimeMetric.Number(id, null, id == RuntimeMetricIds.Index ? "1" : "s", null,
                    "Questo esperimento non possiede tempo o campioni temporali.", FormulaScope.Snapshot);
            else if (id.StartsWith(RuntimeMetricIds.ParameterPrefix, System.StringComparison.Ordinal))
            {
                string target = id.Substring(RuntimeMetricIds.ParameterPrefix.Length);
                foreach (var parameter in result.Configuration.Parameters) if (parameter.Target == target)
                    metric = RuntimeMetric.Number(id, parameter.Value, parameter.Unit, null, scope: FormulaScope.Snapshot);
            }
            return metric != null;
        }
    }
}
