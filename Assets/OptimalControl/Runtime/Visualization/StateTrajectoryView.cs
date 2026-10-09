using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Generic state plot: all points and marker indices come from the same immutable result.</summary>
    public sealed class StateTrajectoryView : MonoBehaviour
    {
        [SerializeField] private StateProjection projection;
        [SerializeField] private TrajectoryRenderer trajectory;
        [SerializeField] private StateMarker current, next;
        [SerializeField] private PlotAxes axes;
        public StateProjection Projection => projection;
        public TrajectoryRenderer Trajectory => trajectory;
        public StateMarker Current => current;
        public StateMarker Next => next;
        public void Render(SessionState state)
        {
            var history = state.Result.History;
            if (trajectory.Source != history) { trajectory.Render(history, projection); axes.Render(projection, history.Model); }
            int t = state.Sample.Timestep;
            current.Render(projection.Project(history, t), history.RevisionId, t);
            next.Render(state.Sample.IsTerminal ? Vector3.zero : projection.Project(history, t + 1),
                history.RevisionId, state.Sample.IsTerminal ? (int?)null : t + 1);
        }
    }
}
