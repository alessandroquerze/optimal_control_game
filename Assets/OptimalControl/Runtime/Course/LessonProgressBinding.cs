using System;
using OptCon.Simulation;

namespace OptCon.Application
{
    /// <summary>Bridges runtime completion to persistence at the outer notification boundary, with lifetime/ownership validation.</summary>
    public sealed class LessonProgressBinding : IDisposable
    {
        private readonly LessonSession lesson;
        private readonly IExperimentSession simulation;
        private readonly ProgressionService progress;
        private readonly Func<bool> canCommit;
        private bool disposed;
        public LessonProgressBinding(LessonSession lesson, IExperimentSession simulation, ProgressionService progress, Func<bool> canCommit)
        {
            this.lesson = lesson; this.simulation = simulation; this.progress = progress; this.canCommit = canCommit;
            lesson.CompletionValidated += OnCompletion;
        }
        private void OnCompletion(LessonCompletion completion)
        {
            simulation.Notifications.Defer(() =>
            {
                if (!disposed && completion.IsCurrent && !simulation.Current.IsClosed && simulation.Current.Error == null
                    && simulation.LastPublicationFailure == null && canCommit())
                    using (simulation.Notifications.Enter()) progress.Commit(completion);
            });
        }
        public void Dispose() { if (disposed) return; disposed = true; lesson.CompletionValidated -= OnCompletion; }
    }
}
