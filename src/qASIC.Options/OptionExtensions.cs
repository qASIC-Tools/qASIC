using System;

namespace qASIC.Options;

/// <summary>Contains useful extensions for classes and interfaces related to the options system.</summary>
public static class OptionExtensions
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
}
