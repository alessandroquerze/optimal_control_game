using System;
using System.Globalization;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>A signed curved torque glyph, not a translational force; radius/span are graphical constants.</summary>
    [RequireComponent(typeof(LineRenderer), typeof(HighlightTarget))]
    public sealed class CurvedTorqueRenderer : MonoBehaviour
    {
        [SerializeField] private int inputComponent;
        [SerializeField] private string expectedUnit = "N·m";
        [SerializeField] private float radius = 0.48f;
        [SerializeField] private TextMesh label;
        public int? SampleIndex => GetComponent<HighlightTarget>().SampleIndex;
        public void Render(SessionState state)
        {
            var sample = state.Sample;
            if (inputComponent < 0 || inputComponent >= sample.Model.Dimensions.Control || sample.Model.ControlUnits[inputComponent] != expectedUnit)
                throw new ArgumentException("Torque glyph requires an explicitly configured rotational input/unit.");
            GetComponent<HighlightTarget>().SetSample(sample.RevisionId, sample.IsTerminal ? (int?)null : sample.Timestep);
            var line = GetComponent<LineRenderer>(); line.useWorldSpace = false;
            label.text = sample.IsTerminal ? "Coppia: N/A al terminale" : "Coppia u[" + inputComponent + "] = " +
                sample.AppliedControl[inputComponent].ToString("G4", CultureInfo.InvariantCulture) + " " + expectedUnit + "\n+ antiorario; dimensione del simbolo fissa";
            if (sample.IsTerminal || sample.AppliedControl[inputComponent] == 0) { line.positionCount = 0; return; }
            float sign = Math.Sign(sample.AppliedControl[inputComponent]);
            const int segments = 24;
            line.positionCount = segments + 5;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (-60 + sign * 240 * i / segments) * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0));
            }
            var end = line.GetPosition(segments);
            var tangent = sign * new Vector3(-end.y, end.x, 0).normalized;
            var normal = new Vector3(end.x, end.y, 0).normalized;
            line.SetPosition(segments + 1, end - tangent * 0.13f + normal * 0.065f);
            line.SetPosition(segments + 2, end);
            line.SetPosition(segments + 3, end - tangent * 0.13f - normal * 0.065f);
            line.SetPosition(segments + 4, end);
        }
    }
}
