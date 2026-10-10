using System;
using OptCon.Simulation;

namespace OptCon.Application
{
    /// <summary>Bridges runtime completion to persistence at the outer notification boundary, with lifetime/ownership validation.</summary>
    public sealed class LessonProgressBinding : IDisposable
    {
        private readonly LessonSession lesson;
        private readonly SimulationSession simulation;
        private readonly ProgressionService progress;
        private readonly Func<bool> canCommit;
        private bool disposed;
        public LessonProgressBinding(LessonSession lesson, SimulationSession simulation, ProgressionService progress, Func<bool> canCommit)
        {
            this.lesson = lesson; this.simulation = simulation; this.progress = progress; this.canCommit = canCommit;
            lesson.CompletionValidated += OnCompletion;
        }
        private void OnCompletion(LessonCompletion completion)
        {
            simulation.Notifications.Defer(() =>
            {
                if (!disposed && completion.IsCurrent && !simulation.State.IsClosed && simulation.State.Error == null
                    && simulation.LastPublicationFailure == null && canCommit())
                    using (simulation.Notifications.Enter()) progress.Commit(completion);
            });
        }
        public void Dispose() { if (disposed) return; disposed = true; lesson.CompletionValidated -= OnCompletion; }
    }
}
