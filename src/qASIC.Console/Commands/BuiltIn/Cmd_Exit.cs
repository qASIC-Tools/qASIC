using System;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Exit : qBuiltinCommand
    {
        protected override string DefaultCommandName => "exit";
        protected override string DefaultDescription => "Closes the application.";
        protected override string[] DefaultAliases => new string[] { "quit" };

        public event Action ExitMethod;

        public override object Run(ConsoleCommandContext context)
        {
            context.CheckArgumentCount(0);
            context.Logs.Log("Goodbye");

            if (ExitMethod != null)
            {
                ExitMethod?.Invoke();
                return null;
            }

            Environment.Exit(0);
            return null;
        }
    }
}