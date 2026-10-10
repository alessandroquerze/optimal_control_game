using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Presentation
{
    /// <summary>Common host binding and readiness evidence; implementations declare their required capability.</summary>
    public interface IExperimentPresentation : IVisualizationStatus
    {
        IExperimentSession BoundExperiment { get; }
        ExperimentCapabilities RequiredCapabilities { get; }
        void BindExperiment(IExperimentSession session, ILessonRuntime lesson, RuntimeMetricResolver metrics);
    }
}
