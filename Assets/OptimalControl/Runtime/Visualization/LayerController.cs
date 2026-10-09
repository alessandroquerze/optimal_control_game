using System;
using UnityEngine;

namespace OptCon.Presentation
{
    [Serializable]
    public sealed class VisualLayer { public string id; public GameObject root; }

    /// <summary>Only visual visibility. No reference to the model or command sink.</summary>
    public sealed class LayerController : MonoBehaviour
    {
        [SerializeField] private VisualLayer[] layers;
        public void Toggle(int index)
        {
            if (index < 0 || index >= layers.Length) throw new ArgumentOutOfRangeException(nameof(index));
            var root = layers[index].root; root.SetActive(!root.activeSelf);
        }
        public bool IsVisible(int index) => layers[index].root.activeSelf;
    }
}
