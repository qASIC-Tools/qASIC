using qASIC.Logging;

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
                consoleTarget.Console = Console;
                consoleTarget.LogOutput = LogOutput;
                consoleTarget.RunTaskResult = RunTaskResult;
                consoleTarget.CleanupLogger = CleanupLogger;
            }
        }

        public qConsole Console { get; set; }

        public bool LogOutput { get; set; } = true;

        /// <summary>
        /// If true, when a command returns a <see cref="System.Threading.Tasks.Task"/> while executing not asynchronously, the task will be executed in the background and console will return a null.
        /// </summary>
        public bool RunTaskResult { get; set; } = true;

        public bool CleanupLogger { get; set; } = true;
    }

    public class qConsoleContext
    {
        public qConsoleContext(string inputString, object previousValue = null)
        {
            this.inputString = inputString;
            this.previousValue = previousValue;
        }

        public virtual qConsoleCommandContext CreateCommandContext() =>
            new qConsoleCommandContext()
            {
                Console = Console,
            };

        public qConsoleParserData ParserData { get; set; }
        public qConsole Console { get; set; }
        public qLogManager Logs { get; set; }
        public qConsoleVariableList Variables { get; set; }

        public string inputString;
        public object previousValue;
    }

    public class qConsoleParserData
    {
        public qLogManager Logs { get; set; }
        public qConsoleVariableList Variables { get; set; }
    }
}