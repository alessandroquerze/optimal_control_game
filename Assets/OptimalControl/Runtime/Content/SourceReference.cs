using System;
using UnityEngine;

namespace OptCon.Content
{
    public enum SourceUsage { Direct, Adaptation, Correction }

    /// <summary>Physical pages count the cover as page 1; slide numbers are independent metadata.</summary>
    [Serializable]
    public sealed class SourceReference
    {
        [SerializeField] private SourceDocumentDefinition document;
        [SerializeField] private int firstPhysicalPage = 1;
        [SerializeField] private int lastPhysicalPage = 1;
        [SerializeField, Tooltip("Optional printed slide number; 0 means unspecified.")]
        private int firstSlide;
        [SerializeField] private int lastSlide;
        [SerializeField] private string section;
        [SerializeField] private SourceUsage usage;
        [SerializeField, TextArea] private string note;

        public SourceDocumentDefinition Document => document;
        public string SourceId => document != null ? document.Id : string.Empty;
        public int FirstPhysicalPage => firstPhysicalPage;
        public int LastPhysicalPage => lastPhysicalPage;
        public int FirstSlide => firstSlide;
        public int LastSlide => lastSlide;
        public string Section => section;
        public SourceUsage Usage => usage;
        public string Note => note;
    }
}
