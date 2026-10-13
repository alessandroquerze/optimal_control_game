using System.Globalization;
using System.Linq;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>One-dimensional admissible component interval, its borders and the saved sample. No enforcement.</summary>
    public sealed class ConstraintRegionVisualizer : MonoBehaviour
    {
        [SerializeField] private string boundId;
        [SerializeField] private LineRenderer border;
        [SerializeField] private StateMarker sample;
        [SerializeField] private TextMesh label;
        [SerializeField] private float displayScale = .25f;
        public SessionState DisplayedState { get; private set; }
        public double? DisplayedResidual { get; private set; }
        public void Render(SessionState state)
        {
            DisplayedState = state; var data = state.Result.Constraints;
            int i = data == null ? -1 : data.Configuration.Path.ToList().FindIndex(b => b.Id == boundId);
            int t = state.Sample.Timestep; DisplayedResidual = i < 0 || state.Sample.IsTerminal ? (double?)null : data.PathResiduals[t][i];
            if (i < 0) { border.positionCount = 0; sample.Render(Vector3.zero, state.Result.RevisionId, null); label.text = "Vincolo non configurato"; return; }
            var b = data.Configuration.Path[i]; float low = StateProjection.ToFloat((b.IsInterval ? b.Minimum : -b.Limit) * displayScale), high = StateProjection.ToFloat(b.Limit * displayScale);
            border.useWorldSpace = false; border.positionCount = 5;
            border.SetPositions(new[] { new Vector3(low,-.12f,0), new Vector3(high,-.12f,0), new Vector3(high,.12f,0), new Vector3(low,.12f,0), new Vector3(low,-.12f,0) });
            double? value = state.Sample.IsTerminal ? (double?)null : (b.IsState ? state.Sample.CurrentState : state.Sample.AppliedControl)[b.Component];
            sample.Render(value.HasValue ? new Vector3(StateProjection.ToFloat(value.Value * displayScale),0,0) : Vector3.zero, state.Result.RevisionId, value.HasValue ? t : (int?)null);
            string N(double v) => v.ToString("G5", CultureInfo.InvariantCulture);
            label.text = "INSIEME AMMISSIBILE · " + b.Name + "\n[" + N(b.IsInterval ? b.Minimum : -b.Limit) + ", " + N(b.Limit) + "] [" + b.Unit + "]\n" + (b.IsInterval ? "g=max(min−v,v−max): " : "g=|" + (b.IsState ? "x" : "u") + "[" + b.Component + "]|−limite: ") +
                (DisplayedResidual.HasValue ? N(DisplayedResidual.Value) + (DisplayedResidual.Value > data.Configuration.Tolerance ? " · VIOLAZIONE ×" : " · entro/sul bordo ✓") : "N/A a T") +
                "\nDiagnostica: nessuna saturazione o solver\nr" + state.Result.RevisionId + " · t=" + t;
        }
    }
}
