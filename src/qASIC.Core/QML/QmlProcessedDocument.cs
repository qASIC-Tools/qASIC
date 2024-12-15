using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace qASIC.QML
{
    public class QmlProcessedDocument : IEnumerable<string>
    {
        public QmlProcessedDocument(string txt) : this(QmlUtility.FormatString(txt).Split("\n")) { }
        public QmlProcessedDocument(string[] lines)
        {
            Lines = lines;
        }

        public string[] Lines { get; set; }
        public int Position { get; set; }
        public string Prefix { get; set;}

        public Dictionary<string, int> InsertPaths { get; set; } = new Dictionary<string, int>();

        public bool FinishedReading =>
            Position >= Lines.Length;

        public string GetLine()
        {
            var line = PeekLine();
            Position++;
            return line;
        }

        public string PeekLine() =>
            Lines[Position];

        public string FormatPath(string path)
        {
            var parts = path.Split('.');
            StringBuilder txt = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                switch (part)
                {
                    case "_":
                        part = GetPathInsertIndex(txt.ToString()).ToString();
                        break;
                    case "@":
                        part = GetNextPathInsertIndex(txt.ToString()).ToString();
                        break;
                }
                
                if (i > 0) txt.Append('.');
                txt.Append(part);
            }

            return $"{Prefix}{txt}";
        }

        private int GetPathInsertIndex(string path)
        {
            if (InsertPaths.TryGetValue(path, out var val))
                return val;

            InsertPaths.Add(path, 0);
            return 0;
        }

        private int GetNextPathInsertIndex(string path)
        {
            if (!InsertPaths.ContainsKey(path))
            {
                InsertPaths.Add(path, 0);
                return 0;
            }

            InsertPaths[path]++;
            return InsertPaths[path];
        }

        public IEnumerator<string> GetEnumerator() =>
            Lines.GetEnumerator() as IEnumerator<string>;

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}