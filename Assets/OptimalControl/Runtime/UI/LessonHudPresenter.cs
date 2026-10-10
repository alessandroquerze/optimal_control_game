using System;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    [RequireComponent(typeof(UIDocument)), DisallowMultipleComponent]
    public sealed class LessonHudPresenter : MonoBehaviour
    {
        private SimulationSession session;
        private LessonDefinition lesson;
        private HighlightRegistry highlights;
        private bool subscribed;
        private TimelinePresenter timeline;
        private ParameterPanelPresenter parameters;
        private CostPanelPresenter costs;
        private ILessonRuntime lessonRuntime;
        private LessonStepPresenter steps;
        private long appliedGeneration = -1;
        private long bindingGeneration;
        private Action<SessionState> sessionCallback;
        private Action<LessonPresentationState> lessonCallback;
        private Action<VisualizationStatus> statusCallback;
        private IVisualizationStatus visualization;
        private Label visualizationDiagnostic;
        public ILessonRuntime BoundLesson => lessonRuntime;
        public FormulaPresenter Formula { get; private set; }
        public SimulationSession BoundSession => session;
        public SessionState DisplayedState { get; private set; }
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
            for (var element = focus; element != null; element = element.parent) if (element is SliderInt) return true;
            return false;
        }
        public void Bind(LessonDefinition destination, SimulationSession value, HighlightRegistry registry, ILessonRuntime runtime = null, IVisualizationStatus visualStatus = null)
        {
            Disconnect(); lesson = destination; session = value; highlights = registry; lessonRuntime = runtime; visualization = visualStatus;
            if (isActiveAndEnabled) Connect();
        }
        private void Connect()
        {
            if (session == null || lesson == null || subscribed) return;
            var root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("lesson-hud");
            if (root == null || lesson.Formulas.Count == 0) throw new InvalidOperationException("Lesson HUD requires its visual tree and formula definitions.");
            Formula = new FormulaPresenter(root, lesson.Formulas, highlights);
            ICommandSink commands = lessonRuntime ?? (ICommandSink)session;
            timeline = new TimelinePresenter(root, commands);
            parameters = new ParameterPanelPresenter(root, lesson.Experiment, commands, session.State);
            costs = new CostPanelPresenter(root, commands, session.State);
            visualizationDiagnostic = root.Q<Label>("visualization-diagnostic");
            if (visualizationDiagnostic == null) { visualizationDiagnostic = new Label { name = "visualization-diagnostic" }; root.Insert(0, visualizationDiagnostic); }
            visualizationDiagnostic.AddToClassList("hud-note");
            long generation = bindingGeneration; var source = session; var education = lessonRuntime; var visual = visualization;
            bool Current() => subscribed && generation == bindingGeneration && ReferenceEquals(session, source);
            sessionCallback = state => { if (Current()) Render(state); };
            lessonCallback = state => { if (Current() && ReferenceEquals(lessonRuntime, education)) Render(source.State); };
            statusCallback = state => { if (Current() && ReferenceEquals(visualization, visual)) RenderVisualizationStatus(); };
            if (lessonRuntime != null)
            {
                steps = new LessonStepPresenter(root, lessonRuntime); lessonRuntime.Changed += lessonCallback;
                Formula.TermSelected += lessonRuntime.RecordTermSelection;
            }
            else root.Q<VisualElement>("lesson-progress").style.display = DisplayStyle.None;
            subscribed = true; session.Changed += sessionCallback;
            if (visualization != null) visualization.StatusChanged += statusCallback;
            Render(session.State);
        }
        private void Render(SessionState state)
        {
            DisplayedState = state; timeline.Render(state); parameters.Render(state); costs.Render(state);
            Formula.Render(state.Result, state.Sample.Timestep);
            ApplyLesson();
            RenderVisualizationStatus();
        }
        private void RenderVisualizationStatus()
        {
            var status = visualization?.Status;
            bool unavailable = status != null && ReferenceEquals(status.Source.Result, session.State.Result) && status.Availability != VisualizationAvailability.Available;
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
                Formula.Render(session.State.Result, session.State.Sample.Timestep);
                if (!state.IsFreeExploration && !string.IsNullOrEmpty(state.Step?.SelectedTermId)) Formula.SelectTerm(state.Step.SelectedTermId, false);
            }
            steps.Render(state);
        }
        private void Disconnect()
        {
            bindingGeneration++; subscribed = false;
            if (session != null && sessionCallback != null) session.Changed -= sessionCallback;
            if (lessonRuntime != null && lessonCallback != null) lessonRuntime.Changed -= lessonCallback;
            if (visualization != null && statusCallback != null) visualization.StatusChanged -= statusCallback;
            sessionCallback = null; lessonCallback = null; statusCallback = null;
            if (visualizationDiagnostic != null) { visualizationDiagnostic.text = ""; visualizationDiagnostic.style.display = DisplayStyle.None; }
            visualizationDiagnostic = null;
            steps?.Dispose(); steps = null; appliedGeneration = -1;
            Formula?.Dispose(); timeline?.Dispose(); parameters?.Dispose(); costs?.Dispose(); costs = null; Formula = null; timeline = null; parameters = null; DisplayedState = null;
        }
        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();
        private void OnDestroy() { Disconnect(); session = null; lesson = null; highlights = null; lessonRuntime = null; }
    }
}
