namespace qASIC.Console
{
    public class ConsoleCommandContext : CommandContext
    {
        public ConsoleCommandContext() { }
        public ConsoleCommandContext(CommandContext other) : base(other)
        { 
            if (other is ConsoleCommandContext gameContext)
            {
                console = gameContext.console;
                LogOutput = gameContext.LogOutput;
            }
        }

        public qConsole console;

        public bool LogOutput { get; set; } = true;

        /// <summary>
        /// If true, when a command returns a <see cref="System.Threading.Tasks.Task"/> while executing not asynchronously, the task will be executed in the background and console will return a null.
        /// </summary>
        public bool RunTaskResult { get; set; } = true;

        public bool CleanupLogger { get; set; } = true;
    }
}