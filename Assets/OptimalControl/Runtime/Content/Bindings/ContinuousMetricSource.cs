using System;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Content
{
    /// <summary>Lookup of frozen functional/grid/comparison evaluations. No multiplication, summation or integration.</summary>
    public sealed class ContinuousMetricSource : IExperimentMetricSource
    {
        public bool TryGet(IExperimentSnapshot snapshot, string id, out RuntimeMetric metric)
        {
            metric = null;
            if (!(snapshot is SessionState state) || !id.StartsWith("continuous.", StringComparison.Ordinal)) return false;
            var cost = state.Result.Configuration.Cost as ContinuousTrackingCost;
            var data = cost == null ? null : state.Result.Costs;
            int t = state.Sample.Timestep;
            double? value = null; string unit = "1"; int? index = null; var scope = FormulaScope.Snapshot;
            string missing = "Funzionale continuo/griglia fisica non disponibile; nessun secondo inventato.";
            switch (id)
            {
                case "continuous.duration": value = cost?.Grid.DurationSeconds; unit = "s"; break;
                case "continuous.transitions": value = cost?.Grid.Transitions; break;
                case "continuous.density": scope = FormulaScope.CurrentTimestep; index = t; unit = "1/s"; value = data == null || state.Sample.IsTerminal ? null : data.RunningDensities[t].Total; missing = "Nessuna densità running a N."; break;
                case "continuous.contribution": scope = FormulaScope.CurrentTimestep; index = t; value = data?.StageAt(t)?.Total; missing = "Nessuna cella running a N."; break;
                case "continuous.cumulative": scope = FormulaScope.CurrentTimestep; index = t; value = data?.Cumulative[t]; break;
                case "continuous.terminal": scope = FormulaScope.Terminal; index = state.Result.History.Horizon; value = data?.Terminal.Total; break;
                case "continuous.total": value = data?.Total; break;
                case "continuous.terminal-value": value = data?.Terminal.Total; break;
                case "continuous.accuracy-angle-error": value = state.Result.Accuracy?.TerminalError[0]; unit = "rad"; missing = "Oracolo solo con g=0 e coppia costante."; break;
                case "continuous.accuracy-error": case "continuous.exact-terminal":
                    metric = RuntimeMetric.FromVector(id, id == "continuous.accuracy-error" ? state.Result.Accuracy?.TerminalError : state.Result.Accuracy?.ExactTerminal,
                        state.Result.History.Model.StateUnits, state.Result.History.Horizon, FormulaScope.Terminal,
                        "Oracolo analitico solo con g=0 e coppia costante; nessuna soluzione esatta dichiarata nel caso nonlineare."); return true;
                case "continuous.comparison.same-problem": value = state.DiscretizationComparison == null ? (double?)null : state.DiscretizationComparison.SamePhysicalProblem ? 1 : 0; break;
                case "continuous.comparison.cost-difference": value = state.DiscretizationComparison?.TotalDifference; break;
                case "continuous.comparison.terminal-difference":
                    metric = RuntimeMetric.FromVector(id, state.DiscretizationComparison?.TerminalStateDifference, state.Result.History.Model.StateUnits, null, FormulaScope.Snapshot,
                        "Due griglie fisiche confrontabili non disponibili, oppure differenza non finita."); return true;
                case "continuous.comparison.a-duration": value = (state.Comparison?.Result.Configuration.Cost as ContinuousTrackingCost)?.Grid.DurationSeconds; unit = "s"; break;
                case "continuous.comparison.a-step": value = (state.Comparison?.Result.Configuration.Cost as ContinuousTrackingCost)?.Grid.StepSeconds; unit = "s"; break;
                case "continuous.comparison.a-transitions": value = (state.Comparison?.Result.Configuration.Cost as ContinuousTrackingCost)?.Grid.Transitions; break;
                default: return false;
            }
            metric = RuntimeMetric.Number(id, value, unit, index, missing, scope); return true;
        }
    }
}
