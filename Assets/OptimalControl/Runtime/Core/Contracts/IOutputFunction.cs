using System;

namespace OptCon.Core
{
    /// <summary>Pure immutable output map h_t(x,u). A state-only map accepts null control at T.</summary>
    public interface IOutputFunction
    {
        int Dimension { get; }
        bool RequiresControl { get; }
        NumericVector Evaluate(int timestep, NumericVector state, NumericVector control);
    }

    /// <summary>Explicit model choice, never a framework assumption that y equals x.</summary>
    public sealed class StateComponentOutput : IOutputFunction
    {
        private readonly int stateDimension, controlDimension, component;
        public int Dimension => 1;
        public bool RequiresControl => false;
        public StateComponentOutput(int stateDimension, int controlDimension, int component)
        {
            if (stateDimension <= 0 || controlDimension < 0 || component < 0 || component >= stateDimension)
                throw new ArgumentOutOfRangeException(nameof(component));
            this.stateDimension = stateDimension; this.controlDimension = controlDimension; this.component = component;
        }
        public NumericVector Evaluate(int timestep, NumericVector state, NumericVector control)
        {
            if (timestep < 0) throw new ArgumentOutOfRangeException(nameof(timestep));
            NumericValidation.RequireDimension(state, stateDimension, nameof(state));
            if (control != null) NumericValidation.RequireDimension(control, controlDimension, nameof(control));
            return new NumericVector(state[component]);
        }
    }
}
