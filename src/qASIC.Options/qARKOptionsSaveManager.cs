using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using qASIC.qARK;

namespace qASIC.Options;

public class qARKOptionsSaveManager : IOptionsSaveManager
{
    public qARKSerializer Serializer { get; set; } = new();
    public string Path { get; set; }

    public virtual void Load(IOptionsList list)
    {
        if (!File.Exists(Path)) return;
        Deserialize(list, File.ReadAllText(Path));
    }

    public virtual async Task LoadAsync(IOptionsList list)
    {
        if (!File.Exists(Path)) return;
        Deserialize(list, await File.ReadAllTextAsync(Path));
    }

    public virtual void Save(IOptionsList list, IEnumerable<IOption> options)
    {
        if (string.IsNullOrWhiteSpace(Path)) return;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        File.WriteAllText(Path, Serialize(File.Exists(Path) ? File.ReadAllText(Path) : string.Empty, list, options)); 
    }

    public virtual async Task SaveAsync(IOptionsList list, IEnumerable<IOption> options)
    {
        if (string.IsNullOrWhiteSpace(Path)) return;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        await File.WriteAllTextAsync(Path, Serialize(File.Exists(Path) ? await File.ReadAllTextAsync(Path) : string.Empty, list, options)); 
    }

    protected string Serialize(string baseText, IOptionsList list, IEnumerable<IOption> options)
    {
        if (baseText == null)
        {
            var newDoc = new qARKDocument();

            foreach (var item in list.OrderBy(x => x.OptionName))
            {
                // if ()
            }

            return Serializer.Serialize(newDoc);
        }

        var doc = baseText != null ? Serializer.Deserialize(baseText) : null;
        foreach (var item in options)
        {
            
        }

        return Serializer.Serialize(doc);
    }

    protected void Deserialize(IOptionsList list, string txt)
    {
        var doc = Serializer.Deserialize(txt);
        var mask = new OptionsListMask(list);
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