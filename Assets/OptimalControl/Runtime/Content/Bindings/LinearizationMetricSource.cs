using System;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Content
{
    /// <summary>Lookup only: every value belongs to the session's frozen nominal/perturbed revision pair.</summary>
    public sealed class LinearizationMetricSource : IExperimentMetricSource
    {
        public const string NominalState = "linearization.nominal-state", NominalInput = "linearization.nominal-input",
            TrueDelta = "linearization.delta-true", LinearDelta = "linearization.delta-linear", DeltaInput = "linearization.delta-input",
            Prediction = "linearization.prediction", Error = "linearization.error", InitialDelta = "linearization.delta-initial",
            NextLinearDelta = "linearization.delta-next", FixedInitial = "linearization.fixed-initial";
        public bool TryGet(IExperimentSnapshot snapshot, string id, out RuntimeMetric metric)
        {
            metric = null;
            if (!(snapshot is SessionState state) || !id.StartsWith("linearization.", StringComparison.Ordinal)) return false;
            var data = state.Linearization; var model = state.Result.History.Model; int t = state.Sample.Timestep;
            string missing = state.LinearizationDiagnostic ?? "Nominale non disponibile: salva una traiettoria con lo stesso modello e orizzonte.";
            bool terminal = state.Sample.IsTerminal;
            NumericVector value = null; var units = model.StateUnits; int? index = t;
            var scope = FormulaScope.CurrentTimestep;
            if (id == FixedInitial)
            {
                metric = RuntimeMetric.Number(id, data == null ? (double?)null : data.HasFixedInitialState ? 1 : 0,
                    "1", null, missing, FormulaScope.Snapshot); return true;
            }
            if (id.StartsWith("linearization.a.", StringComparison.Ordinal) || id.StartsWith("linearization.b.", StringComparison.Ordinal))
            {
                var parts = id.Split('.'); bool a = parts[1] == "a";
                if (parts.Length != 4 || !int.TryParse(parts[2], out int row) || !int.TryParse(parts[3], out int column) ||
                    row < 0 || row >= model.Dimensions.State || column < 0 || column >= (a ? model.Dimensions.State : model.Dimensions.Control)) return false;
                string numerator = model.StateUnits[row], denominator = (a ? model.StateUnits : model.ControlUnits)[column];
                string unit = numerator == denominator ? "1" : "(" + numerator + ")/(" + denominator + ")";
                metric = RuntimeMetric.Number(id, data == null || terminal ? (double?)null : (a ? data.A[t] : data.B[t])[row, column],
                    unit, t, data == null ? missing : "Nessuna transizione o Jacobiano a T."); return true;
            }
            switch (id)
            {
                case NominalState: value = data?.Nominal.History.States[t]; break;
                case NominalInput: units = model.ControlUnits; value = data == null || terminal ? null : data.Nominal.History.Controls[t]; break;
                case TrueDelta: value = data?.TrueDeltaStates[t]; break;
                case LinearDelta: value = data?.LinearDeltaStates[t]; break;
                case DeltaInput: units = model.ControlUnits; value = data == null || terminal ? null : data.DeltaInputs[t]; break;
                case Prediction: value = data?.PredictedStates[t]; break;
                case Error: value = data?.Errors[t]; break;
                case InitialDelta: scope = FormulaScope.Snapshot; index = null; value = data?.LinearDeltaStates[0]; break;
                case NextLinearDelta: scope = FormulaScope.NextTimestep; index = terminal ? (int?)null : t + 1; value = data == null || terminal ? null : data.LinearDeltaStates[t + 1]; break;
                default: return false;
            }
            metric = RuntimeMetric.FromVector(id, value, units, index, scope, data == null ? missing : "Nessun input o next oltre T.");
            return true;
        }
    }
}
