using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    public sealed class TargetVisualizer : MonoBehaviour
    {
        [SerializeField] private StateTrajectoryView plot;
        [SerializeField] private StateMarker target, terminal;
        public SimulationResult DisplayedResult { get; private set; }
        public StateMarker Target => target;
        public StateMarker Terminal => terminal;
        public void Render(SessionState state)
        {
            DisplayedResult = state.Result; var result = state.Result; int t = state.Sample.Timestep, T = result.History.Horizon;
            var reference = result.Configuration.ReferenceState;
            target.Render(reference == null ? Vector3.zero : plot.Projection.ProjectState(result.History, t, reference), result.RevisionId, reference == null ? (int?)null : t);
            terminal.Render(plot.Projection.Project(result.History, T), result.RevisionId, T);
        }
    }
}
