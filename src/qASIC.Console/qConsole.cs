using System.Diagnostics;
using System.Reflection;
using System;
using System.Threading.Tasks;
using System.Linq;
using qASIC.CommandPrompts;
using qASIC.Console.Commands;
using qASIC.Console.Parsing;
using qASIC.Console.Logging;
using qASIC.Logging;

namespace qASIC.Console
{
    [qSkipLogModifiers]
    public class qConsole : IService
    {
        public const string SYSTEM_NAME = "qASIC.Console";
        public const string SYSTEM_VERSION = "1.0.0";

        public qConsole(ICommandList commandList = null, ArgumentsParser parser = null) :
            this(Guid.NewGuid().ToString(), commandList, parser)
        { }

        public qConsole(string name, ICommandList commandList = null, ArgumentsParser parser = null)
        {
            Logs = new qSavableLogManager()
            {
                LogModifiers = new System.Collections.Generic.List<qLogModifier>()
                {
                    new LOGMOD_Color(),
                    new LOGMOD_Prefix(),
                    new LOGMOD_Tag(),
                },
            };

            Name = name;
            CommandList = commandList ?? new qCommandList()
                .AddBuiltInCommands()
                .FindCommands()
                .FindAttributeCommands();

            CommandParser = parser ?? new QuashParser();

            qDebug.OnLog += QDebug_OnLog;
        }

        private void QDebug_OnLog(qLog log)
        {
            if (LogQDebug)
                Log(log);
        }

        /// <summary>Main static instance of <see cref="qConsole"/> that was set using <see cref="SetAsMain"/>.</summary>
        public static qConsole Main { get; private set; }
        /// <summary>Sets this instance as main to make it accessible from property <see cref="Main"/>.</summary>
        /// <returns>Returns itself.</returns>
        public qConsole SetAsMain()
        {
            Main = this;
            return this;
        }

        /// <summary>Changes <see cref="Main"/> to null if it's set to this instance.</summary>
        /// <returns>Returns itself.</returns>
        public qConsole UnsetAsMain()
        {
            if (Main == this)
                Main = null;

            return this;
        }

        private qInstance _instance;
        public qInstance Instance
        {
            get => _instance;
            set
            {
                var oldGetLogsFromInstance = GetLogsFromInstance;
                GetLogsFromInstance = false;

                Targets.StopSyncingWithOther(_instance?.RegisteredObjects);
                _instance = value;
                Targets.SyncWithOther(_instance?.RegisteredObjects);

                GetLogsFromInstance = oldGetLogsFromInstance;
            }
        }

        public string Name { get; set; }

        public qSavableLogManager Logs { get; set; }

        public ICommandList CommandList { get; set; }

        public ArgumentsParser CommandParser { get; set; }

        public qConsoleTheme Theme { get; set; } = qConsoleTheme.Default;

        /// <summary>Should the console log messages from <see cref="qDebug"/>.</summary>
        public bool LogQDebug { get; set; } = true;

        /// <summary>Determines if console should try looking for attributes that can change log messages and colors.</summary>
        public bool UseLogModifierAttributes { get; set; } = true;

        /// <summary>Determines if it should include exceptions when logging normal errors with executing commands.</summary>
        public bool IncludeStackTraceInCommandExceptions { get; set; } = false;

        /// <summary>Determines if it should include exceptions when logging unknown errors with executing commands.</summary>
        public bool IncludeStackTraceInUnknownCommandExceptions { get; set; } = true;

        #region Registering targets
        public qRegisteredObjects Targets { get; private set; } = new qRegisteredObjects();
        #endregion

        #region Executing
        /// <summary>Can the console execute commands using <see cref="Execute(string)"/>.</summary>
        public bool CanParseAndExecute =>
            CommandList != null && CommandParser != null;

        /// <summary>Can the console execute commands using <see cref="Execute(qCommandArgument[])"/>.</summary>
        public bool CanExecute =>
            CommandList != null;

        /// <summary>Executes a command.</summary>
        /// <param name="cmd">Command text that will be parsed and executed.</param>
        public object Execute(string cmd, object previousValue = null) =>
            Execute(CreateContext(cmd, previousValue));

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="cmd">Command text that will be parsed and executed.</param>
        public async Task<object> ExecuteAsync(string cmd, object previousValue = null) =>
            await ExecuteAsync(CreateContext(cmd, previousValue));

