using System;
using System.Globalization;
using System.Linq;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Editor
{
    /// <summary>Generic read-only diagnostics. Displays stored snapshots; performs no model evaluation.</summary>
    public sealed class RolloutDiagnosticsWindow : EditorWindow
    {
        private TrajectoryHistory history;

        public static void ShowHistory(TrajectoryHistory history)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            var window = GetWindow<RolloutDiagnosticsWindow>();
            window.titleContent = new GUIContent("F03 Rollout");
            window.minSize = new Vector2(760, 300);
            window.history = history;
            window.CreateGUI();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            if (history != null) rootVisualElement.Add(new RolloutDiagnosticsView(history));
            else rootVisualElement.Add(new Label("Select an experiment and run Tools > Optimal Control > F03 > Inspect Selected Pendulum Preset."));
        }
    }

    /// <summary>Tabular view reusable with any discrete model dimension and component units.</summary>
    public sealed class RolloutDiagnosticsView : VisualElement
    {
        public TrajectoryHistory History { get; }

        public RolloutDiagnosticsView(TrajectoryHistory history)
        {
            History = history ?? throw new ArgumentNullException(nameof(history));
            style.flexGrow = 1;
            style.paddingTop = style.paddingBottom = 16;
            style.paddingLeft = style.paddingRight = 16;
            style.backgroundColor = new Color(0.075f, 0.10f, 0.14f);
            style.color = new Color(0.93f, 0.95f, 0.98f);
            Add(new Label($"{history.ExperimentId} | {history.Model.Id} v{history.Model.Version} | revision {history.RevisionId}") { name = "rollout-identity" });
            Add(new Label($"T={history.Horizon}; states={history.States.Count}; inputs={history.Controls.Count}; delta [s]={Format(history.Model.DiscretizationStepSeconds)}") { name = "rollout-counts" });
            Add(new Label("Stored discrete samples. Physical time is not a spatial axis. N/A = no input or next state at terminal.") { name = "rollout-convention" });
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.style.flexGrow = 1;
            Add(scroll);
            AddRow(scroll, "header", "t", "tau [s]", "x_t", "u_t", "x_(t+1)");
            for (int t = 0; t <= history.Horizon; t++)
            {
                var snapshot = history.GetSnapshot(t);
                AddRow(scroll, "sample-" + t, t.ToString(CultureInfo.InvariantCulture), Format(snapshot.PhysicalTimeSeconds),
                    FormatMetric(snapshot.Metrics[SimulationMetricIds.CurrentState]),
                    FormatMetric(snapshot.Metrics[SimulationMetricIds.AppliedInput]),
                    FormatMetric(snapshot.Metrics[SimulationMetricIds.NextState]));
            }
        }

        private static string Format(double? value) => value.HasValue ? value.Value.ToString("G10", CultureInfo.InvariantCulture) : "N/A";

        private static string FormatMetric(VectorMetric metric)
        {
            if (metric.Validity != MetricValidity.Available) return "N/A (" + metric.Validity + ")";
            return "[" + string.Join(", ", metric.Value.Select(v => v.ToString("G10", CultureInfo.InvariantCulture))) + "] "
                + "[" + string.Join(", ", metric.Descriptor.ComponentUnits) + "] @" + metric.SampleIndex;
        }

        private static void AddRow(VisualElement parent, string name, params string[] cells)
        {
            var row = new VisualElement { name = name };
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexShrink = 0;
            row.style.marginTop = 8;
            for (int i = 0; i < cells.Length; i++)
            {
                var label = new Label(cells[i]) { name = name + "-" + i };
                label.style.width = i == 0 ? 40 : i == 1 ? 90 : 280;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.flexShrink = 0;
                row.Add(label);
            }
            parent.Add(row);
        }
    }
}
