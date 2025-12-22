namespace qASIC.Options.Menu;

public class OptionsMenuSlider<T> : OptionsMenuItem<T>
{
    public OptionsMenuSlider(string name, string displayName, T minValue, T maxValue)
    {
        this.name = name;
        this.displayName = displayName;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public T minValue;
    public T maxValue;
}

public class OptionsMenuSliderFloat(string name, string displayName, float minValue, float maxValue) : OptionsMenuSlider<float>(name, displayName, minValue, maxValue)  { }

public class OptionsMenuSliderInt(string name, string displayName, int minValue, int maxValue) : OptionsMenuSlider<int>(name, displayName, minValue, maxValue)  { }
