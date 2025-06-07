using qASIC.CmdAutocomplete;

namespace qASIC.Options.Commands
{
    public abstract class OptionsCommand : ICommandLogic, ISupportsAutocomplete
    {
        public OptionsCommand(OptionsManager manager)
        {
            Manager = manager;
        }

        protected OptionsManager Manager { get; private set; }

        public abstract string CommandName { get; }

        public virtual string[] Aliases { get; }

        public virtual string Description { get; }

        public virtual string DetailedDescription { get; }

        public virtual ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish();

        public abstract object Run(qCommandContext context);
    }
}