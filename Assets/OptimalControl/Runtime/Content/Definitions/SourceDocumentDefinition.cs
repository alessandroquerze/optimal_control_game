using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Content/Source Document")]
    public sealed class SourceDocumentDefinition : ContentDefinition
    {
        [SerializeField] private string author;
        [SerializeField] private string documentDate;
        [SerializeField] private string projectRelativePath;
        [SerializeField] private string sha256;
        [SerializeField] private int physicalPageCount;
        [SerializeField, TextArea] private string numberingNotes;

        public string Author => author;
        public string DocumentDate => documentDate;
        public string ProjectRelativePath => projectRelativePath;
        public string Sha256 => sha256;
        public int PhysicalPageCount => physicalPageCount;
        public string NumberingNotes => numberingNotes;
    }
}
