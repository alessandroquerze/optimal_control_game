using System.Globalization;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Geometry of saved physical cells/densities/input samples. Never integrates or evaluates the model.</summary>
    public sealed class ContinuousFunctionalView : MonoBehaviour
    {
        [SerializeField] private LineRenderer rectangles, gridTicks, inputs;
        [SerializeField] private TextMesh label, inputLabel;
        [SerializeField] private float width = 4, height = 1.1f;
        [SerializeField] private int inputComponent;
        public SessionState DisplayedState { get; private set; }
        public LineRenderer Rectangles => rectangles;
        public LineRenderer InputSamples => inputs;
        public void Render(SessionState state)
        {
            DisplayedState = state;
            var costs = state.Result.Costs; var cost = costs?.Configuration as ContinuousTrackingCost;
            foreach (var target in rectangles.GetComponents<HighlightTarget>()) target.SetSample(state.Result.RevisionId,
                cost != null && state.Sample.Timestep < cost.Grid.Transitions ? state.Sample.Timestep : (int?)null);
            if (cost == null) { rectangles.positionCount = gridTicks.positionCount = inputs.positionCount = 0; label.text = "Quadratura fisica N/A"; return; }
            var grid = cost.Grid; int n = grid.Transitions;
            double max = System.Math.Max(1e-12, costs.RunningDensities.Max(c => System.Math.Abs(c.Total)));
            double inputMax = System.Math.Max(1, state.Result.History.Controls.Max(u => System.Math.Abs(u[inputComponent])));
            var boxes = new Vector3[5 * n]; var ticks = new Vector3[3 * (n + 1)]; var controls = new Vector3[2 * n];
            for (int t = 0; t <= n; t++)
            {
                float x = width * StateProjection.ToFloat(grid.TimeAt(t) / grid.DurationSeconds);
                ticks[3*t] = new Vector3(x, 0, 0); ticks[3*t+1] = new Vector3(x, -.08f, 0); ticks[3*t+2] = new Vector3(x, 0, 0);
                if (t == n) continue;
                float next = width * StateProjection.ToFloat(grid.TimeAt(t + 1) / grid.DurationSeconds);
                float y = height * StateProjection.ToFloat(costs.RunningDensities[t].Total / max);
                boxes[5*t] = new Vector3(x,0,0); boxes[5*t+1] = new Vector3(x,y,0); boxes[5*t+2] = new Vector3(next,y,0);
                boxes[5*t+3] = new Vector3(next,0,0); boxes[5*t+4] = new Vector3(x,0,0);
                float u = -1.5f + height * StateProjection.ToFloat(state.Result.History.Controls[t][inputComponent] / inputMax);
                controls[2*t] = new Vector3(x,u,0); controls[2*t+1] = new Vector3(next,u,0);
            }
            rectangles.positionCount = boxes.Length; rectangles.SetPositions(boxes); gridTicks.positionCount = ticks.Length; gridTicks.SetPositions(ticks);
            inputs.positionCount = controls.Length; inputs.SetPositions(controls);
            string N(double v) => v.ToString("G5", CultureInfo.InvariantCulture);
            int k = state.Sample.Timestep;
            label.text = "L_t [1/s], altezza 0…" + N(max) + " · r" + state.Result.RevisionId + "\nτ: 0…" + N(grid.DurationSeconds) +
                " s; " + n + " celle, δ=" + N(grid.StepSeconds) + " s\nArea cella @" + k + " = " + (k == n ? "N/A" : N(costs.Stages[k].Total) + " [1]");
            inputLabel.text = "u_t[" + inputComponent + "] [" + state.Result.History.Model.ControlUnits[inputComponent] + "] vs τ [s]\nScala ±" + N(inputMax) + "; nessun u_N. Gradini: campioni applicati.";
        }
    }
}
