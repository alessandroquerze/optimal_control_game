using System;
using System.Linq;
using OptCon.Content;
using OptCon.Core;
using OptCon.Simulation;
using UnityEditor;

namespace OptCon.Editor
{
    /// <summary>Editor-only verification of an authored pendulum preset. No runtime provider or session.</summary>
    public static class PendulumRolloutDiagnostic
    {
        [MenuItem("Tools/Optimal Control/F03/Inspect Selected Pendulum Preset")]
        private static void InspectSelected()
        {
            try { RolloutDiagnosticsWindow.ShowHistory(Generate(Selection.activeObject as ExperimentDefinition)); }
            catch (ArgumentException exception) { UnityEngine.Debug.LogError("F03 diagnostic: " + exception.Message); }
            catch (RolloutException exception) { UnityEngine.Debug.LogError("F03 diagnostic: " + exception.Message); }
        }

        public static TrajectoryHistory Generate(ExperimentDefinition experiment)
        {
            if (experiment == null) throw new ArgumentException("Select a pendulum ExperimentDefinition asset in the Project.", nameof(experiment));
            if (!experiment.UsesDiscreteTime) throw new ArgumentException("The pendulum diagnostic requires discrete time.", nameof(experiment));
            double Parameter(string target)
            {
                var values = experiment.Parameters.Where(p => p != null && p.ModelTarget == target).ToArray();
                if (values.Length != 1) throw new ArgumentException($"Expected one parameter targeting '{target}'.", nameof(experiment));
                return values[0].DefaultValue;
            }
            var model = new PendulumDynamics(new PendulumParameters(Parameter("model.mass"), Parameter("model.length"), Parameter("model.gravity")), experiment.DiscreteTime.StepSeconds);
            var descriptor = model.Descriptor;
            if (experiment.StateDimension != descriptor.Dimensions.State || experiment.ControlDimension != descriptor.Dimensions.Control || experiment.OutputDimension != descriptor.Dimensions.Output
                || !experiment.StateUnits.SequenceEqual(descriptor.StateUnits) || !experiment.ControlUnits.SequenceEqual(descriptor.ControlUnits) || !experiment.OutputUnits.SequenceEqual(descriptor.OutputUnits))
                throw new ArgumentException("Preset dimensions/units differ from the SI pendulum model.", nameof(experiment));
            int horizon = experiment.DiscreteTime.Horizon;
            if (horizon <= 0) throw new ArgumentOutOfRangeException(nameof(experiment), "Horizon must be positive.");
            var control = new NumericVector(experiment.DiscreteTime.CopyDefaultControl());
            var inputs = new NumericVector[horizon];
            for (int t = 0; t < horizon; t++) inputs[t] = control;
            return RolloutEngine.Rollout(model, new NumericVector(experiment.DiscreteTime.CopyInitialState()), inputs, experiment.Id);
        }
    }
}
