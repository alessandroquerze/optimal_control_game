using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;

namespace OptCon.Content
{
    public static class RuntimeMetricIds
    {
        public const string Index = "time.index", PhysicalTime = "time.physical", PhysicalStep = "time.step", Model = "dynamics.model";
        public const string StageCost = "cost.stage", TrackingCost = "cost.tracking", EffortCost = "cost.effort",
            TerminalCost = "cost.terminal", TotalCost = "cost.total", CumulativeCost = "cost.cumulative", ReferenceState = "state.reference";
        public const string ParameterPrefix = "parameter.";
        public const string Output = "output.current", Horizon = "trajectory.horizon", StateCount = "trajectory.state-count", InputCount = "trajectory.input-count";
    }

    /// <summary>Pure typed value. Scalars are scalars; a one-component vector remains a vector.</summary>
    public sealed class RuntimeMetric
    {
        public string Id { get; }
        public FormulaValueKind Kind { get; }
        public FormulaScope Scope { get; }
        public int Rows { get; }
        public int Columns { get; }
        public IReadOnlyList<string> Units { get; }
        public MetricValidity Validity { get; }
        public string Diagnostic { get; }
        public int? SampleIndex { get; }
        public double? Scalar { get; }
        public NumericVector Vector { get; }
        public ModelConfiguration Model { get; }
        private RuntimeMetric(string id, FormulaValueKind kind, FormulaScope scope, int rows, int columns,
            IReadOnlyList<string> units, int? index, double? scalar, NumericVector vector, ModelConfiguration model,
            MetricValidity validity, string diagnostic)
        {
            Id = id; Kind = kind; Scope = scope; Rows = rows; Columns = columns;
            Units = Array.AsReadOnly(units.ToArray()); SampleIndex = index; Scalar = scalar; Vector = vector; Model = model;
            Validity = validity; Diagnostic = diagnostic;
        }
        public static RuntimeMetric Number(string id, double? value, string unit, int? index, string unavailable = "Dato non disponibile.", FormulaScope scope = FormulaScope.CurrentTimestep)
        {
            if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("A scalar requires a unit; use 1 for dimensionless values.");
            if (value.HasValue) NumericValidation.RequireFinite(value.Value, nameof(value));
            return new RuntimeMetric(id, FormulaValueKind.Scalar, scope, 1, 1, new[] { unit },
                value.HasValue ? index : null, value, null, null, value.HasValue ? MetricValidity.Available : MetricValidity.NotApplicable, value.HasValue ? null : unavailable);
        }
        public static RuntimeMetric FromVector(string id, NumericVector value, IReadOnlyList<string> units, int? index, FormulaScope scope,
            string unavailable = "Nessuna transizione al campione terminale.")
        {
            if (units == null || units.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Vector component units are required.");
            if (value != null && value.Count != units.Count) throw new ArgumentException("Vector shape and units differ.");
            return new RuntimeMetric(id, FormulaValueKind.Vector, scope, units.Count, 1, units, value == null ? (int?)null : index, null, value, null,
                value == null ? MetricValidity.NotApplicable : MetricValidity.Available, value == null ? unavailable : null);
        }
        internal static RuntimeMetric Descriptor(string id, ModelConfiguration model, int? index) =>
            new RuntimeMetric(id, FormulaValueKind.ModelDescriptor, FormulaScope.CurrentTimestep, 0, 0, Array.Empty<string>(), index,
                null, null, model, MetricValidity.Available, null);
        internal RuntimeMetric Component(int index) => new RuntimeMetric(Id, FormulaValueKind.Scalar, Scope, 1, 1,
            new[] { Units[index] }, SampleIndex, Vector == null ? (double?)null : Vector[index], null, null, Validity, Diagnostic);
    }

    /// <summary>Extension point for actual future providers, without forcing matrices or scalars into vectors.</summary>
    public interface IRuntimeMetricSource
    {
        bool TryGet(SimulationResult result, SimulationSnapshot sample, string id, out RuntimeMetric metric);
    }

