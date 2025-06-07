using qASIC.CmdAutocomplete;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Clear : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "clear";
        protected override string DefaultDescription => "Clears the console.";
        protected override string[] DefaultAliases => new string[] { "cls", "clr" };

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish();

        public override object Run(qConsoleCommandContext context)
        {
            context.CheckArgumentCount(0);
            context.Logs.Log(qLog.CreateNow(string.Empty, LogType.Clear, qColor.Clear));
            return null;
        }
    }
}