        private bool PreprocessContext(qConsoleCommandContext context)
        {
            //Prompt
            if (context.prompt != null)
            {
                context.parser = CommandParser.ValueParser;

                if (!context.prompt.CanExecute(context))
                    return false;

                context.args = context.prompt.Prepare(context);
                return true;
            }

            LogUserInput(context);

            //Normal
            if (CommandList == null)
                throw new Exception("Cannot execute commands with no command list!");

            if (context.Logs == null)
                context.Logs = new qLogManager();

            bool registerLogs = context.LogOutput;
            if (registerLogs)
                Logs.RegisterManager(context.Logs);

            if (!CommandList.TryGetCommand(context.commandName, out var command))
            {
                context.Logs.LogError($"Command {context.commandName} doesn't exist");
                Logs.UnregisterManager(context.Logs);
                return false;
            }

            context.command = command;

            return true;
        }

        private void LogUserInput(qConsoleCommandContext context)
        {
            var message = context.inputString;
            if (message == null)
                message = CommandParser == null ?
                    $"{context.commandName} {string.Join(" ", context.args.Select(x => x.arg))}" :
                    CommandParser.ConvertToString(context.commandName, context.args);

            Log(qLog.CreateNow(message, LogType.User, "user_input"));
        }

        private object PostprocessContext(qConsoleCommandContext context, object returnedValue)
        {
            var closeLogs = true;
            if (context.RunTaskResult && returnedValue is Task task)
            {
                closeLogs = false;
                var cmdName = context.commandName;
                Task.Run(async () =>
                {
                    var val = await ExecuteAsync(cmdName, task, context.Logs, false);
                    PostprocessContext(context, val);
                });
            }

            if (returnedValue is CommandPrompt prompt)
            {
                prompt.context = context;
                return returnedValue;
            }

            if (closeLogs && context.CleanupLogger)
            {
                Logs.UnregisterManager(context.Logs);
                context.Logs.Close();
            }

            context.Logs = null;
            return returnedValue;
        }

        /// <summary>Executes a command.</summary>
        /// <param name="context">Command arguments.</param>
        public object Execute(qConsoleCommandContext context)
        {
            //Before
            if (!PreprocessContext(context))
                return null;

            //Executing
            var returnedValue = Execute(context.command.CommandName, () => context.command.Run(context), context.Logs);

            //After
            return PostprocessContext(context, returnedValue);
        }

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="context">Command arguments.</param>
        public async Task<object> ExecuteAsync(qConsoleCommandContext context)
        {
            //Before
            if (!PreprocessContext(context))
                return null;

            //Executing
            var returnedValue = Execute(context.command.CommandName, () => context.command.Run(context), context.Logs);
            if (returnedValue is Task task)
                returnedValue = await ExecuteAsync(context.command.CommandName, task, context.Logs);

            //After
            return PostprocessContext(context, returnedValue);
        }

        /// <summary>Executes a command.</summary>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="command">Command code to execute.</param>
        /// <param name="logOutput">When true, it will log the output value to the console.</param>
        public object Execute(string commandName, Func<object> command, qLogManager logs = null, bool logOutput = true)
        {
            try
            {
                var output = command.Invoke();
                if (logOutput && output != null && !(output is CommandPrompt) && !(output is Task))
                    logs?.Log(output.ToString());

                return output;
            }
            catch (qCommandException e)
            {
                logs?.LogError(e.ToString(IncludeStackTraceInCommandExceptions));
            }
            catch (Exception e)
            {
                logs?.LogError(IncludeStackTraceInUnknownCommandExceptions ?
                    $"There was an error while executing command '{commandName}': {e}" :
                    $"There was an error while executing command '{commandName}'.");
            }

            return null;
        }

        public async Task<object> ExecuteAsync(string commandName, Func<Task> command, qLogManager logs = null, bool logOutput = true) =>
            await ExecuteAsync(commandName, command.Invoke(), logs, logOutput);

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="command">Command task to execute.</param>
        /// <param name="logOutput">When true, it will log the output value to the console.</param>
        public async Task<object> ExecuteAsync(string commandName, Task task, qLogManager logs = null, bool logOutput = true)
        {
            try
            {
                if (!(task is Task<object> objTask))
                {
                    await task;
                    return null;
                }

                var output = await objTask;

                if (logOutput && output != null && !(output is CommandPrompt))
                    logs?.Log(output.ToString());

                return output;
            }
            catch (qCommandException e)
            {
                logs?.LogError(e.ToString(IncludeStackTraceInCommandExceptions));
            }
            catch (Exception e)
            {
                logs?.LogError(IncludeStackTraceInUnknownCommandExceptions ?
                    $"There was an error while executing command '{commandName}': {e}" :
                    $"There was an error while executing command '{commandName}'.");
            }

            return null;
        }

