using System;
using System.Linq;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    [Serializable]
    public sealed class ComponentBoundDefinition
    {
        [SerializeField] private string id, label, unit;
        [SerializeField] private bool isState;
        [SerializeField] private int component;
        [SerializeField] private double limit;
        [SerializeField] private bool isInterval;
        [SerializeField] private double minimum;
        public ComponentBound Freeze() => isInterval ? new ComponentBound(id,label,isState,component,minimum,limit,unit) : new ComponentBound(id, label, isState, component, limit, unit);
    }
    [Serializable]
    public sealed class EndpointConstraintDefinition
    {
        [SerializeField] private string id, label, unit;
        [SerializeField] private int component;
        [SerializeField] private double displacement;
        public EndpointConstraint Freeze() => new EndpointConstraint(id, label, component, displacement, unit);
    }
    [CreateAssetMenu(menuName = "Optimal Control/Content/Constraints")]
    public sealed class ConstraintDefinition : ContentDefinition
    {
        [SerializeField] private ComponentBoundDefinition[] path = Array.Empty<ComponentBoundDefinition>();
        [SerializeField] private EndpointConstraintDefinition[] endpoints = Array.Empty<EndpointConstraintDefinition>();
        [SerializeField] private double tolerance = 1e-9;
        public ConstraintConfiguration Freeze() => new ConstraintConfiguration(path.Select(p => p?.Freeze()), endpoints.Select(e => e?.Freeze()), tolerance);
    }
}
