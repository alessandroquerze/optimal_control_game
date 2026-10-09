using System;

namespace OptCon.Core
{
    /// <summary>Dimensions of a state-space model; no Unity or presentation dependencies.</summary>
    public readonly struct ModelDimensions
    {
        public int State { get; }
        public int Control { get; }
        public int Output { get; }

        public ModelDimensions(int state, int control, int output)
        {
            if (state <= 0) throw new ArgumentOutOfRangeException(nameof(state));
            if (control < 0) throw new ArgumentOutOfRangeException(nameof(control));
            if (output < 0) throw new ArgumentOutOfRangeException(nameof(output));
            State = state;
            Control = control;
            Output = output;
        }
    }
}
