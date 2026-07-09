using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Options;

public sealed class OptionsListMask : IOptionsList
{
    public OptionsListMask(IOptionsList target)
    {
        Target = target;
        target.OnOptionValueChanged += Target_OnOptionValueChanged;
    }

    public IOptionsList Target { get; set; }
    public event Action<IOption> OnOptionValueChanged;

    private Dictionary<string, OptionMask> _masks = [];

    public void AddMask(string optionName, object value)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_masks.TryGetValue(optionName, out var mask))
        {
            mask.Value = value;
            return;
        }

        if (!Target.TryGetOption(optionName, out var option))
            throw new ArgumentException($"Option '{optionName}' doesn't exist in target option list!", nameof(optionName));
        
        var newMask = new OptionMask(option, value);
        newMask.OnValueChanged += OptionMask_OnValueChanged;
        newMask.OnApply += OptionMask_OnApply;
        _masks.Add(optionName, newMask);
        OnOptionValueChanged?.Invoke(newMask);
    }

    public bool RemoveMask(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);

        if (_masks.TryGetValue(optionName, out var mask))
        {
            mask.OnValueChanged -= OptionMask_OnValueChanged;
            mask.OnApply -= OptionMask_OnApply;
        }

        return _masks.Remove(optionName);
    }

    public IEnumerable<OptionMask> GetMasks() =>
        _masks.Select(x => x.Value).ToList();

    public IOption GetOption(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_masks.TryGetValue(optionName, out var optionMask))
            return optionMask;
        
        return Target.GetOption(optionName);
    }

    public bool TryGetOption(string optionName, out IOption result)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_masks.TryGetValue(optionName, out var optionMask))
        {
            result = optionMask;
            return true;
        }

        var val = Target.TryGetOption(optionName, out var option);
        result = option;
        return val;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IOption> GetEnumerator() =>
        Target.Select(x => _masks.TryGetValue(x.OptionName, out var val) ? val : x)
            .ToList()
            .GetEnumerator();
    
    private void Target_OnOptionValueChanged(IOption option)
    {
        // Ignore if mask exists
        if (_masks.ContainsKey(option.OptionName)) return;
        OnOptionValueChanged?.Invoke(option);
    }

    private void OptionMask_OnValueChanged(OptionMask option)
    {
        OnOptionValueChanged?.Invoke(option);
    }

    private void OptionMask_OnApply(OptionMask option)
    {
        option.OnValueChanged -= OptionMask_OnValueChanged;
        option.OnApply -= OptionMask_OnApply;
        _masks.Remove(option.OptionName);
    }
}
