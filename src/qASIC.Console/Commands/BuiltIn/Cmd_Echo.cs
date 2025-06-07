using qASIC.CmdAutocomplete;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Echo : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "echo";
        protected override string DefaultDescription => "Echos a message.";
        protected override string[] DefaultAliases => new string[] { "print" };

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().AddType<string>("message").Finish();

        public override object Run(qConsoleCommandContext context)
        {
            context.CheckArgumentCount(1);
            context.Logs.Log(context[0].arg);
            return null;
        }
    }
}