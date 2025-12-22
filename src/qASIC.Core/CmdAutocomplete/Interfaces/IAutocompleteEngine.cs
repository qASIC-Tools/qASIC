namespace qASIC.CmdAutocomplete;

public interface IAutocompleteEngine
{
    (string, int) Autocomplete(string cmd, int cursorPosition);
}
