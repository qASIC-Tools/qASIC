using System;
using System.Collections.Generic;

namespace qASIC.Options;

/// <summary>Holds options and their values.</summary>
public interface IOptionsList : IEnumerable<IOption>
{
    /// <summary>Retrieves an option.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <returns>Returns the option if found, otherwise <see cref="null"/>.</returns>
    IOption GetOption(string optionName);

    /// <summary>Checks if an option exists.</summary>
    /// <param name="optionName">The name of the option.</param>
    /// <returns>Returns true if the option exists.</returns>
    bool Contains(string optionName);

    /// <summary>Tries to retrieve an option from the list.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="result">The resulting option if found, otherwise <see cref="null"/>.</param>
    /// <returns>Returns true if it was successfull.</returns>
    bool TryGetOption(string optionName, out IOption result);

    /// <summary>Sets multiple option's values, used by <see cref="IOptionsListMask"/>.</summary>
    /// <param name="values">A collection containing option's names and the values they should be set to.</param>
    void ApplyOtherMask(IEnumerable<KeyValuePair<string, object>> values);

    /// <summary>Invoked when option values are changed.</summary>
    event Action<IEnumerable<IOption>> OnOptionValuesChanged;
}
