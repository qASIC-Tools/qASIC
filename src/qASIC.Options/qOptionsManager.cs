using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace qASIC.Options;

/// <summary>Integrates many parts of the options system into one complete package.</summary>
public class qOptionsManager : IService
{
    public qOptionsManager() : this(new qOptionsList()) { }
    public qOptionsManager(IOptionsList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        SavedList = list;
        List = new qOptionsListMask(list);

        SavedList.OnOptionValuesChanged += SavedList_OnOptionValuesChanged;
        List.OnOptionValuesChanged += List_OnOptionValuesChanged;
        RegisteredObjects.OnObjectRegistered += RegisteredObjects_OnObjectRegistered;
    }

    /// <inheritdoc/>
    public qInstance Instance
    {
        get;
        set
        {
            RegisteredObjects.SyncWithOther(field?.RegisteredObjects);
            field = value;
            RegisteredObjects.SyncWithOther(field?.RegisteredObjects);
        }
    }

    /// <summary>Collection of registered objects that will be used by the manager based on the interfaces they implement.</summary>
    public qRegisteredObjects RegisteredObjects { get; } = [];

    /// <summary>An options list containing values that are saved on disk.</summary>
    public IOptionsList SavedList { get; }
    /// <summary>The main options list used by the options manager.</summary>
    public qOptionsListMask List { get; }

    /// <summary>Used when writing changes to disk.</summary>
    public IOptionsSaveManager SaveManager { get; set; } = new qARKOptionsSaveManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "options.qark"));
    /// <summary>Contains custom change listeners.</summary>
    public qOptionChangeListenerCollection ChangeListeners { get; } = [];

    /// <summary>Registers custom options that an object might require.</summary>
    /// <param name="obj">Object from which to get custom options.</param>
    public void RegisterCustomOptions(IUsesCustomOptions obj) =>
        RegisterCustomOptions(obj.CustomOptions);
    
    /// <summary>Registers custom options: adds non-existing ones and ignores existing ones.</summary>
    /// <param name="options">Options to register.</param>
    /// <exception cref="Exception">Thrown when an option already exists, but uses a different value type.</exception>
    public void RegisterCustomOptions(IEnumerable<IOption> options)
    {
        if (SavedList is not IModifiableOptionsList modifiableList) return;

        foreach (var item in options)
        {
            // Ignore if option is already added
            if (SavedList.TryGetOption(item.OptionName, out var existingOption))
            {
                // Throw an exception if existing option uses a different
                // value type, since it might be user error
                if (existingOption.ValueType != item.ValueType)
                    throw new Exception("Error while registering custom options: an option with the same name, but different value type already exists!");
                
                continue;
            }

            modifiableList.Add(item);
        }
    }

    /// <summary>Loads the options list from disk.</summary>
    public void Load()
    {
        SaveManager.Load(List);
    }

    private void RegisteredObjects_OnObjectRegistered(object obj)
    {
        if (obj is IUsesCustomOptions options)
            RegisterCustomOptions(options);
    }

    private void List_OnOptionValuesChanged(IEnumerable<IOption> options)
    {
        var listeners = RegisteredObjects.OfType<IOptionChangeListener>()
            .Concat(ChangeListeners)
            .Distinct()
            .ToList();

        foreach (var item in listeners)
            item.HandleOptionValueChange(List, options);
    }

    private void SavedList_OnOptionValuesChanged(IEnumerable<IOption> options)
    {
        SaveManager?.Save(SavedList, options);
    }
}
