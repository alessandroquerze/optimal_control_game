using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Course")]
    public sealed class CourseDefinition : ContentDefinition
    {
        [SerializeField] private bool isPrototype;
        [SerializeField] private ChapterDefinition[] chapters = Array.Empty<ChapterDefinition>();
        [SerializeField] private SourceDocumentDefinition[] sources = Array.Empty<SourceDocumentDefinition>();

        public bool IsPrototype => isPrototype;
        public IReadOnlyList<ChapterDefinition> Chapters => ContentCollections.View(chapters);
        public IReadOnlyList<SourceDocumentDefinition> Sources => ContentCollections.View(sources);
    }
}
