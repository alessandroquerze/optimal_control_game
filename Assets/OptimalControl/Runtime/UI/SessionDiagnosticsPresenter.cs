using System.Globalization;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>F04 diagnostic indicator only. All numbers come from one session publication.</summary>
    [RequireComponent(typeof(UIDocument)), DisallowMultipleComponent]
    public sealed class SessionDiagnosticsPresenter : MonoBehaviour
    {
        private SimulationSession session;
        private bool subscribed;
        private long bindingGeneration;
        private System.Action<SessionState> callback;
        public SimulationSession BoundSession => session;
        public void Bind(SimulationSession value)
        {
            Unsubscribe(); session = value;
            if (isActiveAndEnabled) Subscribe();
            Render(session?.State);
        }
        private void OnEnable() { Subscribe(); Render(session?.State); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() { Unsubscribe(); session = null; }
        private void Subscribe()
        {
            if (session == null || subscribed) return;
            var source = session; long generation = bindingGeneration;
            callback = state => { if (subscribed && generation == bindingGeneration && ReferenceEquals(session, source)) Render(state); };
            session.Changed += callback; subscribed = true;
        }
        private void Unsubscribe()
        {
            bindingGeneration++; subscribed = false;
            if (session != null && callback != null) session.Changed -= callback;
            callback = null;
        }
        private static string Number(double? value) => value.HasValue ? value.Value.ToString("G10", CultureInfo.InvariantCulture) : "N/A";
        private void Render(SessionState state)
        {
            if (!isActiveAndEnabled) return;
            var label = GetComponent<UIDocument>().rootVisualElement.Q<Label>("session-diagnostic");
            if (label == null) return;
            label.text = state == null ? string.Empty :
                $"Sessione r{state.Result.RevisionId} | t={state.Sample.Timestep}/{state.Sample.Horizon} | δ[s]={Number(state.Sample.DiscretizationStepSeconds)} | τ[s]={Number(state.Sample.PhysicalTimeSeconds)} | {(state.IsPlaying ? "play" : "pause")}" +
                (state.Error == null ? string.Empty : $" | tentativo r{state.AttemptRevisionId} fallito: {state.Error}");
        }
    }
}
