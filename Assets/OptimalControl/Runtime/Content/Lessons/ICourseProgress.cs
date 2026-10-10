using System;
using System.Collections.Generic;

namespace OptCon.Content
{
    public sealed class LessonProgressStatus
    {
        public bool IsCompleted { get; }
        public bool IsAvailable => true; // Prerequisites guide exploration; they do not lock content.
        public IReadOnlyList<string> MissingPrerequisiteIds { get; }
        public LessonProgressStatus(bool completed, IEnumerable<string> missing)
        { IsCompleted = completed; MissingPrerequisiteIds = Array.AsReadOnly(new List<string>(missing).ToArray()); }
    }
    public interface ICourseProgress
    {
        event Action Changed;
        string Diagnostic { get; }
        LessonProgressStatus GetStatus(string lessonId);
    }
}
