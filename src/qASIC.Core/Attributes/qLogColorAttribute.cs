using System;

namespace qASIC;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class qLogColorAttribute : Attribute
{
    public qLogColorAttribute(GenericColor color)
    {
        Color = qColor.GetGenericColor(color);
    }

    public qLogColorAttribute(byte red, byte green, byte blue) : this(red, green, blue, 255) { }
    public qLogColorAttribute(byte red, byte green, byte blue, byte alpha)
    {
        Color = new qColor(red, green, blue, alpha);
    }

    public qColor Color { get; private set; }
}
