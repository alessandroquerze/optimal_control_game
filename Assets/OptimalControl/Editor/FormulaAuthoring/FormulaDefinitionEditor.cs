using System;
using System.Linq;
using OptCon.Content;
using OptCon.Presentation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace OptCon.Editor
{
    [CustomEditor(typeof(FormulaDefinition))]
    public sealed class FormulaDefinitionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement(); InspectorElement.FillDefaultInspector(root, serializedObject, this);
            var preview = new VisualElement(); var detail = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/OptimalControl/UI/LessonHud.uss"); if (sheet != null) preview.styleSheets.Add(sheet);
            void Refresh()
            {
                serializedObject.ApplyModifiedProperties(); preview.Clear(); var definition = (FormulaDefinition)target;
                try
                {
                    definition.Layout?.Validate(definition.Terms);
                    IFormulaView view = new StructuredFormulaView(); preview.Add(view.Element);
                    view.Show(definition, id => { var term = definition.Terms.First(t => t.Id == id); view.Select(id); detail.text = term.Symbol + " · " + term.Label + "\n" + term.Meaning + "\n" + term.Value?.MetricId + " · " + term.Value?.Scope; });
                    if (definition.Layout == null || !definition.Layout.IsDefined) preview.Add(new Label(definition.Expression));
                    detail.text = "Layout valido. Preview editoriale: nessun valore runtime inventato.";
                }
                catch (ArgumentException exception) { detail.text = exception.Message; }
            }
            root.Add(new Button(Refresh) { text = "Verifica layout e aggiorna anteprima" }); root.Add(preview); root.Add(detail); Refresh(); return root;
        }
    }
}
