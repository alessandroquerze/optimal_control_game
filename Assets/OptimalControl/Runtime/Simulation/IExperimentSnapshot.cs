using System;
using System.Collections.Generic;
using OptCon.Core;

namespace OptCon.Simulation
{
    [Flags]
    public enum ExperimentCapabilities { None = 0, Parameters = 1, Trajectory = 2, Objective = 4 }

    /// <summary>Pure frozen inputs. Domain-specific configuration stays on its concrete capability.</summary>
    public interface IExperimentConfiguration
    {
        string ExperimentId { get; }
        IReadOnlyList<ModelParameterValue> Parameters { get; }
    }
    public interface IExperimentResult
    {
        long RevisionId { get; }
        IExperimentConfiguration Configuration { get; }
    }
    /// <summary>One immutable publication. Time/trajectory are deliberately absent from this contract.</summary>
    public interface IExperimentSnapshot
    {
        IExperimentResult Result { get; }
        ExperimentCapabilities Capabilities { get; }
        long AttemptRevisionId { get; }
        string Error { get; }
        bool IsClosed { get; }
    }
    /// <summary>Shared lifetime, publication and parameter/reset command boundary; unsupported commands return false.</summary>
    public interface IExperimentSession : ICommandSink, IDisposable
    {
        IExperimentSnapshot Current { get; }
        ExperimentCapabilities Capabilities { get; }
        SessionNotificationScope Notifications { get; }
        SessionPublicationException LastPublicationFailure { get; }
        IDisposable Subscribe(Action<IExperimentSnapshot> observer);
        bool Supports(SimulationCommandKind command);
    }
    public sealed class ExperimentSubscription : IDisposable
    {
        private Action disconnect;
        public ExperimentSubscription(Action disconnect) => this.disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
        public void Dispose() { var action = disconnect; disconnect = null; action?.Invoke(); }
    }
}
