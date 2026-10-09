using System;
using System.Linq;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Reusable angular-state adapter. Length is read from the RESULT configuration, never a mutable asset.</summary>
    public sealed class AngularStateView : MonoBehaviour
    {
        [SerializeField] private int angleComponent;
        [SerializeField] private string lengthParameterTarget = "model.length";
        [SerializeField] private AngularBodyView current, next;
        public AngularBodyView Current => current;
        public AngularBodyView Next => next;
        public void Render(SessionState state)
        {
            var model = state.Result.Configuration.Model;
            if (angleComponent < 0 || angleComponent >= model.Descriptor.Dimensions.State || model.Descriptor.StateUnits[angleComponent] != "rad")
                throw new ArgumentException("Angular body requires a state component in radians.");
            var length = model.Parameters.SingleOrDefault(p => p.Target == lengthParameterTarget);
            if (length == null || length.Unit != "m" || length.Value <= 0) throw new ArgumentException("Angular body requires a positive length parameter in metres.");
            var sample = state.Sample;
            current.Render(sample.CurrentState[angleComponent], length.Value, sample.RevisionId, sample.Timestep);
            next.Render(sample.IsTerminal ? 0 : sample.NextState[angleComponent], length.Value, sample.RevisionId,
                sample.IsTerminal ? (int?)null : sample.Timestep + 1);
        }
    }
}
