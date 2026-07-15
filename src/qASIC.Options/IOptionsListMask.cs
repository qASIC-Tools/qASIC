using System.Collections.Generic;

namespace qASIC.Options;

/// <summary>Imitates a target <see cref="IOptionsList"/> while masking some of its values.</summary>
public interface IOptionsListMask
{
    /// <summary>The target <see cref="IOptionsList"/> that is being targetted by the mask.</summary>
    IOptionsList Target { get; }

    /// <summary>Applies all masked values to the target list.</summary>
    void ApplyMask();

    /// <summary>Removed all masked options and reverts their values to the ones in the target list.</summary>
    void RevertMask();

    /// <summary>Retrieves a mask</summary>
    /// <param name="optionName">Name of the option the mask is targetting</param>
    /// <returns>Returns the retrieved mask.</returns>
    qOptionMask GetMask(string optionName);
    
    /// <summary>Retrieves all masked options.</summary>
    /// <returns>Returns a collection containing all masked options in the mask.</returns>
    IEnumerable<qOptionMask> GetAllMasks();

    /// <summary>Checks if a mask exists in the list.</summary>
    /// <param name="optionName">Name of the option the mask is targetting.</param>
    /// <returns>Returns true if the mask exists.</returns>
    bool ContainsMask(string optionName);
    
    /// <summary>Adds a new masked option or changes the value of an existing one.</summary>
    /// <param name="optionName">Name of the option to mask.</param>
    /// <param name="value">The value of the masked option.</param>
    void AddMask(string optionName, object value);
    
    /// <summary>Removes a masked option from the list if it exists.</summary>
    /// <param name="optionName">Name of the option to remove.</param>
    /// <returns>Return true if the masked value existed and was successfullly removed.</returns>
    bool RemoveMask(string optionName);
}
