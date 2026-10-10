using OptCon.Simulation;
using OptCon.Content;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>One session subscription fans a coherent publication out to configured visual consumers.</summary>
    public sealed class ExperimentVisualization : MonoBehaviour
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
        public SimulationSession BoundSession => session;
        public SessionState DisplayedState { get; private set; }
        public void Bind(SimulationSession value, ILessonRuntime runtime = null)
        {
            Unsubscribe(); session = value; lesson = runtime; layers = GetComponent<LayerController>(); appliedGeneration = -1;
            if (isActiveAndEnabled) { Subscribe(); if (value != null) Render(value.State); }
            if (value == null) { DisplayedState = null; highlights.Clear(); }
        }
        private void Subscribe() { if (session != null && !subscribed) { session.Changed += Render; if (lesson != null) lesson.Changed += ApplyPresentation; subscribed = true; } }
        private void Unsubscribe() { if (subscribed) { if (session != null) session.Changed -= Render; if (lesson != null) lesson.Changed -= ApplyPresentation; } subscribed = false; }
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
            foreach (var plot in plots) plot.Render(state);
            foreach (var body in angularBodies) body.Render(state);
            foreach (var torque in torques) torque.Render(state);
            foreach (var transition in transitions) transition.Render(state);
            foreach (var cost in costs) cost.Render(state);
            foreach (var target in targets) target.Render(state);
            foreach (var comparison in comparisons) comparison.Render(state);
            if (lesson != null) ApplyPresentation(lesson.Presentation);
            highlights.Refresh(); DisplayedState = state;
        }
    }
}
