using OptCon.Core;

namespace OptCon.Simulation
{
    public interface ICommandSink { bool Execute(SimulationCommand command); }
    public enum SimulationCommandKind { Play, Pause, StepForward, StepBackward, Seek, Reset, SetInput, SetParameter, SetInitialState, SetPhysicalStep, SetPlaybackRate, SetHorizon, CaptureComparison, ClearComparison, SetInitialPerturbation, SetInputPerturbation }

    /// <summary>Pure commands shared by tests, diagnostics and future UI/input adapters.</summary>
    public sealed class SimulationCommand
    {
        public SimulationCommandKind Kind { get; }
        public int Index { get; }
        public NumericVector Vector { get; }
        public string Target { get; }
        public double Value { get; }
        public double? PhysicalStep { get; }
        public InputEditScope Scope { get; }
        private SimulationCommand(SimulationCommandKind kind, int index = 0, NumericVector vector = null,
            string target = null, double value = 0, double? step = null, InputEditScope scope = InputEditScope.Single)
        { Kind = kind; Index = index; Vector = vector; Target = target; Value = value; PhysicalStep = step; Scope = scope; }
        public static SimulationCommand SetHorizon(int horizon) => new SimulationCommand(SimulationCommandKind.SetHorizon, index: horizon);
        public static SimulationCommand CaptureComparison() => new SimulationCommand(SimulationCommandKind.CaptureComparison);
        public static SimulationCommand ClearComparison() => new SimulationCommand(SimulationCommandKind.ClearComparison);
        public static SimulationCommand Play() => new SimulationCommand(SimulationCommandKind.Play);
        public static SimulationCommand Pause() => new SimulationCommand(SimulationCommandKind.Pause);
        public static SimulationCommand StepForward() => new SimulationCommand(SimulationCommandKind.StepForward);
        public static SimulationCommand StepBackward() => new SimulationCommand(SimulationCommandKind.StepBackward);
        public static SimulationCommand Seek(int t) => new SimulationCommand(SimulationCommandKind.Seek, index: t);
        public static SimulationCommand Reset() => new SimulationCommand(SimulationCommandKind.Reset);
        public static SimulationCommand SetInput(int t, NumericVector value, InputEditScope scope = InputEditScope.Single) => new SimulationCommand(SimulationCommandKind.SetInput, t, value, scope: scope);
        public static SimulationCommand SetParameter(string target, double value) => new SimulationCommand(SimulationCommandKind.SetParameter, target: target, value: value);
        public static SimulationCommand SetInitialState(NumericVector value) => new SimulationCommand(SimulationCommandKind.SetInitialState, vector: value);
        public static SimulationCommand SetPhysicalStep(double? seconds) => new SimulationCommand(SimulationCommandKind.SetPhysicalStep, step: seconds);
        public static SimulationCommand SetPlaybackRate(double samplesPerSecond) => new SimulationCommand(SimulationCommandKind.SetPlaybackRate, value: samplesPerSecond);
        public static SimulationCommand SetInitialPerturbation(int component, double amplitude) => new SimulationCommand(SimulationCommandKind.SetInitialPerturbation, index: component, value: amplitude);
        public static SimulationCommand SetInputPerturbation(int t, int component, double amplitude) => new SimulationCommand(SimulationCommandKind.SetInputPerturbation, index: t, target: component.ToString(System.Globalization.CultureInfo.InvariantCulture), value: amplitude);
    }
}
