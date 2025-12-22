namespace qASIC.Options.Commands;

public class RevertOptions(OptionsManager manager) : OptionsCommand(manager)
{
    public override string CommandName => "revertoptions";
    public override string[] Aliases => ["revertsettings", "optionsrevert", "settingsrevert"];
    public override string Description => "Loads options from disk while discarding any unsaved changes.";

    public override object Run(qCommandContext context)
    {
        context.CheckArgumentCount(0);
        Manager.Revert();
        return null;
    }
}
