using System;

namespace qASIC.qARK;

/// <summary>Element that represents an empty line.</summary>
public class qARKSpace : qARKElement
{
    public qARKSpace(int count = 1) : base()
    {
        Count = count;
    }

    private int count;
    public int Count
    {
        get => count;
        set => count = Math.Max(value, 1);
    }

    public override string CreateContent(SerializationStyle style) =>
        style == SerializationStyle.RawData ? string.Empty : new('\n', Count);

    public override bool ShouldParse(qARKTextRead processed, qARKDocument doc) =>
        string.IsNullOrWhiteSpace(processed.PeekLine());

    public override void Parse(qARKTextRead processed, qARKDocument doc)
    {
        int i = 0;
        while (!processed.FinishedReading && string.IsNullOrWhiteSpace(processed.PeekLine()))
        {
            i++;
            processed.GetLine();
        }

        doc.AddElement(new qARKSpace(i));
    }

    public override string ToString() =>
        $"qARK Space (spaceCount: {count})";
}
