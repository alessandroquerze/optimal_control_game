using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    public sealed class RolloutException : Exception
    {
        public int Timestep { get; }
        public RolloutException(int timestep, string message, Exception innerException = null)
            : base($"Rollout failed at t={timestep}: {message}", innerException) => Timestep = timestep;
    }

    /// <summary>Computes a complete candidate or throws. No partial history is published on failure.</summary>
    public static class RolloutEngine
    {
        public static TrajectoryHistory Rollout(IDiscreteDynamics model, NumericVector initialState,
            IReadOnlyList<NumericVector> controls, string experimentId, long revisionId = 0)
            => Calculate(model, initialState, controls, experimentId, revisionId, null, 0);

        /// <summary>Reuse a verified prefix for the same immutable model; recompute only the affected suffix.</summary>
        public static TrajectoryHistory RecalculateFrom(TrajectoryHistory previous, IDiscreteDynamics model,
            IReadOnlyList<NumericVector> controls, int firstChangedInput, long revisionId)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            if (model == null || !ReferenceEquals(model.Descriptor, previous.Model)) throw new ArgumentException("Prefix reuse requires the same immutable model descriptor.", nameof(model));
            if (controls == null || controls.Count != previous.Horizon || firstChangedInput < 0 || firstChangedInput > previous.Horizon)
                throw new ArgumentException("Prefix and new controls must have the same horizon and a valid boundary.");
            for (int t = 0; t < firstChangedInput; t++)
            {
                NumericValidation.RequireDimension(controls[t], previous.Model.Dimensions.Control, nameof(controls));
                for (int i = 0; i < controls[t].Count; i++)
                    if (controls[t][i] != previous.Controls[t][i]) throw new ArgumentException("An earlier input changed; it cannot reuse this prefix.");
            }
            return Calculate(model, previous.States[0], controls, previous.ExperimentId, revisionId, previous, firstChangedInput);
        }

        private static TrajectoryHistory Calculate(IDiscreteDynamics model, NumericVector initialState,
            IReadOnlyList<NumericVector> controls, string experimentId, long revisionId, TrajectoryHistory prefix, int start)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            var descriptor = model.Descriptor ?? throw new ArgumentException("Model descriptor is required.", nameof(model));
            NumericValidation.RequireDimension(initialState, descriptor.Dimensions.State, nameof(initialState));
            if (controls == null) throw new ArgumentNullException(nameof(controls));
            int horizon = controls.Count;
            if (horizon <= 0 || horizon == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(controls), "At least one transition and T+1 representable samples are required.");
            if (string.IsNullOrWhiteSpace(experimentId)) throw new ArgumentException("Experiment ID is required.", nameof(experimentId));
            if (revisionId < 0) throw new ArgumentOutOfRangeException(nameof(revisionId));
            if (descriptor.DiscretizationStepSeconds.HasValue && !NumericValidation.IsFinite(horizon * descriptor.DiscretizationStepSeconds.Value))
                throw new ArgumentOutOfRangeException(nameof(controls), "Physical duration overflows double precision.");
            var applied = new NumericVector[horizon];
            for (int t = 0; t < horizon; t++)
            {
                NumericValidation.RequireDimension(controls[t], descriptor.Dimensions.Control, nameof(controls));
                applied[t] = controls[t];
            }
            var states = new NumericVector[horizon + 1];
            states[0] = initialState;
            for (int t = 1; t <= start; t++) states[t] = prefix.States[t];
            for (int t = start; t < horizon; t++)
            {
                try
                {
                    var next = model.Evaluate(t, states[t], applied[t]);
                    NumericValidation.RequireDimension(next, descriptor.Dimensions.State, "model result");
                    states[t + 1] = next;
                }
                catch (Exception exception) when (exception is ArithmeticException || exception is ArgumentException || exception is InvalidOperationException)
                {
                    throw new RolloutException(t, exception.Message, exception);
                }
            }
            return new TrajectoryHistory(experimentId, revisionId, descriptor, states, applied);
        }
    }
}
