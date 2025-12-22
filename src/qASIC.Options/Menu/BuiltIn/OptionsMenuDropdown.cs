namespace qASIC.Options.Menu.BuiltIn;

public class OptionsMenuDropdown<T> : OptionsMenuItem<T>
{
    public OptionsMenuDropdown(string name, string displayName, params T[] values)
    {
        this.name = name;
        this.displayName = displayName;
        this.values = values;
    }

    public T[] values;
}

public class OptionsMenuDropdownString(string name, string displayName, params string[] values) : OptionsMenuDropdown<string>(name, displayName, values) { }

public class OptionsMenuDropdownFloat(string name, string displayName, params float[] values) : OptionsMenuDropdown<float>(name, displayName, values) { }

public class OptionsMenuDropdownInt(string name, string displayName, params int[] values) : OptionsMenuDropdown<int>(name, displayName, values) { }

public class OptionsMenuDropdownBool(string name, string displayName, params bool[] values) : OptionsMenuDropdown<bool>(name, displayName, values) { }
