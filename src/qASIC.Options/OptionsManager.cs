using System;
using System.Collections.Generic;

namespace qASIC.Options;

public class OptionsManager
{
    public OptionsManager() : this(new OptionsListMask(new OptionsList())) { }
    public OptionsManager(IOptionsList list)
    {
        List = list;
        BaseList = list;
        while (BaseList is IOptionsListMask mask)
            BaseList = mask.Target;
        
        ArgumentNullException.ThrowIfNull(List);
        ArgumentNullException.ThrowIfNull(BaseList);

        BaseList.OnOptionValuesChanged += BaseList_OnOptionValuesChanged;
    }

    public IOptionsList BaseList { get; }
    public IOptionsList List { get; private set; }

    public IOptionsSaveManager SaveManager { get; set; }

    private void BaseList_OnOptionValuesChanged(IEnumerable<IOption> options)
    {
        SaveManager?.Save(BaseList, options);
    }
}
