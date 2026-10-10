using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using OptCon.Core;
using OptCon.Simulation;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Generic editorial parameters and input components. Only the session validates and simulates edits.</summary>
    public sealed class ParameterPanelPresenter : IDisposable
    {
        private readonly VisualElement root;
        private readonly ICommandSink commands;
        private readonly List<(DoubleField field, string target)> parameters = new List<(DoubleField, string)>();
        private readonly List<DoubleField> inputs = new List<DoubleField>();
        private readonly List<Action> cleanup = new List<Action>();
        private readonly DropdownField scope;
        private SessionState state;
        public ParameterPanelPresenter(VisualElement root, ExperimentDefinition experiment, ICommandSink commands, SessionState initial)
        {
            this.root = root; this.commands = commands;
            var list = root.Q<VisualElement>("parameter-fields"); list.Clear();
            foreach (var descriptor in experiment.Parameters)
            {
                string target = descriptor.ModelTarget;
                var field = new DoubleField(descriptor.Label + " [" + descriptor.Unit + "]") { name = "parameter-" + target, isDelayed = true,
                    tooltip = descriptor.Description + " · range [" + descriptor.Minimum + ", " + descriptor.Maximum + "] · passo " + descriptor.Step };
                EventCallback<ChangeEvent<double>> handler = e => commands.Execute(SimulationCommand.SetParameter(target, e.newValue));
                field.RegisterValueChangedCallback(handler); cleanup.Add(() => field.UnregisterValueChangedCallback(handler));
                list.Add(field); parameters.Add((field, target));
            }
            scope = root.Q<DropdownField>("input-scope"); scope.choices = new List<string> { "Solo transizione t", "Coda da t" }; scope.SetValueWithoutNotify(scope.choices[1]);
            var inputList = root.Q<VisualElement>("input-fields"); inputList.Clear();
            var model = initial.Result.Configuration.Model.Descriptor;
            for (int i = 0; i < model.Dimensions.Control; i++)
            {
                int component = i;
                var field = new DoubleField("u[" + i + "] [" + model.ControlUnits[i] + "]") { name = "input-" + i, isDelayed = true };
                EventCallback<ChangeEvent<double>> handler = e => ChangeInput(component, e.newValue);
                field.RegisterValueChangedCallback(handler); cleanup.Add(() => field.UnregisterValueChangedCallback(handler)); inputList.Add(field); inputs.Add(field);
            }
        }
        private void ChangeInput(int component, double value)
        {
            if (state == null || !state.TryGetEditableInput(state.Sample.Timestep, out var editable)) return;
            if (!NumericValidation.IsFinite(value)) { root.Q<Label>("parameter-status").text = "Input rifiutato: valore non finito."; RenderFields(); return; }
            var values = editable.ToArray(); values[component] = value;
            commands.Execute(SimulationCommand.SetInput(state.Sample.Timestep, new NumericVector(values), scope.index == 0 ? InputEditScope.Single : InputEditScope.Tail));
        }
        public void Render(SessionState value) { state = value; root.Q<Label>("parameter-status").text = value.InputEditingDiagnostic ?? "Valori della revisione valida; le modifiche ricalcolano la simulazione."; RenderFields(); }
        private void RenderFields()
        {
            foreach (var entry in parameters)
            {
                var value = state.Result.Configuration.Model.Parameters.FirstOrDefault(p => p.Target == entry.target);
                entry.field.SetEnabled(value != null); if (value != null) entry.field.SetValueWithoutNotify(value.Value);
            }
            bool editable = state.TryGetEditableInput(state.Sample.Timestep, out _);
            scope.SetEnabled(editable);
            root.Q<VisualElement>("input-fields").style.display = state.Sample.IsTerminal ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q<Label>("input-unavailable").style.display = state.Sample.IsTerminal ? DisplayStyle.Flex : DisplayStyle.None;
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].SetEnabled(editable);
                if (!state.Sample.IsTerminal) inputs[i].SetValueWithoutNotify(state.Sample.AppliedControl[i]);
            }
        }
        public void Dispose() { foreach (var remove in cleanup) remove(); root.Q<VisualElement>("parameter-fields").Clear(); root.Q<VisualElement>("input-fields").Clear(); state = null; }
    }
}
