using System;

namespace OptCon.Content
{
    /// <summary>Presentation-facing navigation contract. No scene operations or completion tracking.</summary>
    public interface ICourseNavigation
    {
        CourseDefinition Course { get; }
        LessonDefinition ActiveLesson { get; }
        bool IsTransitioning { get; }
        bool IsReady { get; }
        string StatusMessage { get; }
        event Action Changed;
        bool OpenLesson(string lessonId);
        bool ReturnToMenu();
    }
}
