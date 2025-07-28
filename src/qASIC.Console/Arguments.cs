namespace qASIC.Console
{
    public class qConsoleCommandContext : qCommandContext
    {
        public qConsoleCommandContext() { }
        public qConsoleCommandContext(qCommandContext other) : base(other) { }

        public override void CopyTo(qCommandContext target)
        {
            base.CopyTo(target);
            if (target is qConsoleCommandContext consoleTarget)
            {
                consoleTarget.console = console;
                consoleTarget.LogOutput = LogOutput;
                consoleTarget.RunTaskResult = RunTaskResult;
                consoleTarget.CleanupLogger = CleanupLogger;
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