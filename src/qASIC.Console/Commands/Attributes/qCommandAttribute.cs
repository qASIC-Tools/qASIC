using System;

namespace qASIC.Console;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class qCommandAttribute : Attribute
{
    public qCommandAttribute(string name) : this(name, null) { }

    public qCommandAttribute(string name, params string[] aliases)
    {
        Name = name;
        Aliases = aliases;
    }

    public string Name { get; }
    public string[] Aliases { get; } = null;
    public string Description { get; set; } = null;
    public string DetailedDescription { get; set; } = null;
    public bool UseRegisteredTargets { get; protected set; } = true;
}
