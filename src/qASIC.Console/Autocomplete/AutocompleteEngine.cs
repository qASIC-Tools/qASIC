using qASIC.CmdAutocomplete;

namespace qASIC.Console.Autocomplete;

public abstract class AutocompleteEngine(qConsole console) : IAutocompleteEngine
{
    public qConsole Console { get; set; } = console;

    public abstract (string, int) Autocomplete(string cmd, int cursorPosition);
}
