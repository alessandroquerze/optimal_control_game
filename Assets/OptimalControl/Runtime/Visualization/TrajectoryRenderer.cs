using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Connects saved discrete samples in index order; segments are a visual guide, not continuous integration.</summary>
    [RequireComponent(typeof(LineRenderer)), DisallowMultipleComponent]
    public sealed class TrajectoryRenderer : MonoBehaviour
    {
        public TrajectoryHistory Source { get; private set; }
        public void Render(TrajectoryHistory history, StateProjection projection, int? lastSample = null)
        {
            var line = GetComponent<LineRenderer>();
            var points = new Vector3[(lastSample ?? history.Horizon) + 1];
            for (int i = 0; i < points.Length; i++) points[i] = projection.Project(history, i);
            // Prepare the entire projection before changing any visible geometry or source identity.
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            Source = history;
        }
        /// <summary>Projects supplied derived samples in the original history's declared coordinate/time domain.</summary>
        public void RenderDerived(TrajectoryHistory context, System.Collections.Generic.IReadOnlyList<OptCon.Core.NumericVector> states, StateProjection projection)
        {
            if (states == null || states.Count != context.States.Count) throw new System.ArgumentException("Derived sample domain differs from its source history.");
            var points = new Vector3[states.Count];
            for (int i = 0; i < points.Length; i++) points[i] = projection.ProjectState(context, i, states[i]);
            var line = GetComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = points.Length;
            line.SetPositions(points); Source = context;
        }
    }
}