    public sealed class SessionMetricSource : IRuntimeMetricSource
    {
        public bool TryGet(SimulationResult result, SimulationSnapshot sample, string id, out RuntimeMetric metric)
        {
            metric = null;
            if (sample.Metrics.TryGetValue(id, out var vector))
                metric = RuntimeMetric.FromVector(id, vector.Value, vector.Descriptor.ComponentUnits, vector.SampleIndex,
                    id == SimulationMetricIds.NextState ? FormulaScope.NextTimestep : FormulaScope.CurrentTimestep);
            else if (id == RuntimeMetricIds.Index) metric = RuntimeMetric.Number(id, sample.Timestep, "1", sample.Timestep);
            else if (id == RuntimeMetricIds.PhysicalTime) metric = RuntimeMetric.Number(id, sample.PhysicalTimeSeconds, "s", sample.Timestep, "Il modello non dichiara tempo fisico τ.");
            else if (id == RuntimeMetricIds.PhysicalStep) metric = RuntimeMetric.Number(id, sample.DiscretizationStepSeconds, "s", sample.Timestep, "Il modello non dichiara passo fisico δ.");
            else if (id == RuntimeMetricIds.Model) metric = RuntimeMetric.Descriptor(id, result.Configuration.Model, sample.IsTerminal ? (int?)null : sample.Timestep);
            else if (id == RuntimeMetricIds.Output)
                metric = RuntimeMetric.FromVector(id, result.Outputs?.Samples[sample.Timestep], result.History.Model.OutputUnits,
                    sample.Timestep, FormulaScope.CurrentTimestep, result.Outputs == null ? "Il modello non fornisce una funzione di output." : "L'output richiede un input; u_T non esiste.");
            else if (id == RuntimeMetricIds.Horizon || id == RuntimeMetricIds.StateCount || id == RuntimeMetricIds.InputCount)
                metric = RuntimeMetric.Number(id, id == RuntimeMetricIds.StateCount ? result.History.States.Count : result.History.Controls.Count,
                    "1", null, scope: FormulaScope.Snapshot);
            else if (id == RuntimeMetricIds.ReferenceState) metric = RuntimeMetric.FromVector(id, result.Configuration.ReferenceState, result.History.Model.StateUnits, sample.Timestep, FormulaScope.CurrentTimestep);
            else if (id == RuntimeMetricIds.StageCost || id == RuntimeMetricIds.TrackingCost || id == RuntimeMetricIds.EffortCost
                || id == RuntimeMetricIds.TerminalCost || id == RuntimeMetricIds.TotalCost || id == RuntimeMetricIds.CumulativeCost)
            {
                var costs = result.Costs;
                double? value = null; int? index = sample.Timestep;
                var scope = id == RuntimeMetricIds.TerminalCost ? FormulaScope.Terminal : FormulaScope.CurrentTimestep;
                if (costs != null)
                {
                    var stage = costs.StageAt(sample.Timestep);
                    if (id == RuntimeMetricIds.StageCost) value = stage?.Total;
                    else if (id == RuntimeMetricIds.TrackingCost) value = stage?.Tracking;
                    else if (id == RuntimeMetricIds.EffortCost) value = stage?.Effort;
                    else if (id == RuntimeMetricIds.CumulativeCost) value = costs.Cumulative[sample.Timestep];
                    else if (id == RuntimeMetricIds.TotalCost) { value = costs.Total; index = null; }
                    else { value = costs.Terminal.Total; index = result.History.Horizon; }
                }
                metric = RuntimeMetric.Number(id, value, costs?.Configuration.CostUnit ?? "1", index,
                    costs == null ? "Nessun modello di costo configurato." : "Al terminale non esistono stage cost ne input u_T.", scope);
            }
            else if (id.StartsWith(RuntimeMetricIds.ParameterPrefix, StringComparison.Ordinal))
            {
                string target = id.Substring(RuntimeMetricIds.ParameterPrefix.Length);
                var parameter = result.Configuration.Model.Parameters.Concat(result.Configuration.Cost?.Parameters ?? Array.Empty<ModelParameterValue>()).FirstOrDefault(p => p.Target == target);
                if (parameter != null) metric = RuntimeMetric.Number(id, parameter.Value, parameter.Unit, sample.Timestep);
            }
            return metric != null;
        }
    }

    public sealed class MetricResolution
    {
        public IExperimentResult Result { get; }
        public string MetricId { get; }
        public int ComponentSelection { get; }
        public RuntimeMetric Value { get; }
        public MetricValidity Validity { get; }
        public string Diagnostic { get; }
        public int? SampleIndex => Value?.SampleIndex;
        internal MetricResolution(IExperimentResult result, string id, int component, RuntimeMetric value, MetricValidity validity, string diagnostic)
        { Result = result; MetricId = id; ComponentSelection = component; Value = value; Validity = validity; Diagnostic = diagnostic; }
    }

