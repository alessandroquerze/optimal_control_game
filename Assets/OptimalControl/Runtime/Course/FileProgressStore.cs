using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OptCon.Application
{
    /// <summary>Versioned JSON adapter. Writes via a sibling temporary file; never embeds Unity APIs in progress rules.</summary>
    public sealed class FileProgressStore : IProgressStore
    {
        [Serializable] private sealed class FileData { public int schemaVersion; public RecordData[] completions; }
        [Serializable] private sealed class RecordData
        {
            public string courseId, courseVersion, lessonId, lessonVersion;
            public int lessonSchemaVersion;
        }
        public string Path { get; }
        public FileProgressStore(string path) { Path = System.IO.Path.GetFullPath(path); }
        public ProgressDocument Load()
        {
            if (!File.Exists(Path)) return null;
            FileData data;
            try { data = JsonUtility.FromJson<FileData>(File.ReadAllText(Path)); }
            catch (ArgumentException exception) { throw new InvalidDataException("Invalid progress JSON.", exception); }
            if (data == null || data.schemaVersion < 1) throw new InvalidDataException("Missing progress schema version.");
            // Preserve future files byte-for-byte: the service enters read-only mode.
            if (data.schemaVersion != ProgressDocument.CurrentSchemaVersion) return new ProgressDocument(schemaVersion: data.schemaVersion);
            if (data.completions == null) throw new InvalidDataException("Missing completions collection.");
            try { return new ProgressDocument(data.completions.Select(r => r == null ? throw new ArgumentException("Null record.")
                : new CompletionRecord(r.courseId, r.courseVersion, r.lessonId, r.lessonVersion, r.lessonSchemaVersion))); }
            catch (ArgumentException exception) { throw new InvalidDataException("Invalid completion record.", exception); }
        }
        public void Save(ProgressDocument document)
        {
            if (document == null || document.SchemaVersion != ProgressDocument.CurrentSchemaVersion) throw new InvalidDataException("Unsupported progress schema.");
            var data = new FileData { schemaVersion = document.SchemaVersion, completions = document.Completions.Select(r => new RecordData
                { courseId = r.CourseId, courseVersion = r.CourseVersion, lessonId = r.LessonId, lessonVersion = r.LessonVersion, lessonSchemaVersion = r.LessonSchemaVersion }).ToArray() };
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temporary = Path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(Path)) File.Replace(temporary, Path, null); else File.Move(temporary, Path);
        }
    }
}
