using System;
using System.Collections;
using System.Collections.Generic;

namespace qASIC.Options;

/// <summary>A collection of <see cref="IOptionChangeListener"/>s.</summary>
public class OptionChangeListenerCollection : IEnumerable<IOptionChangeListener>
{
    private List<IOptionChangeListener> _list = [];

    /// <summary>Adds a new listener to the collection. If the listener is already added, it won't be added again.</summary>
    /// <param name="listener">Listener to add.</param>
    public void Add(IOptionChangeListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (!_list.Contains(listener))
            _list.Add(listener);
    }

    /// <summary>Checks if a listener is part of the collection.</summary>
    /// <param name="listener">Listener to check.</param>
    /// <returns>Returns true if the collection contains the listener.</returns>
    public bool Contains(IOptionChangeListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        return _list.Contains(listener);
    }

    /// <summary>Removes a listener from the collection if it exists.</summary>
    /// <param name="listener">Listener to remove</param>
    /// <returns>Returns true if the listener was successfully found and removed.</returns>
    public bool Remove(IOptionChangeListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        return _list.Remove(listener);
    }

    public IEnumerator<IOptionChangeListener> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
