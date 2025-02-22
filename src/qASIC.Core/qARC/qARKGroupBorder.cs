namespace qASIC.qARK
{
    public class qARKGroupBorder : qARKElement
    {
        public qARKGroupBorder() : base() { }
        public qARKGroupBorder(string relativePath) : this(relativePath, relativePath) { }

        public qARKGroupBorder(string path, string relativePath)
        {
            Path = path;
            RelativePath = relativePath;
        }

        public string Path { get; set; }
        public string RelativePath { get; set; }

        public bool IsEnding =>
            string.IsNullOrWhiteSpace(Path);

        public override string CreateContent() =>
            IsEnding ?
            "---\n" :
            $"--- {RelativePath} ---\n";

        public override bool ShouldParse(qARKProcessedDocument processed, qARKDocument doc) =>
            processed.PeekLine().TrimStart().StartsWith('-');

        public override void Parse(qARKProcessedDocument processed, qARKDocument doc)
        {
            var relativePath = processed.GetLine()
                .Trim()
                .Trim('-')
                .Trim();

            processed.Prefix = string.Empty;
            processed.PreviousEntry = null;
            var path = processed.FormatPath(relativePath);

            if (string.IsNullOrEmpty(relativePath))
            {
                doc.AddElement(new qARKGroupBorder());
                return;
            }

            processed.Prefix = $"{path}.";

            doc.AddElement(new qARKGroupBorder(path, relativePath));
        }

        public override string ToString() =>
            $"qARK Group Border '{CreateContent().Trim()}' (fullPath: {Path})";
    }
}