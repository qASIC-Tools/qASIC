using qASIC.Console.Autocomplete;

namespace qASIC.Console.Commands
{
    public abstract class qCommandLogic : ICommandLogic, ISupportsAutocomplete
    {
        public abstract string CommandName { get; }

        public virtual string[] Aliases => new string[0];

        public virtual string Description => null;

        public virtual string DetailedDescription => null;

        public virtual ACData CommandAutocomplete { get; protected set; }

        public object Run(qCommandContext context) =>
            Run(context as qConsoleCommandContext);

        public abstract object Run(qConsoleCommandContext context);
    }
}