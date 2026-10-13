using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OptCon.Content;
using OptCon.Simulation;
using OptCon.Application;
using OptCon.Presentation;
using UnityEditor;
using UnityEngine;

namespace OptCon.Editor
{
    public sealed class CatalogIssue
    {
        public ContentDefinition Asset { get; }
        public string Code { get; }
        public string Field { get; }
        public string Message { get; }

        internal CatalogIssue(ContentDefinition asset, string code, string field, string message)
        {
            Asset = asset;
            Code = code;
            Field = field;
            Message = message;
        }

        public override string ToString() => $"{Code}: {Asset?.Id ?? "<catalog>"} / {Field}: {Message}";
    }

    public sealed class CatalogValidationResult
    {
        private readonly List<CatalogIssue> issues = new List<CatalogIssue>();
        public IReadOnlyList<CatalogIssue> Issues => issues.AsReadOnly();
        public bool IsValid => issues.Count == 0;
        internal void Add(ContentDefinition asset, string code, string field, string message) =>
            issues.Add(new CatalogIssue(asset, code, field, message));
    }

    /// <summary>Editor catalog checks; execution validation also checks formula bindings against a provider result.</summary>
    public static class CatalogValidator
    {
        public static CatalogValidationResult ValidateCatalog(CourseDefinition course, string projectRoot = null, bool verifySourceFiles = true)
            => ValidateCatalog(course, projectRoot, verifySourceFiles, new CourseRuntimeServices());
        public static CatalogValidationResult ValidateCatalog(CourseDefinition course, string projectRoot, bool verifySourceFiles, CourseRuntimeServices services)
        {
            var result = new CatalogValidationResult();
            if (course == null)
            {
                result.Add(null, "MissingCourse", "course", "Select a CourseDefinition asset.");
                return result;
            }
            new CatalogCheck(course, projectRoot ?? Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                verifySourceFiles, result, services ?? new CourseRuntimeServices()).Run();
            return result;
        }

