using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Options;

/// <summary>Holds options and their values.</summary>
public class qOptionsList : IModifiableOptionsList
{
    /// <inheritdoc/>
    public event Action<IEnumerable<IOption>> OnOptionValuesChanged;

    private bool _supressEvents;
    private Dictionary<string, IOption> _options = [];

    public IOption this[string optionName]
    {
        get => _options[optionName];
    }

    /// <inheritdoc/>
    public void Add(IOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (_options.ContainsKey(option.OptionName))
            throw new ArgumentException($"Option '{option.OptionName}' already exists!", nameof(option));
        
        _options.Add(option.OptionName, option);
        option.OnValueChanged += Option_OnValueChanged;
    }

    /// <inheritdoc/>
    public bool Remove(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_options.TryGetValue(optionName, out var option))
            option.OnValueChanged -= Option_OnValueChanged;
        
        return _options.Remove(optionName);
    }

    /// <inheritdoc/>
    public bool Contains(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        return _options.ContainsKey(optionName);
    }

    /// <summary>Tries to retrieve an option from the list.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="result">The resulting option if found, otherwise <see cref="null"/>.</param>
    /// <returns>Returns true if it was successfull.</returns>
    public bool TryGetOption(string optionName, out IOption result)
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
    public IOption GetOption(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        return _options.TryGetValue(optionName, out var result) ? result : null;
    }

    /// <inheritdoc/>
    public void ApplyOtherMask(IEnumerable<KeyValuePair<string, object>> values)
    {
        if (values.Any(x => !_options.ContainsKey(x.Key)))
            throw new ArgumentException("Cannot apply options that don't exist!", nameof(values));
        
        var targets = values.Select(x => new KeyValuePair<IOption, object>(_options[x.Key], x.Value));

        _supressEvents = true;
        foreach (var item in targets)
            item.Key.Value = item.Value;
        _supressEvents = false;

        OnOptionValuesChanged?.Invoke(targets.Select(x => x.Key).ToList());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IOption> GetEnumerator() => _options.Select(x => x.Value)
        .ToList()
        .GetEnumerator();
    
    private void Option_OnValueChanged(IOption option)
    {
        if (_supressEvents) return;
        OnOptionValuesChanged?.Invoke([option]);
    }
}
