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
    }
}
