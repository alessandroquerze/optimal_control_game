using System;
using System.Collections.Generic;

namespace OptCon.Application
{
    public sealed class CompletionRecord
    {
        public string CourseId { get; }
        public string CourseVersion { get; }
        public string LessonId { get; }
        public string LessonVersion { get; }
        public int LessonSchemaVersion { get; }
        public CompletionRecord(string courseId, string courseVersion, string lessonId, string lessonVersion, int lessonSchemaVersion)
        {
            if (string.IsNullOrWhiteSpace(courseId) || string.IsNullOrWhiteSpace(courseVersion) || string.IsNullOrWhiteSpace(lessonId)
                || string.IsNullOrWhiteSpace(lessonVersion) || lessonSchemaVersion < 1) throw new ArgumentException("Invalid completion identity/version.");
            CourseId = courseId; CourseVersion = courseVersion; LessonId = lessonId; LessonVersion = lessonVersion; LessonSchemaVersion = lessonSchemaVersion;
        }
    }
    public sealed class ProgressDocument
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion { get; }
        public IReadOnlyList<CompletionRecord> Completions { get; }
        public ProgressDocument(IEnumerable<CompletionRecord> completions = null, int schemaVersion = CurrentSchemaVersion)
        {
            var records = new List<CompletionRecord>(completions ?? Array.Empty<CompletionRecord>());
            var identities = new HashSet<Tuple<string, string, string, int>>();
            foreach (var record in records)
                if (record == null || !identities.Add(Tuple.Create(record.CourseId, record.LessonId, record.LessonVersion, record.LessonSchemaVersion)))
                    throw new ArgumentException("Completion records must be non-null and unique by course/lesson/version.");
            SchemaVersion = schemaVersion; Completions = Array.AsReadOnly(records.ToArray());
        }
    }
    /// <summary>Storage boundary. null means no save; failures must be explicit, never a fabricated empty profile.</summary>
    public interface IProgressStore
    {
        ProgressDocument Load();
        void Save(ProgressDocument document);
    }
    public sealed class MemoryProgressStore : IProgressStore
    {
        private ProgressDocument document;
        public int SaveCount { get; private set; }
        public ProgressDocument Load() => document;
        public void Save(ProgressDocument value) { document = value ?? throw new ArgumentNullException(nameof(value)); SaveCount++; }
    }
}
