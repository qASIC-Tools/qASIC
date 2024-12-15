using System;
using System.Linq;
using System.Text;

namespace qASIC.QML
{
    public class QmlEntry : QmlElement
    {
        public QmlEntry() : base() { }
        public QmlEntry(string relativePath, object value) : this(relativePath, relativePath, value) { }

        public QmlEntry(string path, string relativePath, object value)
        {
            Path = path;
            RelativePath = relativePath;
            Value = value?.ToString() ?? string.Empty;
        }

        public string Path { get; set; }
        public string RelativePath { get; set; }

        private string value;
        public string Value
        {
            get => value;
            set => this.value = QmlUtility.FormatString(value);
        }

        public bool IsArrayItem { get; set; }
        public bool IsArrayStart { get; set; }

        #region Getting Value
        public T GetValue<T>(T defaultValue = default) =>
            QmlUtility.ParseValue<T>(Value, defaultValue);

        public object GetValue(Type type, object defaultValue = null) =>
            QmlUtility.ParseValue(type, Value, defaultValue);

        public bool TryGetValue<T>(out T result) =>
            TryGetValue(default, out result);

        public bool TryGetValue<T>(T defaultValue, out T result) =>
            QmlUtility.TryParseValue(Value, defaultValue, out result);

        public bool TryGetValue(Type type, out object result) =>
            TryGetValue(type, default, out result);

        public bool TryGetValue(Type type, object defaultValue, out object result) =>
            QmlUtility.TryParseValue(type, Value, defaultValue, out result);
        #endregion

        public override string CreateContent()
        {
            if (IsArrayStart)
                return $"{RelativePath}|\n";

            if (IsArrayItem)
                return $"* {QmlUtility.PrepareValueStringForExport(Value)}\n";

            return $"{RelativePath} = {QmlUtility.PrepareValueStringForExport(Value)}\n";
        }

        public override bool ShouldParse(QmlProcessedDocument processed, QmlDocument doc)
        {
            var line = processed.PeekLine();
            return line.Contains('=') || line.TrimEnd().EndsWith('|') || line.TrimStart().StartsWith('*');
        }

        public override void Parse(QmlProcessedDocument processed, QmlDocument doc)
        {
            var line = processed.GetLine();

            bool isArrayStart = line.TrimEnd().EndsWith('|');
            bool isArrayItem = line.TrimStart().StartsWith('*');

            var relativePath = string.Empty;
            var txt = string.Empty;

            if (isArrayStart)
            {
                relativePath = line.TrimEnd();
                relativePath = relativePath.Substring(0, relativePath.Length - 1);
            }

            if (!isArrayStart && isArrayItem)
            {
                //If there is no array start or any entry before, ignore
                if (processed.PreviousEntry == null)
                    return;

                relativePath = processed.PreviousEntry.RelativePath;
                txt = line.TrimStart();
                txt = txt.Substring(1, txt.Length - 1)
                    .Trim();
            }

            if (!isArrayStart && !isArrayItem)
            {
                var mainLineParts = line.Split("=");
                relativePath = mainLineParts[0];
                txt = string.Join("=", mainLineParts.Skip(1))
                    .Trim();
            }

            var path = processed.FormatPath(relativePath);

            var el = new QmlEntry(path, relativePath, isArrayStart ? string.Empty : processed.GetValue(txt))
            {
                IsArrayStart = isArrayStart,
                IsArrayItem = isArrayItem,
            };

            doc.AddElement(el);
            processed.PreviousEntry = el;
        }

        public override string ToString()
        {
            var txt = new StringBuilder("QML Entry");

            txt.Append((IsArrayStart, IsArrayItem) switch
            {
                (true, false) => $" '{RelativePath}|'",
                (false, true) => $" '* {Value}'",
                _ => $" '{RelativePath} = {Value}'",
            });

            txt.Append($" (fullPath: {Path})");
            return txt.ToString();
        }
    }
}