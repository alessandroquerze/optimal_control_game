using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    /// <summary>Data only. Evaluators and session progression belong to Application in F08.</summary>
    [Serializable]
    public sealed class LessonStepDefinition
    {
        [SerializeField] private string id;
        [SerializeField, TextArea] private string instruction;
        [SerializeField] private FormulaDefinition formulaFocus;
        [SerializeField] private string[] enabledControlIds = Array.Empty<string>();
        [SerializeField] private string[] visibleLayerIds = Array.Empty<string>();
        [SerializeField] private LessonConditionDefinition[] conditions = Array.Empty<LessonConditionDefinition>();

        public string Id => id;
        public string Instruction => instruction;
        public FormulaDefinition FormulaFocus => formulaFocus;
        public IReadOnlyList<string> EnabledControlIds => ContentCollections.View(enabledControlIds);
        public IReadOnlyList<string> VisibleLayerIds => ContentCollections.View(visibleLayerIds);
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
}
