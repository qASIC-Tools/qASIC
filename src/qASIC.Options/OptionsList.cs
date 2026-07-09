using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Options;

/// <summary>A list of all options.</summary>
public sealed class OptionsList : IOptionsList
{
    /// <summary>Invoked when an option's value is changed.</summary>
    public event Action<IOption> OnOptionValueChanged;

    private Dictionary<string, Option> _options = [];

    public Option this[string optionName]
    {
        get => _options[optionName];
    }

    /// <summary>Adds a new option to the list.</summary>
    /// <param name="option">The new option.</param>
    public void Add(Option option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (_options.ContainsKey(option.OptionName))
            throw new ArgumentException($"Option '{option.OptionName}' already exists!", nameof(option));
        
        _options.Add(option.OptionName, option);
        option.OnValueChanged += Option_OnValueChanged;
    }

    /// <summary>Removes an option.</summary>
    /// <param name="optionName">The name of the option.</param>
    /// <returns>Returns true if the option was successfully found and removed.</returns>
    public bool Remove(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_options.TryGetValue(optionName, out var option))
            option.OnValueChanged -= Option_OnValueChanged;
        
        return _options.Remove(optionName);
    }

    /// <summary>Checks if an option exists.</summary>
    /// <param name="optionName">The name of the option.</param>
    /// <returns>Returns true if the option exists.</returns>
    public bool Contains(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        return _options.ContainsKey(optionName);
    }

    /// <summary>Tries to retrieve an option from the list.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="result">The resulting option if found, otherwise <see cref="null"/>.</param>
    /// <returns>Returns true if it was successfull.</returns>
    public bool TryGetOption(string optionName, out Option result)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (!_options.ContainsKey(optionName))
        {
            result = null;
            return false;
        }

        result = _options[optionName];
        return true;
    }

    /// <summary>Retrieves an option.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <returns>Returns the option if found, otherwise <see cref="null"/>.</returns>
    public Option GetOption(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        return _options.TryGetValue(optionName, out var result) ? result : null;
    }

    IOption IOptionsList.GetOption(string optionName) => GetOption(optionName);
    bool IOptionsList.TryGetOption(string optionName, out IOption result)
    {
        var val = TryGetOption(optionName, out var option);
        result = option;
        return val;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IOption> GetEnumerator() => _options.Select(x => x.Value)
        .ToList()
        .GetEnumerator();
    
    private void Option_OnValueChanged(Option option)
    {
        OnOptionValueChanged?.Invoke(option);
    }
}
