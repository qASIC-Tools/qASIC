using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Options;

public class qOptionsListMask : IOptionsList, IOptionsListMask
{
    public qOptionsListMask(IOptionsList target)
    {
        Target = target;
        target.OnOptionValuesChanged += Target_OnOptionValuesChanged;
    }

    /// <inheritdoc/>
    public IOptionsList Target { get; set; }

    /// <inheritdoc/>
    public event Action<IEnumerable<IOption>> OnOptionValuesChanged;

    private bool _supressEvents;
    private Dictionary<string, qOptionMask> _masks = [];

    /// <inheritdoc/>
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
        
        var newMask = new qOptionMask(option, value);
        newMask.OnValueChanged += OptionMask_OnValueChanged;
        newMask.OnApply += OptionMask_OnApply;
        _masks.Add(optionName, newMask);
        
        if (!_supressEvents)
            OnOptionValuesChanged?.Invoke([newMask]);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public qOptionMask GetMask(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_masks.TryGetValue(optionName, out var mask))
            return mask;
        
        throw new ArgumentException($"Mask for option '{optionName}' doesn't exist!", nameof(optionName));
    }
    
    /// <inheritdoc/>
    public IEnumerable<qOptionMask> GetAllMasks() =>
        _masks.Select(x => x.Value).ToList();
    
    /// <inheritdoc/>
    public bool Contains(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        return Target.Contains(optionName);
    }

    /// <inheritdoc/>
    public bool ContainsMask(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        return _masks.ContainsKey(optionName);
    }

    /// <inheritdoc/>
    public IOption GetOption(string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);
        if (_masks.TryGetValue(optionName, out var optionMask))
            return optionMask;
        
        return Target.GetOption(optionName);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void ApplyMask()
    {
        var masksToApply = _masks.Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Value))
            .ToList();
        
        _masks.Clear();
        Target.ApplyOtherMask(masksToApply);
    }

    /// <inheritdoc/>
    public void RevertMask()
    {
        var masksToRevert = _masks.Select(x => x.Value)
            .ToList();

        _supressEvents = true;
        foreach (var item in masksToRevert)
            RemoveMask(item.OptionName);
        _supressEvents = false;
        
        OnOptionValuesChanged?.Invoke(masksToRevert);
    }

    /// <inheritdoc/>
    public void ApplyOtherMask(IEnumerable<KeyValuePair<string, object>> values)
    {
        if (values.Any(x => !Target.Contains(x.Key)))
            throw new ArgumentException("Cannot apply options that don't exist!", nameof(values));

        var existingMasks = values.Where(x => _masks.ContainsKey(x.Key))
            .Select(x => new KeyValuePair<qOptionMask, object>(_masks[x.Key], x.Value))
            .ToList();
        
        var newMasks = values.GroupBy(x => x.Key)
            .Select(x => x.Last())
            .Where(x => !_masks.ContainsKey(x.Key))
            .ToList();
        
        _supressEvents = true;
        foreach (var item in existingMasks) item.Key.Value = item.Value;
        foreach (var item in newMasks) AddMask(item.Key, item.Value);
        _supressEvents = false;

        OnOptionValuesChanged?.Invoke(values.Select(x => _masks[x.Key]).ToList());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IOption> GetEnumerator() =>
        Target.Select(x => _masks.TryGetValue(x.OptionName, out var val) ? val : x)
            .ToList()
            .GetEnumerator();
    
    private void Target_OnOptionValuesChanged(IEnumerable<IOption> options)
    {
        // Ignore masked options
        var targets = options.Where(x => !_masks.ContainsKey(x.OptionName))
            .ToList();

        if (targets.Count == 0) return;
        OnOptionValuesChanged?.Invoke(targets);
    }

    private void OptionMask_OnValueChanged(IOption option)
    {
        if (_supressEvents) return;
        OnOptionValuesChanged?.Invoke([option]);
    }

    private void OptionMask_OnApply(qOptionMask option)
    {
        option.OnValueChanged -= OptionMask_OnValueChanged;
        option.OnApply -= OptionMask_OnApply;
        _masks.Remove(option.OptionName);
    }
}
