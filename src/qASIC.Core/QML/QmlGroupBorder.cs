namespace qASIC.QML
{
    public class QmlGroupBorder : QmlElement
    {
        public QmlGroupBorder() : base() { }
        public QmlGroupBorder(string relativePath) : this(relativePath, relativePath) { }

        public QmlGroupBorder(string path, string relativePath)
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

        public override bool ShouldParse(QmlProcessedDocument processed, QmlDocument doc) =>
            processed.PeekLine().TrimStart().StartsWith('-');

        public override void Parse(QmlProcessedDocument processed, QmlDocument doc)
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
                doc.AddElement(new QmlGroupBorder());
                return;
            }

            processed.Prefix = $"{path}.";

            doc.AddElement(new QmlGroupBorder(path, relativePath));
        }
    }
}