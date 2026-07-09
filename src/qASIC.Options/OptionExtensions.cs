namespace qASIC.Options;

public static class OptionExtensions
{
    /// <summary>Tries to retrieve the option's value of the provided type.</summary>
    /// <typeparam name="T">The type of <see cref="Value"/>.</typeparam>
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
    /// <returns>Returns <see cref="Value"/> cast to type <see cref="T"/>.</returns>
    public static T GetValue<T>(this IOption option) =>
        (T)option.Value;
}
