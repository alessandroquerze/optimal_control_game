using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OptCon.Content
{
    public enum FormulaLayoutKind { Text, Term, Row, Fraction, Script, Sum, Integral, Matrix }
    [Serializable]
    public sealed class FormulaLayoutNode
    {
        [SerializeField] private string id;
        [SerializeField] private FormulaLayoutKind kind;
        [SerializeField] private string text;
        [SerializeField] private string termId;
        [SerializeField] private string[] children = Array.Empty<string>();
        [SerializeField] private int rows;
        [SerializeField] private int columns;
        public string Id => id;
        public FormulaLayoutKind Kind => kind;
        public string Text => text;
        public string TermId => termId;
        public IReadOnlyList<string> Children => ContentCollections.View(children);
        public int Rows => rows;
        public int Columns => columns;
    }
    /// <summary>Flat serialized tree of display nodes, never a mathematical evaluator or a runtime Matrix value.</summary>
    [Serializable]
    public sealed class FormulaLayout
    {
        [SerializeField] private string rootId;
        [SerializeField] private FormulaLayoutNode[] nodes = Array.Empty<FormulaLayoutNode>();
        public string RootId => rootId;
        public IReadOnlyList<FormulaLayoutNode> Nodes => ContentCollections.View(nodes);
        public bool IsDefined => Nodes.Count != 0;
        public void Validate(IReadOnlyList<FormulaTermDefinition> terms)
        {
            if (!IsDefined) return;
            if (Nodes.Count > 256) throw new ArgumentException("Formula layout exceeds 256 nodes.");
            var map = new Dictionary<string, FormulaLayoutNode>(StringComparer.Ordinal);
            foreach (var node in Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || map.ContainsKey(node.Id)) throw new ArgumentException("Layout node IDs must be present and unique.");
                if (!Enum.IsDefined(typeof(FormulaLayoutKind), node.Kind)) throw new ArgumentException("Unknown layout node kind.");
                map.Add(node.Id, node);
                int arity = node.Kind == FormulaLayoutKind.Fraction ? 2 : node.Kind == FormulaLayoutKind.Script || node.Kind == FormulaLayoutKind.Sum || node.Kind == FormulaLayoutKind.Integral ? 3 : -1;
                if (arity >= 0 && node.Children.Count != arity) throw new ArgumentException("Layout arity mismatch: " + node.Id);
                if ((node.Kind == FormulaLayoutKind.Text || node.Kind == FormulaLayoutKind.Term) && node.Children.Count != 0) throw new ArgumentException("Leaf layout node has children.");
                if (node.Kind == FormulaLayoutKind.Term && !terms.Any(t => t != null && t.Id == node.TermId)) throw new ArgumentException("Unknown layout term: " + node.TermId);
                if (node.Kind == FormulaLayoutKind.Matrix && (node.Rows < 1 || node.Columns < 1 || (long)node.Rows * node.Columns != node.Children.Count)) throw new ArgumentException("Matrix layout shape mismatch: " + node.Id);
            }
            var active = new HashSet<string>(); var seen = new HashSet<string>();
            void Visit(string id, int depth)
            {
                if (id == null || !map.TryGetValue(id, out var node)) throw new ArgumentException("Missing layout node: " + id);
                if (depth > 16 || !active.Add(id)) throw new ArgumentException("Layout cycle or excessive depth.");
                seen.Add(id); foreach (string child in node.Children) Visit(child, depth + 1); active.Remove(id);
            }
            Visit(rootId, 0);
            if (seen.Count != map.Count) throw new ArgumentException("Unreachable formula layout nodes.");
        }
    }
}
