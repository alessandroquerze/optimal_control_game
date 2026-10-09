using UnityEngine;
using OptCon.Core;

namespace OptCon.Presentation
{
    /// <summary>Geometric pose conversion: angle zero points down; positive angles rotate counterclockwise in XY.</summary>
    [RequireComponent(typeof(HighlightTarget))]
    public sealed class AngularBodyView : MonoBehaviour
    {
        [SerializeField] private Transform rod, body;
        [SerializeField] private double sceneUnitsPerMetre = 1;
        public void Render(double radians, double lengthMetres, long revision, int? sample)
        {
            GetComponent<HighlightTarget>().SetSample(revision, sample);
            gameObject.SetActive(sample.HasValue);
            if (!sample.HasValue) return;
            NumericValidation.RequirePositive(sceneUnitsPerMetre, nameof(sceneUnitsPerMetre));
            float length = StateProjection.ToFloat(lengthMetres * sceneUnitsPerMetre);
            transform.localRotation = Quaternion.AngleAxis(StateProjection.ToFloat(radians * (180d / System.Math.PI)), Vector3.forward);
            rod.localPosition = new Vector3(0, -length / 2, 0);
            rod.localScale = new Vector3(0.035f, length / 2, 0.035f);
            body.localPosition = new Vector3(0, -length, 0);
        }
    }
}
