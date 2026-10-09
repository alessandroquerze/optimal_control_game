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
        public void Bind(LessonDefinition destination, SimulationSession value, HighlightRegistry registry)
        {
            Disconnect(); lesson = destination; session = value; highlights = registry;
            if (isActiveAndEnabled) Connect();
        }
        private void Connect()
        {
            if (session == null || lesson == null || subscribed) return;
            var root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("lesson-hud");
            if (root == null || lesson.Formulas.Count == 0) throw new InvalidOperationException("Lesson HUD requires its visual tree and formula definitions.");
            Formula = new FormulaPresenter(root, lesson.Formulas, highlights);
            timeline = new TimelinePresenter(root, session);
            parameters = new ParameterPanelPresenter(root, lesson.Experiment, session, session.State);
            session.Changed += Render; subscribed = true; Render(session.State);
        }
        private void Render(SessionState state)
        {
            DisplayedState = state; timeline.Render(state); parameters.Render(state);
            Formula.Render(state.Result, state.Sample.Timestep);
        }
        private void Disconnect()
        {
            if (subscribed && session != null) session.Changed -= Render; subscribed = false;
            Formula?.Dispose(); timeline?.Dispose(); parameters?.Dispose(); Formula = null; timeline = null; parameters = null; DisplayedState = null;
        }
        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();
        private void OnDestroy() { Disconnect(); session = null; lesson = null; highlights = null; }
    }
}
