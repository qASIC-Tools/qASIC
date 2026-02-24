namespace qASIC.qARK;

/// <summary>Describes how a <see cref="qARKDocument"/> should be serialized.</summary>
public enum SerializationStyle
{
    /// <summary>Standard style, where every element is serialized based on their own properties.</summary>
    Normal,
    /// <summary>This will result in the output consisting of only entries in "path = value" style. All comments, groups, etc. will be discarded.</summary>
    RawData,
}