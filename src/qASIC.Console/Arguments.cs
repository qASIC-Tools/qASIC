namespace qASIC.Console
{
    public class GameCommandContext : CommandContext
    {
        public GameCommandContext() { }
        public GameCommandContext(CommandContext other) : base(other)
        { 
            if (other is GameCommandContext gameContext)
            {
                console = gameContext.console;
                LogOutput = gameContext.LogOutput;
            }
        }

        public GameConsole console;

        public bool LogOutput { get; set; } = true;
    }
}