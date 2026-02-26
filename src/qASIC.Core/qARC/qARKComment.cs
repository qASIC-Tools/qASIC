using System;
using System.Text;

namespace qASIC.qARK;

/// <summary>Element that represents a comment.</summary>
public class qARKComment : qARKElement
{
    public qARKComment() : base() { }
    public qARKComment(string comment)
    {
        Comment = comment;
    }

    private string comment = "";
    public string Comment
    {
        get => comment;
        set => comment = qARKUtility.FormatStringValue(value);
    }

    public override string CreateContent(SerializationStyle style = SerializationStyle.Normal) =>
        style == SerializationStyle.RawData ? string.Empty : $"# {Comment.Replace("\n", "\n# ")}\n";

    public override bool ShouldParse(qARKTextRead processed, qARKDocument doc) =>
        processed.PeekLine().Trim().StartsWith("#");

    public override void Parse(qARKTextRead processed, qARKDocument doc)
    {
        var comment = new StringBuilder();
        while (!processed.FinishedReading && processed.PeekLine().TrimStart().StartsWith("#"))
        {
            var line = processed.GetLine().TrimStart();
            var trimCount = Math.Min(2, line.Length);
            line = line[trimCount..];
            comment.Append($"\n{line}");
        }

        var txt = comment.ToString();
        doc.AddElement(new qARKComment(txt[1..]));
    }

    public override string ToString() =>
        $"qARK Comment '{CreateContent().Trim()}'";
}
