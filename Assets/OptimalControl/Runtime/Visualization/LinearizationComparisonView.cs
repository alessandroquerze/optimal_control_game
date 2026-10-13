using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Displays saved comparison samples. Only coordinate projection and glyph geometry occur here.</summary>
    public sealed class LinearizationComparisonView : MonoBehaviour
    {
        [SerializeField] private StateProjection projection;
        [SerializeField] private TrajectoryRenderer nominal, prediction;
        [SerializeField] private VectorRenderer deltaState, error;
        [SerializeField] private CurvedTorqueRenderer deltaTorque;
        [SerializeField] private int torqueComponent;
        [SerializeField] private double errorDisplayScale = 20;
        [SerializeField] private TextMesh label;
        public SessionState DisplayedState { get; private set; }
        public LinearizationComparison DisplayedComparison { get; private set; }
        public void Render(SessionState state)
        {
            DisplayedState = state; DisplayedComparison = state.Linearization;
            var data = state.Linearization; int t = state.Sample.Timestep;
            if (data == null)
            {
                foreach (var line in new[] { nominal.GetComponent<LineRenderer>(), prediction.GetComponent<LineRenderer>(), deltaState.GetComponent<LineRenderer>(), error.GetComponent<LineRenderer>() }) line.positionCount = 0;
                deltaTorque.RenderValue(null, state.Result.History.Model.ControlUnits[torqueComponent], state.Result.RevisionId, null, "Δu_t");
                foreach (var target in GetComponentsInChildren<HighlightTarget>(true)) target.SetSample(state.Result.RevisionId, null);
                label.text = "Linearizzazione: N/A\n" + state.LinearizationDiagnostic; return;
            }
            var history = state.Result.History;
            nominal.Render(data.Nominal.History, projection); prediction.RenderDerived(history, data.PredictedStates, projection);
            var anchor = projection.Project(data.Nominal.History, t);
            var predicted = projection.ProjectState(history, t, data.PredictedStates[t]);
            deltaState.Render(anchor, projection.ProjectDisplacement(history.Model, data.TrueDeltaStates[t]), "proiezione dello stato (scale degli assi)", 1);
            error.Render(predicted, projection.ProjectDisplacement(history.Model, data.Errors[t]), "proiezione errore (scale degli assi)", errorDisplayScale);
            foreach (var target in new[] { nominal.GetComponent<HighlightTarget>(), prediction.GetComponent<HighlightTarget>(), deltaState.GetComponent<HighlightTarget>(), error.GetComponent<HighlightTarget>() })
                target?.SetSample(state.Result.RevisionId, t);
            deltaTorque.RenderValue(t == data.Horizon ? (double?)null : data.DeltaInputs[t][torqueComponent],
                history.Model.ControlUnits[torqueComponent], state.Result.RevisionId, t == data.Horizon ? (int?)null : t, "Δu_t");
            label.text = "CONFRONTO NEL PIANO DI FASE\nNominale: viola · vero: blu\nPrevisione lineare: ambra\nΔx: ciano · errore: rosso ×" + errorDisplayScale +
                "\nScale: quelle degli assi (θ,ω)\nNessuna posizione fisica\nNominale r" + data.Nominal.RevisionId + " / vero r" + history.RevisionId + " · t=" + t;
        }
    }
}
