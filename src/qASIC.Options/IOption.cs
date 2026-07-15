using System;

namespace qASIC.Options;

/// <summary>A single option that holds a value and it's name.</summary>
public interface IOption
{
    /// <summary>Name of the option.</summary>
    string OptionName { get; }
    /// <summary>The default value of the option.</summary>
    object DefaultValue { get; }
    /// <summary>The current value the option is set to.</summary>
    object Value { get; set; }
    /// <summary>Type of value the option is using.</summary>
    Type ValueType { get; }

    /// <summary>Checks if a value is valid and can be assigned to the option.</summary>
    /// <param name="value">Value to check.</param>
    /// <returns>Returns true if the <see cref="value"/> can be assigned to <see cref="IOption.Value"/>.</returns>
    bool IsValidValue(object value);

    /// <summary>Invoked when <see cref="Value"/> is changed.</summary>
    event Action<IOption> OnValueChanged;
}
