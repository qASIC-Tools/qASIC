namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Echo : qConsoleCommand
    {
        public override string CommandName => "echo";
        public override string Description => "Echos a message.";
        public override string[] Aliases => new string[] { "print" };

        public override object Run(GameCommandContext context)
        {
            context.CheckArgumentCount(1);
            context.Logs.Log(context[0].arg);
            return null;
        }
    }
}