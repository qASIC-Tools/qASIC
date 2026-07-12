using System;

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
    }

    public IOptionsList BaseList { get; }

    public IOptionsList List { get; private set; }
}
