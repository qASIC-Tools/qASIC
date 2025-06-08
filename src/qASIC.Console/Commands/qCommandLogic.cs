using qASIC.CmdAutocomplete;

namespace qASIC.Console.Commands
{
    public abstract class qCommandLogic : ICommandLogic, ISupportsAutocomplete
    {
        public abstract string CommandName { get; }

        public virtual string[] Aliases => new string[0];

        public virtual string Description => null;

        public virtual string DetailedDescription => null;

        public virtual ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish();

        public object Run(qCommandContext context) =>
            Run(context as qConsoleCommandContext);

        public abstract object Run(qConsoleCommandContext context);
    }
}