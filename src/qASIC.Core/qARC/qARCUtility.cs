namespace qASIC.qARK;

/// <summary>General utility methods used by qARK.</summary>
public static partial class qARKUtility
{
    public static string FormatStringValue(string s) =>
        (s ?? string.Empty)
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

    public static string PrepareValueStringForExport(string s) =>
        (s.Contains('\n') || s.StartsWith(' ') || s.EndsWith(' ')) ?
        $"\"{s.Replace("\"", "\"\"")}\"" :
        s.Replace("\"", "\"\"");
}
