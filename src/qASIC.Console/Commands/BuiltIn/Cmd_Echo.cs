namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Echo : qBuiltinCommand
    {
        protected override string DefaultCommandName => "echo";
        protected override string DefaultDescription => "Echos a message.";
        protected override string[] DefaultAliases => new string[] { "print" };

        public override object Run(ConsoleCommandContext context)
        {
            context.CheckArgumentCount(1);
            context.Logs.Log(context[0].arg);
            return null;
        }
    }
}