using System.Collections.Generic;

namespace qASIC.Options;

/// <summary>Allows you to register a custom listener in <see cref="qOptionChangeListenerCollection"/>.</summary>
public interface IOptionChangeListener
{
    /// <summary>Handles change value events from an options list.</summary>
    /// <param name="list">The list that invoked the event.</param>
    /// <param name="options">Collection of options that were changed.</param>
    void HandleOptionValueChange(IOptionsList list, IEnumerable<IOption> options);    
}
