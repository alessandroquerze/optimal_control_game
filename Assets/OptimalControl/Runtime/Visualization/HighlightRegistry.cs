using UnityEngine;

namespace OptCon.Presentation
{
    public sealed class HighlightRegistry : MonoBehaviour
    {
        private HighlightTarget[] targets;
        private string role;
        private int? sample;
        private long? revision;
        public void Select(string semanticRole, int? resolvedSampleIndex)
        {
            role = semanticRole; sample = resolvedSampleIndex; revision = null; Refresh();
        }
        public void Select(string semanticRole, int? resolvedSampleIndex, long revisionId)
        { role = semanticRole; sample = resolvedSampleIndex; revision = revisionId; Refresh(); }
        public string DescribeSelection()
        {
            int matches = 0;
            if (targets != null) foreach (var target in targets) if (target.IsHighlighted) matches++;
            return matches == 0 ? " · target non presente" : " · " + matches + " rappresentazioni; rispetta la visibilità dei layer 1/2/3";
        }
        public void Refresh()
        {
            if (targets == null) targets = GetComponentsInChildren<HighlightTarget>(true);
            foreach (var target in targets)
                target.Highlight(role != null && target.SemanticRole == role && target.SampleIndex.HasValue &&
                    (!sample.HasValue || target.SampleIndex == sample) && (!revision.HasValue || target.RevisionId == revision));
        }
        public void Clear() { role = null; sample = null; revision = null; Refresh(); }
    }
}
