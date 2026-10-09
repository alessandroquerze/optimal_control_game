using OptCon.Core;
using System;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Draws a supplied display vector. Its value/unit and scale are explicit; it has no dynamics.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class VectorRenderer : MonoBehaviour
    {
        [SerializeField] private float headLength = 0.12f;
        public string Unit { get; private set; }
        public double SceneUnitsPerUnit { get; private set; }
        public void Render(Vector3 origin, Vector3 value, string unit, double sceneUnitsPerUnit)
        {
            if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("A display vector needs an explicit unit.", nameof(unit));
            NumericValidation.RequirePositive(sceneUnitsPerUnit, nameof(sceneUnitsPerUnit));
            var displacement = new Vector3(StateProjection.ToFloat(value.x * sceneUnitsPerUnit),
                StateProjection.ToFloat(value.y * sceneUnitsPerUnit), StateProjection.ToFloat(value.z * sceneUnitsPerUnit));
            Unit = unit; SceneUnitsPerUnit = sceneUnitsPerUnit;
            var line = GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            if (displacement.sqrMagnitude == 0) { line.positionCount = 0; return; }
            var end = origin + displacement;
            var direction = displacement.normalized;
            var side = Vector3.Cross(direction, Mathf.Abs(direction.z) < 0.9f ? Vector3.forward : Vector3.up).normalized;
            float size = Mathf.Min(headLength, displacement.magnitude * 0.3f);
            line.positionCount = 5;
            line.SetPositions(new[] { origin, end, end - direction * size + side * size * 0.5f, end, end - direction * size - side * size * 0.5f });
        }
    }
}
