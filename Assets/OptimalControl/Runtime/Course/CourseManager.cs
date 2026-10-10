using System;
using System.Collections.Generic;
using OptCon.Content;
using OptCon.Simulation;
using UnityEngine.SceneManagement;

namespace OptCon.Application
{
    /// <summary>Explicit composition barrier; knows no chapter or model implementation.</summary>
    public sealed class CourseManager : ICourseNavigation, IDisposable
    {
        private readonly Dictionary<string, LessonDefinition> lessons = new Dictionary<string, LessonDefinition>(StringComparer.Ordinal);
        private readonly LessonLoader loader;
        private readonly string menuScenePath;
        private readonly Func<NavigationTransition, Scene, SimulationSession> compose;
        private readonly Action<NavigationTransition, Scene, SimulationSession> bind;
        private readonly Action release;
        private bool disposed, busy, dispatching;
        private long nextTransition;
        private NavigationTransition pendingTransition;
        public CourseDefinition Course { get; }
        public LessonDefinition ActiveLesson { get; private set; }
        public SimulationSession ActiveSession { get; private set; }
        public NavigationUpdate State { get; private set; }
        public bool IsReady => State != null && State.IsReady && !disposed && !IsTransitioning;
        public bool IsTransitioning => busy || pendingTransition != null;
        public string StatusMessage { get; private set; } = string.Empty;
        public event Action Changed;
        public event Action<NavigationUpdate> LifecycleChanged;

        public CourseManager(CourseDefinition course, LessonLoader loader, string menuScenePath,
            Func<NavigationTransition, Scene, SimulationSession> compose,
            Action<NavigationTransition, Scene, SimulationSession> bind, Action release)
        {
            Course = course != null ? course : throw new ArgumentNullException(nameof(course));
            this.loader = loader != null ? loader : throw new ArgumentNullException(nameof(loader));
            this.menuScenePath = menuScenePath;
            this.compose = compose ?? throw new ArgumentNullException(nameof(compose));
            this.bind = bind ?? throw new ArgumentNullException(nameof(bind));
            this.release = release ?? throw new ArgumentNullException(nameof(release));
            foreach (var chapter in course.Chapters)
            {
                if (chapter == null) throw new ArgumentException("Course contains a missing chapter.", nameof(course));
                foreach (var lesson in chapter.Lessons)
                {
                    if (lesson == null || string.IsNullOrWhiteSpace(lesson.Id)) throw new ArgumentException("Course contains a missing lesson/ID.", nameof(course));
                    if (lessons.TryGetValue(lesson.Id, out var existing))
                    {
                        if (existing == lesson) continue;
                        throw new ArgumentException("Different lessons share the same ID.", nameof(course));
                    }
                    lessons.Add(lesson.Id, lesson);
                }
            }
        }

        private bool Blocked => disposed || IsTransitioning || dispatching;
        public bool OpenLesson(string lessonId)
        {
            if (Blocked) return false;
            if (lessonId == null || !lessons.TryGetValue(lessonId, out var lesson)) return Reject("La lezione richiesta non è presente nel corso.");
            string path = lesson.Workspace?.AssetPath;
            if (string.Equals(path, menuScenePath, StringComparison.Ordinal)) return Reject("Il workspace della lezione deve essere distinto dal menu.");
            return Navigate(path, lesson);
        }
        public bool ReturnToMenu() => !Blocked && Navigate(menuScenePath, null);

        private bool Navigate(string path, LessonDefinition destination)
        {
            if (!loader.CanNavigate(path, out string rejection)) return Reject(rejection);
            var transition = new NavigationTransition(++nextTransition, destination, path);
            var source = ActiveSession;
            if (source != null && source.Notifications.IsActive)
            {
                // First accepted request wins. Keep its identity; teardown starts at the outermost synchronous boundary.
                pendingTransition = transition;
                source.Notifications.Defer(() =>
                {
                    if (disposed || !ReferenceEquals(pendingTransition, transition)) return;
                    pendingTransition = null;
                    if (ReferenceEquals(ActiveSession, source) && !source.State.IsClosed) BeginNavigate(transition);
                });
                return true;
            }
            return BeginNavigate(transition);
        }
        private bool BeginNavigate(NavigationTransition transition)
        {
            string path = transition.ScenePath;
            string rejection;
            busy = true; StatusMessage = "Caricamento…";
            Publish(transition, NavigationPhase.Requested, default, null);
            release(); ActiveSession = null;
            if (!loader.TryNavigate(path, (success, error, scene) => Complete(transition, scene, success, error), out rejection))
            {
                Complete(transition, loader.CurrentScene, false, rejection);
                return false;
            }
            return true;
        }

        private void Complete(NavigationTransition transition, Scene scene, bool success, string error)
        {
            if (disposed) return;
            if (success)
            {
                Publish(transition, NavigationPhase.SceneLoaded, scene, null);
                Publish(transition, NavigationPhase.Composing, scene, null);
                try
                {
                    ActiveSession = compose(transition, scene);
                    if (transition.Destination != null && ActiveSession == null) throw new InvalidOperationException("Lesson composition did not provide a session.");
                    ActiveLesson = transition.Destination;
                    Publish(transition, NavigationPhase.Binding, scene, ActiveSession);
                    bind(transition, scene, ActiveSession);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is ArithmeticException || exception is RolloutException)
                { success = false; error = exception.Message; }
            }
            if (!success)
            {
                release(); ActiveSession = null;
                ActiveLesson = transition.Destination;
                // Recovery UI is bound without claiming that a session is ready.
                if (scene.IsValid() && scene.isLoaded) bind(transition, scene, null);
            }
            StatusMessage = success ? string.Empty : error;
            busy = false;
            Publish(transition, success ? NavigationPhase.Ready : NavigationPhase.Failed, scene, ActiveSession);
        }

        private void Publish(NavigationTransition transition, NavigationPhase phase, Scene scene, SimulationSession session)
        {
            State = new NavigationUpdate(transition, phase, scene, session);
            Notify(() => { LifecycleChanged?.Invoke(State); Changed?.Invoke(); });
        }
        private void Notify(Action action)
        {
            bool wasDispatching = dispatching; dispatching = true;
            try { action(); } finally { dispatching = wasDispatching; }
        }
        private bool Reject(string message) { StatusMessage = message; Notify(() => Changed?.Invoke()); return false; }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; pendingTransition = null; release(); ActiveSession = null; ActiveLesson = null;
            Changed = null; LifecycleChanged = null;
        }
    }
}
