using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Simulation;

namespace OptCon.Content
{
    /// <summary>Shared access vocabulary, independent of concrete UI elements and lesson IDs.</summary>
    public static class LessonControls
    {
        public const string Play = "play", Step = "step", Seek = "seek", Input = "input", Parameter = "parameter",
            InitialState = "initial-state", PhysicalStep = "physical-step", PlaybackRate = "playback-rate",
            Horizon = "horizon", Comparison = "comparison", Layers = "layers", Formula = "formula",
            InitialPerturbation = "initial-perturbation", InputPerturbation = "input-perturbation";
        public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { Play, Step, Seek, Input, Parameter,
            InitialState, PhysicalStep, PlaybackRate, Horizon, Comparison, Layers, Formula, InitialPerturbation, InputPerturbation });
        public static string For(SimulationCommand command)
        {
            switch (command.Kind)
            {
                case SimulationCommandKind.Play: return Play;
                case SimulationCommandKind.StepForward: case SimulationCommandKind.StepBackward: return Step;
                case SimulationCommandKind.Seek: return Seek;
                case SimulationCommandKind.SetInput: return Input;
                case SimulationCommandKind.SetParameter: return Parameter;
                case SimulationCommandKind.SetInitialState: return InitialState;
                case SimulationCommandKind.SetInitialPerturbation: return InitialPerturbation;
                case SimulationCommandKind.SetInputPerturbation: return InputPerturbation;
                case SimulationCommandKind.SetPhysicalStep: return PhysicalStep;
                case SimulationCommandKind.SetPlaybackRate: return PlaybackRate;
                case SimulationCommandKind.SetHorizon: return Horizon;
                case SimulationCommandKind.CaptureComparison: case SimulationCommandKind.ClearComparison: return Comparison;
                default: return null;
            }
        }
    }

    public sealed class LessonPresentationState
    {
        public string LessonId { get; }
        public LessonStepPlan Step { get; }
        public int StepIndex { get; }
        public int StepCount { get; }
        public long Generation { get; }
        public bool IsFreeExploration { get; }
        public bool IsCompleted { get; }
        public bool CanAdvance { get; }
        public string Feedback { get; }
        public string Error { get; }
        public IReadOnlyList<bool> ConditionsMet { get; }
        public LessonPresentationState(string lessonId, LessonStepPlan step, int index, int count, long generation,
            bool free, bool completed, bool canAdvance, string feedback, IEnumerable<bool> conditions, string error = null)
        {
            LessonId = lessonId; Step = step; StepIndex = index; StepCount = count; Generation = generation;
            IsFreeExploration = free; IsCompleted = completed; CanAdvance = canAdvance; Feedback = feedback;
            ConditionsMet = Array.AsReadOnly(conditions.ToArray());
            Error = error;
        }
        public bool Allows(string control) => Error == null && (IsFreeExploration || Step == null || Step.EnabledControls.Contains(control));
        public bool AllowsParameter(string target) => Allows(LessonControls.Parameter) &&
            (IsFreeExploration || Step == null || Step.EditableParameters.Contains(target));
        public bool Allows(SimulationCommand command)
        {
            if (command == null) return false;
            // Pause and reset always remain available, including during a failed candidate.
            if (command.Kind == SimulationCommandKind.Pause || command.Kind == SimulationCommandKind.Reset) return true;
            if (command.Kind == SimulationCommandKind.SetParameter) return AllowsParameter(command.Target);
            string control = LessonControls.For(command);
            return control != null && Allows(control);
        }
    }

    /// <summary>Presentation can request orchestration, but never references Application's implementation.</summary>
    public interface ILessonRuntime : ICommandSink
    {
        LessonPresentationState Presentation { get; }
        event Action<LessonPresentationState> Changed;
        bool Next();
        bool Previous();
        bool Restart();
        bool Explore();
        bool Answer(string answerId);
        void RecordTermSelection(string formulaId, string termId);
    }
}
