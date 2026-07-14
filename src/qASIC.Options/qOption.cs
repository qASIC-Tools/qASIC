using System;

namespace qASIC.Options;

/// <summary>Represents a single option.</summary>
/// <param name="optionName">Name of the option.</param>
/// <param name="defaultValue">The default value of the option.</param>
/// <param name="value">The current value the option is set to.</param>
public class qOption(Type valueType, string optionName, object defaultValue, object value) : IOption
{
    /// <summary>Creates a new instance.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="defaultValue">The default and current value the option is set to.</param>
    public qOption(Type valueType, string optionName, object defaultValue) : this(valueType, optionName, defaultValue, defaultValue) { }

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

    /// <inheritdoc/>
    public event Action<IOption> OnValueChanged;

    /// <inheritdoc/>
    public bool IsValidValue(object value) =>
        value == null ?
            (!ValueType.IsValueType && Nullable.GetUnderlyingType(ValueType) != null) :
            value.GetType().IsAssignableTo(ValueType);
}

public class qOption<T> : qOption
{
    /// <inheritdoc/>
    public qOption(string optionName, T defaultValue, T value) : base(typeof(T), optionName, defaultValue, value) { }
    /// <inheritdoc/>
    public qOption(string optionName, T defaultValue) : base(typeof(T), optionName, defaultValue) { }

    /// <summary>Retrieves the value cast into the option's type.</summary>
    /// <returns>Returns <see cref="qOption.Value"/> cast into <see cref="T"/>.</returns>
    public T GetValue() =>
        this.GetValue<T>();
    
    /// <summary>Registers an event that is invoked when <see cref="Value"/> is changed.</summary>
    /// <param name="onValueChanged">The event to register.</param>
    public void RegisterValueListener(Action<T> onValueChanged) =>
        this.RegisterValueListener<T>(onValueChanged);

    /// <summary>Unregisters an event that was previously registered using <see cref="RegisterValueListener"/>.</summary>
    /// <param name="onValueChanged">The event to unregister.</param>
    public void UnregisterValueListener(Action<T> onValueChanged) =>
        this.UnregisterValueListener<T>(onValueChanged);
}
