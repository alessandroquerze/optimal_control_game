using System.Linq;
using OptCon.Application;
using OptCon.Core;
using OptCon.Simulation;
using UnityEditor;
using UnityEngine;

namespace OptCon.Editor
{
    /// <summary>Editor-only command probe for F04; no runtime HUD or student input router.</summary>
    [CustomEditor(typeof(ExperimentHost))]
    public sealed class ExperimentHostEditor : UnityEditor.Editor
    {
        private int inputIndex;
        private InputEditScope inputScope;
        public override void OnInspectorGUI()
        {
            var host = (ExperimentHost)target;
            var session = host.Session;
            if (session == null) { EditorGUILayout.HelpBox("Enter Play and open a lesson to inspect its session.", MessageType.Info); return; }
            var state = session.State;
            EditorGUILayout.LabelField("Result revision / attempt", state.Result.RevisionId + " / " + state.AttemptRevisionId);
            EditorGUILayout.LabelField("Sample", state.Sample.Timestep + " / " + state.Sample.Horizon);
            EditorGUILayout.LabelField("Physical step [s]", state.Sample.DiscretizationStepSeconds?.ToString("G10") ?? "N/A");
            if (state.Error != null) EditorGUILayout.HelpBox(state.Error, MessageType.Error);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play")) session.Execute(SimulationCommand.Play());
            if (GUILayout.Button("Pause")) session.Execute(SimulationCommand.Pause());
            if (GUILayout.Button("Step -")) session.Execute(SimulationCommand.StepBackward());
            if (GUILayout.Button("Step +")) session.Execute(SimulationCommand.StepForward());
            if (GUILayout.Button("Reset")) session.Execute(SimulationCommand.Reset());
            EditorGUILayout.EndHorizontal();
            int seek = EditorGUILayout.IntSlider("Seek", state.Sample.Timestep, 0, state.Sample.Horizon);
            if (seek != state.Sample.Timestep) session.Execute(SimulationCommand.Seek(seek));
            double rate = EditorGUILayout.DoubleField("Playback [samples/s]", state.SamplesPerSecond);
            if (rate != state.SamplesPerSecond) session.Execute(SimulationCommand.SetPlaybackRate(rate));
            inputIndex = EditorGUILayout.IntSlider("Input sample", inputIndex, 0, state.Sample.Horizon - 1);
            inputScope = (InputEditScope)EditorGUILayout.EnumPopup("Input scope", inputScope);
            var input = state.RequestedConfiguration.Controls[inputIndex].ToArray();
            bool changed = false;
            for (int i = 0; i < input.Length; i++)
            {
                double value = EditorGUILayout.DoubleField("u[" + i + "]", input[i]);
                changed |= value != input[i]; input[i] = value;
            }
            if (changed && input.All(NumericValidation.IsFinite)) session.Execute(SimulationCommand.SetInput(inputIndex, new NumericVector(input), inputScope));
            foreach (var parameter in state.RequestedConfiguration.Model.Parameters)
            {
                double value = EditorGUILayout.DoubleField(parameter.Target + " [" + parameter.Unit + "]", parameter.Value);
                if (value != parameter.Value) session.Execute(SimulationCommand.SetParameter(parameter.Target, value));
            }
            if (state.IsPlaying) Repaint();
        }
    }
}
