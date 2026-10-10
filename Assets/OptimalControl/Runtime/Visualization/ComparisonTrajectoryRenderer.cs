using System;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>A and B under their own configurations. Only the common index/physical interval is overlaid.</summary>
    public sealed class ComparisonTrajectoryRenderer : MonoBehaviour
    {
        [SerializeField] private StateProjection projection;
        [SerializeField] private TrajectoryRenderer savedA, currentB;
        [SerializeField] private TextMesh label;
        public ComparisonSnapshot DisplayedComparison { get; private set; }
        public SimulationResult DisplayedResult { get; private set; }
        public void Render(SessionState state)
        {
            DisplayedResult = state.Result; DisplayedComparison = state.Comparison;
            var a = state.Comparison?.Result; var b = state.Result;
            if (a == null) { Clear(); label.text = "Confronto: salva A dal pannello costi"; return; }
            var stepA = a.History.Model.DiscretizationStepSeconds; var stepB = b.History.Model.DiscretizationStepSeconds;
            if (stepA.HasValue != stepB.HasValue || a.Configuration.Model.Descriptor.Id != b.Configuration.Model.Descriptor.Id)
            { Clear(); label.text = "A/B: domini diversi, sovrapposizione N/A"; return; }
            int endA, endB;
            if (stepA.HasValue)
            {
                double duration = Math.Min(a.History.PhysicalTimeAt(a.History.Horizon).Value, b.History.PhysicalTimeAt(b.History.Horizon).Value);
                endA = Math.Min(a.History.Horizon, (int)Math.Floor(duration / stepA.Value + 1e-10));
                endB = Math.Min(b.History.Horizon, (int)Math.Floor(duration / stepB.Value + 1e-10));
            }
            else endA = endB = Math.Min(a.History.Horizon, b.History.Horizon);
            savedA.Render(a.History, projection, endA); currentB.Render(b.History, projection, endB);
            label.text = "A viola spessa / B blu sottile · fase\nA r" + a.RevisionId + " T=" + a.History.Horizon + " · B r" + b.RevisionId + " T=" + b.History.Horizon
                + "\nCampioni comuni: A 0…" + endA + " / B 0…" + endB;
        }
        private void Clear() { savedA.GetComponent<LineRenderer>().positionCount = 0; currentB.GetComponent<LineRenderer>().positionCount = 0; }
    }
}
