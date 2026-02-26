namespace qASIC.qARK;

/// <summary>Element that represents a group border.</summary>
public class qARKGroupBorder : qARKElement
{
    public qARKGroupBorder() : base() { }
    public qARKGroupBorder(string relativePath) : this(relativePath, relativePath) { }

    public qARKGroupBorder(string absolutePath, string relativePath)
    {
        AbsolutePath = absolutePath;
        RelativePath = relativePath;
    }

    public string AbsolutePath { get; set; } = "";
    public string RelativePath { get; set; } = "";

    public bool IsEnding =>
        string.IsNullOrWhiteSpace(AbsolutePath);

    public override string CreateContent(SerializationStyle style = SerializationStyle.Normal)
    {
        if (style == SerializationStyle.RawData)
            return string.Empty;

        return IsEnding ?
            "---\n" :
            $"--- {RelativePath} ---\n";
    }

    public override bool ShouldParse(qARKTextRead processed, qARKDocument doc) =>
        processed.PeekLine().TrimStart().StartsWith('-');

    public override void Parse(qARKTextRead processed, qARKDocument doc)
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
        $"qARK Group Border '{CreateContent().Trim()}' (fullPath: {AbsolutePath})";
}
