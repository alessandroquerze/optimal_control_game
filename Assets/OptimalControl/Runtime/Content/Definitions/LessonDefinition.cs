using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Lesson")]
    public sealed class LessonDefinition : ContentDefinition
    {
        [SerializeField, TextArea] private string objective;
        [SerializeField] private string[] prerequisiteLessonIds = Array.Empty<string>();
        [SerializeField] private SceneReference workspace = new SceneReference();
        [SerializeField] private ExperimentDefinition experiment;
        [SerializeField] private FormulaDefinition[] formulas = Array.Empty<FormulaDefinition>();
        [SerializeField] private SourceReference[] sources = Array.Empty<SourceReference>();
        [SerializeField] private LessonStepDefinition[] steps = Array.Empty<LessonStepDefinition>();
        [SerializeField, TextArea] private string completionCriteria;

        public string Objective => objective;
        public IReadOnlyList<string> PrerequisiteLessonIds => ContentCollections.View(prerequisiteLessonIds);
        public SceneReference Workspace => workspace;
        public ExperimentDefinition Experiment => experiment;
        public IReadOnlyList<FormulaDefinition> Formulas => ContentCollections.View(formulas);
        public IReadOnlyList<SourceReference> Sources => ContentCollections.View(sources);
        public IReadOnlyList<LessonStepDefinition> Steps => ContentCollections.View(steps);
        public string CompletionCriteria => completionCriteria;
    }

    /// <summary>Serialized runtime metadata. Editor validation owns resolution to a scene asset.</summary>
    [Serializable]
    public sealed class SceneReference
    {
        [SerializeField] private string assetPath;
        public string AssetPath => assetPath;
    }
}
