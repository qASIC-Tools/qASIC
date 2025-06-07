using System;
using System.Collections.Generic;

namespace qASIC.CmdAutocomplete
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

        public virtual bool ValidateArg(IAutocompleteEngine engine, string arg) =>
            true;

        public virtual IEnumerable<string> GetAvaliableValues(IAutocompleteEngine engine) =>
            Array.Empty<string>();
    }

    public class ACOptionArgument : ACArgument
    {
        public ACOptionArgument(Type type, string name, params string[] options) : base(type, name)
        {
            this.options = new List<string>(options);
        }

        public List<string> options;

        public override bool ValidateArg(IAutocompleteEngine engine, string arg) =>
            options.Contains(arg);

        public override IEnumerable<string> GetAvaliableValues(IAutocompleteEngine engine) =>
            options;
    }
}
