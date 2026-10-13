using System;
using System.Linq;
using OptCon.Core;

namespace OptCon.Simulation
{
    /// <summary>Physical laws retained by a revision; each grid samples them independently, never by copying indices.</summary>
    public sealed class ContinuousSampling
    {
        public double DurationSeconds => StateReference.DurationSeconds;
        public PhysicalStepProfile StateReference { get; }
        public PhysicalStepProfile InputReference { get; }
        public PhysicalStepProfile ControlLaw { get; }
        public System.Collections.Generic.IReadOnlyList<double> StepChoices { get; }
        public ContinuousSampling(PhysicalStepProfile stateReference, PhysicalStepProfile inputReference, PhysicalStepProfile controlLaw,
            System.Collections.Generic.IEnumerable<double> stepChoices = null)
        {
            StateReference = stateReference ?? throw new ArgumentNullException(nameof(stateReference));
            InputReference = inputReference ?? throw new ArgumentNullException(nameof(inputReference));
            ControlLaw = controlLaw ?? throw new ArgumentNullException(nameof(controlLaw));
            if (InputReference.DurationSeconds != DurationSeconds || ControlLaw.DurationSeconds != DurationSeconds || !InputReference.Units.SequenceEqual(ControlLaw.Units))
                throw new ArgumentException("Physical profiles must share H and input units.");
            var choices = (stepChoices ?? Array.Empty<double>()).ToArray();
            if (choices.Distinct().Count() != choices.Length) throw new ArgumentException("Duplicate physical step choices.");
            foreach (double choice in choices) PhysicalTimeGrid.ForDuration(DurationSeconds, choice);
            StepChoices = Array.AsReadOnly(choices);
        }
        public SampledReference SampleReference(PhysicalTimeGrid grid)
        {
            if (grid.DurationSeconds != DurationSeconds) throw new ArgumentException("Reference/grid duration mismatch.");
            return new SampledReference(Enumerable.Range(0, grid.Transitions + 1).Select(t => StateReference.At(grid.TimeAt(t))).ToArray(),
                Enumerable.Range(0, grid.Transitions).Select(t => InputReference.At(grid.TimeAt(t))).ToArray(), StateReference.Units, InputReference.Units, grid);
        }
        public SimulationConfiguration Compose(string experimentId, ModelConfiguration model, NumericVector initialState,
            ContinuousTrackingCost cost, ConstraintConfiguration constraints = null)
        {
            if (!model.Descriptor.DiscretizationStepSeconds.HasValue) throw new ArgumentException("Continuous sampling requires physical seconds.");
            var grid = PhysicalTimeGrid.ForDuration(DurationSeconds, model.Descriptor.DiscretizationStepSeconds.Value);
            var reference = SampleReference(grid);
            var controls = Enumerable.Range(0, grid.Transitions).Select(t => ControlLaw.At(grid.TimeAt(t))).ToArray();
            return new SimulationConfiguration(experimentId, model, initialState, controls, cost: cost.OnReference(reference), constraints: constraints, continuousSampling: this);
        }
        public ContinuousSampling EditInput(PhysicalTimeGrid grid, int sample, NumericVector value, InputEditScope scope) =>
            new ContinuousSampling(StateReference, InputReference, ControlLaw.WithInterval(grid.TimeAt(sample),
                scope == InputEditScope.Tail ? DurationSeconds : grid.TimeAt(sample + 1), value), StepChoices);
        public void Validate(ModelDescriptor model, System.Collections.Generic.IReadOnlyList<NumericVector> controls, CostConfiguration cost)
        {
            if (!(cost is ContinuousTrackingCost continuous) || !StateReference.Units.SequenceEqual(model.StateUnits) ||
                !ControlLaw.Units.SequenceEqual(model.ControlUnits) || continuous.Grid.DurationSeconds != DurationSeconds || continuous.Grid.Transitions != controls.Count)
                throw new ArgumentException("Continuous sampling, model and functional are incompatible.");
            for (int t = 0; t <= controls.Count; t++)
            {
                double time = continuous.Grid.TimeAt(t);
                if (!continuous.Reference.States[t].SequenceEqual(StateReference.At(time)) ||
                    (t < controls.Count && (!continuous.Reference.Inputs[t].SequenceEqual(InputReference.At(time)) || !controls[t].SequenceEqual(ControlLaw.At(time)))))
                    throw new ArgumentException("Sampled input/reference does not belong to the frozen physical laws/grid.");
            }
        }
    }
}