    /// <summary>The ONLY typing/component rule. Pure lookup of one immutable result; never evaluates dynamics.</summary>
    public sealed class RuntimeMetricResolver
    {
        private readonly IRuntimeMetricSource source;
        private readonly IReadOnlyList<IExperimentMetricSource> experimentSources;
        public RuntimeMetricResolver(IRuntimeMetricSource source = null, IEnumerable<IExperimentMetricSource> experimentSources = null)
        {
            this.source = source ?? new SessionMetricSource();
            this.experimentSources = Array.AsReadOnly((experimentSources ?? new IExperimentMetricSource[] { new ObjectiveMetricSource() }).ToArray());
            if (this.experimentSources.Any(s => s == null)) throw new ArgumentException("Missing metric source.");
        }
        public MetricResolution Resolve(FormulaTermDefinition term, SimulationResult result, int cursor)
        {
            if (result != null && (cursor < 0 || cursor > result.History.Horizon))
                return new MetricResolution(result, term?.Value?.MetricId, term?.Value?.Component ?? -1, null, MetricValidity.Invalid, "SampleIndex fuori range.");
            return Resolve(term, result?.Select(cursor));
        }
        public MetricResolution Resolve(FormulaTermDefinition term, IExperimentSnapshot snapshot)
        {
            var result = snapshot?.Result;
            var binding = term?.Value;
            if (binding != null && term.Highlight != null && !string.IsNullOrWhiteSpace(term.Highlight.SemanticRole) && term.Highlight.Scope != binding.Scope)
                return new MetricResolution(result, binding.MetricId, binding.Component, null, MetricValidity.Invalid, "Value/highlight scope incompatibili.");
            return Resolve(snapshot, binding?.MetricId, binding?.Kind ?? FormulaValueKind.Scalar,
                binding?.Scope ?? FormulaScope.CurrentTimestep, binding?.Component ?? -1,
                term == null || term.Rows < 0 ? (int?)null : term.Rows,
                term == null || term.Columns < 0 ? (int?)null : term.Columns,
                term == null || term.ComponentUnits.Count == 0 ? null : term.ComponentUnits);
        }
        public MetricResolution Resolve(SimulationResult result, int cursor, string id, FormulaValueKind expected,
            FormulaScope scope = FormulaScope.CurrentTimestep, int component = -1, int? rows = null, int? columns = null,
            IReadOnlyList<string> expectedUnits = null)
        {
            if (result != null && (cursor < 0 || cursor > result.History.Horizon)) return new MetricResolution(result, id, component, null, MetricValidity.Invalid, "SampleIndex fuori range.");
            return Resolve(result?.Select(cursor), id, expected, scope, component, rows, columns, expectedUnits);
        }
        public MetricResolution Resolve(IExperimentSnapshot snapshot, string id, FormulaValueKind expected,
            FormulaScope scope = FormulaScope.Snapshot, int component = -1, int? rows = null, int? columns = null,
            IReadOnlyList<string> expectedUnits = null)
        {
            var result = snapshot?.Result;
            RuntimeMetric metric = null;
            MetricResolution Invalid(string message) => new MetricResolution(result, id, component, metric, MetricValidity.Invalid, message);
            if (result == null) return Invalid("Risultato assente.");
            if (string.IsNullOrWhiteSpace(id)) return new MetricResolution(result, id, component, null, MetricValidity.NotApplicable, "Termine descrittivo senza binding runtime.");
            if (!Enum.IsDefined(typeof(FormulaScope), scope) || scope == FormulaScope.CurrentIteration) return Invalid("Scope non supportato; nessuna iterazione disponibile.");
            var trajectory = snapshot as SessionState;
            if (trajectory == null && scope != FormulaScope.Snapshot) return Invalid("Lo scope richiede una trajectory; usare Snapshot per valori globali statici.");
            // Terminal selects ONCE. Next is intrinsic to state.next. Static providers never receive a fabricated sample.
            if (trajectory != null)
            {
                var sample = scope == FormulaScope.Terminal ? trajectory.Result.History.GetSnapshot(trajectory.Result.History.Horizon) : trajectory.Sample;
                source.TryGet(trajectory.Result, sample, id, out metric);
            }
            if (metric == null) foreach (var provider in experimentSources) if (provider.TryGet(snapshot, id, out metric)) break;
            if (metric == null) return Invalid("Metrica sconosciuta: " + id);
            if (scope != FormulaScope.Terminal && metric.Scope != scope) return Invalid("Scope incompatibile con la metrica " + id);
            if (component < -1) return Invalid("Selezione di componente negativa non valida.");
            if (component >= 0)
            {
                if (metric.Kind != FormulaValueKind.Vector) return Invalid("Una componente richiede una metrica Vector.");
                if (component >= metric.Rows) return Invalid("Componente fuori range: " + component + "; dimensione " + metric.Rows);
                metric = metric.Component(component);
            }
            if (metric.Kind != expected) return Invalid("Expected kind " + expected + ", actual " + metric.Kind + "; nessuna conversione implicita.");
            if ((rows.HasValue && metric.Rows != rows.Value) || (columns.HasValue && metric.Columns != columns.Value)) return Invalid("Forma incompatibile: " + metric.Rows + "×" + metric.Columns);
            if (expectedUnits != null && !metric.Units.SequenceEqual(expectedUnits)) return Invalid("Unità incompatibili con la metrica " + id);
            return new MetricResolution(result, id, component, metric, metric.Validity, metric.Diagnostic);
        }
    }
}
