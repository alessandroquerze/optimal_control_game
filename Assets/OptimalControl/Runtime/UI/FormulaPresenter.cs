using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Formats resolved values only. No dynamics, mutable model copies or renderer lookups.</summary>
    public sealed class FormulaPresenter : IDisposable
    {
        private readonly VisualElement root;
        private readonly IFormulaView view;
        private readonly HighlightRegistry registry;
        private readonly FormulaBindingResolver resolver;
        private readonly IReadOnlyList<FormulaDefinition> formulas;
        private readonly Dictionary<string, Label> values = new Dictionary<string, Label>();
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        private readonly DropdownField choice;
        private int formulaIndex;
        private string selected;
        private IReadOnlyList<string> disclosedTerms;
        public event Action<string, string> TermSelected;
        public IExperimentSnapshot DisplayedSnapshot { get; private set; }
        public SimulationResult DisplayedResult => DisplayedSnapshot?.Result as SimulationResult;
        public int? Cursor { get; private set; }
        public IReadOnlyDictionary<string, MetricResolution> Resolutions { get; private set; }
        public FormulaDefinition Formula => formulas[formulaIndex];
        public FormulaPresenter(VisualElement root, IReadOnlyList<FormulaDefinition> formulas, HighlightRegistry registry, RuntimeMetricResolver metrics)
        {
            resolver = new FormulaBindingResolver(metrics);
            this.root = root; this.formulas = formulas; this.registry = registry;
            view = new StructuredFormulaView();
            var expression = root.Q<Label>("formula-expression");
            expression.parent.Insert(expression.parent.IndexOf(expression) + 1, view.Element);
            choice = root.Q<DropdownField>("formula-choice");
            choice.choices = formulas.Select(f => f.Title + " · " + f.Id).ToList(); choice.SetValueWithoutNotify(choice.choices[0]);
            choice.RegisterValueChangedCallback(ChangeFormula);
            Build();
        }
        private void ChangeFormula(ChangeEvent<string> evt)
        {
            if (!choice.enabledSelf) { choice.SetValueWithoutNotify(choice.choices[formulaIndex]); return; }
            int index = choice.choices.IndexOf(evt.newValue);
            if (index < 0) { choice.SetValueWithoutNotify(choice.choices[formulaIndex]); return; }
            SetFormula(formulas[index].Id);
        }
        public void SetFormula(string id)
        {
            int index = -1;
            for (int i = 0; i < formulas.Count; i++) if (formulas[i].Id == id) { index = i; break; }
            if (index < 0) throw new ArgumentException("Formula does not belong to the lesson: " + id);
            formulaIndex = index; selected = null; disclosedTerms = null; registry?.Clear(); Build();
            choice.SetValueWithoutNotify(choice.choices[index]);
            if (DisplayedSnapshot != null) Render(DisplayedSnapshot);
        }
        public void SetDisclosedTerms(IReadOnlyList<string> terms)
        {
            disclosedTerms = terms; view.Disclose(terms);
            foreach (var button in buttons) button.Value.style.display = terms == null || terms.Count == 0 || terms.Contains(button.Key) ? DisplayStyle.Flex : DisplayStyle.None;
            if (terms != null && terms.Count > 0 && !terms.Contains(selected)) selected = terms[0];
        }
        private void Build()
        {
            root.Q<Label>("formula-expression").text = Formula.Expression;
            // Same editorial expression beside the guided question; no second binding or calculation.
            var context = root.Q<Label>("lesson-formula-expression");
            if (context != null) context.text = Formula.Expression;
            view.Show(Formula, id => SelectTerm(id));
            root.Q<Label>("formula-expression").style.display = Formula.Layout != null && Formula.Layout.IsDefined ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q<Label>("formula-description").text = Formula.Description;
            root.Q<Label>("formula-assumptions").text = Formula.Assumptions;
            var list = root.Q<VisualElement>("formula-terms"); list.Clear(); values.Clear(); buttons.Clear();
            foreach (var term in Formula.Terms)
            {
                string id = term.Id;
                var button = new Button(() => SelectTerm(id)) { name = "term-" + id, tooltip = term.Meaning };
                button.AddToClassList("term");
                var symbol = new Label(term.Symbol + "  ·  " + term.Label); symbol.AddToClassList("term-name");
                var value = new Label(); value.AddToClassList("term-value"); button.Add(symbol); button.Add(value); list.Add(button);
                values.Add(id, value); buttons.Add(id, button);
            }
            selected = Formula.Terms[0].Id;
        }
        public void Render(SimulationResult result, int cursor) => Render(result.Select(cursor));
        public void Render(IExperimentSnapshot snapshot)
        {
            DisplayedSnapshot = snapshot; Cursor = snapshot is SessionState trajectory ? trajectory.Sample.Timestep : (int?)null;
            var resolutions = new Dictionary<string, MetricResolution>();
            foreach (var term in Formula.Terms)
            {
                var resolved = resolver.Resolve(term, snapshot); resolutions.Add(term.Id, resolved);
                values[term.Id].text = Format(resolved, term.Value?.NumericFormat, false);
                buttons[term.Id].EnableInClassList("unavailable", resolved.Validity != MetricValidity.Available);
            }
            Resolutions = new System.Collections.ObjectModel.ReadOnlyDictionary<string, MetricResolution>(resolutions);
            SelectTerm(selected, false);
        }
        public void SelectTerm(string id, bool notify = true)
        {
            var term = Formula.Terms.FirstOrDefault(t => t.Id == id);
            if (term == null || Resolutions == null || (disclosedTerms != null && disclosedTerms.Count > 0 && !disclosedTerms.Contains(id))) return;
            selected = id; view.Select(id); var resolved = Resolutions[id];
            foreach (var button in buttons) button.Value.EnableInClassList("selected", button.Key == id);
            root.Q<Label>("term-detail-title").text = term.Symbol + " · " + term.Label;
            root.Q<Label>("term-detail-meaning").text = term.Meaning;
            root.Q<Label>("term-detail-value").text = Format(resolved, term.Value?.NumericFormat);
            root.Q<Label>("term-detail-status").text = resolved.Validity + " · " +
                (resolved.SampleIndex.HasValue ? "campione @" + resolved.SampleIndex : "nessun campione associato") +
                (resolved.Value == null ? "" : " · " + resolved.Value.Kind + " " + resolved.Value.Rows + "×" + resolved.Value.Columns) +
                (resolved.Diagnostic == null ? "" : "\n" + resolved.Diagnostic);
            string role = term.Highlight?.SemanticRole;
            if (resolved.Validity == MetricValidity.Available && (resolved.SampleIndex.HasValue || resolved.Value?.Scope == FormulaScope.Snapshot) && !string.IsNullOrWhiteSpace(role) && term.Highlight.Scope == term.Value.Scope)
            {
                registry?.Select(role, resolved.SampleIndex, resolved.Result.RevisionId);
                root.Q<Label>("term-detail-world").text = "Legame 3D: " + role +
                    (registry == null ? " · nessun registry nello scenario" : registry.DescribeSelection());
            }
            else { registry?.Clear(); root.Q<Label>("term-detail-world").text = "Nessun elemento 3D applicabile."; }
            if (notify) TermSelected?.Invoke(Formula.Id, id);
        }
        public static string Format(MetricResolution resolved, string numericFormat = "G6", bool detailed = true)
        {
            if (resolved.Validity != MetricValidity.Available) return "N/A · " + resolved.Validity +
                (detailed || resolved.Validity == MetricValidity.Invalid ? " · " + resolved.Diagnostic : "");
            var metric = resolved.Value;
            string Number(double value) => value.ToString(string.IsNullOrEmpty(numericFormat) ? "G6" : numericFormat, CultureInfo.InvariantCulture);
            try
            {
                switch (metric.Kind)
                {
                    case FormulaValueKind.Scalar: return Number(metric.Scalar.Value) + " " + metric.Units[0];
                    case FormulaValueKind.Vector: return "[" + string.Join("; ", Enumerable.Range(0, metric.Rows).Select(i => Number(metric.Vector[i]) + " " + metric.Units[i])) + "]";
                    case FormulaValueKind.ModelDescriptor:
                        if (!detailed) return metric.Model.Descriptor.Id + " v" + metric.Model.Descriptor.Version;
                        return metric.Model.Descriptor.Id + " v" + metric.Model.Descriptor.Version + "\n" +
                            string.Join(" · ", metric.Model.Parameters.Select(p => p.Target + "=" + Number(p.Value) + " " + p.Unit));
                    default: return "Tipo non supportato dal formatter: " + metric.Kind;
                }
            }
            catch (FormatException) { return "Formato numerico non valido: " + numericFormat; }
        }
        public void Dispose() { choice.UnregisterValueChangedCallback(ChangeFormula); registry?.Clear(); root.Q<VisualElement>("formula-terms").Clear(); view.Dispose(); TermSelected = null; DisplayedSnapshot = null; Resolutions = null; }
    }
}
