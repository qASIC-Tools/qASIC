using System;
using System.Collections.Generic;

namespace qASIC.Options;

public interface IOptionsList : IEnumerable<IOption>
{
    /// <summary>Retrieves an option.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <returns>Returns the option if found, otherwise <see cref="null"/>.</returns>
    IOption GetOption(string optionName);

    /// <summary>Tries to retrieve an option from the list.</summary>
    /// <param name="optionName">Name of the option.</param>
    /// <param name="result">The resulting option if found, otherwise <see cref="null"/>.</param>
    /// <returns>Returns true if it was successfull.</returns>
    bool TryGetOption(string optionName, out IOption result);

    event Action<IOption> OnOptionValueChanged;
}
