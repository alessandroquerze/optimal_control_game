using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Chapter")]
    public sealed class ChapterDefinition : ContentDefinition
    {
        [SerializeField] private LessonDefinition[] lessons = Array.Empty<LessonDefinition>();
        [SerializeField] private SourceReference[] sources = Array.Empty<SourceReference>();

        public IReadOnlyList<LessonDefinition> Lessons => ContentCollections.View(lessons);
        public IReadOnlyList<SourceReference> Sources => ContentCollections.View(sources);
    }
}
