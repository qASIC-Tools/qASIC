namespace qASIC.Parsing
{
    public class qColorParser : ValueParser<qColor>
    {
        public override bool TryParse(string s, out qColor result) =>
            qColor.TryParse(s, out result);
    }
}
