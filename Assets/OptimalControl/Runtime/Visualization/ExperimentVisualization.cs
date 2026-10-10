using OptCon.Simulation;
using OptCon.Content;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>One session subscription fans a coherent publication out to configured visual consumers.</summary>
    public sealed class ExperimentVisualization : MonoBehaviour, IVisualizationStatus
    {
        [SerializeField] private StateTrajectoryView[] plots;
        [SerializeField] private AngularStateView[] angularBodies;
        [SerializeField] private CurvedTorqueRenderer[] torques;
        [SerializeField] private HighlightRegistry highlights;
        [SerializeField] private TransitionHighlightView[] transitions = System.Array.Empty<TransitionHighlightView>();
        [SerializeField] private CostContributionVisualizer[] costs = System.Array.Empty<CostContributionVisualizer>();
        [SerializeField] private TargetVisualizer[] targets = System.Array.Empty<TargetVisualizer>();
        [SerializeField] private ComparisonTrajectoryRenderer[] comparisons = System.Array.Empty<ComparisonTrajectoryRenderer>();
        private SimulationSession session;
        private bool subscribed;
        private ILessonRuntime lesson;
        private LayerController layers;
        private long appliedGeneration = -1;
        private long bindingGeneration;
        private System.Action<SessionState> sessionCallback;
        private System.Action<LessonPresentationState> lessonCallback;
        private readonly System.Collections.Generic.Dictionary<Renderer, bool> hidden = new System.Collections.Generic.Dictionary<Renderer, bool>();
        public VisualizationStatus Status { get; private set; }
        public event System.Action<VisualizationStatus> StatusChanged;
        public SimulationSession BoundSession => session;
        public SessionState DisplayedState { get; private set; }
        public void Bind(SimulationSession value, ILessonRuntime runtime = null)
        {
            Unsubscribe(); session = value; lesson = runtime; layers = GetComponent<LayerController>(); appliedGeneration = -1;
            if (isActiveAndEnabled) { Subscribe(); if (value != null) Render(value.State); }
            if (value == null) { RestoreRenderers(); DisplayedState = null; Status = null; highlights.Clear(); }
        }
        private void Subscribe()
        {
            if (session == null || subscribed) return;
            long generation = bindingGeneration; var source = session; var education = lesson;
            sessionCallback = state => { if (subscribed && generation == bindingGeneration && ReferenceEquals(session, source)) Render(state); };
            lessonCallback = state => { if (subscribed && generation == bindingGeneration && ReferenceEquals(lesson, education)) ApplyPresentation(state); };
            subscribed = true; source.Changed += sessionCallback;
            if (education != null) education.Changed += lessonCallback;
        }
        private void Unsubscribe()
        {
            bindingGeneration++; subscribed = false;
            if (session != null && sessionCallback != null) session.Changed -= sessionCallback;
            if (lesson != null && lessonCallback != null) lesson.Changed -= lessonCallback;
            sessionCallback = null; lessonCallback = null;
        }
        private void ApplyPresentation(LessonPresentationState state)
        {
            if (!state.IsFreeExploration || appliedGeneration != state.Generation) layers?.Apply(state);
            appliedGeneration = state.Generation; highlights.Refresh();
        }
        private void OnEnable() { Subscribe(); if (session != null) Render(session.State); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
        private void Render(SessionState state)
        {
            DisplayedState = state; RestoreRenderers();
            try
            {
                foreach (var plot in plots) plot.Render(state);
                foreach (var body in angularBodies) body.Render(state);
                foreach (var torque in torques) torque.Render(state);
                foreach (var transition in transitions) transition.Render(state);
                foreach (var cost in costs) cost.Render(state);
                foreach (var target in targets) target.Render(state);
                foreach (var comparison in comparisons) comparison.Render(state);
                if (lesson != null) ApplyPresentation(lesson.Presentation);
                highlights.Refresh();
                Status = new VisualizationStatus(state, VisualizationAvailability.Available);
            }
            catch (NonRepresentableDisplayException exception)
            { Unavailable(state, VisualizationAvailability.NonRepresentable, exception.Message); }
            catch (System.Exception exception)
            {
                Unavailable(state, VisualizationAvailability.Failed, exception.ToString());
                StatusChanged?.Invoke(Status);
                throw; // Unexpected defects remain explicit; session dispatch still notifies other consumers.
            }
            StatusChanged?.Invoke(Status);
        }
        private void Unavailable(SessionState state, VisualizationAvailability availability, string diagnostic)
        {
            // A failed group is never left partially visible or semantically selectable.
            foreach (var surface in GetComponentsInChildren<Renderer>(true)) { hidden[surface] = surface.enabled; surface.enabled = false; }
            foreach (var target in GetComponentsInChildren<HighlightTarget>(true)) target.SetSample(state.Result.RevisionId, null);
            highlights.Refresh();
            Status = new VisualizationStatus(state, availability, diagnostic);
        }
        private void RestoreRenderers()
        {
            foreach (var pair in hidden) if (pair.Key != null) pair.Key.enabled = pair.Value;
            hidden.Clear();
        }
    }
}
