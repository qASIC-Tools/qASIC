namespace qASIC.Console.Commands.BuiltIn
{
    public class Clear : GameCommand
    {
        public override string CommandName => "clear";
        public override string Description => "Clears the console.";
        public override string[] Aliases => new string[] { "cls", "clr" };

        public override object Run(GameCommandContext context)
        {
            context.CheckArgumentCount(0);
            context.Logs.Log(qLog.CreateNow(string.Empty, LogType.Clear, qColor.Clear));
            return null;
        }
    }
}