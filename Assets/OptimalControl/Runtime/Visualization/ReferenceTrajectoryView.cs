using System.Collections.Generic;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Presentation
{
    /// <summary>Desired samples, dashed guides and a diamond marker; never labels this curve a valid rollout.</summary>
    public sealed class ReferenceTrajectoryView : MonoBehaviour
    {
        [SerializeField] private StateProjection projection;
        [SerializeField] private LineRenderer template;
        [SerializeField] private StateMarker current;
        [SerializeField] private TextMesh label;
        private readonly List<LineRenderer> segments = new List<LineRenderer>();
        public SessionState DisplayedState { get; private set; }
        public StateMarker Current => current;
        public void Render(SessionState state)
        {
            DisplayedState = state; var data = state.Result.Reference; int t = state.Sample.Timestep;
            template.positionCount = 0;
            int count = data == null ? 0 : data.Reference.Horizon * 3;
            while (segments.Count < count)
            {
                var go = new GameObject("DesiredGuide"); go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = template.sharedMaterial;
                line.useWorldSpace = false; line.widthMultiplier = template.widthMultiplier; line.numCapVertices = 2;
                segments.Add(line);
            }
            // Fractions are a drawing pattern only, not additional reference samples or interpolation of the model.
            for (int i = 0; i < segments.Count; i++)
            {
                var line = segments[i]; line.positionCount = i < count ? 2 : 0;
                if (i >= count) continue;
                int sample = i / 3; float a = (i % 3) / 3f;
                Vector3 start = projection.ProjectState(state.Result.History, sample, data.Reference.States[sample]);
                Vector3 end = projection.ProjectState(state.Result.History, sample + 1, data.Reference.States[sample + 1]);
                line.SetPosition(0, Vector3.Lerp(start, end, a)); line.SetPosition(1, Vector3.Lerp(start, end, a + .2f));
            }
            current.Render(data == null ? Vector3.zero : projection.ProjectState(state.Result.History, t, data.Reference.States[t]), state.Result.RevisionId, data == null ? (int?)null : t);
            label.text = data == null ? "Riferimento: N/A" : "DESIDERATO: tratteggio + diamante\nREALE: linea continua + sfera\nGuide fra campioni, non integrazione\nLa curva desiderata può violare f\nr" + state.Result.RevisionId + " · t=" + t;
        }
    }
}
