using System.Globalization;
using System.Linq;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Graphs saved stage/terminal contributions and saved prefix sums. Scaling is display geometry only.</summary>
    public sealed class CostContributionVisualizer : MonoBehaviour
    {
        [SerializeField] private LineRenderer stages, cumulative;
        [SerializeField] private StateMarker current, terminal;
        [SerializeField] private TextMesh label, cumulativeLabel, stageLabel, totalLabel;
        [SerializeField] private float width = 3.2f, height = 1.2f;
        [SerializeField] private string totalSymbol = "J";
        public SimulationResult DisplayedResult { get; private set; }
        public double ContributionScaleMaximum { get; private set; }
        public double CumulativeScaleMaximum { get; private set; }
        public LineRenderer StageLine => stages;
        public LineRenderer CumulativeLine => cumulative;
        public StateMarker Terminal => terminal;
        public void Render(SessionState state)
        {
            DisplayedResult = state.Result; var costs = state.Result.Costs;
            stages.positionCount = costs?.Stages.Count ?? 0; cumulative.positionCount = costs?.Cumulative.Count ?? 0;
            if (costs == null)
            {
                current.Render(Vector3.zero, state.Result.RevisionId, null); terminal.Render(Vector3.zero, state.Result.RevisionId, null);
                label.text = "Costo non configurato"; cumulativeLabel.text = stageLabel.text = totalLabel.text = ""; return;
            }
            int T = costs.History.Horizon;
            ContributionScaleMaximum = System.Math.Max(1e-12, System.Math.Max(costs.Terminal.Total, costs.Stages.Max(c => c.Total)));
            CumulativeScaleMaximum = System.Math.Max(1e-12, costs.Cumulative.Max());
            for (int t = 0; t < T; t++) stages.SetPosition(t, ContributionPoint(t, costs.Stages[t].Total, T));
            for (int t = 0; t <= T; t++) cumulative.SetPosition(t, new Vector3(width * t / T, 1.9f + height * StateProjection.ToFloat(costs.Cumulative[t] / CumulativeScaleMaximum), 0));
            int cursor = state.Sample.Timestep;
            foreach (var target in cumulative.GetComponents<HighlightTarget>()) target.SetSample(state.Result.RevisionId, cursor);
            current.Render(cursor == T ? Vector3.zero : ContributionPoint(cursor, costs.Stages[cursor].Total, T), state.Result.RevisionId, cursor == T ? (int?)null : cursor);
            terminal.Render(ContributionPoint(T, costs.Terminal.Total, T), state.Result.RevisionId, T);
            string N(double value) => value.ToString("G6", CultureInfo.InvariantCulture);
            string unit = costs.Configuration.CostUnit;
            bool physical = costs.Configuration.Semantics == OptCon.Core.CostSemantics.ContinuousFunctionalLeftRectangle;
            label.text = (physical ? "Quadratura continua vs τ · r" : "Costo discreto vs indice t · r") + state.Result.RevisionId;
            cumulativeLabel.text = "C_t: stage PRIMA di t, esclude ℓ_T\nScala verticale [" + unit + "]: 0…" + N(CumulativeScaleMaximum);
            stageLabel.text = "Stage t<T; diamante: ℓ_T @" + T + "\nScala verticale [" + unit + "]: 0…" + N(ContributionScaleMaximum);
            totalLabel.text = "t: 0…" + T + " [indice] · " + totalSymbol + "=" + N(costs.Total) + " [" + unit + "]\nNessun fattore δ; linee = guide fra campioni";
            if (physical)
            {
                var grid = ((OptCon.Core.ContinuousTrackingCost)costs.Configuration).Grid;
                cumulativeLabel.text = "C_t: celle PRIMA di t, esclude Φ\nScala [" + unit + "]: 0…" + N(CumulativeScaleMaximum);
                stageLabel.text = "δL_t, t<N; diamante: Φ @N\nScala [" + unit + "]: 0…" + N(ContributionScaleMaximum);
                totalLabel.text = "τ: 0…" + N(grid.DurationSeconds) + " s · N=" + T + " · δ=" + N(grid.StepSeconds) + " s\n" + totalSymbol + "=" + N(costs.Total) + " [" + unit + "]; Φ una volta";
            }
        }
        private Vector3 ContributionPoint(int t, double value, int T) => new Vector3(width * t / T, height * StateProjection.ToFloat(value / ContributionScaleMaximum), 0);
    }
}
