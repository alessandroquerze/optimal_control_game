using System;
using OptCon.Simulation;

namespace OptCon.Presentation
{
    public enum VisualizationAvailability { Available, NonRepresentable, Failed }
    /// <summary>Presentation outcome for one exact session publication; never a mathematical error.</summary>
    public sealed class VisualizationStatus
    {
        public SessionState Source { get; }
        public VisualizationAvailability Availability { get; }
        public string Diagnostic { get; }
        public VisualizationStatus(SessionState source, VisualizationAvailability availability, string diagnostic = null)
        { Source = source; Availability = availability; Diagnostic = diagnostic; }
    }
    public interface IVisualizationStatus
    {
        VisualizationStatus Status { get; }
        event Action<VisualizationStatus> StatusChanged;
    }
    public sealed class NonRepresentableDisplayException : ArgumentException
    { public NonRepresentableDisplayException() : base("Display coordinate is not representable as a finite float.") { } }
}
