using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Non-trajectory parameter/reset adapter, composed in the same HUD shell.</summary>
    public sealed class SnapshotParameterPresenter : IDisposable
    {
        private readonly List<(DoubleField field, EventCallback<ChangeEvent<double>> callback)> fields = new List<(DoubleField, EventCallback<ChangeEvent<double>>) >();
        private readonly VisualElement panel;
        private readonly ICommandSink commands;
        private readonly Button reset;
        public SnapshotParameterPresenter(VisualElement root, ExperimentDefinition experiment, ICommandSink commands)
        {
            this.commands = commands; panel = root.Q<VisualElement>("snapshot-parameters"); panel.Clear();
            panel.Add(new Label("Parametri dell’esperimento") { name = "snapshot-parameter-title" });
            foreach (var parameter in experiment.Parameters)
            {
                var field = new DoubleField(parameter.Label + " [" + parameter.Unit + "]") { name = "parameter-" + parameter.ModelTarget, isDelayed = true, tooltip = parameter.Description };
                string target = parameter.ModelTarget;
                EventCallback<ChangeEvent<double>> callback = evt => { if (field.enabledInHierarchy) commands.Execute(SimulationCommand.SetParameter(target, evt.newValue)); };
                field.RegisterValueChangedCallback(callback); fields.Add((field, callback)); panel.Add(field);
            }
            reset = root.Q<Button>("reset"); reset.clicked += Reset;
        }
        private void Reset() => commands.Execute(SimulationCommand.Reset());
        public void Render(IExperimentSnapshot snapshot)
        {
            foreach (var pair in fields)
            {
                var value = snapshot.Result.Configuration.Parameters.Single(p => "parameter-" + p.Target == pair.field.name);
                pair.field.SetValueWithoutNotify(value.Value); pair.field.SetEnabled(!snapshot.IsClosed);
            }
        }
        public void Dispose()
        {
            reset.clicked -= Reset;
            foreach (var pair in fields) pair.field.UnregisterValueChangedCallback(pair.callback);
            fields.Clear(); panel.Clear();
        }
    }
}
