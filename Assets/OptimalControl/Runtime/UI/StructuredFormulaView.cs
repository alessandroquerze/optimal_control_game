using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Small display-tree renderer: rows, fractions, scripts, large operators and matrices. No parser/eval.</summary>
    public sealed class StructuredFormulaView : IFormulaView
    {
        private readonly Dictionary<string, List<Button>> terms = new Dictionary<string, List<Button>>();
        public VisualElement Element { get; } = new ScrollView(ScrollViewMode.Horizontal) { name = "formula-structured" };
        public void Show(FormulaDefinition formula, Action<string> selectTerm)
        {
            Element.Clear(); terms.Clear(); Element.AddToClassList("math-layout");
            if (formula.Layout == null || !formula.Layout.IsDefined) { Element.style.display = DisplayStyle.None; return; }
            formula.Layout.Validate(formula.Terms); Element.style.display = DisplayStyle.Flex;
            var map = formula.Layout.Nodes.ToDictionary(n => n.Id);
            VisualElement Row() { var row = new VisualElement(); row.AddToClassList("math-row"); return row; }
            VisualElement Build(string id)
            {
                var node = map[id]; var box = new VisualElement { name = "math-" + id };
                switch (node.Kind)
                {
                    case FormulaLayoutKind.Text: return new Label(node.Text ?? "") { name = "math-" + id };
                    case FormulaLayoutKind.Term:
                        var term = formula.Terms.First(t => t.Id == node.TermId);
                        var button = new Button(() => selectTerm?.Invoke(term.Id)) { name = "inline-term-" + term.Id, text = string.IsNullOrEmpty(node.Text) ? term.Symbol : node.Text, tooltip = term.Meaning };
                        button.AddToClassList("math-term");
                        if (!terms.TryGetValue(term.Id, out var list)) terms.Add(term.Id, list = new List<Button>());
                        list.Add(button); return button;
                    case FormulaLayoutKind.Row:
                        box.AddToClassList("math-row"); foreach (string child in node.Children) box.Add(Build(child)); return box;
                    case FormulaLayoutKind.Fraction:
                        box.AddToClassList("math-fraction"); var numerator = Build(node.Children[0]); numerator.AddToClassList("math-numerator"); box.Add(numerator); box.Add(Build(node.Children[1])); return box;
                    case FormulaLayoutKind.Script:
                        box.AddToClassList("math-row"); box.Add(Build(node.Children[0]));
                        var scripts = new VisualElement(); scripts.AddToClassList("math-scripts"); scripts.Add(Build(node.Children[2])); scripts.Add(Build(node.Children[1])); box.Add(scripts); return box;
                    case FormulaLayoutKind.Sum: case FormulaLayoutKind.Integral:
                        box.AddToClassList("math-row"); var limits = new VisualElement(); limits.AddToClassList("math-limits");
                        limits.Add(Build(node.Children[1])); var glyph = new Label(node.Kind == FormulaLayoutKind.Sum ? "Σ" : "∫"); glyph.AddToClassList("math-operator"); limits.Add(glyph); limits.Add(Build(node.Children[0]));
                        box.Add(limits); box.Add(Build(node.Children[2])); return box;
                    case FormulaLayoutKind.Matrix:
                        box.AddToClassList("math-row"); box.tooltip = "Matrix layout " + node.Rows + "×" + node.Columns;
                        var grid = new VisualElement(); grid.AddToClassList("math-matrix");
                        for (int i = 0; i < node.Rows; i++) { var row = Row(); for (int j = 0; j < node.Columns; j++) { var cell = Build(node.Children[i * node.Columns + j]); cell.AddToClassList("math-cell"); row.Add(cell); } grid.Add(row); }
                        box.Add(new Label("[")); box.Add(grid); box.Add(new Label("]")); return box;
                    default: throw new ArgumentException("Unsupported formula layout.");
                }
            }
            Element.Add(Build(formula.Layout.RootId));
        }
        public void Select(string termId) { foreach (var pair in terms) foreach (var button in pair.Value) button.EnableInClassList("selected", pair.Key == termId); }
        public void Disclose(IReadOnlyList<string> ids) { foreach (var pair in terms) foreach (var button in pair.Value) button.SetEnabled(ids == null || ids.Count == 0 || ids.Contains(pair.Key)); }
        public void Dispose() { Element.Clear(); Element.RemoveFromHierarchy(); terms.Clear(); }
    }
}
