using System;
using System.Collections.Generic;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Editors send amplitudes; the pure session applies them relative to the frozen nominal.</summary>
    public sealed class PerturbationPanelPresenter : IDisposable
    {
        private readonly VisualElement panel, initialGroup, inputGroup;
        private readonly Label status;
        private readonly Button capture;
        private readonly Action captureAction;
        private readonly RuntimeMetricResolver metrics;
        private readonly List<Action> cleanup = new List<Action>();
        private readonly List<(DoubleField field, Slider slider, bool initial, int component)> fields = new List<(DoubleField, Slider, bool, int)>();
        private SessionState state;
        private LessonPresentationState policy;
        public PerturbationPanelPresenter(VisualElement root, ICommandSink commands, SessionState initial, RuntimeMetricResolver resolver)
        {
            metrics = resolver; panel = root.Q<VisualElement>("linearization-controls"); panel.Clear();
            capture = new Button { name = "nominal-capture", text = "Salva traiettoria nominale" };
            captureAction = () => commands.Execute(SimulationCommand.CaptureComparison()); capture.clicked += captureAction;
            panel.Add(capture); status = new Label { name = "linearization-status" }; status.AddToClassList("hud-note"); panel.Add(status);
            initialGroup = new VisualElement { name = "initial-perturbations" }; inputGroup = new VisualElement { name = "input-perturbations" };
            panel.Add(initialGroup); panel.Add(inputGroup);
            var model = initial.Result.History.Model;
            void Add(bool isInitial, int component, string unit)
            {
                string id = (isInitial ? "perturb-initial-" : "perturb-input-") + component;
                var group = isInitial ? initialGroup : inputGroup;
                var field = new DoubleField((isInitial ? "Δx₀[" : "Δu_t[") + component + "] [" + unit + "]") { name = id, isDelayed = true };
                // This is a display range, not a mathematical bound. The double field permits values beyond it.
                var slider = new Slider(-1, 1) { name = id + "-slider" };
                void Send(double value)
                {
                    if (state == null) return;
                    if (!commands.Execute(isInitial ? SimulationCommand.SetInitialPerturbation(component, value) :
                        SimulationCommand.SetInputPerturbation(state.Sample.Timestep, component, value)))
                    {
                        Render(state, policy);
                        status.text += "\nModifica rifiutata: nessuna nuova revisione; valori validi conservati.";
                    }
                }
                EventCallback<ChangeEvent<double>> edit = e => Send(e.newValue);
                EventCallback<ChangeEvent<float>> slide = e => Send(e.newValue);
                field.RegisterValueChangedCallback(edit); slider.RegisterValueChangedCallback(slide);
                cleanup.Add(() => { field.UnregisterValueChangedCallback(edit); slider.UnregisterValueChangedCallback(slide); });
                group.Add(field); group.Add(slider); fields.Add((field, slider, isInitial, component));
            }
            for (int i = 0; i < model.Dimensions.State; i++) Add(true, i, model.StateUnits[i]);
            for (int i = 0; i < model.Dimensions.Control; i++) Add(false, i, model.ControlUnits[i]);
        }
        public void Render(SessionState publication, LessonPresentationState presentation)
        {
            state = publication; policy = presentation;
            bool offered = (state.Capabilities & ExperimentCapabilities.Linearization) != 0;
            bool Allowed(string control) => policy == null || policy.Allows(control);
            bool initial = Allowed(LessonControls.InitialPerturbation), input = Allowed(LessonControls.InputPerturbation), save = Allowed(LessonControls.Comparison);
            panel.style.display = offered && (initial || input || save) ? DisplayStyle.Flex : DisplayStyle.None;
            capture.style.display = save ? DisplayStyle.Flex : DisplayStyle.None;
            capture.SetEnabled(save && !state.IsClosed && state.Error == null);
            initialGroup.style.display = initial ? DisplayStyle.Flex : DisplayStyle.None;
            inputGroup.style.display = input && !state.Sample.IsTerminal ? DisplayStyle.Flex : DisplayStyle.None;
            status.text = state.Linearization == null ? "N/A · " + state.LinearizationDiagnostic :
                "Nominale r" + state.Linearization.Nominal.RevisionId + " · rollout r" + state.Result.RevisionId +
                (state.Linearization.HasFixedInitialState ? " · Δx₀=0 (iniziale fissato)" : " · iniziale perturbato") +
                "\nΔu modifica solo la transizione t selezionata. Slider: −1…1 nelle unità indicate; campo numerico senza questo limite.";
            if (state.Error != null) status.text += "\nEditing indisponibile dopo richiesta fallita: Reset o recupera la richiesta.";
            if (state.Sample.IsTerminal) status.text += "\nΔu_T: N/A — il campione terminale non ha una transizione/input.";
            foreach (var item in fields)
            {
                var resolution = metrics.Resolve(state, item.initial ? LinearizationMetricSource.InitialDelta : LinearizationMetricSource.DeltaInput,
                    FormulaValueKind.Scalar, item.initial ? FormulaScope.Snapshot : FormulaScope.CurrentTimestep, item.component);
                bool available = resolution.Validity == MetricValidity.Available && state.Error == null && !state.IsClosed;
                item.field.SetEnabled(available && (item.initial ? initial : input)); item.slider.SetEnabled(item.field.enabledSelf);
                if (resolution.Value?.Scalar is double value)
                {
                    item.field.SetValueWithoutNotify(value);
                    item.slider.SetValueWithoutNotify((float)Math.Max(-1, Math.Min(1, value)));
                }
            }
        }
        public void Dispose()
        {
            capture.clicked -= captureAction; foreach (var remove in cleanup) remove(); panel.Clear(); state = null;
        }
    }
}
