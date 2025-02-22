using qASIC.Parsing;
using System.Text;

namespace qASIC.qARK
{
    public class qARKSerializer
    {
        public qARKSerializer() : this(new ModularParser()) { }
        public qARKSerializer(ModularParser parser)
        {
            Parser = parser;
        }

        public qARKElement[] DeserializeElements { get; set; } = new qARKElement[]
        {
            new qARKSpace(),
            new qARKComment(),
            new qARKGroupBorder(),
            new qARKEntry(),
        };

        public ModularParser Parser { get; set; }

        public string Serialize(qARKDocument document)
        {
            var txt = new StringBuilder();

            foreach (var element in document)
                txt.Append(element.CreateContent());

            var finalTxt = txt.ToString();

            if (finalTxt.EndsWith("\n"))
                finalTxt = finalTxt.Substring(0, finalTxt.Length - 1);

            return finalTxt;
        }

        public qARKDocument Deserialize(string txt)
        {
            var processed = new qARKProcessedDocument(txt);
            var doc = new qARKDocument(Parser);

            while (!processed.FinishedReading)
            {
                bool parsed = false;
                foreach (var item in DeserializeElements)
                {
                    if (!item.ShouldParse(processed, doc)) continue;
                    item.Parse(processed, doc);
                    parsed = true;
                    break;
                }

                if (!parsed)
                    processed.GetLine();
            }

            return doc;
        }
    }
}