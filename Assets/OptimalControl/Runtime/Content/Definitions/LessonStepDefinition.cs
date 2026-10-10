using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    /// <summary>Editorial data only; compiled into an immutable runtime plan when opening a lesson.</summary>
    [Serializable]
    public sealed class LessonStepDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string objective;
        [SerializeField, TextArea] private string instruction;
        [SerializeField, TextArea] private string help;
        [SerializeField] private FormulaDefinition formulaFocus;
        [SerializeField] private string selectedTermId;
        [SerializeField] private string[] visibleTermIds = Array.Empty<string>();
        [SerializeField] private string[] enabledControlIds = Array.Empty<string>();
        [SerializeField] private string[] editableParameterTargets = Array.Empty<string>();
        [SerializeField] private string[] visibleLayerIds = Array.Empty<string>();
        [SerializeField] private bool showCosts;
        [SerializeField] private LessonEntryAction[] entryActions = Array.Empty<LessonEntryAction>();
        [SerializeField, TextArea] private string question;
        [SerializeField] private LessonAnswerDefinition[] answers = Array.Empty<LessonAnswerDefinition>();
        [SerializeField] private LessonConditionDefinition[] conditions = Array.Empty<LessonConditionDefinition>();

        public string Id => id;
        public string Title => title;
        public string Objective => objective;
        public string Instruction => instruction;
        public string Help => help;
        public FormulaDefinition FormulaFocus => formulaFocus;
        public string SelectedTermId => selectedTermId;
        public IReadOnlyList<string> VisibleTermIds => ContentCollections.View(visibleTermIds);
        public IReadOnlyList<string> EnabledControlIds => ContentCollections.View(enabledControlIds);
        public IReadOnlyList<string> EditableParameterTargets => ContentCollections.View(editableParameterTargets);
        public IReadOnlyList<string> VisibleLayerIds => ContentCollections.View(visibleLayerIds);
        public bool ShowCosts => showCosts;
        public IReadOnlyList<LessonEntryAction> EntryActions => ContentCollections.View(entryActions);
        public string Question => question;
        public IReadOnlyList<LessonAnswerDefinition> Answers => ContentCollections.View(answers);
        public IReadOnlyList<LessonConditionDefinition> Conditions => ContentCollections.View(conditions);
    }

    [Serializable]
    public sealed class LessonConditionDefinition
    {
        [SerializeField] private string evaluatorId;
        [SerializeField] private string targetId;
        [SerializeField] private string expectedAnswerId;

        public string EvaluatorId => evaluatorId;
        public string TargetId => targetId;
        public string ExpectedAnswerId => expectedAnswerId;
    }

    [Serializable]
    public sealed class LessonAnswerDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string text;
        [SerializeField, TextArea] private string feedback;
        public string Id => id;
        public string Text => text;
        public string Feedback => feedback;
    }

    [Serializable]
    public sealed class LessonEntryAction
    {
        [SerializeField] private OptCon.Simulation.SimulationCommandKind kind;
        [SerializeField] private int index;
        [SerializeField] private string target;
        [SerializeField] private double value;
        [SerializeField] private double[] vector = Array.Empty<double>();
        [SerializeField] private OptCon.Simulation.InputEditScope scope;
        public OptCon.Simulation.SimulationCommand CreateCommand()
        {
            switch (kind)
            {
                case OptCon.Simulation.SimulationCommandKind.Pause: return OptCon.Simulation.SimulationCommand.Pause();
                case OptCon.Simulation.SimulationCommandKind.Reset: return OptCon.Simulation.SimulationCommand.Reset();
                case OptCon.Simulation.SimulationCommandKind.Seek: return OptCon.Simulation.SimulationCommand.Seek(index);
                case OptCon.Simulation.SimulationCommandKind.SetInput: return OptCon.Simulation.SimulationCommand.SetInput(index, new OptCon.Core.NumericVector(vector), scope);
                case OptCon.Simulation.SimulationCommandKind.SetParameter: return OptCon.Simulation.SimulationCommand.SetParameter(target, value);
                case OptCon.Simulation.SimulationCommandKind.SetInitialState: return OptCon.Simulation.SimulationCommand.SetInitialState(new OptCon.Core.NumericVector(vector));
                case OptCon.Simulation.SimulationCommandKind.ClearComparison: return OptCon.Simulation.SimulationCommand.ClearComparison();
                default: throw new ArgumentException("Unsupported lesson entry command: " + kind);
            }
        }
    }
}
