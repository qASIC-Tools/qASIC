using System;
using System.Collections.Generic;

namespace qASIC.CmdAutocomplete;

/// <summary>Holds data for a command's argument</summary>
public class ACArgument(Type type, string name)
{
    public Type type = type;
    public string name = name;

    public virtual bool ValidateArg(IAutocompleteEngine engine, string arg) =>
        true;

    public virtual IEnumerable<string> GetAvaliableValues(IAutocompleteEngine engine) =>
        [];
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
