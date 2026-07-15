using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using qASIC.qARK;

namespace qASIC.Options;

/// <summary>A save manager for the options system that uses qARK for serialization.</summary>
public class qARKOptionsSaveManager : IOptionsSaveManager
{
    /// <summary>Creates a new instance.</summary>
    public qARKOptionsSaveManager() { }
    /// <summary>Creates a new instance.</summary>
    /// <param name="path">Path to the save file</param>
    public qARKOptionsSaveManager(string path)
    {
        Path = path;
    }

    /// <summary>The qARK serializer that's used by the save manager.</summary>
    public qARKSerializer Serializer { get; set; } = new();
    /// <summary>Path to the save file.</summary>
    public string Path { get; set; }

    /// <inheritdoc/>
    public virtual void Load(IOptionsList list)
    {
        if (!File.Exists(Path)) return;
        Deserialize(list, File.ReadAllText(Path));
    }

    /// <inheritdoc/>
    public virtual void Save(IOptionsList list, IEnumerable<IOption> options)
    {
        if (string.IsNullOrWhiteSpace(Path)) return;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        File.WriteAllText(Path, Serialize(File.Exists(Path) ? File.ReadAllText(Path) : string.Empty, list, options)); 
    }

    /// <summary>Serializes changed values based on the existing save file. If no changes were made or a save file doesn't exist, all values on the list are serialized.</summary>
    /// <param name="baseText">The text from an existing save file. When it's set to <see cref="null"/>, the method assumes that there is no existing file and serializes the entire list.</param>
    /// <param name="list">The target options list.</param>
    /// <param name="options">Collection of changed options.</param>
    /// <returns>Returns the serialized result.</returns>
    protected string Serialize(string baseText, IOptionsList list, IEnumerable<IOption> options)
    {
        // If a file doesn't already exist or no options were changed,
        // serialize the entire list
        if (baseText == null || !options.Any())
        {
            var newDoc = new qARKDocument();

            foreach (var item in list.OrderBy(x => x.OptionName))
            {
                switch (item.Value)
                {
                    case IEnumerable enumerable:
                        newDoc.AddArrayEntryFromValues(item.OptionName, enumerable.OfType<object>());
                        break;
                    default:
                        newDoc.AddEntry(item.OptionName, item.Value);
                        break;
                }
            }

            return Serializer.Serialize(newDoc);
        }

        // Change only modified values
        var doc = baseText != null ? Serializer.Deserialize(baseText) : null;
        foreach (var item in options.OrderBy(x => x.OptionName))
        {
            switch (item.Value)
            {
                case IEnumerable enumerable:
                    doc.SetValues(item.OptionName, [..enumerable.OfType<object>()]);
                    break;
                default:
                    doc.SetValue(item.OptionName, item.Value);
                    break;
            }
        }

        return Serializer.Serialize(doc);
    }

    protected void Deserialize(IOptionsList list, string txt)
    {
        var doc = Serializer.Deserialize(txt);
        var mask = new qOptionsListMask(list);
        foreach (var item in list)
        {
            // If it's list
            if (item.ValueType.IsGenericType &&
                item.ValueType.GetGenericTypeDefinition() == typeof(List<>) &&
                item.ValueType.GetGenericArguments() is [ Type valType ])
            {
                var values = doc.GetValueArray(valType, item.OptionName);
                var newList = (IList)Activator.CreateInstance(item.ValueType);
                for (int i = 0; i < values.Count; i++)
                    newList.Add(values[i]);
                
                mask.AddMask(item.OptionName, newList);
                continue;
            }

            // If it's an array
            if (item.ValueType.IsArray)
            {
                var values = doc.GetValueArray(item.ValueType.GetElementType(), item.OptionName);
                var array = Array.CreateInstance(item.ValueType.GetElementType(), values.Count);
                for (int i = 0; i < values.Count; i++)
                    array.SetValue(values[i], i);

                mask.AddMask(item.OptionName, array);
                continue;
            }

            mask.AddMask(item.OptionName, doc.GetValue(item.OptionName, item.ValueType, item.DefaultValue));
        }

        mask.ApplyMask();
    }
}
