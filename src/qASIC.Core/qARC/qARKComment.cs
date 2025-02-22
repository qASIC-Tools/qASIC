using System;
using System.Text;

namespace qASIC.qARK
{
    public class qARKComment : qARKElement
    {
        public qARKComment() : base() { }
        public qARKComment(string comment)
        {
            Comment = comment;
        }

        private string comment;
        public string Comment
        {
            get => comment;
            set => comment = qARKUtility.FormatString(value);
        }

        public override string CreateContent() =>
            $"# {Comment.Replace("\n", "\n# ")}\n";

        public override bool ShouldParse(qARKProcessedDocument processed, qARKDocument doc) =>
            processed.PeekLine().Trim().StartsWith("#");

        public override void Parse(qARKProcessedDocument processed, qARKDocument doc)
        {
            var comment = new StringBuilder();
            while (!processed.FinishedReading && processed.PeekLine().TrimStart().StartsWith("#"))
            {
                var line = processed.GetLine().TrimStart();
                var trimCount = Math.Min(2, line.Length);
                line = line.Substring(trimCount, line.Length - trimCount);
                comment.Append($"\n{line}");
            }

            var txt = comment.ToString();
            doc.AddElement(new qARKComment(txt.Substring(1, txt.Length - 1)));
        }

        public override string ToString() =>
            $"qARK Comment '{CreateContent().Trim()}'";
    }
}