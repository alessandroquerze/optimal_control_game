using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Highlights the saved current transition in a declared plot, never predicts a state.</summary>
    [RequireComponent(typeof(LineRenderer), typeof(HighlightTarget))]
    public sealed class TransitionHighlightView : MonoBehaviour
    {
        [SerializeField] private StateTrajectoryView plot;
        public void Render(SessionState state)
        {
            var line = GetComponent<LineRenderer>();
            var target = GetComponent<HighlightTarget>();
            target.SetSample(state.Result.RevisionId, state.Sample.IsTerminal ? (int?)null : state.Sample.Timestep);
            line.positionCount = state.Sample.IsTerminal ? 0 : 2;
            if (state.Sample.IsTerminal) return;
            int t = state.Sample.Timestep;
            line.SetPosition(0, plot.Projection.Project(state.Result.History, t));
            line.SetPosition(1, plot.Projection.Project(state.Result.History, t + 1));
        }
    }
}
