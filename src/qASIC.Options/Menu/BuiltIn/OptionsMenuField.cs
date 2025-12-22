namespace qASIC.Options.Menu;

public class OptionsMenuField<T> : OptionsMenuItem<T>
{
    public OptionsMenuField(string name, string displayName)
    {
        this.name = name;
        this.displayName = displayName;
    }
}

public class OptionsMenuFieldString(string name, string displayName) : OptionsMenuField<string>(name, displayName) { }

public class OptionsMenuFieldFloat(string name, string displayName) : OptionsMenuField<float>(name, displayName) { }

public class OptionsMenuFieldInt(string name, string displayName) : OptionsMenuField<int>(name, displayName) { }
