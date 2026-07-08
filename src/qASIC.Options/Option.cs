using System;

namespace qASIC.Options;

/// <summary>Represents a single option.</summary>
/// <param name="optionName">Name of the option.</param>
/// <param name="defaultValue">The default value of the option.</param>
/// <param name="value">The current value the option is set to.</param>
public class Option(string optionName, object defaultValue, object value)
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

    /// <summary>Tries to retrieve the option's value of the provided type.</summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
    /// <param name="result">The resulted value if successfull, otherwise <see cref="default"/>.</param>
    /// <returns>Returns true if <see cref="Value"/> is of type <see cref="T"/>.</returns>
    public bool TryGetValue<T>(out T result)
    {
        if (Value is T val)
        {
            result = val;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Retrieves the option's value of the provided type.</summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
    /// <returns>Returns <see cref="Value"/> cast to type <see cref="T"/>.</returns>
    public T GetValue<T>() =>
        (T)Value;
}
