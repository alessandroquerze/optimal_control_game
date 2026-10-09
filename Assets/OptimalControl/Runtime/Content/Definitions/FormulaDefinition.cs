using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Formula")]
    public sealed class FormulaDefinition : ContentDefinition
    {
        [SerializeField, TextArea] private string expression;
        [SerializeField, TextArea] private string sourceTranscription;
        [SerializeField, TextArea] private string assumptions;
        [SerializeField] private FormulaTermDefinition[] terms = Array.Empty<FormulaTermDefinition>();
        [SerializeField] private SourceReference[] sources = Array.Empty<SourceReference>();

        public string Expression => expression;
        public string SourceTranscription => sourceTranscription;
        public string Assumptions => assumptions;
        public IReadOnlyList<FormulaTermDefinition> Terms => ContentCollections.View(terms);
        public IReadOnlyList<SourceReference> Sources => ContentCollections.View(sources);
    }

    [Serializable]
    public sealed class FormulaTermDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string symbol;
        [SerializeField] private string label;
        [SerializeField, TextArea] private string meaning;
        [SerializeField] private int rows = 1;
        [SerializeField] private int columns = 1;
        [SerializeField] private string[] componentUnits = Array.Empty<string>();
        [SerializeField] private ValueBinding value = new ValueBinding();
        [SerializeField] private HighlightBinding highlight = new HighlightBinding();

        public string Id => id;
        public string Symbol => symbol;
        public string Label => label;
        public string Meaning => meaning;
        public int Rows => rows;
        public int Columns => columns;
        public IReadOnlyList<string> ComponentUnits => ContentCollections.View(componentUnits);
        public ValueBinding Value => value;
        public HighlightBinding Highlight => highlight;
    }
}
