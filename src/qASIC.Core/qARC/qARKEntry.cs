using qASIC.Parsing;
using System;
using System.Linq;

namespace qASIC.qARK;

/// <summary>Element that represents an entry.</summary>
public class qARKEntry : qARKElement
{
    public qARKEntry() : base() { }
    public qARKEntry(string relativePath, string value) : this(relativePath, relativePath, value) { }

    public qARKEntry(string absolutePath, string relativePath, string value)
    {
        AbsolutePath = absolutePath;
        RelativePath = relativePath;
        Value = value;
    }

    public string AbsolutePath { get; set; } = "";
    public string RelativePath { get; set; } = "";

    private string value;
    public string Value
    {
        get => value;
        set => this.value = qARKUtility.FormatStringValue(value);
    }

    public bool IsArrayItem { get; set; }
    public bool IsArrayStart { get; set; }

    public ModularParser Parser { get; set; }

    #region Getting Value
    public T GetValue<T>(T defaultValue = default) =>
        Parser?.TryParse<T>(Value, out var result) == true ?
            result :
            defaultValue;

    public object GetValue(Type type, object defaultValue = null) =>
        Parser?.TryParse(type, Value, out var result) == true ?
            result :
            defaultValue;

    public bool TryGetValue<T>(out T result) =>
        TryGetValue(default, out result);

    public bool TryGetValue<T>(T defaultValue, out T result)
    {
        if (Parser == null)
        {
            result = defaultValue;
            return false;
        }

        if (Parser.TryParse(Value, out result))
            return true;

        result = defaultValue;
        return false;
    }

    public bool TryGetValue(Type type, out object result) =>
        TryGetValue(type, default, out result);

    public bool TryGetValue(Type type, object defaultValue, out object result)
    {
        if (Parser == null)
        {
            result = defaultValue;
            return false;
        }

        if (Parser.TryParse(type, Value, out result))
            return true;

        result = defaultValue;
        return false;
    }
    #endregion

    public override string CreateContent(SerializationStyle style = SerializationStyle.Normal)
    {
        if (style == SerializationStyle.RawData)
            return $"{AbsolutePath} = {qARKUtility.PrepareValueStringForExport(Value)}\n";

        if (IsArrayStart)
            return $"{RelativePath}|\n";

        if (IsArrayItem)
            return $"* {qARKUtility.PrepareValueStringForExport(Value)}\n";

        return $"{RelativePath} = {qARKUtility.PrepareValueStringForExport(Value)}\n";
    }

    public override bool ShouldParse(qARKTextRead processed, qARKDocument doc)
    {
        var line = processed.PeekLine();
        return line.Contains('=') || line.TrimEnd().EndsWith('|') || line.TrimStart().StartsWith('*');
    }

    public override void Parse(qARKTextRead processed, qARKDocument doc)
    {
        var line = processed.GetLine();

        bool isArrayStart = line.TrimEnd().EndsWith('|');
        bool isArrayItem = line.TrimStart().StartsWith('*');

        var relativePath = string.Empty;
        var txt = string.Empty;

        if (isArrayStart)
        {
            relativePath = line.Trim();
            relativePath = relativePath.Substring(0, relativePath.Length - 1);
        }

        if (!isArrayStart && isArrayItem)
        {
            //If there is no array start or any entry before, ignore
            if (processed.PreviousEntry == null)
                return;

            relativePath = processed.PreviousEntry.RelativePath;
            txt = line.Trim();
            txt = txt[1..].Trim();
        }

        if (!isArrayStart && !isArrayItem)
        {
            var mainLineParts = line.Split("=");
            relativePath = mainLineParts[0].Trim();
            txt = string.Join("=", mainLineParts.Skip(1))
                .Trim();
        }

        var path = processed.FormatPath(relativePath);

        var el = new qARKEntry(path, relativePath, isArrayStart ? string.Empty : processed.GetValue(txt))
        {
            IsArrayStart = isArrayStart,
            IsArrayItem = isArrayItem,
            Parser = doc.Parser,
        };

        doc.AddElement(el);
        processed.PreviousEntry = el;
    }

    public override string ToString() =>
        $"qARK Entry '{CreateContent().Trim()}' (fullPath: {AbsolutePath})";
}
