namespace qASIC.Options;

/// <summary>This interface is used for registering options without the use of reflections. It contains a method for registering custom settings that an object would require.</summary>
public interface ICanRegisterOptions
{
    /// <summary>Registers its own custom options.</summary>
    void RegisterOptions(OptionsList options);
}
