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
        private IExperimentSession session;
        private System.IDisposable subscription;
        private bool subscribed;
        private long bindingGeneration;
        private System.Action<IExperimentSnapshot> callback;
        public IExperimentSession BoundExperiment => session;
        public SimulationSession BoundSession => session as SimulationSession;
        public void Bind(IExperimentSession value)
        {
            Unsubscribe(); session = value;
            if (isActiveAndEnabled) Subscribe();
            Render(session?.Current);
        }
        private void OnEnable() { Subscribe(); Render(session?.Current); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() { Unsubscribe(); session = null; }
        private void Subscribe()
        {
            if (session == null || subscribed) return;
            var source = session; long generation = bindingGeneration;
            callback = state => { if (subscribed && generation == bindingGeneration && ReferenceEquals(session, source)) Render(state); };
            subscription = session.Subscribe(callback); subscribed = true;
        }
        private void Unsubscribe()
        {
            bindingGeneration++; subscribed = false;
            subscription?.Dispose(); subscription = null;
            callback = null;
        }
        private static string Number(double? value) => value.HasValue ? value.Value.ToString("G10", CultureInfo.InvariantCulture) : "N/A";
        private void Render(IExperimentSnapshot publication)
        {
            var state = publication as SessionState;
            RenderDiagnostic(publication, state);
        }
        private void RenderDiagnostic(IExperimentSnapshot publication, SessionState state)
        {
            if (!isActiveAndEnabled) return;
            var label = GetComponent<UIDocument>().rootVisualElement.Q<Label>("session-diagnostic");
            if (label == null) return;
            label.text = publication == null ? string.Empty : state == null ? "Snapshot r" + publication.Result.RevisionId + (publication.Error == null ? "" : " | " + publication.Error) :
                $"Sessione r{state.Result.RevisionId} | t={state.Sample.Timestep}/{state.Sample.Horizon} | δ[s]={Number(state.Sample.DiscretizationStepSeconds)} | τ[s]={Number(state.Sample.PhysicalTimeSeconds)} | {(state.IsPlaying ? "play" : "pause")}" +
                (state.Error == null ? string.Empty : $" | tentativo r{state.AttemptRevisionId} fallito: {state.Error}");
        }
    }
}
