using System;
using System.Globalization;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    public enum ProjectionCoordinate { StateComponent, PhysicalTime, DiscreteIndex, Constant }

    [Serializable]
    public sealed class ProjectionAxis
    {
        public ProjectionCoordinate coordinate;
        public int component;
        public string label;
        public string unit;
        public double scale = 1;
        public double offset;

        public double Read(TrajectoryHistory history, int sample)
        {
            switch (coordinate)
            {
                case ProjectionCoordinate.StateComponent: return history.States[sample][component];
                case ProjectionCoordinate.PhysicalTime:
                    return history.PhysicalTimeAt(sample) ?? throw new ArgumentException("Projection requires physical time; use an explicit index axis for index-only models.");
                case ProjectionCoordinate.DiscreteIndex: return sample;
                default: return 0;
            }
        }

        public void Validate(ModelDescriptor model)
        {
            NumericValidation.RequirePositive(scale, nameof(scale));
            NumericValidation.RequireFinite(offset, nameof(offset));
            if (!Enum.IsDefined(typeof(ProjectionCoordinate), coordinate)) throw new ArgumentException("Unknown projection coordinate.");
            if (coordinate == ProjectionCoordinate.Constant) return;
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Each projected axis requires a label.");
            string expected;
            if (coordinate == ProjectionCoordinate.StateComponent)
            {
                if (component < 0 || component >= model.Dimensions.State) throw new ArgumentException("Projection state component is outside model dimensions.");
                expected = model.StateUnits[component];
            }
            else if (coordinate == ProjectionCoordinate.PhysicalTime)
            {
                if (!model.DiscretizationStepSeconds.HasValue) throw new ArgumentException("Projection requires physical time.");
                expected = "s";
            }
            else expected = "1";
            if (unit != expected) throw new ArgumentException("Projection axis unit does not match its mathematical source: " + label);
        }

        public string Description => coordinate == ProjectionCoordinate.Constant ? "constant plane" :
            label + " [" + unit + "]; " + scale.ToString("G4", CultureInfo.InvariantCulture) + " unità scena/(" + unit + ")";
    }

    /// <summary>Explicit display projection only. Never evaluates a transition, wraps angles or invents seconds.</summary>
    [CreateAssetMenu(menuName = "Optimal Control/Visualization/State Projection")]
    public sealed class StateProjection : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private ProjectionAxis x = new ProjectionAxis();
        [SerializeField] private ProjectionAxis y = new ProjectionAxis();
        [SerializeField] private ProjectionAxis z = new ProjectionAxis { coordinate = ProjectionCoordinate.Constant };
        public string Title => title;
        public ProjectionAxis X => x;
        public ProjectionAxis Y => y;
        public ProjectionAxis Z => z;
        public void Validate(ModelDescriptor model) { x.Validate(model); y.Validate(model); z.Validate(model); }
        public string OmittedComponents(ModelDescriptor model)
        {
            var axes = new[] { x, y, z };
            var omitted = Enumerable.Range(0, model.Dimensions.State)
                .Where(i => !axes.Any(a => a.coordinate == ProjectionCoordinate.StateComponent && a.component == i));
            var names = omitted.Select(i => "x[" + i + "]").ToArray();
            return "Componenti dello stato omesse: " + (names.Length == 0 ? "nessuna" : string.Join(", ", names));
        }
        public Vector3 Project(TrajectoryHistory history, int sample)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            if (sample < 0 || sample > history.Horizon) throw new ArgumentOutOfRangeException(nameof(sample));
            Validate(history.Model);
            return new Vector3(ToFloat(x.offset + x.scale * x.Read(history, sample)),
                ToFloat(y.offset + y.scale * y.Read(history, sample)), ToFloat(z.offset + z.scale * z.Read(history, sample)));
        }
        public static float ToFloat(double value)
        {
            if (!NumericValidation.IsFinite(value) || value > float.MaxValue || value < -float.MaxValue)
                throw new ArgumentException("Display coordinate is not representable as a finite float.");
            return (float)value;
        }
    }
}
