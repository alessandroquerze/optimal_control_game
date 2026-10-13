using System;
using System.Collections.Generic;
using System.Linq;

namespace OptCon.Core
{
    /// <summary>A component interval: legacy symmetric g=|v|-limit, or g=max(min-v,v-max). Diagnostic only.</summary>
    public sealed class ComponentBound
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsState { get; }
        public int Component { get; }
        public double Limit { get; }
        public bool IsInterval { get; }
        public double Minimum { get; }
        public double Maximum => Limit;
        public string Unit { get; }
        public ComponentBound(string id, string name, bool isState, int component, double limit, string unit)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit) || component < 0) throw new ArgumentException("Bound metadata is invalid.");
            NumericValidation.RequirePositive(limit, nameof(limit));
            Id = id; Name = name; IsState = isState; Component = component; Limit = limit; Minimum = -limit; Unit = unit;
        }
        public double Evaluate(NumericVector state, NumericVector input)
        { double v = (IsState ? state : input)[Component]; double g = IsInterval ? Math.Max(Minimum-v,v-Maximum) : Math.Abs(v)-Limit; NumericValidation.RequireFinite(g, "path residual"); return g; }
        public ComponentBound(string id, string name, bool isState, int component, double minimum, double maximum, string unit)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit) || component < 0) throw new ArgumentException("Bound metadata is invalid.");
            NumericValidation.RequireFinite(minimum, nameof(minimum)); NumericValidation.RequireFinite(maximum, nameof(maximum));
            if(minimum>=maximum) throw new ArgumentException("Interval requires minimum < maximum.");
            Id=id; Name=name; IsState=isState; Component=component; Minimum=minimum; Limit=maximum; Unit=unit; IsInterval=true;
        }
        public ComponentBound WithLimit(double value) => IsInterval ? new ComponentBound(Id,Name,IsState,Component,Minimum,value,Unit) : new ComponentBound(Id, Name, IsState, Component, value, Unit);
        public ComponentBound WithMinimum(double value) => IsInterval ? new ComponentBound(Id,Name,IsState,Component,value,Maximum,Unit) : throw new ArgumentException("Symmetric bound has no independent minimum.");
    }
    /// <summary>r(x0,xT)=xT[i]-x0[i]-displacement=0. Separate from desired curves and cost weights.</summary>
    public sealed class EndpointConstraint
    {
        public string Id { get; }
        public string Name { get; }
        public int Component { get; }
        public double Displacement { get; }
        public string Unit { get; }
        public EndpointConstraint(string id, string name, int component, double displacement, string unit)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit) || component < 0) throw new ArgumentException("Endpoint metadata is invalid.");
            NumericValidation.RequireFinite(displacement, nameof(displacement));
            Id = id; Name = name; Component = component; Displacement = displacement; Unit = unit;
        }
        public double Evaluate(NumericVector initial, NumericVector terminal)
        { double r = terminal[Component] - initial[Component] - Displacement; NumericValidation.RequireFinite(r, "endpoint residual"); return r; }
    }
    public sealed class ConstraintConfiguration
    {
        public IReadOnlyList<ComponentBound> Path { get; }
        public IReadOnlyList<EndpointConstraint> Endpoints { get; }
        public IReadOnlyList<ModelParameterValue> Parameters { get; }
        public double Tolerance { get; }
        public ConstraintConfiguration(IEnumerable<ComponentBound> path, IEnumerable<EndpointConstraint> endpoints, double tolerance = 1e-9)
        {
            var bounds = path?.ToArray() ?? throw new ArgumentNullException(nameof(path));
            var ends = endpoints?.ToArray() ?? throw new ArgumentNullException(nameof(endpoints));
            if (bounds.Any(b => b == null) || ends.Any(e => e == null)) throw new ArgumentException("Null constraint.");
            var ids = bounds.Select(b => b.Id).Concat(ends.Select(e => e.Id)).ToArray();
            if (ids.Distinct().Count() != ids.Length) throw new ArgumentException("Constraint IDs must be unique.");
            NumericValidation.RequireFinite(tolerance, nameof(tolerance)); if (tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
            Path = Array.AsReadOnly(bounds); Endpoints = Array.AsReadOnly(ends); Tolerance = tolerance;
            Parameters = Array.AsReadOnly(bounds.SelectMany(b => b.IsInterval ? new[] { new ModelParameterValue("constraint." + b.Id + ".limit", b.Limit, b.Unit), new ModelParameterValue("constraint." + b.Id + ".minimum", b.Minimum, b.Unit) } : new[] { new ModelParameterValue("constraint." + b.Id + ".limit", b.Limit, b.Unit) }).ToArray());
        }
        public void Validate(ModelDescriptor model)
        {
            foreach (var b in Path)
            {
                var units = b.IsState ? model.StateUnits : model.ControlUnits;
                if (b.Component >= units.Count || units[b.Component] != b.Unit) throw new ArgumentException("Path constraint component/unit differs from model.");
            }
            foreach (var e in Endpoints) if (e.Component >= model.StateUnits.Count || model.StateUnits[e.Component] != e.Unit) throw new ArgumentException("Endpoint component/unit differs from model.");
        }
        public ConstraintConfiguration WithParameter(string target, double value)
        {
            if (!Parameters.Any(p => p.Target == target)) throw new ArgumentException("Unknown constraint parameter: " + target);
            return new ConstraintConfiguration(Path.Select(b => "constraint." + b.Id + ".limit" == target ? b.WithLimit(value) : "constraint." + b.Id + ".minimum" == target ? b.WithMinimum(value) : b), Endpoints, Tolerance);
        }
    }
}
