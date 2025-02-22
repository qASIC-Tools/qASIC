namespace qASIC.qARK
{
    public static class qARKUtility
    {
        public static string FormatString(string s) =>
            (s ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        public static string PrepareValueStringForExport(string s) =>
            (s.Contains("\n") || s.StartsWith(" ") || s.EndsWith(" ")) ?
            $"\"{s.Replace("\"", "\"\"")}\"" :
            s.Replace("\"", "\"\"");
    }
}