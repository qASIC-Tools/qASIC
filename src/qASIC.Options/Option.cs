using System;

namespace qASIC.Options;

/// <summary>Represents a single option.</summary>
/// <param name="optionName">Name of the option.</param>
/// <param name="defaultValue">The default value of the option.</param>
/// <param name="value">The current value the option is set to.</param>
public class Option(Type valueType, string optionName, object defaultValue, object value) : IOption
{
    /// <summary>Creates a new instance.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="defaultValue">The default and current value the option is set to.</param>
    public Option(Type valueType, string optionName, object defaultValue) : this(valueType, optionName, defaultValue, defaultValue) { }

    /// <inheritdoc/>
    public string OptionName { get; } = optionName;

    /// <inheritdoc/>
    public object DefaultValue { get; } = defaultValue;

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
    public Type ValueType { get; } = valueType;

    /// <summary>Invoked when <see cref="Value"/> is changed.</summary>
    public event Action<Option> OnValueChanged;

    /// <inheritdoc/>
    public bool IsValidValue(object value) =>
        value == null ?
            (!ValueType.IsValueType && Nullable.GetUnderlyingType(ValueType) != null) :
            value.GetType().IsAssignableTo(ValueType);
}

public class Option<T> : Option
{
    /// <inheritdoc/>
    public Option(string optionName, T defaultValue, T value) : base(typeof(T), optionName, defaultValue, value) { }
    /// <inheritdoc/>
    public Option(string optionName, T defaultValue) : base(typeof(T), optionName, defaultValue) { }

    /// <summary>Retrieves the value cast into the option's type.</summary>
    /// <returns>Returns <see cref="Option.Value"/> cast into <see cref="T"/>.</returns>
    public T GetValue() =>
        this.GetValue<T>();
}
