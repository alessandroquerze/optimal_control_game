using System;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    [RequireComponent(typeof(UIDocument)), DisallowMultipleComponent]
    public sealed class LessonHudPresenter : MonoBehaviour
    {
        private IExperimentSession session;
        private IDisposable sessionSubscription;
        private SnapshotParameterPresenter snapshotParameters;
        private LessonDefinition lesson;
        private HighlightRegistry highlights;
        private bool subscribed;
        private TimelinePresenter timeline;
        private ParameterPanelPresenter parameters;
        private CostPanelPresenter costs;
        private PerturbationPanelPresenter perturbations;
        private ILessonRuntime lessonRuntime;
        private LessonStepPresenter steps;
        private long appliedGeneration = -1;
        private long bindingGeneration;
        private Action<IExperimentSnapshot> sessionCallback;
        private Action<LessonPresentationState> lessonCallback;
        private Action<VisualizationStatus> statusCallback;
        private IVisualizationStatus visualization;
        private Label visualizationDiagnostic;
        private RuntimeMetricResolver metrics;
        public ILessonRuntime BoundLesson => lessonRuntime;
        public FormulaPresenter Formula { get; private set; }
        public IExperimentSession BoundExperiment => session;
        public SimulationSession BoundSession => session as SimulationSession;
        public IExperimentSnapshot DisplayedSnapshot { get; private set; }
        public SessionState DisplayedState => DisplayedSnapshot as SessionState;
        public bool HasEditingFocus
        {
            get
            {
                var focus = GetComponent<UIDocument>().rootVisualElement.focusController?.focusedElement as VisualElement;
                for (var element = focus; element != null; element = element.parent)
                    if (element is TextField || element is DoubleField || element is IntegerField || element is DropdownField) return true;
                return false;
            }
        }
        public bool BlocksLessonShortcut(string action)
        {
            if (HasEditingFocus) return true;
            if (action != "StepForward" && action != "StepBackward") return false;
            // A focused slider owns the arrow keys; P/R and input shortcuts remain usable.
            var focus = GetComponent<UIDocument>().rootVisualElement.focusController?.focusedElement as VisualElement;
            for (var element = focus; element != null; element = element.parent) if (element is SliderInt || element is Slider) return true;
            return false;
        }
        public void Bind(LessonDefinition destination, IExperimentSession value, HighlightRegistry registry, ILessonRuntime runtime = null, IVisualizationStatus visualStatus = null, RuntimeMetricResolver metricResolver = null)
        {
            // Standalone/test binding is itself a composition boundary; leaf presenters never create providers.
            metrics = metricResolver ?? new RuntimeMetricResolver();
            Disconnect(); lesson = destination; session = value; highlights = registry; lessonRuntime = runtime; visualization = visualStatus;
            if (isActiveAndEnabled) Connect();
        }
        private void Connect()
        {
            if (session == null || lesson == null || subscribed) return;
            var root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("lesson-hud");
            if (root == null || lesson.Formulas.Count == 0) throw new InvalidOperationException("Lesson HUD requires its visual tree and formula definitions.");
            Formula = new FormulaPresenter(root, lesson.Formulas, highlights, metrics);
            ICommandSink commands = lessonRuntime ?? (ICommandSink)session;
            if (session.Current is SessionState trajectory)
            {
                timeline = new TimelinePresenter(root, commands);
                parameters = new ParameterPanelPresenter(root, lesson.Experiment, commands, trajectory);
                costs = new CostPanelPresenter(root, commands, trajectory, metrics, lesson.Experiment.Cost?.TotalSymbol ?? "J");
                // Tools are an editorial opt-in, not an automatic consequence of a model's capability.
                // Older lessons keep their original focus/navigation surface in free exploration too.
                if (lesson.Steps.Any(step => step.EnabledControlIds.Contains(LessonControls.InitialPerturbation) ||
                    step.EnabledControlIds.Contains(LessonControls.InputPerturbation)))
                    perturbations = new PerturbationPanelPresenter(root, commands, trajectory, metrics);
                else root.Q<VisualElement>("linearization-controls").style.display = DisplayStyle.None;
            }
            else snapshotParameters = new SnapshotParameterPresenter(root, lesson.Experiment, commands);
            visualizationDiagnostic = root.Q<Label>("visualization-diagnostic");
            if (visualizationDiagnostic == null) { visualizationDiagnostic = new Label { name = "visualization-diagnostic" }; root.Insert(0, visualizationDiagnostic); }
            visualizationDiagnostic.AddToClassList("hud-note");
            long generation = bindingGeneration; var source = session; var education = lessonRuntime; var visual = visualization;
            bool Current() => subscribed && generation == bindingGeneration && ReferenceEquals(session, source);
            sessionCallback = state => { if (Current()) Render(state); };
            lessonCallback = state => { if (Current() && ReferenceEquals(lessonRuntime, education)) Render(source.Current); };
            statusCallback = state => { if (Current() && ReferenceEquals(visualization, visual)) RenderVisualizationStatus(); };
            if (lessonRuntime != null)
            {
                steps = new LessonStepPresenter(root, lessonRuntime); lessonRuntime.Changed += lessonCallback;
                Formula.TermSelected += lessonRuntime.RecordTermSelection;
            }
            else root.Q<VisualElement>("lesson-progress").style.display = DisplayStyle.None;
            subscribed = true; sessionSubscription = session.Subscribe(sessionCallback);
            if (visualization != null) visualization.StatusChanged += statusCallback;
            Render(session.Current);
        }
        private void Render(IExperimentSnapshot state)
        {
            DisplayedSnapshot = state;
            if (state is SessionState trajectory) { timeline.Render(trajectory); parameters.Render(trajectory); costs.Render(trajectory); }
            else
            {
                snapshotParameters.Render(state);
                var status = GetComponent<UIDocument>().rootVisualElement.Q<Label>("timeline-status");
                status.text = "Snapshot r" + state.Result.RevisionId + (state.Error == null ? "" : " \u00b7 tentativo r" + state.AttemptRevisionId + " fallito: " + state.Error);
            }
            Formula.Render(state);
            ApplyLesson();
            if (state is SessionState perturbed) perturbations?.Render(perturbed, lessonRuntime?.Presentation);
            else GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("linearization-controls").style.display = DisplayStyle.None;
            ApplyCapabilities();
            RenderVisualizationStatus();
        }
        private void RenderVisualizationStatus()
        {
            var status = visualization?.Status;
            bool unavailable = status != null && ReferenceEquals(status.Source.Result, session.Current.Result) && status.Availability != VisualizationAvailability.Available;
            visualizationDiagnostic.text = unavailable ? "3D unavailable · r" + status.Source.Result.RevisionId + ": " + status.Diagnostic + " I valori matematici restano validi." : "";
            visualizationDiagnostic.style.display = unavailable ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void ApplyLesson()
        {
            if (lessonRuntime == null) return;
            var state = lessonRuntime.Presentation;
            if (appliedGeneration != state.Generation)
            {
                appliedGeneration = state.Generation;
                Formula.SetFormula(state.IsFreeExploration || string.IsNullOrEmpty(state.Step?.FormulaId) ? lesson.Formulas[0].Id : state.Step.FormulaId);
                Formula.SetDisclosedTerms(state.IsFreeExploration ? null : state.Step?.VisibleTerms);
                Formula.Render(session.Current);
                if (!state.IsFreeExploration && !string.IsNullOrEmpty(state.Step?.SelectedTermId)) Formula.SelectTerm(state.Step.SelectedTermId, false);
            }
            steps.Render(state);
        }
        private void ApplyCapabilities()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            bool trajectory = (session.Capabilities & ExperimentCapabilities.Trajectory) != 0;
            foreach (string name in new[] { "step-back", "play-pause", "step-next", "timeline-seek" }) root.Q<VisualElement>(name).style.display = trajectory ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<VisualElement>("snapshot-parameters").style.display = trajectory ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q<VisualElement>("dynamic-legend").style.display = trajectory ? DisplayStyle.Flex : DisplayStyle.None;
            if (!trajectory) { root.Q<VisualElement>("parameter-panel").style.display = DisplayStyle.None; root.Q<VisualElement>("cost-panel").style.display = DisplayStyle.None; }
        }
        private void Disconnect()
        {
            bindingGeneration++; subscribed = false;
            sessionSubscription?.Dispose(); sessionSubscription = null;
            if (lessonRuntime != null && lessonCallback != null) lessonRuntime.Changed -= lessonCallback;
            if (visualization != null && statusCallback != null) visualization.StatusChanged -= statusCallback;
            sessionCallback = null; lessonCallback = null; statusCallback = null;
            if (visualizationDiagnostic != null) { visualizationDiagnostic.text = ""; visualizationDiagnostic.style.display = DisplayStyle.None; }
            visualizationDiagnostic = null;
            steps?.Dispose(); steps = null; appliedGeneration = -1;
            Formula?.Dispose(); timeline?.Dispose(); parameters?.Dispose(); costs?.Dispose(); perturbations?.Dispose(); perturbations = null; costs = null; Formula = null; timeline = null; parameters = null; snapshotParameters?.Dispose(); snapshotParameters = null; DisplayedSnapshot = null;
        }
        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();
        private void OnDestroy() { Disconnect(); session = null; lesson = null; highlights = null; lessonRuntime = null; }
    }
}
