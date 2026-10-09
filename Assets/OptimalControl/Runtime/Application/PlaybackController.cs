using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Application
{
    /// <summary>Frame adapter only; no FixedUpdate, dynamics integration or physical step inference.</summary>
    [DisallowMultipleComponent]
    public sealed class PlaybackController : MonoBehaviour
    {
        private SimulationSession session;
        public void Bind(SimulationSession value) => session = value;
        public void Tick(double presentationSeconds) => session?.AdvancePlayback(presentationSeconds);
        private void Update() => Tick(Time.unscaledDeltaTime);
        private void OnDestroy() => session = null;
    }
}