        /// <summary>
        /// Adds structural prerequisites for execution. A valid catalog alone does not mean that
        /// a mathematical provider, scenario, or future lesson evaluator has been implemented.
        /// Provider-specific mathematical validation is introduced with the actual providers.
        /// </summary>
        public static CatalogValidationResult ValidateExecutionConfiguration(CourseDefinition course, string projectRoot = null, bool verifySourceFiles = true)
            => ValidateExecutionConfiguration(course, projectRoot, verifySourceFiles, new CourseRuntimeServices());
        public static CatalogValidationResult ValidateExecutionConfiguration(CourseDefinition course, string projectRoot, bool verifySourceFiles, CourseRuntimeServices services)
        {
            services = services ?? new CourseRuntimeServices();
            var result = ValidateCatalog(course, projectRoot, verifySourceFiles, services);
            if (course == null) return result;
            foreach (var experiment in course.Chapters.Where(c => c != null)
                .SelectMany(c => c.Lessons).Where(l => l != null)
                .Select(l => l.Experiment).Where(e => e != null).Distinct())
            {
                if (experiment.HasConflictingProviders) result.Add(experiment, "ConflictingProviders", "provider", "Assign only one experiment provider.");
                if (experiment.Provider == null)
                    result.Add(experiment, "MissingModelProvider", "provider", "No concrete experiment provider is assigned.");
                else
                {
                    bool trajectory = (experiment.Provider.Capabilities & ExperimentCapabilities.Trajectory) != 0;
                    if (trajectory != experiment.UsesDiscreteTime)
                        result.Add(experiment, "UnsupportedRuntimeCapability", "usesDiscreteTime", "Declared transitions differ from provider capabilities.");
                    if (experiment.Provider is ModelDefinition dynamics && trajectory)
                    {
                        try
                        {
                            var expected = experiment.GetModelDimensions(); var actual = dynamics.Dimensions;
                            if (actual.State != expected.State || actual.Control != expected.Control || actual.Output != expected.Output)
                                result.Add(experiment, "ProviderDimensions", "provider", "Provider dimensions differ from declared dimensions.");
                            dynamics.CreateConfiguration(experiment);
                        }
                        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
                        { result.Add(experiment, "ProviderConfiguration", "provider", exception.Message); }
                    }
                }
                if (experiment.ScenarioPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(experiment.ScenarioPrefab))
                    result.Add(experiment, "MissingScenarioPrefab", "scenarioPrefab", "A scenario prefab asset is required for execution.");
                else
                {
                    if (experiment.ScenarioPrefab.GetComponent<ExperimentHost>() == null)
                        result.Add(experiment, "MissingScenarioHost", "scenarioPrefab", "Scenario root requires ExperimentHost.");
                    var views = experiment.ScenarioPrefab.GetComponents<MonoBehaviour>().OfType<IExperimentPresentation>().ToArray();
                    if (views.Length != 1) result.Add(experiment, "MissingScenarioPresentation", "scenarioPrefab", "Scenario root requires exactly one experiment presentation.");
                    else if (experiment.Provider != null && (experiment.Provider.Capabilities & views[0].RequiredCapabilities) != views[0].RequiredCapabilities)
                        result.Add(experiment, "PresentationCapability", "scenarioPrefab", "Presentation requires unavailable experiment capabilities.");
                }
            }
            // Validate bindings against an actual immutable provider result, using the same resolver as the HUD.
            foreach (var lesson in course.Chapters.Where(c => c != null).SelectMany(c => c.Lessons).Where(l => l != null).Distinct())
            {
                var experiment = lesson.Experiment;
                if (experiment == null || experiment.Provider == null) continue;
                try
                {
                    using (var session = experiment.Provider.CreateExperimentSession(experiment))
                    {
                        if (session.Capabilities != experiment.Provider.Capabilities) throw new ArgumentException("Session/provider capability mismatch.");
                        BindingValidator.Validate(lesson, session.Current, services.Metrics, result);
                        if (session.Current is OptCon.Simulation.SessionState trajectory)
                        {
                            ValidateEntryDomains(lesson, trajectory.Result.Configuration, result);
                            if (experiment.ScenarioPrefab != null)
                                foreach (var plot in experiment.ScenarioPrefab.GetComponentsInChildren<StateTrajectoryView>(true))
                                {
                                    if (plot.Projection == null) throw new ArgumentException("Trajectory view requires an explicit projection.");
                                    plot.Projection.Validate(trajectory.Result.History.Model);
                                }
                        }
                        else ValidateParameterEntryDomains(lesson, session.Current.Result.Configuration.Parameters, result);
                        var targets = session.Current.Result.Configuration.Parameters.Select(p => p.Target).ToArray();
                        var plan = new LessonPlan(lesson);
                        foreach (var step in plan.Steps)
                        {
                            LessonCapabilityRequirements.Validate(step, session);
                            foreach (var condition in step.Conditions)
                            {
                                services.Conditions.Resolve(condition.EvaluatorId, session.Capabilities);
                                services.Conditions.ValidateBinding(condition, session.Current);
                            }
                        }
                        var layers = experiment.ScenarioPrefab == null ? Array.Empty<string>() : experiment.ScenarioPrefab.GetComponent<LayerController>()?.Ids ?? Array.Empty<string>();
                        foreach (var step in lesson.Steps.Where(s => s != null))
                        {
                            foreach (var target in step.EditableParameterTargets.Where(t => !targets.Contains(t))) result.Add(lesson, "UnknownStepParameter", "steps." + step.Id + ".editableParameterTargets", "Unknown provider target: " + target);
                            foreach (var layer in step.VisibleLayerIds.Where(l => !layers.Contains(l))) result.Add(lesson, "UnknownStepLayer", "steps." + step.Id + ".visibleLayerIds", "Unknown scenario layer: " + layer);
                        }
                    }
                }
                catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is RolloutException || exception is ArithmeticException)
                { result.Add(experiment, "ProviderResult", "model", exception.Message); }
            }
            return result;
        }