        public virtual qConsoleCommandContext CreateContext(string cmd, object previousValue)
        {
            if (previousValue is CommandPrompt prompt &&
                prompt.context is qConsoleCommandContext promptContext)
            {
                promptContext.prompt = prompt;
                promptContext.inputString = cmd;
                promptContext.args = prompt.ParseArguments ?
                    ParseCommandArguments(cmd, previousValue) :
                    prompt.Prepare(prompt.context);

                if (prompt.ParseArguments)
                    promptContext.commandName = ParseCommandName(cmd, previousValue);

                return promptContext;
            }

            var context = new qConsoleCommandContext()
            {
                inputString = cmd,
                commandName = ParseCommandName(cmd, previousValue),
                args = ParseCommandArguments(cmd, previousValue),
                console = this,
            };

            return context;
        }

        protected qCommandArgument[] ParseCommandArguments(string cmd, object returnedValue)
        {
            if (CommandParser == null)
                throw new Exception("Cannot parse commands with no parser!");

            var args = CommandParser.ParseArguments(cmd);

            return args;
        }

        protected string ParseCommandName(string cmd, object returnedValue)
        {
            if (CommandParser == null)
                throw new Exception("Cannot parse commands with no parser!");

            return CommandParser.ParseCommandName(cmd);
        }
        #endregion

        #region Registering Loggables
        private bool _getLogsFromInstance = true;
        /// <summary>Whenever to log messages from <see cref="Instance"/>.</summary>
        public bool GetLogsFromInstance
        {
            get => _getLogsFromInstance;
            set
            {
                if (_getLogsFromInstance == value) return;
                _getLogsFromInstance = value;

                if (_instance == null) return;

                switch (_getLogsFromInstance)
                {
                    case true:
                        Logs.RegisterLoggable(_instance);
                        break;
                    case false:
                        Logs.UnregisterLoggable(_instance);
                        break;
                }
            }
        }
        #endregion

        #region Logging
        /// <summary>Logs a message to the console.</summary>
        /// <param name="message">Message to log.</param>
        /// <param name="stackTraceIndex">Index used for gathering log customization attributes.</param>
        public void Log(string message) =>
            Log(qLog.CreateNow(message, qDebug.DEFAULT_TAG));

        /// <summary>Logs a warning message to the console.</summary>
        /// <param name="message">Message to log.</param>
        /// <param name="stackTraceIndex">Index used for gathering log customization attributes.</param>
        public void LogWarning(string message) =>
            Log(qLog.CreateNow(message, qDebug.WARNING_TAG));

        /// <summary>Logs an error message to the console.</summary>
        /// <param name="message">Message to log.</param>
        /// <param name="stackTraceIndex">Index used for gathering log customization attributes.</param>
        public void LogError(string message) =>
            Log(qLog.CreateNow(message, qDebug.ERROR_TAG));

        /// <summary>Logs a message to the console with a color.</summary>
        /// <param name="message">Message to log.</param>
        /// <param name="color">Message color.</param>
        /// <param name="stackTraceIndex">Index used for gathering log customization attributes.</param>
        public void Log(string message, qColor color) =>
            Log(qLog.CreateNow(message, color));

        /// <summary>Logs a message to the console with a color.</summary>
        /// <param name="message">Message to log.</param>
        /// <param name="tag">Message tag.</param>
        /// <param name="stackTraceIndex">Index used for gathering log customization attributes.</param>
        public void Log(string message, string tag) =>
            Log(qLog.CreateNow(message, tag));

        /// <summary>Logs a log to the console.</summary>
        /// <param name="stackTraceIndex">Index used for gathering log customization attributes.</param>
        /// <param name="useLogModifiers">If true, the console will check for color attributes.</param>
        public void Log(qLog log)
        {
            Logs.Log(log);
        }

        /// <summary>Clears the console. Previous logs will still be there, but they won't show up in the output.</summary>
        public void Clear() =>
            Log(qLog.CreateNow(string.Empty, LogType.Clear, qDebug.DEFAULT_TAG));

        public qColor GetLogColor(qLog log) =>
            Theme.GetLogColor(log);
        #endregion
    }
}