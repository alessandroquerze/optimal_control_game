using UnityEngine;

namespace OptCon.Presentation
{
    [RequireComponent(typeof(HighlightTarget))]
    public sealed class StateMarker : MonoBehaviour
    {
        public int? SampleIndex => GetComponent<HighlightTarget>().SampleIndex;
        public long RevisionId => GetComponent<HighlightTarget>().RevisionId;
        public void RenderGlobal(Vector3 point, long revision)
        {
            foreach (var target in GetComponents<HighlightTarget>()) target.SetGlobal(revision, true);
            transform.localPosition = point; gameObject.SetActive(true);
        }
        public void Render(Vector3 point, long revision, int? sample)
        {
            foreach (var target in GetComponents<HighlightTarget>()) target.SetSample(revision, sample);
            gameObject.SetActive(sample.HasValue);
            if (sample.HasValue) transform.localPosition = point;
        }
    }
}
