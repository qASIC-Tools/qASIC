using System.Collections.Generic;

namespace qASIC.Options;

/// <summary>Responsible for saving the options list.</summary>
public interface IOptionsSaveManager
{
    /// <summary>Saves an options list to disk.</summary>
    /// <param name="list">The option list to save.</param>
    /// <param name="options">Collection of options that were changed. You can use this to only update modified values.</param>
    void Save(IOptionsList list, IEnumerable<IOption> options);
    /// <summary>Loads an options list's values from disk.</summary>
    /// <param name="list">The option list that the method should set values on.</param>
    void Load(IOptionsList list);
}
