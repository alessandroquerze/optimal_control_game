using System;
using OptCon.Content;

namespace OptCon.Application
{
    /// <summary>Explicit per-composition providers, shared by runtime consumers and editor validation.</summary>
    public sealed class CourseRuntimeServices
    {
        public RuntimeMetricResolver Metrics { get; }
        public LessonConditionRegistry Conditions { get; }
        public CourseRuntimeServices(RuntimeMetricResolver metrics = null, LessonConditionRegistry conditions = null)
        { Metrics = metrics ?? new RuntimeMetricResolver(); Conditions = conditions ?? new LessonConditionRegistry(); }
    }
}
