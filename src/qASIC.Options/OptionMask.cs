using System;

namespace qASIC.Options;

public sealed class OptionMask(IOption target, object value) : IOption
{
    public OptionMask(IOption target) : this(target, target.Value) { }

    /// <summary>The target option.</summary>
    public IOption Target { get; } = target;
    /// <inheritdoc/>
    public string OptionName => Target.OptionName;
    /// <inheritdoc/>
    public object DefaultValue => Target.DefaultValue;
    /// <inheritdoc/>
    public object Value
    {
        get;
        set
        {
            field = value;
            OnValueChanged?.Invoke(this);
        }
    } = value;

    /// <summary>Invoked when the value of the mask is changed.</summary>
    public event Action<OptionMask> OnValueChanged;
    /// <summary>Invoked when the value of the mask is applied to the target option.</summary>
    public event Action<OptionMask> OnApply;

    /// <summary>Applies the value of the mask to the target option.</summary>
    public void Apply()
    {
        Target.Value = Value;
        OnApply?.Invoke(this);
    }
}
