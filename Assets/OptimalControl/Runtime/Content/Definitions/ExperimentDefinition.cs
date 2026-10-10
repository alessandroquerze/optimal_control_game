using System;
using System.Collections.Generic;
using OptCon.Core;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Experiment")]
    public sealed class ExperimentDefinition : ContentDefinition
    {
        [SerializeField, Tooltip("May remain unassigned in an editorial draft. No runtime model is fabricated.")]
        private ModelDefinition model;
        [SerializeField] private GameObject scenarioPrefab;
        [SerializeField] private CostDefinition cost;
        [SerializeField] private int stateDimension;
        [SerializeField] private int controlDimension;
        [SerializeField] private int outputDimension;
        [SerializeField] private string[] stateUnits = Array.Empty<string>();
        [SerializeField] private string[] controlUnits = Array.Empty<string>();
        [SerializeField] private string[] outputUnits = Array.Empty<string>();
        [SerializeField, Tooltip("Has discrete transitions indexed by t; does not imply a physical duration in seconds.")]
        private bool usesDiscreteTime;
        [SerializeField] private DiscreteTimePreset discreteTime = new DiscreteTimePreset();
        [SerializeField] private LessonParameterDefinition[] parameters = Array.Empty<LessonParameterDefinition>();
        [SerializeField] private SourceReference[] sources = Array.Empty<SourceReference>();

        public ModelDefinition Model => model;
        public CostDefinition Cost => cost;
        public GameObject ScenarioPrefab => scenarioPrefab;
        public int StateDimension => stateDimension;
        public int ControlDimension => controlDimension;
        public int OutputDimension => outputDimension;
        public ModelDimensions GetModelDimensions() => new ModelDimensions(stateDimension, controlDimension, outputDimension);
        public IReadOnlyList<string> StateUnits => ContentCollections.View(stateUnits);
        public IReadOnlyList<string> ControlUnits => ContentCollections.View(controlUnits);
        public IReadOnlyList<string> OutputUnits => ContentCollections.View(outputUnits);
        public bool UsesDiscreteTime => usesDiscreteTime;
        public DiscreteTimePreset DiscreteTime => discreteTime;
        public IReadOnlyList<LessonParameterDefinition> Parameters => ContentCollections.View(parameters);
        public IReadOnlyList<SourceReference> Sources => ContentCollections.View(sources);
    }

    /// <summary>Authored discrete transitions with optional physical time; never a playback rate or live state.</summary>
    [Serializable]
    public sealed class DiscreteTimePreset
    {
        [SerializeField] private int horizon;
        // Legacy presets omitted this field and always interpreted stepSeconds as physical time.
        [SerializeField, Tooltip("Whether the discrete transition has a physical step in seconds. Disable for index-only dynamics.")]
        private bool hasPhysicalTimeStep = true;
        [SerializeField] private double stepSeconds;
        [SerializeField] private double[] initialState = Array.Empty<double>();
        [SerializeField] private double[] defaultControl = Array.Empty<double>();
        [SerializeField] private double[] referenceState = Array.Empty<double>();

        public int Horizon => horizon;
        public bool HasPhysicalTimeStep => hasPhysicalTimeStep;
        public double? StepSeconds => hasPhysicalTimeStep ? stepSeconds : (double?)null;
        public IReadOnlyList<double> InitialState => ContentCollections.View(initialState);
        public IReadOnlyList<double> DefaultControl => ContentCollections.View(defaultControl);
        public IReadOnlyList<double> ReferenceState => ContentCollections.View(referenceState);
        public double[] CopyInitialState() => (double[])(initialState ?? Array.Empty<double>()).Clone();
        public double[] CopyDefaultControl() => (double[])(defaultControl ?? Array.Empty<double>()).Clone();
        public double[] CopyReferenceState() => (double[])(referenceState ?? Array.Empty<double>()).Clone();
    }
}