        private static void ValidateParameterEntryDomains(LessonDefinition lesson, IReadOnlyList<OptCon.Core.ModelParameterValue> parameters, CatalogValidationResult result)
        {
            foreach (var step in lesson.Steps.Where(s => s != null)) foreach (var action in step.EntryActions.Where(a => a != null))
            {
                var command = action.CreateCommand();
                if (command.Kind != SimulationCommandKind.SetParameter) continue;
                try
                {
                    if (!parameters.Any(p => p.Target == command.Target)) throw new ArgumentException("Unknown provider parameter: " + command.Target);
                    var declared = lesson.Experiment.Parameters.Single(p => p.ModelTarget == command.Target);
                    new ParameterRule(command.Target, declared.Minimum, declared.Maximum, declared.ValueKind != ParameterValueKind.Real).Validate(command.Value);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
                { result.Add(lesson, "LessonEntryDomain", "steps." + step.Id + ".entryActions", exception.Message); }
            }
        }

        private static void ValidateEntryDomains(LessonDefinition lesson, SimulationConfiguration preset, CatalogValidationResult result)
        {
            foreach (var step in lesson.Steps.Where(s => s != null)) foreach (var action in step.EntryActions)
            {
                try
                {
                    if (action == null) throw new ArgumentException("Missing entry action.");
                    var command = action.CreateCommand();
                    switch (command.Kind)
                    {
                        case SimulationCommandKind.Seek:
                            if (command.Index < 0 || command.Index > preset.Horizon) throw new ArgumentException("Seek index is outside preset [0,T].");
                            break;
                        case SimulationCommandKind.SetInput: preset.WithInput(command.Index, command.Vector, command.Scope); break;
                        case SimulationCommandKind.SetInitialState: preset.WithInitialState(command.Vector); break;
                        case SimulationCommandKind.SetParameter:
                            var declared = lesson.Experiment.Parameters.FirstOrDefault(p => p.ModelTarget == command.Target);
                            if (declared != null) new ParameterRule(command.Target, declared.Minimum, declared.Maximum, declared.ValueKind != ParameterValueKind.Real).Validate(command.Value);
                            if (preset.Cost != null && preset.Cost.Parameters.Any(p => p.Target == command.Target)) preset.Cost.WithParameter(command.Target, command.Value);
                            else if (preset.Constraints != null && preset.Constraints.Parameters.Any(p => p.Target == command.Target)) preset.Constraints.WithParameter(command.Target, command.Value);
                            else preset.Model.WithParameter(command.Target, command.Value);
                            break;
                    }
                }
                catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is ArithmeticException)
                { result.Add(lesson, "LessonEntryDomain", "steps." + step.Id + ".entryActions", exception.Message); }
            }
        }

        [MenuItem("Tools/Optimal Control/Validate Selected Course/Catalog")]
        private static void ValidateSelectedCatalog() => Report(ValidateCatalog(Selection.activeObject as CourseDefinition), "Catalog");

        [MenuItem("Tools/Optimal Control/Validate Selected Course/Execution Configuration")]
        private static void ValidateSelectedExecution() => Report(ValidateExecutionConfiguration(Selection.activeObject as CourseDefinition), "Execution configuration");

        private static void Report(CatalogValidationResult result, string label)
        {
            if (result.IsValid) Debug.Log($"{label}: valid. Catalog validation does not certify a runnable lesson.", Selection.activeObject);
            else foreach (var issue in result.Issues) Debug.LogError(issue.ToString(), issue.Asset);
        }

        private sealed class CatalogCheck
        {
            private readonly CourseDefinition course;
            private readonly string root;
            private readonly bool verifyFiles;
            private readonly CatalogValidationResult result;
            private readonly HashSet<ContentDefinition> visited = new HashSet<ContentDefinition>();
            private readonly Dictionary<string, ContentDefinition> identities = new Dictionary<string, ContentDefinition>(StringComparer.Ordinal);
            private readonly Dictionary<string, LessonDefinition> lessons = new Dictionary<string, LessonDefinition>(StringComparer.Ordinal);
            private readonly HashSet<SourceDocumentDefinition> registeredSources;
            private readonly CourseRuntimeServices services;

            internal CatalogCheck(CourseDefinition course, string root, bool verifyFiles, CatalogValidationResult result, CourseRuntimeServices services)
            {
                this.course = course;
                this.root = Path.GetFullPath(root);
                this.verifyFiles = verifyFiles;
                this.result = result;
                this.services = services;
                registeredSources = new HashSet<SourceDocumentDefinition>(course.Sources.Where(s => s != null));
            }

