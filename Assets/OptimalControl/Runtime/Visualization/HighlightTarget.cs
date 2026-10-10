using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Presentation identity, using an already resolved absolute sample index.</summary>
    public sealed class HighlightTarget : MonoBehaviour
    {
        [SerializeField] private string semanticRole;
        [SerializeField] private Renderer[] surfaces;
        public string SemanticRole => semanticRole;
        public long RevisionId { get; private set; }
        public int? SampleIndex { get; private set; }
        public bool IsHighlighted { get; private set; }
        public bool IsGlobalAvailable { get; private set; }
        public void SetGlobal(long revision, bool available) { RevisionId = revision; SampleIndex = null; IsGlobalAvailable = available; }
        public void SetSample(long revision, int? sample) { RevisionId = revision; SampleIndex = sample; IsGlobalAvailable = false; }
        public void Highlight(bool selected)
        {
            IsHighlighted = selected;
            foreach (var surface in surfaces)
            {
                if (surface == null) continue;
                var block = new MaterialPropertyBlock();
                if (selected) block.SetColor("_BaseColor", new Color(1f, 0.9f, 0.25f));
                surface.SetPropertyBlock(block);
            }
        }
    }
}
