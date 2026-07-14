using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Options;

/// <summary>Integrates many parts of the options system into one complete package.</summary>
public class qOptionsManager
{
    public qOptionsManager() : this(new qOptionsList()) { }
    public qOptionsManager(IOptionsList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        SavedList = list;
        List = new qOptionsListMask(list);

        SavedList.OnOptionValuesChanged += SavedList_OnOptionValuesChanged;
        List.OnOptionValuesChanged += List_OnOptionValuesChanged;
    }

    public qInstance Instance { get; set; }

    /// <summary>An options list containing values that are saved on disk.</summary>
    public IOptionsList SavedList { get; }
    /// <summary>The main options list used by the options manager.</summary>
    public qOptionsListMask List { get; }

    /// <summary>Used when writing changes to disk.</summary>
    public IOptionsSaveManager SaveManager { get; set; }
    /// <summary>Contains custom change listeners.</summary>
    public qOptionChangeListenerCollection ChangeListeners { get; set; }

    private void List_OnOptionValuesChanged(IEnumerable<IOption> options)
    {
        foreach (var item in ChangeListeners.ToList())
            item.HandleOptionValueChange(List, options);
    }

    private void SavedList_OnOptionValuesChanged(IEnumerable<IOption> options)
    {
        SaveManager?.Save(SavedList, options);
    }
}
