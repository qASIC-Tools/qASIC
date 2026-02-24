using System;
using System.Collections;
using System.Collections.Generic;

namespace qASIC.Options;

[Serializable]
public class OptionsList : IEnumerable<KeyValuePair<string, OptionsList.ListItem>>
{
    private Dictionary<string, ListItem> Values { get; set; } = [];

    #region Dictionary
    public ListItem this[string key]
    {
        get => Values[OptionsManager.FormatKeyString(key)];
    }

    public int Count => Values.Count;

    /// <summary>Sets the value of a specified item.</summary>
    /// <param name="key">Name of the item.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="silent">When true, this method won't invoke any events.</param>
    public OptionsList Set(string key, object value)
    {
        key = OptionsManager.FormatKeyString(key);

        if (Values.TryGetValue(key, out var item))
        {
            item.value = value;
            return this;
        }

        Values.Add(key, new ListItem(key, value));
        return this;
    }

    public void Clear() =>
        Values.Clear();

    public bool ContainsKey(string key) =>
        Values.ContainsKey(OptionsManager.FormatKeyString(key));

    public IEnumerator<KeyValuePair<string, ListItem>> GetEnumerator() =>
        Values.GetEnumerator();

    public bool Remove(string key) =>
        Values.Remove(OptionsManager.FormatKeyString(key));

    /// <summary>Gets the value associated with the specified key.</summary>
    /// <returns><c>true</c> if the <see cref="OptionsList"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(string key, out ListItem value) =>
        Values.TryGetValue(OptionsManager.FormatKeyString(key), out value);

    IEnumerator IEnumerable.GetEnumerator() =>
        Values.GetEnumerator();
    #endregion

    /// <summary>Merge items of a different list into this one.</summary>
    /// <param name="list">List to merge with this one.</param>
    /// <param name="silent">When true, this method won't invoke any events.</param>
    public void MergeFromOther(OptionsList list)
    {
        foreach (var item in list)
        {
            //Set only value if item already exists
            if (ContainsKey(item.Value.name))
            {
                Set(item.Value.name, item.Value);
                continue;
            }

            //Set the entire item (including default) if doesn't exists
            Set(item.Value.name, item);
        }
    }

    [Serializable]
    public class ListItem(string name, object value, object defaultValue)
    {
        public ListItem(string name) : this(name, default, default) { }
        public ListItem(string name, object value) : this(name, value, value) { }

        /// <summary>Name of the item.</summary>
        public string name = name;
        public object value = value;
        public object defaultValue = defaultValue;

        public override string ToString() =>
            $"{name}: {value} (default: {defaultValue})";
    }
}
