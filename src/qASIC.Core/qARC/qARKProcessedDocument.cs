using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace qASIC.qARK
{
    public class qARKProcessedDocument : IEnumerable<string>
    {
        public qARKProcessedDocument(string txt) : this(qARKUtility.FormatString(txt).Split("\n")) { }
        public qARKProcessedDocument(string[] lines)
        {
            Lines = lines;
        }

        public string[] Lines { get; set; }
        public int Position { get; set; }
        public string Prefix { get; set; }

        public qARKEntry PreviousEntry { get; set; } = null;

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
            path = $"{Prefix}{path}";

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

            return txt.ToString();
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

        public string GetValue(string currentLine)
        {
            var quoteCount = 0;
            while (quoteCount < currentLine.Length && currentLine[quoteCount] == '\"')
                quoteCount++;

            //If it doesn't start with an odd amount of quotations,
            //we can just skip the rest and replace double quotations
            //with single ones
            if (quoteCount % 2 == 0)
                return currentLine.Replace("\"\"", "\"");

            currentLine = currentLine.Substring(1, currentLine.Length - 1);

            var value = new StringBuilder();
            while (true)
            {
                int emptyCount = 0;
                var txtParts = currentLine.Split('\"');

                for (int i = 0; i < txtParts.Length; i++)
                {
                    bool empty = string.IsNullOrEmpty(txtParts[i]);

                    if (empty)
                    {
                        emptyCount++;

                        //If two empty parts are after each other,
                        //we can reset the count and add a "
                        if (emptyCount == 2)
                        {
                            value.Append('\"');
                            emptyCount = 0;
                        }

                        continue;
                    }

                    //If it's not empty

                    if (i != 0) //Ignore if first
                    {
                        //If two non-empty parts were after each other,
                        //it means there was a single quotation mark
                        //between them that marks the end
                        if (emptyCount == 0)
                        {
                            //Add empty count to properly exit loop
                            emptyCount++;
                            break;
                        }

                        value.Append('\"');
                    }

                    emptyCount = 0;
                    value.Append(txtParts[i]);
                }

                if (emptyCount == 1)
                    break;

                if (FinishedReading)
                    break;

                //Move to the next line
                value.Append('\n');
                currentLine = GetLine();
            }

            return value.ToString();
        }

        public IEnumerator<string> GetEnumerator() =>
            Lines.GetEnumerator() as IEnumerator<string>;

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}