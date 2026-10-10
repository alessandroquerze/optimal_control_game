using System;
using System.Collections.Generic;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Generic decision-plane point and scalar ordinate; consumes typed metrics from one publication.</summary>
    public sealed class MetricPointVisualization : MonoBehaviour, IExperimentPresentation
    {
        [SerializeField] private string pointMetric = ObjectiveMetricSource.Decision;
        [SerializeField] private string ordinateMetric = ObjectiveMetricSource.Value;
        [SerializeField] private int horizontalComponent;
        [SerializeField] private int verticalComponent = 1;
        [SerializeField] private float pointScale = 1;
        [SerializeField] private float ordinateScale = .25f;
        [SerializeField] private StateMarker point;
        [SerializeField] private StateMarker ordinate;
        [SerializeField] private TextMesh valueLabel;
        private IExperimentSession session;
        private ILessonRuntime lesson;
        private RuntimeMetricResolver metrics;
        private IDisposable subscription;
        private Action<LessonPresentationState> lessonCallback;
        private long generation, appliedGeneration = -1;
        private readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();
        public IExperimentSession BoundExperiment => session;
        public ExperimentCapabilities RequiredCapabilities => ExperimentCapabilities.Objective;
        public IExperimentSnapshot DisplayedSnapshot { get; private set; }
        public VisualizationStatus Status { get; private set; }
        public StateMarker Point => point;
        public StateMarker Ordinate => ordinate;
        public event Action<VisualizationStatus> StatusChanged;
        public void BindExperiment(IExperimentSession value, ILessonRuntime runtime, RuntimeMetricResolver resolver)
        {
            Disconnect(); session = value; lesson = runtime; metrics = resolver; appliedGeneration = -1;
            if (isActiveAndEnabled) Connect();
            if (value == null) { Restore(); DisplayedSnapshot = null; Status = null; GetComponent<HighlightRegistry>()?.Clear(); }
        }
        private void Connect()
        {
            if (session == null || subscription != null) return;
            var source = session; var education = lesson; long current = generation;
            subscription = source.Subscribe(snapshot => { if (current == generation && ReferenceEquals(source, session)) Render(snapshot); });
            lessonCallback = state => { if (current == generation && ReferenceEquals(lesson, education)) ApplyLesson(state); };
            if (lesson != null) lesson.Changed += lessonCallback;
            Render(source.Current);
        }
        private void ApplyLesson(LessonPresentationState state)
        {
            if (!state.IsFreeExploration || appliedGeneration != state.Generation) GetComponent<LayerController>()?.Apply(state);
            appliedGeneration = state.Generation; GetComponent<HighlightRegistry>()?.Refresh();
        }
        private void Disconnect()
        {
            generation++; subscription?.Dispose(); subscription = null;
            if (lesson != null && lessonCallback != null) lesson.Changed -= lessonCallback;
            lessonCallback = null;
        }
        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();
        private void OnDestroy() => Disconnect();
        private void Render(IExperimentSnapshot snapshot)
        {
            DisplayedSnapshot = snapshot; Restore();
            try
            {
                var position = metrics.Resolve(snapshot, pointMetric, FormulaValueKind.Vector);
                var value = metrics.Resolve(snapshot, ordinateMetric, FormulaValueKind.Scalar);
                if (position.Validity != MetricValidity.Available || value.Validity != MetricValidity.Available)
                    throw new InvalidOperationException(position.Diagnostic ?? value.Diagnostic ?? "Point metrics unavailable.");
                if (horizontalComponent < 0 || verticalComponent < 0 || horizontalComponent >= position.Value.Rows || verticalComponent >= position.Value.Rows)
                    throw new ArgumentException("Point projection component outside metric shape.");
                // Geometry only. Objective value is saved in the result, never evaluated by this view.
                var xy = new Vector3(StateProjection.ToFloat(position.Value.Vector[horizontalComponent] * pointScale),
                    StateProjection.ToFloat(position.Value.Vector[verticalComponent] * pointScale), 0);
                var height = new Vector3(0, StateProjection.ToFloat(value.Value.Scalar.Value * ordinateScale), 0);
                point.RenderGlobal(xy, snapshot.Result.RevisionId); ordinate.RenderGlobal(height, snapshot.Result.RevisionId);
                valueLabel.text = FormulaPresenter.Format(value);
                if (lesson != null) ApplyLesson(lesson.Presentation);
                GetComponent<HighlightRegistry>()?.Refresh(); Status = new VisualizationStatus(snapshot, VisualizationAvailability.Available);
            }
            catch (NonRepresentableDisplayException exception) { Unavailable(snapshot, VisualizationAvailability.NonRepresentable, exception.Message); }
            catch (Exception exception)
            {
                Unavailable(snapshot, VisualizationAvailability.Failed, exception.ToString()); StatusChanged?.Invoke(Status); throw;
            }
            StatusChanged?.Invoke(Status);
        }
        private void Unavailable(IExperimentSnapshot snapshot, VisualizationAvailability availability, string diagnostic)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) { hidden[renderer] = renderer.enabled; renderer.enabled = false; }
            foreach (var target in GetComponentsInChildren<HighlightTarget>(true)) target.SetSample(snapshot.Result.RevisionId, null);
            GetComponent<HighlightRegistry>()?.Refresh(); Status = new VisualizationStatus(snapshot, availability, diagnostic);
        }
        private void Restore() { foreach (var pair in hidden) if (pair.Key != null) pair.Key.enabled = pair.Value; hidden.Clear(); }
    }
}
