namespace qASIC.Options;

public interface IOption
{
    string OptionName { get; }
    object DefaultValue { get; }
    object Value { get; set; }
}
