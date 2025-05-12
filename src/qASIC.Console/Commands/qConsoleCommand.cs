namespace qASIC.Console.Commands
{
    public abstract class qConsoleCommand : ICommand
    {
        public abstract string CommandName { get; }

        public virtual string[] Aliases => new string[0];

        public virtual string Description => null;

        public virtual string DetailedDescription => null;

        public object Run(CommandContext context) =>
            Run(context as ConsoleCommandContext);

        public abstract object Run(ConsoleCommandContext context);
    }
}