namespace qASIC;

/// <summary>When trying to find the calling type of a logging method, if this attribute is present, the class / struct will be skipped when looking for attributes modifying the log.</summary>
[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class qSkipLogModifiersAttribute : System.Attribute { }
