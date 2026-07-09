using System;

namespace qASIC.Options;

/// <summary>Represents a single option.</summary>
/// <param name="optionName">Name of the option.</param>
/// <param name="defaultValue">The default value of the option.</param>
/// <param name="value">The current value the option is set to.</param>
public sealed class Option(string optionName, object defaultValue, object value) : IOption
{
    /// <summary>Creates a new instance.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="defaultValue">The default and current value the option is set to.</param>
    public Option(string optionName, object defaultValue) : this(optionName, defaultValue, defaultValue) { }

    /// <summary>Name of the option.</summary>
    public string OptionName { get; } = optionName;

    /// <summary>The default value of the option.</summary>
    public object DefaultValue { get; } = defaultValue;

    /// <summary>The current value the option is set to.</summary>
    public object Value
    {
        get;
        set
        {
            field = value;
            OnValueChanged?.Invoke(this);
        }
    } = value;

    /// <summary>Invoked when <see cref="Value"/> is changed.</summary>
    public event Action<Option> OnValueChanged;
}
