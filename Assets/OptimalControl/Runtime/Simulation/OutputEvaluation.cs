using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Outputs evaluated once during result construction, with that result's frozen model and history.</summary>
    public sealed class OutputEvaluation
    {
        public IOutputFunction Function { get; }
        public TrajectoryHistory History { get; }
        public IReadOnlyList<NumericVector> Samples { get; }
        internal OutputEvaluation(IOutputFunction function, TrajectoryHistory history)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            History = history ?? throw new ArgumentNullException(nameof(history));
            if (function.Dimension != history.Model.Dimensions.Output || function.Dimension <= 0)
                throw new ArgumentException("Output function dimension differs from model metadata.");
            var values = new NumericVector[history.Horizon + 1];
            for (int t = 0; t < values.Length; t++)
            {
                if (t == history.Horizon && function.RequiresControl) continue;
                try
                {
                    values[t] = function.Evaluate(t, history.States[t], t < history.Horizon ? history.Controls[t] : null);
                    NumericValidation.RequireDimension(values[t], function.Dimension, "output result");
                }
                catch (Exception exception) when (exception is ArithmeticException || exception is ArgumentException || exception is InvalidOperationException)
                { throw new RolloutException(t, "Output evaluation: " + exception.Message, exception); }
            }
            Samples = Array.AsReadOnly(values);
        }
    }
}
