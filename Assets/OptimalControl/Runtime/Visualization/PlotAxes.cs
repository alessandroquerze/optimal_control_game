using UnityEngine;
using OptCon.Core;

namespace OptCon.Presentation
{
    /// <summary>Labels belong to the displayed space, including unit, scale and any omitted components.</summary>
    public sealed class PlotAxes : MonoBehaviour
    {
        [SerializeField] private TextMesh title;
        [SerializeField] private TextMesh xLabel, yLabel, zLabel;
        [SerializeField] private bool compactLabels;
        public void Render(StateProjection projection, ModelDescriptor model = null)
        {
            title.text = projection.Title + "\nCampioni discreti; segmenti come guida";
            if (model != null) title.text += "\n" + projection.OmittedComponents(model);
            if (compactLabels) title.text = title.text.Replace("Componenti dello stato omesse: ", "Omesse: ").Replace(" · proiezione esplicita", "");
            xLabel.text = projection.X.Description; yLabel.text = projection.Y.Description;
            zLabel.text = projection.Z.coordinate == ProjectionCoordinate.Constant ? "" : projection.Z.Description;
        }
    }
}