            internal void Run()
            {
                Visit(course);
                foreach (var lesson in lessons.Values)
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var prerequisite in lesson.PrerequisiteLessonIds)
                    {
                        if (!seen.Add(prerequisite ?? string.Empty)) Error(lesson, "DuplicatePrerequisite", "prerequisiteLessonIds", "Repeated prerequisite.");
                        if (string.IsNullOrWhiteSpace(prerequisite) || !lessons.ContainsKey(prerequisite))
                            Error(lesson, "MissingPrerequisite", "prerequisiteLessonIds", $"Unknown lesson ID '{prerequisite}'.");
                    }
                }
                var marks = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var lesson in lessons.Values) CheckPrerequisiteCycle(lesson, marks);
            }

            private void Visit(ContentDefinition asset)
            {
                if (asset == null || !visited.Add(asset)) return;
                if (string.IsNullOrEmpty(asset.Id) || !Regex.IsMatch(asset.Id, @"^[A-Za-z0-9][A-Za-z0-9._-]*$"))
                    Error(asset, "InvalidId", "id", "Provide an explicit stable ID using letters, digits, dots, hyphens, or underscores.");
                else if (identities.ContainsKey(asset.Id)) Error(asset, "DuplicateId", "id", $"ID also used by '{identities[asset.Id].name}'.");
                else identities.Add(asset.Id, asset);
                Required(asset, "title", asset.Title);
                Required(asset, "contentVersion", asset.ContentVersion);
                if (asset.SchemaVersion < 1) Error(asset, "SchemaVersion", "schemaVersion", "Schema version must be positive.");

                if (asset is CourseDefinition catalog)
                {
                    VisitList(catalog, "sources", catalog.Sources, true);
                    VisitList(catalog, "chapters", catalog.Chapters, true);
                }
                else if (asset is ChapterDefinition chapter)
                {
                    Sources(chapter, chapter.Sources);
                    VisitList(chapter, "lessons", chapter.Lessons, true);
                }
                else if (asset is LessonDefinition lesson)
                {
                    if (!string.IsNullOrEmpty(lesson.Id) && !lessons.ContainsKey(lesson.Id)) lessons.Add(lesson.Id, lesson);
                    Required(lesson, "objective", lesson.Objective);
                    Required(lesson, "completionCriteria", lesson.CompletionCriteria);
                    Sources(lesson, lesson.Sources);
                    CheckScene(lesson);
                    if (lesson.Experiment == null) Error(lesson, "MissingExperiment", "experiment", "An editorial experiment definition is required.");
                    else Visit(lesson.Experiment);
                    VisitList(lesson, "formulas", lesson.Formulas, true);
                    Steps(lesson);
                }
                else if (asset is ExperimentDefinition experiment) Experiment(experiment);
                else if (asset is FormulaDefinition formula) Formula(formula);
                else if (asset is SourceDocumentDefinition source) Source(source);
            }

            private void VisitList<T>(ContentDefinition owner, string field, IReadOnlyList<T> items, bool requireAny) where T : ContentDefinition
            {
                if (requireAny && items.Count == 0) Error(owner, "EmptyCollection", field, "At least one entry is required.");
                var seen = new HashSet<T>();
                foreach (var item in items)
                {
                    if (item == null) Error(owner, "MissingReference", field, "An asset reference is missing.");
                    else
                    {
                        if (!seen.Add(item)) Error(owner, "RepeatedReference", field, "The same asset occurs more than once.");
                        Visit(item);
                    }
                }
            }

            private void Sources(ContentDefinition owner, IReadOnlyList<SourceReference> sources)
            {
                if (sources.Count == 0) Error(owner, "MissingSource", "sources", "Provide source provenance for this content.");
                foreach (var reference in sources)
                {
                    if (reference == null || reference.Document == null || !registeredSources.Contains(reference.Document))
                    {
                        Error(owner, "UnknownSource", "sources", "Source must be registered in this course.");
                        continue;
                    }
                    if (reference.FirstPhysicalPage < 1 || reference.LastPhysicalPage < reference.FirstPhysicalPage ||
                        reference.LastPhysicalPage > reference.Document.PhysicalPageCount)
                        Error(owner, "SourcePageRange", "sources", $"Invalid physical page range for '{reference.SourceId}'.");
                    if (reference.FirstSlide < 0 || reference.LastSlide < reference.FirstSlide ||
                        (reference.FirstSlide == 0 && reference.LastSlide != 0))
                        Error(owner, "SourceSlideRange", "sources", "Slide numbers are an independent optional range.");
                    Required(owner, "sources.section", reference.Section);
                    if (!Enum.IsDefined(typeof(SourceUsage), reference.Usage)) Error(owner, "SourceUsage", "sources", "Unknown provenance kind.");
                    if (reference.Usage != SourceUsage.Direct) Required(owner, "sources.note", reference.Note);
                }
            }

            private void Source(SourceDocumentDefinition source)
            {
                Required(source, "author", source.Author);
                Required(source, "numberingNotes", source.NumberingNotes);
                if (!DateTime.TryParseExact(source.DocumentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    Error(source, "SourceDate", "documentDate", "Use yyyy-MM-dd.");
                if (source.PhysicalPageCount < 1) Error(source, "SourcePageCount", "physicalPageCount", "Page count must include the cover.");
                if (string.IsNullOrEmpty(source.Sha256) || !Regex.IsMatch(source.Sha256, "^[A-Fa-f0-9]{64}$"))
                    Error(source, "SourceHash", "sha256", "Expected a SHA-256 digest of the original PDF.");
                try
                {
                    string path = Path.GetFullPath(Path.Combine(root, source.ProjectRelativePath ?? string.Empty));
                    string docsRoot = Path.Combine(root, "Docs") + Path.DirectorySeparatorChar;
                    if (Path.IsPathRooted(source.ProjectRelativePath ?? string.Empty) ||
                        !path.StartsWith(docsRoot, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        Error(source, "SourcePath", "projectRelativePath", "Use a project-relative PDF path inside Docs, including future subfolders.");
                        return;
                    }
                    if (!verifyFiles) return;
                    if (!File.Exists(path)) { Error(source, "MissingSourceFile", "projectRelativePath", "PDF does not exist."); return; }
                    using (var stream = File.OpenRead(path))
                    using (var sha = SHA256.Create())
                    {
                        string actualHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                        if (!string.Equals(actualHash, source.Sha256, StringComparison.OrdinalIgnoreCase))
                            Error(source, "SourceHashMismatch", "sha256", "PDF bytes differ from the registered version.");
                    }
                }
                catch (Exception exception) when (exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException || exception is NotSupportedException)
                {
                    Error(source, "SourcePath", "projectRelativePath", exception.Message);
                }
            }

            private void CheckScene(LessonDefinition lesson)
            {
                string path = lesson.Workspace?.AssetPath;
                if (string.IsNullOrWhiteSpace(path) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    Error(lesson, "MissingWorkspaceScene", "workspace.assetPath", "Reference an existing scene asset.");
                else if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == path))
                    Error(lesson, "WorkspaceNotInBuild", "workspace.assetPath", "Scene must be enabled in Build Settings.");
            }

            private void Experiment(ExperimentDefinition experiment)
            {
                Sources(experiment, experiment.Sources);
                if (experiment.StateDimension < 0 || experiment.ControlDimension < 0 || experiment.OutputDimension < 0 ||
                    (experiment.UsesDiscreteTime && experiment.StateDimension == 0))
                    Error(experiment, "ModelDimensions", "dimensions", "Dimensions must be non-negative, with a positive state dimension for discrete dynamics.");
                Units(experiment, "stateUnits", experiment.StateUnits, experiment.StateDimension);
                Units(experiment, "controlUnits", experiment.ControlUnits, experiment.ControlDimension);
                Units(experiment, "outputUnits", experiment.OutputUnits, experiment.OutputDimension);
                if (experiment.Provider != null) Visit(experiment.Provider);
                if (experiment.Cost != null) Visit(experiment.Cost);
                if (experiment.Cost is QuadraticTrackingCostDefinition quadratic && quadratic.SampledReference != null) Visit(quadratic.SampledReference);
                if (experiment.Constraints != null) Visit(experiment.Constraints);
                if (!experiment.UsesDiscreteTime && experiment.Constraints != null)
                    Error(experiment, "TrajectoryContract", "constraints", "These constraint definitions require a discrete trajectory provider.");
                if (experiment.UsesDiscreteTime)
                {
                    var preset = experiment.DiscreteTime;
                    if (preset == null) Error(experiment, "MissingPreset", "discreteTime", "Discrete dynamics require an initial preset.");
                    else
                    {
                        if (preset.Horizon < 1)
                            Error(experiment, "TimePreset", "discreteTime.horizon", "Use a positive transition count.");
                        double? physicalStep = preset.StepSeconds;
                        if (physicalStep.HasValue && (!Finite(physicalStep.Value) || physicalStep.Value <= 0))
                            Error(experiment, "TimePreset", "discreteTime.stepSeconds", "A declared physical step must be finite and strictly positive in seconds.");
                        Vector(experiment, "discreteTime.initialState", preset.InitialState, experiment.StateDimension);
                        Vector(experiment, "discreteTime.defaultControl", preset.DefaultControl, experiment.ControlDimension);
                        if (preset.ReferenceState.Count > 0) Vector(experiment, "discreteTime.referenceState", preset.ReferenceState, experiment.StateDimension);
                    }
                }
                var parameterIds = new HashSet<string>(StringComparer.Ordinal);
                var parameterTargets = new HashSet<string>(StringComparer.Ordinal);
                foreach (var parameter in experiment.Parameters)
                {
                    if (parameter == null) { Error(experiment, "MissingParameter", "parameters", "Null parameter descriptor."); continue; }
                    LocalId(experiment, "parameters.id", parameter.Id, parameterIds);
                    Required(experiment, "parameters.label", parameter.Label);
                    Required(experiment, "parameters.unit", parameter.Unit);
                    Required(experiment, "parameters.description", parameter.Description);
                    Required(experiment, "parameters.modelTarget", parameter.ModelTarget);
                    if (!parameterTargets.Add(parameter.ModelTarget ?? string.Empty))
                        Error(experiment, "DuplicateParameterTarget", "parameters.modelTarget", "Two parameters would independently override the same target.");
                    if (!Enum.IsDefined(typeof(ParameterValueKind), parameter.ValueKind) || !Enum.IsDefined(typeof(ParameterRecalculation), parameter.Recalculation))
                        Error(experiment, "ParameterKind", "parameters", "Unknown parameter kind or recalculation policy.");
                    if (!Finite(parameter.DefaultValue) || !Finite(parameter.Minimum) || !Finite(parameter.Maximum) || !Finite(parameter.Step) ||
                        parameter.Step <= 0 || parameter.Minimum > parameter.Maximum || parameter.DefaultValue < parameter.Minimum || parameter.DefaultValue > parameter.Maximum)
                        Error(experiment, "ParameterRange", "parameters", "Default, limits and positive step must be finite and consistent.");
                    if (parameter.ValueKind == ParameterValueKind.Integer &&
                        new[] { parameter.DefaultValue, parameter.Minimum, parameter.Maximum, parameter.Step }.Any(v => v != Math.Truncate(v)))
                        Error(experiment, "IntegerParameter", "parameters", "An integer parameter needs integer values, limits and step.");
                    if (parameter.ValueKind == ParameterValueKind.Boolean &&
                        (parameter.Minimum != 0 || parameter.Maximum != 1 || parameter.Step != 1 || (parameter.DefaultValue != 0 && parameter.DefaultValue != 1)))
                        Error(experiment, "BooleanParameter", "parameters", "A Boolean descriptor uses 0/1, limits 0..1 and step 1.");
                }
            }

            private void Formula(FormulaDefinition formula)
            {
                try { formula.Layout?.Validate(formula.Terms); }
                catch (ArgumentException exception) { Error(formula, "FormulaLayout", "layout", exception.Message); }
                Required(formula, "expression", formula.Expression);
                Required(formula, "sourceTranscription", formula.SourceTranscription);
                Required(formula, "assumptions", formula.Assumptions);
                Sources(formula, formula.Sources);
                if (formula.Terms.Count == 0) Error(formula, "EmptyTerms", "terms", "A selectable formula requires terms.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var term in formula.Terms)
                {
                    if (term == null) { Error(formula, "MissingTerm", "terms", "Null term descriptor."); continue; }
                    LocalId(formula, "terms.id", term.Id, ids);
                    Required(formula, "terms.symbol", term.Symbol);
                    Required(formula, "terms.label", term.Label);
                    Required(formula, "terms.meaning", term.Meaning);
                    if (term.Value == null || string.IsNullOrWhiteSpace(term.Value.MetricId)) continue; // Explanatory terms can be unbound.
                    if (!Enum.IsDefined(typeof(FormulaValueKind), term.Value.Kind) || !Enum.IsDefined(typeof(FormulaScope), term.Value.Scope) ||
                        (term.Highlight != null && !Enum.IsDefined(typeof(FormulaScope), term.Highlight.Scope))) Error(formula, "BindingKind", "terms", "Unknown value kind or temporal scope.");
                    if (term.Highlight != null && !string.IsNullOrWhiteSpace(term.Highlight.SemanticRole) && term.Value.Scope != term.Highlight.Scope) Error(formula, "BindingScope", "terms", "Value and highlight must refer to the same temporal target.");
                    if (term.Value.Component < -1) Error(formula, "BindingComponent", "terms", "Use -1 for a whole value or a zero-based component.");
                    if (term.Value.Kind == FormulaValueKind.ModelDescriptor)
                    {
                        if (term.Rows != 0 || term.Columns != 0 || term.ComponentUnits.Count != 0 || term.Value.Component != -1)
                            Error(formula, "DescriptorShape", "terms", "A model descriptor has no numerical shape, component or fabricated units.");
                    }
                    else
                    {
                        bool inferredVector = term.Value.Kind == FormulaValueKind.Vector && term.Rows == -1 && term.Columns == 1 && term.ComponentUnits.Count == 0;
                        if ((!inferredVector && term.Rows < 1) || term.Columns < 1 || (term.Value.Kind == FormulaValueKind.Scalar && (term.Rows != 1 || term.Columns != 1)) ||
                            (term.Value.Kind == FormulaValueKind.Vector && term.Columns != 1)) Error(formula, "TermShape", "terms", "Shape must agree with scalar/vector/matrix kind.");
                        long size = (long)term.Rows * term.Columns;
                        if (!inferredVector) Units(formula, "terms.componentUnits", term.ComponentUnits, size);
                        try { _ = 0.35d.ToString(term.Value.NumericFormat, CultureInfo.InvariantCulture); }
                        catch (FormatException) { Error(formula, "NumericFormat", "terms.value.numericFormat", "Invalid numeric format."); }
                    }
                }
            }

            private void Steps(LessonDefinition lesson)
            {
                try
                {
                    var plan = new LessonPlan(lesson);
                    var registry = services.Conditions;
                    foreach (var step in plan.Steps) foreach (var condition in step.Conditions) registry.Resolve(condition.EvaluatorId);
                }
                catch (ArgumentException exception) { Error(lesson, "LessonPlan", "steps", exception.Message); }
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var step in lesson.Steps)
                {
                    if (step == null) { Error(lesson, "MissingStep", "steps", "Null step descriptor."); continue; }
                    LocalId(lesson, "steps.id", step.Id, ids);
                    Required(lesson, "steps.instruction", step.Instruction);
                    if (step.FormulaFocus != null && !lesson.Formulas.Contains(step.FormulaFocus))
                        Error(lesson, "UnknownFormulaFocus", "steps.formulaFocus", "Focused formula must belong to the lesson.");
                    foreach (var condition in step.Conditions)
                        if (condition == null || string.IsNullOrWhiteSpace(condition.EvaluatorId))
                            Error(lesson, "ConditionDescriptor", "steps.conditions", "A condition needs an evaluator ID; no evaluator is executed by this validator.");
                }
            }

            private void CheckPrerequisiteCycle(LessonDefinition lesson, Dictionary<string, int> marks)
            {
                if (marks.TryGetValue(lesson.Id, out int mark))
                {
                    if (mark == 1) Error(lesson, "PrerequisiteCycle", "prerequisiteLessonIds", "Prerequisites contain a cycle.");
                    return;
                }
                marks[lesson.Id] = 1;
                foreach (var id in lesson.PrerequisiteLessonIds)
                    if (id != null && lessons.TryGetValue(id, out var prerequisite)) CheckPrerequisiteCycle(prerequisite, marks);
                marks[lesson.Id] = 2;
            }

            private void Units(ContentDefinition asset, string field, IReadOnlyList<string> units, long count)
            {
                if (units.Count != count || units.Any(string.IsNullOrWhiteSpace))
                    Error(asset, "Units", field, "Declare one unit per component; use '1' for dimensionless numbers.");
            }

            private void Vector(ContentDefinition asset, string field, IReadOnlyList<double> values, int dimension)
            {
                if (values.Count != dimension || values.Any(v => !Finite(v))) Error(asset, "PresetVector", field, "Preset dimensions and finite values must match the model.");
            }

            private void LocalId(ContentDefinition asset, string field, string id, HashSet<string> ids)
            {
                if (string.IsNullOrWhiteSpace(id)) Error(asset, "InvalidLocalId", field, "Provide a stable local ID.");
                else if (!ids.Add(id)) Error(asset, "DuplicateLocalId", field, $"Repeated ID '{id}'.");
            }

            private void Required(ContentDefinition asset, string field, string value)
            {
                if (string.IsNullOrWhiteSpace(value)) Error(asset, "RequiredField", field, "A non-empty value is required.");
            }

            private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
            private void Error(ContentDefinition asset, string code, string field, string message) => result.Add(asset, code, field, message);
        }
    }
}
