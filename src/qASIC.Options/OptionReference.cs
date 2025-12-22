namespace qASIC.Options;

public class OptionReference<T>(OptionsManager manager, string optionName)
{
    private readonly OptionsManager manager = manager;

    public string OptionName { get; } = optionName;

    public T Value
    {
        get => manager.GetOption<T>(OptionName);
        set => manager.SetOption(OptionName, value);
    }

    public static explicit operator T(OptionReference<T> reference) =>
        reference.Value;
}
