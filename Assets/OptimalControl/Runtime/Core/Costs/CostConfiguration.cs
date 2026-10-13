using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Core
{
    public interface ICostModel
    {
        CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input);
        CostContribution EvaluateTerminal(NumericVector state);
        CostContribution EvaluateTerminal(int terminalIndex, NumericVector state);
    }

    /// <summary>Immutable objective contract. Weights include the units needed to produce CostUnit.</summary>
    public enum CostSemantics { DiscreteAdditive, ContinuousFunctionalLeftRectangle }
    public abstract class CostConfiguration : ICostModel
    {
        public abstract string Id { get; }
        public abstract string CostUnit { get; }
        public virtual CostSemantics Semantics => CostSemantics.DiscreteAdditive;
        public virtual string RunningUnit => CostUnit;
        public abstract NumericVector ReferenceState { get; }
        public virtual SampledReference Reference => null;
        public virtual NumericVector StateReferenceAt(int sample) => Reference == null ? ReferenceState : Reference.States[sample];
        public abstract IReadOnlyList<ModelParameterValue> Parameters { get; }
        public abstract void Validate(ModelDescriptor model);
        public virtual void Validate(ModelDescriptor model, int horizon) { Validate(model); Reference?.Validate(model, horizon); }
        public abstract CostConfiguration WithParameter(string target, double value);
        public abstract CostContribution EvaluateStage(int timestep, NumericVector state, NumericVector input);
        public abstract CostContribution EvaluateTerminal(NumericVector state);
        public virtual CostContribution EvaluateTerminal(int terminalIndex, NumericVector state) => EvaluateTerminal(state);
    }

    public enum CostRole { Unassigned, Stage, Terminal }
    public sealed class CostTerm
    {
        public string Id { get; }
        public string Name { get; }
        public string Symbol { get; }
        public string Description { get; }
        public string Unit { get; }
        public double Value { get; }
        public CostTerm(string id, string name, string symbol, string description, string unit, double value)
        {
            if (new[] { id, name, symbol, description, unit }.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Cost term metadata is required.");
            NumericValidation.RequireFinite(value, nameof(value));
            Id = id; Name = name; Symbol = symbol; Description = description; Unit = unit; Value = value;
        }
        public CostTerm WithValue(double value) => new CostTerm(Id, Name, Symbol, Description, Unit, value);
    }

    public sealed class CostContribution
    {
        public IReadOnlyList<CostTerm> Terms { get; }
        public CostRole Role { get; }
        public int? SampleIndex { get; }
        public long? RevisionId { get; }
        // Compatibility accessors for the original preview, not the general contribution contract.
        public double Tracking => Find("tracking")?.Value ?? throw new InvalidOperationException("No tracking contribution.");
        public double Effort => Find("effort")?.Value ?? throw new InvalidOperationException("No effort contribution.");
        public double Total { get; }
        public CostTerm Find(string id) => Terms.FirstOrDefault(t => t.Id == id);
        public CostContribution(double tracking, double effort) : this(new[] {
            new CostTerm("tracking", "Tracking", "ℓˣ", "Deviazione dello stato dal riferimento", "1", tracking),
            new CostTerm("effort", "Effort", "ℓᵘ", "Deviazione del controllo dal riferimento", "1", effort) }) { }
        public CostContribution(IEnumerable<CostTerm> terms, CostRole role = CostRole.Unassigned, int? sampleIndex = null, long? revisionId = null)
        {
            var copy = terms?.ToArray() ?? throw new ArgumentNullException(nameof(terms));
            if (copy.Length == 0 || copy.Any(t => t == null) || copy.Select(t => t.Id).Distinct().Count() != copy.Length)
                throw new ArgumentException("Cost contributions require unique, non-null terms.");
            if (sampleIndex < 0) throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            Terms = Array.AsReadOnly(copy); Role = role; SampleIndex = sampleIndex; RevisionId = revisionId;
            Total = copy.Sum(t => t.Value);
            NumericValidation.RequireFinite(Total, nameof(Total));
        }
        public CostContribution At(CostRole role, int sample, long revision) => new CostContribution(Terms, role, sample, revision);
    }
}
