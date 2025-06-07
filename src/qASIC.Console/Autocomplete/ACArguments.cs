using qASIC.CmdAutocomplete;
using System.Collections.Generic;

namespace qASIC.Console.Autocomplete
{
    public class ACCommandArgument : ACArgument
    {
        public ACCommandArgument(string name) : base(typeof(string), name) { }

        public override IEnumerable<string> GetAvaliableValues(IAutocompleteEngine engine) => (engine as AutocompleteEngine).Console.CommandList
            .GetSortedCommandNames();
    }
}
