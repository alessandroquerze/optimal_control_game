using System;
using System.Collections.Generic;
using OptCon.Content;

namespace OptCon.Application
{
    /// <summary>Owns the active lesson identity; navigates catalog data without creating experiments.</summary>
    public sealed class CourseManager : ICourseNavigation, IDisposable
    {
        private readonly Dictionary<string, LessonDefinition> lessons = new Dictionary<string, LessonDefinition>(StringComparer.Ordinal);
        private readonly LessonLoader loader;
        private readonly string menuScenePath;
        private bool disposed;

        public CourseDefinition Course { get; }
        public LessonDefinition ActiveLesson { get; private set; }
        public bool IsTransitioning => loader != null && loader.IsLoading;
        public string StatusMessage { get; private set; } = string.Empty;
        public event Action Changed;

        public CourseManager(CourseDefinition course, LessonLoader loader, string menuScenePath)
        {
            Course = course != null ? course : throw new ArgumentNullException(nameof(course));
            this.loader = loader != null ? loader : throw new ArgumentNullException(nameof(loader));
            this.menuScenePath = menuScenePath;
            foreach (var chapter in course.Chapters)
            {
                if (chapter == null) throw new ArgumentException("Course contains a missing chapter.", nameof(course));
                foreach (var lesson in chapter.Lessons)
                {
                    if (lesson == null || string.IsNullOrWhiteSpace(lesson.Id))
                        throw new ArgumentException("Course contains a missing lesson or a duplicate/empty lesson ID.", nameof(course));
                    if (lessons.TryGetValue(lesson.Id, out var existing))
                    {
                        if (existing == lesson) continue;
                        throw new ArgumentException("Different lessons share the same ID.", nameof(course));
                    }
                    lessons.Add(lesson.Id, lesson);
                }
            }
        }

        public bool OpenLesson(string lessonId)
        {
            if (disposed || IsTransitioning) return false;
            if (lessonId == null || !lessons.TryGetValue(lessonId, out var lesson))
                return Reject("La lezione richiesta non è presente nel corso.");
            string path = lesson.Workspace?.AssetPath;
            if (string.Equals(path, menuScenePath, StringComparison.Ordinal))
                return Reject("Il workspace della lezione deve essere distinto dal menu.");
            return Navigate(path, lesson);
        }

        public bool ReturnToMenu() => !disposed && !IsTransitioning && Navigate(menuScenePath, null);

        private bool Navigate(string path, LessonDefinition destination)
        {
            if (!loader.TryNavigate(path, (success, error) =>
            {
                if (disposed) return;
                if (success) ActiveLesson = destination;
                StatusMessage = success ? string.Empty : error;
                Changed?.Invoke();
            }, out string rejection)) return Reject(rejection);
            StatusMessage = IsTransitioning ? "Caricamento…" : StatusMessage;
            Changed?.Invoke();
            return true;
        }

        private bool Reject(string message)
        {
            StatusMessage = message;
            Changed?.Invoke();
            return false;
        }

        public void Dispose()
        {
            disposed = true;
            Changed = null;
        }
    }
}
