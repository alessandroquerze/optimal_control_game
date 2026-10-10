using OptCon.Simulation;

namespace OptCon.Application
{
    /// <summary>A receipt minted only after successful educational completion publication.</summary>
    public sealed class LessonCompletion
    {
        private readonly LessonSession source;
        internal bool Cancelled { get; set; }
        public string LessonId { get; }
        public string ContentVersion { get; }
        public int SchemaVersion { get; }
        public SimulationResult Result { get; }
        internal long Generation { get; }
        internal bool IsCurrent => !Cancelled && source.IsCurrentCompletion(this);
        internal LessonCompletion(LessonSession source, string id, string version, int schema, SimulationResult result, long generation)
        { this.source = source; LessonId = id; ContentVersion = version; SchemaVersion = schema; Result = result; Generation = generation; }
    }
}
