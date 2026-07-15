namespace qASIC.Options;

/// <summary>A variant of <see cref="IOptionsList"/> to which you can also add and remove items dynamically.</summary>
public interface IModifiableOptionsList : IOptionsList
{
    /// <summary>Adds a new option to the list.</summary>
    /// <param name="option">The new option.</param>
    void Add(IOption option);

    /// <summary>Removes an option.</summary>
    /// <param name="optionName">The name of the option.</param>
    /// <returns>Returns true if the option was successfully found and removed.</returns>
    bool Remove(string optionName);
}
