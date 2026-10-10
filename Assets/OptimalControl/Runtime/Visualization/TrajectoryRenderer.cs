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
            line.useWorldSpace = false;
            line.positionCount = (lastSample ?? history.Horizon) + 1;
            for (int i = 0; i < line.positionCount; i++) line.SetPosition(i, projection.Project(history, i));
            Source = history;
        }
    }
}
