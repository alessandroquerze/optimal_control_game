using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using UnityEngine;

namespace OptCon.Presentation
{
    [Serializable]
    public sealed class VisualLayer
    {
        public string id;
        public GameObject root;
        public GameObject[] additionalRoots = Array.Empty<GameObject>();
        public void SetVisible(bool visible) { if (root != null) root.SetActive(visible); foreach (var item in additionalRoots) if (item != null) item.SetActive(visible); }
    }

    /// <summary>Only visual visibility. No reference to the model or command sink.</summary>
    public sealed class LayerController : MonoBehaviour
    {
        [SerializeField] private VisualLayer[] layers;
        public void Toggle(int index)
        {
            if (index < 0 || index >= layers.Length) throw new ArgumentOutOfRangeException(nameof(index));
            var root = layers[index].root; layers[index].SetVisible(!root.activeSelf);
        }
        public bool IsVisible(int index) => layers[index].root.activeSelf;
        public IReadOnlyList<string> Ids => Array.AsReadOnly(layers.Select(l => l.id).ToArray());
        public bool IsVisible(string id) => layers.First(l => l.id == id).root.activeSelf;
        public void Apply(LessonPresentationState presentation)
        {
            if (presentation == null) return;
            var visible = presentation.IsFreeExploration || presentation.Step == null ? null : presentation.Step.VisibleLayers;
            if (visible != null && visible.Any(id => !layers.Any(l => l.id == id))) throw new ArgumentException("Step requests an unknown visual layer.");
            foreach (var layer in layers) layer.SetVisible(visible == null || visible.Contains(layer.id));
        }
    }
}
