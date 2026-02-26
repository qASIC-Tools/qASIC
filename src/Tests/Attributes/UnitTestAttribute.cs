namespace Tests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class UnitTestAttribute(string name) : Attribute
{
    public UnitTestAttribute() : this("") { }
    
    public string Name { get; } = name;
}