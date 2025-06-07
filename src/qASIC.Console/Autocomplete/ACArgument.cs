using System;
using System.Collections.Generic;

namespace qASIC.Console.Autocomplete
{
    /// <summary>Holds data for a command's argument</summary>
    public class ACArgument
    {
        public ACArgument(Type type, string name)
        {
            this.type = type;
            this.name = name;
        }

        public Type type;
        public string name;

        public virtual bool ValidateArg(AutocompleteEngine engine, string arg) =>
            true;

        public virtual IEnumerable<string> GetAvaliableValues(AutocompleteEngine engine) =>
            Array.Empty<string>();
    }

    public class ACOptionArgument : ACArgument
    {
        public ACOptionArgument(Type type, string name, params string[] options) : base(type, name)
        {
            this.options = new List<string>(options);
        }

        public List<string> options;

        public override bool ValidateArg(AutocompleteEngine engine, string arg) =>
            options.Contains(arg);

        public override IEnumerable<string> GetAvaliableValues(AutocompleteEngine engine) =>
            options;
    }

    public class ACCommandArgument : ACArgument
    {
        public ACCommandArgument(string name) : base(typeof(string), name) { }

        public override IEnumerable<string> GetAvaliableValues(AutocompleteEngine engine) => engine.Console.CommandList
            .GetSortedCommandNames();
    }
}
