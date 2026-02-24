namespace qASIC.qARK;

/// <summary>Generic element of qARK.</summary>
public abstract class qARKElement
{
    public abstract string CreateContent(SerializationStyle style);
    public abstract bool ShouldParse(qARKTextRead processed, qARKDocument doc);
    public abstract void Parse(qARKTextRead processed, qARKDocument doc);
}
