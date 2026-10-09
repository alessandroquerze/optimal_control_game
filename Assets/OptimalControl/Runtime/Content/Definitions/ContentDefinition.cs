using System;
using System.Collections.Generic;
using UnityEngine;

namespace OptCon.Content
{
    public abstract class ContentDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable author-assigned identity, independent of filename and list order.")]
        private string id;
        [SerializeField, Min(1)] private int schemaVersion = 1;
        [SerializeField] private string contentVersion = "1";
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public int SchemaVersion => schemaVersion;
        public string ContentVersion => contentVersion;
        public string Title => title;
        public string Description => description;
    }

    internal static class ContentCollections
    {
        internal static IReadOnlyList<T> View<T>(T[] values) => Array.AsReadOnly(values ?? Array.Empty<T>());
    }
}
