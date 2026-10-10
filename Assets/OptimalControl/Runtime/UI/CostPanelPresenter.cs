using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Cost commands and formatted metrics only. Own objectives are explicit for A and B.</summary>
    public sealed class CostPanelPresenter : IDisposable
    {
        private readonly VisualElement root;
        private readonly List<Action> cleanup = new List<Action>();
        private readonly Dictionary<string, DoubleField> fields = new Dictionary<string, DoubleField>();
        private readonly RuntimeMetricResolver resolver;
        private readonly IntegerField horizon;
        private readonly Button save, clear;
        public SimulationResult DisplayedResult { get; private set; }
        public CostPanelPresenter(VisualElement root, ICommandSink commands, SessionState initial, RuntimeMetricResolver metrics)
        {
            resolver = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.root = root;
            var list = root.Q<VisualElement>("cost-parameters"); list.Clear();
            if (initial.Result.Configuration.Cost != null) foreach (var parameter in initial.Result.Configuration.Cost.Parameters)
            {
                string target = parameter.Target;
                var field = new DoubleField(ParameterLabel(target) + " [" + parameter.Unit + "]") { name = "parameter-" + target, isDelayed = true };
                EventCallback<ChangeEvent<double>> handler = e => commands.Execute(SimulationCommand.SetParameter(target, e.newValue));
                field.RegisterValueChangedCallback(handler); cleanup.Add(() => field.UnregisterValueChangedCallback(handler)); fields.Add(target, field); list.Add(field);
            }
            horizon = root.Q<IntegerField>("horizon");
            EventCallback<ChangeEvent<int>> change = e => commands.Execute(SimulationCommand.SetHorizon(e.newValue));
            horizon.RegisterValueChangedCallback(change); cleanup.Add(() => horizon.UnregisterValueChangedCallback(change));
            save = root.Q<Button>("comparison-save"); clear = root.Q<Button>("comparison-clear");
            Action capture = () => commands.Execute(SimulationCommand.CaptureComparison()); Action remove = () => commands.Execute(SimulationCommand.ClearComparison());
            save.clicked += capture; clear.clicked += remove; cleanup.Add(() => save.clicked -= capture); cleanup.Add(() => clear.clicked -= remove);
        }
        public void Render(SessionState state)
        {
            DisplayedResult = state.Result; horizon.SetEnabled(true); horizon.SetValueWithoutNotify(state.Result.History.Horizon);
            root.Q<VisualElement>("cost-panel").style.display = state.Result.Costs == null ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var field in fields)
            {
                var parameter = state.Result.Configuration.Cost?.Parameters.FirstOrDefault(p => p.Target == field.Key);
                field.Value.SetEnabled(parameter != null); if (parameter != null) field.Value.SetValueWithoutNotify(parameter.Value);
            }
            string Format(string id, FormulaScope scope = FormulaScope.CurrentTimestep) => FormulaPresenter.Format(resolver.Resolve(state.Result, state.Sample.Timestep, id, FormulaValueKind.Scalar, scope), "G6", false);
            root.Q<Label>("cost-live").text = "B · r" + state.Result.RevisionId + " · t=" + state.Sample.Timestep + "/" + state.Result.History.Horizon
                + "\nStage " + Format(RuntimeMetricIds.StageCost) + "\nTracking " + Format(RuntimeMetricIds.TrackingCost) + " · effort " + Format(RuntimeMetricIds.EffortCost)
                + "\nC_t (stage già percorsi) " + Format(RuntimeMetricIds.CumulativeCost) + "\nTerminale previsto " + Format(RuntimeMetricIds.TerminalCost, FormulaScope.Terminal)
                + "\nJ (intero rollout) " + Format(RuntimeMetricIds.TotalCost);
            var a = state.Comparison?.Result;
            root.Q<Label>("comparison-status").text = a == null ? "A non salvato. Salva A, poi modifica B." : Describe("A", a) + "\n" + Describe("B", state.Result)
                + "\nCosti sotto i rispettivi obiettivi: cambiare pesi/target cambia il criterio. Nessuna etichetta di ottimalità.";
            clear.SetEnabled(a != null);
        }
        private static string ParameterLabel(string target)
        {
            if (target.StartsWith("cost.qt.")) return "Q_T[" + target.Substring(8) + "]";
            if (target.StartsWith("cost.q.")) return "Q[" + target.Substring(7) + "]";
            if (target.StartsWith("cost.r.")) return "R[" + target.Substring(7) + "]";
            if (target.StartsWith("cost.target.")) return "x^des[" + target.Substring(12) + "]";
            if (target.StartsWith("cost.inputTarget.")) return "u^des[" + target.Substring(17) + "]";
            return target;
        }
        private static string Describe(string label, SimulationResult result)
        {
            string Number(double value) => value.ToString("G6", CultureInfo.InvariantCulture);
            var parameters = result.Configuration.Model.Parameters.Concat(result.Configuration.Cost?.Parameters ?? Array.Empty<OptCon.Core.ModelParameterValue>());
            return label + " · r" + result.RevisionId + " · T=" + result.History.Horizon + " · δ=" + (result.History.Model.DiscretizationStepSeconds.HasValue ? Number(result.History.Model.DiscretizationStepSeconds.Value) + " s" : "N/A")
                + " · J=" + (result.Costs == null ? "N/A" : Number(result.Costs.Total) + " [" + result.Costs.Configuration.CostUnit + "]")
                + "\nx0=[" + string.Join(";", result.Configuration.InitialState.Select(Number)) + "] ; u0=[" + string.Join(";", result.Configuration.Controls[0].Select(Number)) + "]\n" + result.Configuration.Model.Descriptor.Id + " · " + result.Configuration.Cost?.Id + "\n" + string.Join("; ", parameters.Select(p => p.Target + "=" + Number(p.Value) + " [" + p.Unit + "]"));
        }
        public void Dispose() { foreach (var remove in cleanup) remove(); root.Q<VisualElement>("cost-parameters").Clear(); DisplayedResult = null; }
    }
}
