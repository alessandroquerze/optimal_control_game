using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OptCon.Content;

namespace OptCon.Application
{
    /// <summary>Persistent completion domain. Catalog order, runtime step index and scene names are never progress keys.</summary>
    public sealed class ProgressionService : ICourseProgress
    {
        private sealed class LessonIdentity
        {
            public readonly string Id, Version;
            public readonly int Schema;
            public readonly string[] Prerequisites;
            public LessonIdentity(LessonDefinition lesson)
            { Id = lesson.Id; Version = lesson.ContentVersion; Schema = lesson.SchemaVersion; Prerequisites = lesson.PrerequisiteLessonIds.ToArray(); }
        }
        private readonly Dictionary<string, LessonIdentity> lessons = new Dictionary<string, LessonIdentity>(StringComparer.Ordinal);
        private readonly IProgressStore store;
        private readonly string courseId, courseVersion;
        private ProgressDocument document;
        private bool readOnly;
        public string Diagnostic { get; private set; }
        public event Action Changed;
        public ProgressionService(CourseDefinition course, IProgressStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            if (course == null) throw new ArgumentNullException(nameof(course));
            ValidateIdentity(course); courseId = course.Id; courseVersion = course.ContentVersion;
            var definitions = new Dictionary<string, LessonDefinition>(StringComparer.Ordinal);
            foreach (var chapter in course.Chapters)
            {
                ValidateIdentity(chapter);
                foreach (var lesson in chapter.Lessons)
                {
                    ValidateIdentity(lesson);
                    if (definitions.TryGetValue(lesson.Id, out var existing))
                    { if (existing != lesson) throw new ArgumentException("Duplicate lesson identity."); continue; }
                    definitions.Add(lesson.Id, lesson); lessons.Add(lesson.Id, new LessonIdentity(lesson));
                }
            }
            var marks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var lesson in lessons.Values) Visit(lesson, marks);
            try
            {
                document = store.Load() ?? new ProgressDocument();
                if (document.SchemaVersion != ProgressDocument.CurrentSchemaVersion)
                { readOnly = true; Diagnostic = "Unsupported progress schema " + document.SchemaVersion + "; save preserved, progress read-only."; }
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { document = new ProgressDocument(); readOnly = true; Diagnostic = "Cannot read progress; save preserved: " + exception.Message; }
        }
        private static void ValidateIdentity(ContentDefinition value)
        {
            if (value == null || string.IsNullOrEmpty(value.Id) || !Regex.IsMatch(value.Id, @"^[A-Za-z0-9][A-Za-z0-9._-]*$")
                || value.SchemaVersion < 1 || string.IsNullOrWhiteSpace(value.ContentVersion)) throw new ArgumentException("Invalid catalog identity/version.");
        }
        private void Visit(LessonIdentity lesson, Dictionary<string, int> marks)
        {
            if (marks.TryGetValue(lesson.Id, out int mark)) { if (mark == 1) throw new ArgumentException("Prerequisite cycle: " + lesson.Id); return; }
            marks[lesson.Id] = 1;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in lesson.Prerequisites)
            {
                if (string.IsNullOrWhiteSpace(id) || !lessons.TryGetValue(id, out var previous)) throw new ArgumentException("Missing prerequisite: " + id);
                if (!seen.Add(id)) throw new ArgumentException("Duplicate prerequisite: " + id);
                Visit(previous, marks);
            }
            marks[lesson.Id] = 2;
        }
        private bool Completed(LessonIdentity lesson) => !readOnly && document.Completions.Any(r => r.CourseId == courseId && r.LessonId == lesson.Id
            && r.LessonVersion == lesson.Version && r.LessonSchemaVersion == lesson.Schema);
        public LessonProgressStatus GetStatus(string lessonId)
        {
            if (lessonId == null || !lessons.TryGetValue(lessonId, out var lesson)) throw new ArgumentException("Unknown lesson: " + lessonId);
            return new LessonProgressStatus(Completed(lesson), lesson.Prerequisites.Where(id => !Completed(lessons[id])));
        }
        internal bool Commit(LessonCompletion completion)
        {
            if (readOnly || completion == null || !completion.IsCurrent || !lessons.TryGetValue(completion.LessonId, out var lesson)
                || completion.ContentVersion != lesson.Version || completion.SchemaVersion != lesson.Schema) return false;
            if (Completed(lesson)) return true;
            var candidate = new ProgressDocument(document.Completions.Concat(new[] { new CompletionRecord(courseId, courseVersion, lesson.Id, lesson.Version, lesson.Schema) }));
            try { store.Save(candidate); }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { Diagnostic = "Completion not saved: " + exception.Message; Changed?.Invoke(); return false; }
            document = candidate; Diagnostic = null; Changed?.Invoke(); return true;
        }
    }
}
