using qASIC.CmdAutocomplete;

namespace qASIC.Options.Commands;

public abstract class OptionsCommand(OptionsManager manager) : ICommandLogic, ISupportsAutocomplete
{
    protected OptionsManager Manager { get; } = manager;

    public abstract string CommandName { get; }

    public virtual string[] Aliases { get; }

    public virtual string Description { get; }

    public virtual string DetailedDescription { get; }

    public virtual ACData CommandAutocomplete => new ACData()
        .AddVariant().Finish();

    public abstract object Run(qCommandContext context);
}
