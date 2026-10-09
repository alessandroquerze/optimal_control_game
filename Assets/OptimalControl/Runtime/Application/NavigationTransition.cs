using OptCon.Content;
using OptCon.Simulation;
using UnityEngine.SceneManagement;

namespace OptCon.Application
{
    public enum NavigationPhase { Requested, SceneLoaded, Composing, Binding, Ready, Failed }
    /// <summary>Destination captured at request time, including when lessons share a scene.</summary>
    public sealed class NavigationTransition
    {
        public long Id { get; }
        public LessonDefinition Destination { get; }
        public string LessonId { get; }
        public string ScenePath { get; }
        public NavigationTransition(long id, LessonDefinition destination, string scenePath)
        { Id = id; Destination = destination; LessonId = destination != null ? destination.Id : null; ScenePath = scenePath; }
    }
    public sealed class NavigationUpdate
    {
        public NavigationTransition Transition { get; }
        public NavigationPhase Phase { get; }
        public Scene Scene { get; }
        public SimulationSession Session { get; }
        public bool IsReady => Phase == NavigationPhase.Ready;
        internal NavigationUpdate(NavigationTransition transition, NavigationPhase phase, Scene scene, SimulationSession session)
        { Transition = transition; Phase = phase; Scene = scene; Session = session; }
    }
}
