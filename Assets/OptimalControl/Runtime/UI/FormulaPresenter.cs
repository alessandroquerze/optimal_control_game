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
        private readonly HighlightRegistry registry;
        private readonly FormulaBindingResolver resolver = new FormulaBindingResolver();
        private readonly IReadOnlyList<FormulaDefinition> formulas;
        private readonly Dictionary<string, Label> values = new Dictionary<string, Label>();
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        private readonly DropdownField choice;
        private int formulaIndex;
        private string selected;
        public SimulationResult DisplayedResult { get; private set; }
        public int Cursor { get; private set; }
        public IReadOnlyDictionary<string, MetricResolution> Resolutions { get; private set; }
        public FormulaDefinition Formula => formulas[formulaIndex];
        public FormulaPresenter(VisualElement root, IReadOnlyList<FormulaDefinition> formulas, HighlightRegistry registry)
        {
            this.root = root; this.formulas = formulas; this.registry = registry;
            choice = root.Q<DropdownField>("formula-choice");
            choice.choices = formulas.Select(f => f.Title + " · " + f.Id).ToList(); choice.SetValueWithoutNotify(choice.choices[0]);
            choice.RegisterValueChangedCallback(ChangeFormula);
            Build();
        }
        private void ChangeFormula(ChangeEvent<string> evt)
        {
            int index = choice.choices.IndexOf(evt.newValue);
            if (index < 0) { choice.SetValueWithoutNotify(choice.choices[formulaIndex]); return; }
            formulaIndex = index; selected = null; registry?.Clear(); Build();
            if (DisplayedResult != null) Render(DisplayedResult, Cursor);
        }
        private void Build()
        {
            root.Q<Label>("formula-expression").text = Formula.Expression;
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
        public void Render(SimulationResult result, int cursor)
        {
            DisplayedResult = result; Cursor = cursor;
            var resolutions = new Dictionary<string, MetricResolution>();
            foreach (var term in Formula.Terms)
            {
                var resolved = resolver.Resolve(term, result, cursor); resolutions.Add(term.Id, resolved);
                values[term.Id].text = Format(resolved, term.Value?.NumericFormat, false);
                buttons[term.Id].EnableInClassList("unavailable", resolved.Validity != MetricValidity.Available);
            }
            Resolutions = new System.Collections.ObjectModel.ReadOnlyDictionary<string, MetricResolution>(resolutions);
            SelectTerm(selected);
        }
        public void SelectTerm(string id)
        {
            var term = Formula.Terms.FirstOrDefault(t => t.Id == id);
            if (term == null || Resolutions == null) return;
            selected = id; var resolved = Resolutions[id];
            foreach (var button in buttons) button.Value.EnableInClassList("selected", button.Key == id);
            root.Q<Label>("term-detail-title").text = term.Symbol + " · " + term.Label;
            root.Q<Label>("term-detail-meaning").text = term.Meaning;
            root.Q<Label>("term-detail-value").text = Format(resolved, term.Value?.NumericFormat);
            root.Q<Label>("term-detail-status").text = resolved.Validity + " · " +
                (resolved.SampleIndex.HasValue ? "campione @" + resolved.SampleIndex : "nessun campione associato") +
                (resolved.Value == null ? "" : " · " + resolved.Value.Kind + " " + resolved.Value.Rows + "×" + resolved.Value.Columns) +
                (resolved.Diagnostic == null ? "" : "\n" + resolved.Diagnostic);
            string role = term.Highlight?.SemanticRole;
            if (resolved.Validity == MetricValidity.Available && resolved.SampleIndex.HasValue && !string.IsNullOrWhiteSpace(role) && term.Highlight.Scope == term.Value.Scope)
            {
                registry?.Select(role, resolved.SampleIndex, resolved.Result.RevisionId);
                root.Q<Label>("term-detail-world").text = "Legame 3D: " + role +
                    (registry == null ? " · nessun registry nello scenario" : registry.DescribeSelection());
            }
            else { registry?.Clear(); root.Q<Label>("term-detail-world").text = "Nessun elemento 3D applicabile."; }
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
        public void Dispose() { choice.UnregisterValueChangedCallback(ChangeFormula); registry?.Clear(); root.Q<VisualElement>("formula-terms").Clear(); DisplayedResult = null; Resolutions = null; }
    }
}
