using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Options;

/// <summary>Contains useful extensions for classes and interfaces related to the options system.</summary>
public static class qOptionExtensions
{
    /// <summary>Tries to retrieve the option's value of the provided type.</summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
    /// <param name="option">The target option.</option>
    /// <param name="result">The resulted value if successfull, otherwise <see cref="default"/>.</param>
    /// <returns>Returns true if <see cref="Value"/> is of type <see cref="T"/>.</returns>
    public static bool TryGetValue<T>(this IOption option, out T result)
    {
        if (option.Value is T val)
        {
            result = val;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Retrieves the option's value of the provided type.</summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
    /// <param name="option">The target option.</option>
    /// <returns>Returns <see cref="Value"/> cast to type <see cref="T"/>.</returns>
    public static T GetValue<T>(this IOption option) =>
        (T)option.Value;
    
    /// <summary>Registers an event that is invoked when <see cref="IOption.Value"/> is changed.</summary>
    /// <typeparam name="T">Type of value the option is holding</typeparam>
    /// <param name="option">The target option.</option>
    /// <param name="onValueChanged">The event to register.</param>
    public static void RegisterValueListener<T>(this IOption option, Action<T> onValueChanged)
    {
        option.OnValueChanged += (opt) => ValueListenerEventMethod(opt, onValueChanged);
    }

    /// <summary>Unregisters an event that was previously registered using <see cref="RegisterValueListener"/>.</summary>
    /// <typeparam name="T">Type of value the option is holding</typeparam>
    /// <param name="option">The target option.</option>
    /// <param name="onValueChanged">The event to unregister.</param>
    public static void UnregisterValueListener<T>(this IOption option, Action<T> onValueChanged)
    {
        option.OnValueChanged -= (opt) => ValueListenerEventMethod(opt, onValueChanged);
    }

    private static void ValueListenerEventMethod<T>(IOption option, Action<T> onValueChanged)
    {
        if (option.Value is T val)
            onValueChanged?.Invoke(val);
    }

    /// <summary>Retrieves a value of an option from a list.</summary>
    /// <param name="list">The target list.</param>
    /// <param name="optionName">Name of the option.</param>
    /// <returns>Returns the option's value.</returns>
    public static object GetValue(this IOptionsList list, string optionName) =>
        list.GetOption(optionName).Value;
    
    /// <summary>Tries to retrieve a value of an option from a list.</summary>
    /// <param name="list">The target list.</param>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="value">When successfull, the retrieved option value. Otherwise <see cref="null"/>.</param>
    /// <returns>Returns true, if the option was successfully found.</returns>
    public static bool TryGetValue(this IOptionsList list, string optionName, out object value)
    {
        if (list.TryGetOption(optionName, out var option))
        {
            value = option.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Retrieves a value of an option from a list.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="list">The target list.</param>
    /// <param name="optionName">Name of the option.</param>
    /// <returns>Returns the option's value.</returns>
    public static object GetValue<T>(this IOptionsList list, string optionName) =>
        list.GetOption(optionName).GetValue<T>();
    
    /// <summary>Tries to retrieve a value of an option from a list.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="list">The target list.</param>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="value">When successfull, the retrieved option value. Otherwise <see cref="null"/>.</param>
    /// <returns>Returns true, if the option was successfully found.</returns>
    public static bool TryGetValue<T>(this IOptionsList list, string optionName, out T value)
    {
        if (list.TryGetOption(optionName, out var option) &&
            option.TryGetValue(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Sets the value of an option.</summary>
    /// <param name="list">The target list.</param>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="value">The new value of option.</param>
    public static void SetValue(this IOptionsList list, string optionName, object value) =>
        list.GetOption(optionName).Value = value;
    
    /// <summary>Sets the value of an option mask and applies it's value.</summary>
    /// <param name="mask">The target list mask.</param>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="value">The new value of option.</param>
    public static void SetValueAndApply(this IOptionsListMask mask, string optionName, object value)
    {
        mask.AddMask(optionName, value);
        mask.GetMask(optionName).Apply();
    }
}
