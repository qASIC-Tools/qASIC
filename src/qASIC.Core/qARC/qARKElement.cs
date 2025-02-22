namespace qASIC.qARK
{
    public abstract class qARKElement
    {
        public abstract string CreateContent();
        public abstract bool ShouldParse(qARKProcessedDocument processed, qARKDocument doc);
        public abstract void Parse(qARKProcessedDocument processed, qARKDocument doc);
    }
}