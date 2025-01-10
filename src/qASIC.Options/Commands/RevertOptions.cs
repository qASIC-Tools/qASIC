namespace qASIC.Options.Commands
{
    public class RevertOptions : OptionsCommand
    {
        public RevertOptions(OptionsManager manager) : base(manager) { }

        public override string CommandName => "revertoptions";
        public override string[] Aliases => new string[] { "revertsettings", "optionsrevert", "settingsrevert" };
        public override string Description => "Loads options from disk while discarding any unsaved changes.";

        public override object Run(CommandContext context)
        {
            context.CheckArgumentCount(0);
            Manager.Revert();
            return null;
        }
    }
}