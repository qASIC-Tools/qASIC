using qASIC.CmdAutocomplete;
using System.Collections.Generic;

namespace qASIC.Console.Autocomplete;

public class ACCommandArgument(string name) : ACArgument(typeof(string), name)
{
    public override IEnumerable<string> GetAvaliableValues(IAutocompleteEngine engine) => (engine as AutocompleteEngine).Console.CommandList
        .GetSortedCommandNames();
}
