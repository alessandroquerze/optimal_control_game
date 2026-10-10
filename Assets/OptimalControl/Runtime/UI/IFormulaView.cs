using System;
using System.Collections.Generic;
using OptCon.Content;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Replaceable rendering boundary. Bindings and numeric values remain outside the view.</summary>
    public interface IFormulaView : IDisposable
    {
        VisualElement Element { get; }
        void Show(FormulaDefinition formula, Action<string> selectTerm);
        void Select(string termId);
        void Disclose(IReadOnlyList<string> termIds);
    }
}
