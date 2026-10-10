using System;
using System.Linq;
using OptCon.Simulation;
using OptCon.Presentation;
using OptCon.Content;
using UnityEngine;

namespace OptCon.Application
{
    /// <summary>Owns one common experiment lifetime; trajectory adapters are bound only when offered.</summary>
    [DisallowMultipleComponent]
    public sealed class ExperimentHost : MonoBehaviour
    {
        public IExperimentSession Experiment { get; private set; }
        public SimulationSession Session => Experiment as SimulationSession;
        public LessonSession Lesson { get; private set; }
        public IExperimentPresentation Presentation { get; private set; }
        public ICommandSink Commands => Lesson ?? (ICommandSink)Experiment;
        public void Initialize(IExperimentSession session, LessonSession lesson = null, RuntimeMetricResolver metrics = null)
        {
            if (Experiment != null) throw new InvalidOperationException("Host already owns an experiment.");
            Experiment = session ?? throw new ArgumentNullException(nameof(session));
            Lesson = lesson;
            Presentation = GetComponents<MonoBehaviour>().OfType<IExperimentPresentation>().SingleOrDefault();
            if (Presentation == null) throw new InvalidOperationException("Scenario root requires one experiment presentation.");
            if ((session.Capabilities & Presentation.RequiredCapabilities) != Presentation.RequiredCapabilities)
                throw new InvalidOperationException("Scenario presentation requires unavailable experiment capabilities.");
            Presentation.BindExperiment(session, lesson, metrics ?? new RuntimeMetricResolver());
            foreach (var input in GetComponentsInChildren<LessonInputRouter>(true)) input.Bind(Session, lesson);
            GetComponent<PlaybackController>()?.Bind(Session);
        }
        public void Close()
        {
            GetComponent<PlaybackController>()?.Bind(null);
            foreach (var input in GetComponentsInChildren<LessonInputRouter>(true)) input.Bind(null);
            Presentation?.BindExperiment(null, null, null); Presentation = null;
            Lesson?.Dispose(); Lesson = null;
            Experiment?.Dispose(); Experiment = null;
        }
        private void OnDestroy() => Close();
    }
}
