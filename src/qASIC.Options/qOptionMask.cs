using System;

namespace qASIC.Options;

public class qOptionMask(IOption target, object value) : IOption
{
    public qOptionMask(IOption target) : this(target, target.Value) { }

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
            if (!IsValidValue(value))
                throw new ArgumentException($"Value needs to be of type '{ValueType}'.");
            
            field = value;
            OnValueChanged?.Invoke(this);
        }
    } = value;
    /// <inheritdoc/>
    public Type ValueType => Target.ValueType;

    /// <inheritdoc/>
    public bool IsValidValue(object value) => Target.IsValidValue(value);

    /// <summary>Invoked when the value of the mask is changed.</summary>
    public event Action<IOption> OnValueChanged;
    /// <summary>Invoked when the value of the mask is applied to the target option.</summary>
    public event Action<qOptionMask> OnApply;

    /// <summary>Applies the value of the mask to the target option.</summary>
    public void Apply()
    {
        Target.Value = Value;
        OnApply?.Invoke(this);
    }
}
