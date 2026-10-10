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
        public void Bind(LessonDefinition destination, SimulationSession value, HighlightRegistry registry, ILessonRuntime runtime = null)
        {
            Disconnect(); lesson = destination; session = value; highlights = registry; lessonRuntime = runtime;
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
            if (lessonRuntime != null)
            {
                steps = new LessonStepPresenter(root, lessonRuntime); lessonRuntime.Changed += OnLessonChanged;
                Formula.TermSelected += lessonRuntime.RecordTermSelection;
            }
            else root.Q<VisualElement>("lesson-progress").style.display = DisplayStyle.None;
            session.Changed += Render; subscribed = true; Render(session.State);
        }
        private void Render(SessionState state)
        {
            DisplayedState = state; timeline.Render(state); parameters.Render(state); costs.Render(state);
            Formula.Render(state.Result, state.Sample.Timestep);
            ApplyLesson();
        }
        private void OnLessonChanged(LessonPresentationState state) { if (subscribed) Render(session.State); }
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
            if (subscribed && session != null) session.Changed -= Render; subscribed = false;
            if (lessonRuntime != null) lessonRuntime.Changed -= OnLessonChanged;
            steps?.Dispose(); steps = null; appliedGeneration = -1;
            Formula?.Dispose(); timeline?.Dispose(); parameters?.Dispose(); costs?.Dispose(); costs = null; Formula = null; timeline = null; parameters = null; DisplayedState = null;
        }
        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();
        private void OnDestroy() { Disconnect(); session = null; lesson = null; highlights = null; lessonRuntime = null; }
    }
}
