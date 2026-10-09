using OptCon.Simulation;
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
        private SimulationSession session;
        private bool subscribed;
        public SimulationSession BoundSession => session;
        public SessionState DisplayedState { get; private set; }
        public void Bind(SimulationSession value)
        {
            Unsubscribe(); session = value;
            if (isActiveAndEnabled) { Subscribe(); if (value != null) Render(value.State); }
            if (value == null) { DisplayedState = null; highlights.Clear(); }
        }
        private void Subscribe() { if (session != null && !subscribed) { session.Changed += Render; subscribed = true; } }
        private void Unsubscribe() { if (session != null && subscribed) session.Changed -= Render; subscribed = false; }
        private void OnEnable() { Subscribe(); if (session != null) Render(session.State); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
        private void Render(SessionState state)
        {
            foreach (var plot in plots) plot.Render(state);
            foreach (var body in angularBodies) body.Render(state);
            foreach (var torque in torques) torque.Render(state);
            foreach (var transition in transitions) transition.Render(state);
            highlights.Refresh(); DisplayedState = state;
        }
    }
}
