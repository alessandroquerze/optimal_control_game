using System;
using OptCon.Simulation;
using OptCon.Presentation;
using UnityEngine;

namespace OptCon.Application
{
    /// <summary>Owns the lifetime of one pure session and its presentation-clock adapter.</summary>
    [RequireComponent(typeof(PlaybackController)), DisallowMultipleComponent]
    public sealed class ExperimentHost : MonoBehaviour
    {
        public SimulationSession Session { get; private set; }
        public ICommandSink Commands => Session;
        public void Initialize(SimulationSession session)
        {
            if (Session != null) throw new InvalidOperationException("Host already owns a session.");
            Session = session ?? throw new ArgumentNullException(nameof(session));
            foreach (var view in GetComponentsInChildren<ExperimentVisualization>(true)) view.Bind(session);
            foreach (var input in GetComponentsInChildren<LessonInputRouter>(true)) input.Bind(session);
            GetComponent<PlaybackController>().Bind(session);
        }
        public void Close()
        {
            GetComponent<PlaybackController>()?.Bind(null);
            foreach (var input in GetComponentsInChildren<LessonInputRouter>(true)) input.Bind(null);
            foreach (var view in GetComponentsInChildren<ExperimentVisualization>(true)) view.Bind(null);
            Session?.Dispose(); Session = null;
        }
        private void OnDestroy() => Close();
    }
}
