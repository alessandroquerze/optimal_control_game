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
        private readonly string totalSymbol;
        private readonly IntegerField horizon;
        private readonly Button save, clear;
        private readonly SliderInt stepChoice;
        private readonly DoubleField physicalStep;
        private SessionState displayedState;
        private bool rendering;
        public SimulationResult DisplayedResult { get; private set; }
        public CostPanelPresenter(VisualElement root, ICommandSink commands, SessionState initial, RuntimeMetricResolver metrics, string totalSymbol = "J")
        {
            resolver = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.totalSymbol = totalSymbol;
            this.root = root;
            stepChoice = root.Q<SliderInt>("discretization-choice"); physicalStep = root.Q<DoubleField>("physical-step");
            EventCallback<ChangeEvent<int>> choose = e => { if (!rendering && displayedState?.Result.Configuration.ContinuousSampling != null)
                commands.Execute(SimulationCommand.SetPhysicalStep(displayedState.Result.Configuration.ContinuousSampling.StepChoices[e.newValue])); };
            EventCallback<ChangeEvent<double>> changeStep = e => { if (!rendering) commands.Execute(SimulationCommand.SetPhysicalStep(e.newValue)); };
            stepChoice.RegisterValueChangedCallback(choose); physicalStep.RegisterValueChangedCallback(changeStep);
            cleanup.Add(() => stepChoice.UnregisterValueChangedCallback(choose)); cleanup.Add(() => physicalStep.UnregisterValueChangedCallback(changeStep));
            var list = root.Q<VisualElement>("cost-parameters"); list.Clear();
            foreach (var parameter in initial.Result.Configuration.Parameters.Where(p => p.Target.StartsWith("cost.", StringComparison.Ordinal) || p.Target.StartsWith("constraint.", StringComparison.Ordinal)))
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
            DisplayedResult = state.Result; displayedState = state; bool sampled = state.Result.Configuration.Reference != null;
            var continuous = state.Result.Configuration.Cost as OptCon.Core.ContinuousTrackingCost;
            var sampling = state.Result.Configuration.ContinuousSampling;
            root.Q<VisualElement>("discretization-controls").style.display = sampling == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (sampling != null)
            {
                rendering = true;
                try
                {
                    stepChoice.highValue = System.Math.Max(0, sampling.StepChoices.Count - 1);
                    stepChoice.SetValueWithoutNotify(sampling.StepChoices.ToList().IndexOf(continuous.Grid.StepSeconds));
                    physicalStep.SetValueWithoutNotify(continuous.Grid.StepSeconds);
                }
                finally { rendering = false; }
                stepChoice.SetEnabled(sampling.StepChoices.Count > 0 && state.Error == null); physicalStep.SetEnabled(!state.IsClosed);
                root.Q<Label>("physical-grid-status").text = "H=" + continuous.Grid.DurationSeconds + " s · N=" + continuous.Grid.Transitions +
                    " transizioni · δ=" + continuous.Grid.StepSeconds + " s. H=Nδ; input/reference ricampionati dalle leggi fisiche.";
            }
            if (root.Q<Foldout>("cost-panel") is Foldout costPanel) costPanel.text = continuous == null ? "Costo discreto, limiti e confronto A/B" : "Funzionale continuo, quadratura e confronto A/B";
            root.Q<Label>("cost-semantics").text = continuous == null ? "Costo discreto per campione [1], senza δ. Q, QT ≥ 0; R > 0; convenzione ½." :
                "L [1/s] → δL [1]. Rettangolo sinistro; Φ [1] una volta, senza δ. Nessun ½ (I p.36). Pesi running per secondo; terminali senza /s.";
            horizon.SetEnabled(!sampled); horizon.SetValueWithoutNotify(state.Result.History.Horizon);
            var policy = root.Q<Label>("horizon-policy"); if (policy != null) policy.text = continuous != null ?
                "H fisso; scegli δ per ricampionare N, input e reference sulla stessa durata fisica. N+1 stati e N input." : sampled ?
                "Riferimento campionato su T fisso: un nuovo orizzonte richiede una curva coerente. Nessun ricampionamento implicito." : "Estendere T ripete l'ultimo input; ridurlo tronca la sequenza.";
            var legend = root.Q<Label>("cost-visual-legend"); if (legend != null) legend.text = continuous != null ?
                "Desiderato tratteggiato ≠ rollout Euler. Linee fra campioni: guide grafiche. Rettangoli: densità normalizzata; area numerica δL salvata. A/B: stessa durata H, griglie proprie." : sampled ?
                "Desiderato: tratteggio e diamante; rollout: linea continua e sfera. Il grafico dei costi separa stage e terminale. Un limite diagnostica, non satura l'input." :
                "Sfera ciano: target; diamante viola: x_T e costo terminale. Costi vs indice t. A: linea viola spessa; B: blu sottile.";
            root.Q<VisualElement>("cost-panel").style.display = state.Result.Costs == null ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var field in fields)
            {
                var parameter = state.Result.Configuration.Parameters.FirstOrDefault(p => p.Target == field.Key);
                field.Value.SetEnabled(parameter != null); if (parameter != null) field.Value.SetValueWithoutNotify(parameter.Value);
            }
            string Format(string id, FormulaScope scope = FormulaScope.CurrentTimestep) => FormulaPresenter.Format(resolver.Resolve(state, id, FormulaValueKind.Scalar, scope), "G6", false);
            root.Q<Label>("cost-live").text = "B · r" + state.Result.RevisionId + " · t=" + state.Sample.Timestep + "/" + state.Result.History.Horizon
                + (continuous == null ? "\nStage " : "\nContributo δL_t ") + Format(RuntimeMetricIds.StageCost)
                + (continuous == null ? "" : "\nDensità L_t " + Format("continuous.density")) + "\n" + DescribeTerms(state.Result.Costs?.StageAt(state.Sample.Timestep))
                + "\nC_t (stage già percorsi) " + Format(RuntimeMetricIds.CumulativeCost) + "\nTerminale previsto " + Format(RuntimeMetricIds.TerminalCost, FormulaScope.Terminal)
                + "\n" + totalSymbol + " (indice di prestazione, intero rollout) " + Format(RuntimeMetricIds.TotalCost);
            var a = state.Comparison?.Result;
            root.Q<Label>("comparison-status").text = a == null ? "A non salvato. Salva A, poi modifica B." : Describe("A", a) + "\n" + Describe("B", state.Result)
                + "\nCosti sotto i rispettivi obiettivi: cambiare pesi/target cambia il criterio. Nessuna etichetta di ottimalità.";
            if (state.DiscretizationComparison != null) root.Q<Label>("comparison-status").text +=
                "\nStesso problema fisico: " + (state.DiscretizationComparison.SamePhysicalProblem ? "sì" : "NO: leggi/parametri/criterio differiscono") +
                ". Differenza J_B−J_A=" + Format("continuous.comparison.cost-difference", FormulaScope.Snapshot) +
                ". Una griglia fine non è una soluzione continua esatta.";
            clear.SetEnabled(a != null);
        }
        private static string DescribeTerms(OptCon.Core.CostContribution contribution) => contribution == null ? "Contributi stage: N/A a T" :
            string.Join(" · ", contribution.Terms.Select(t => t.Name + " (" + t.Symbol + ")=" + t.Value.ToString("G6", CultureInfo.InvariantCulture) + " [" + t.Unit + "]"));
        private static string ParameterLabel(string target)
        {
            if (target.StartsWith("cost.qt.")) return "Q_T[" + target.Substring(8) + "]";
            if (target.StartsWith("cost.q.")) return "Q[" + target.Substring(7) + "]";
            if (target.StartsWith("cost.r.")) return "R[" + target.Substring(7) + "]";
            if (target.StartsWith("cost.target.")) return "x^des[" + target.Substring(12) + "]";
            if (target.StartsWith("cost.inputTarget.")) return "u^des[" + target.Substring(17) + "]";
            return target;
        }
        private string Describe(string label, SimulationResult result)
        {
            string Number(double value) => value.ToString("G6", CultureInfo.InvariantCulture);
            var parameters = result.Configuration.Parameters;
            return label + " · r" + result.RevisionId + " · T=" + result.History.Horizon + " · δ=" + (result.History.Model.DiscretizationStepSeconds.HasValue ? Number(result.History.Model.DiscretizationStepSeconds.Value) + " s" : "N/A")
                + (result.Configuration.Cost is OptCon.Core.ContinuousTrackingCost functional ? " · H=" + Number(functional.Grid.DurationSeconds) + " s" : "")
                + " · " + totalSymbol + "=" + (result.Costs == null ? "N/A" : Number(result.Costs.Total) + " [" + result.Costs.Configuration.CostUnit + "]")
                + "\nx0=[" + string.Join(";", result.Configuration.InitialState.Select(Number)) + "] ; u0=[" + string.Join(";", result.Configuration.Controls[0].Select(Number)) + "]\n" + result.Configuration.Model.Descriptor.Id + " · " + result.Configuration.Cost?.Id + "\n" + string.Join("; ", parameters.Select(p => p.Target + "=" + Number(p.Value) + " [" + p.Unit + "]"));
        }
        public void Dispose() { foreach (var remove in cleanup) remove(); root.Q<VisualElement>("cost-parameters").Clear(); DisplayedResult = null; }
    }
}
