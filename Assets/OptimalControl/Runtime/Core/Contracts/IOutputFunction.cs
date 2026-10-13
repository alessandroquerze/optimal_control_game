using System;

namespace OptCon.Core
{
    /// <summary>Ordered state selection; preserves vector shape.</summary>
    public sealed class StateComponentsOutput : IOutputFunction
    {
        private readonly int stateDimension, controlDimension;
        private readonly int[] components;
        public int Dimension => components.Length;
        public bool RequiresControl => false;
        public StateComponentsOutput(int stateDimension, int controlDimension, params int[] components)
        {
            if(stateDimension<=0 || controlDimension<0 || components==null || components.Length==0) throw new ArgumentException("Output selections required.");
            foreach(int i in components) if(i<0 || i>=stateDimension) throw new ArgumentOutOfRangeException(nameof(components));
            this.stateDimension=stateDimension; this.controlDimension=controlDimension; this.components=(int[])components.Clone();
        }
        public NumericVector Evaluate(int timestep, NumericVector state, NumericVector control)
        {
            if(timestep<0) throw new ArgumentOutOfRangeException(nameof(timestep));
            NumericValidation.RequireDimension(state,stateDimension,nameof(state));
            if(control!=null) NumericValidation.RequireDimension(control,controlDimension,nameof(control));
            var values=new double[Dimension]; for(int i=0;i<Dimension;i++) values[i]=state[components[i]];
            return new NumericVector(values);
        }
    }
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
