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

        public qConsole(ICommandList commandList = null, ConsoleParser parser = null) :
            this(Guid.NewGuid().ToString(), commandList, parser)
        { }

        public qConsole(string name, ICommandList commandList = null, ConsoleParser parser = null)
        {
            Logs = new qSavableLogManager()
            {
                LogModifiers = new System.Collections.Generic.List<qLogModifier>()
                {
                    new Logmod_Color(),
                    new Logmod_Prefix(),
                    new Logmod_Tag(),
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

        public ConsoleParser CommandParser { get; set; }

        public qConsoleVariableList Variables { get; private set; } = new qConsoleVariableList();

        public qConsoleTheme Theme { get; set; } = qConsoleTheme.Default;

        /// <summary>Should the console log messages from <see cref="qDebug"/>.</summary>
        public bool LogQDebug { get; set; } = true;

        /// <summary>Determines if it should include exceptions when logging normal errors with executing commands.</summary>
        public bool IncludeStackTraceInCommandExceptions { get; set; } = false;

        /// <summary>Determines if it should include exceptions when logging unknown errors with executing commands.</summary>
        public bool IncludeStackTraceInUnknownCommandExceptions { get; set; } = true;

        #region Registering targets
        public qRegisteredObjects Targets { get; private set; } = new qRegisteredObjects();
        #endregion

        /// <summary>Can the console execute commands using <see cref="Execute(string)"/>.</summary>
        public bool CanParseAndExecute =>
            CommandList != null && CommandParser != null;

        /// <summary>Can the console execute commands using <see cref="Execute(qCommandArgument[])"/>.</summary>
        public bool CanExecute =>
            CommandList != null;

        #region Execute
        /// <summary>Executes a string.</summary>
        /// <param name="inputString">Command input that will be parsed and executed.</param>
        /// <param name="previousValue">Previously returned value by this method.</param>
        /// <returns>Passes the returned value from executing. This will either be a value of a command, a <see cref="Task"/>
        /// that is executing asynchronous commands or a <see cref="CommandPrompt"/> that's used to make commands interactive. 
        /// Make sure to store this value and pass it in <paramref name="previousValue"/> when executing this method again.</returns>
        public object Execute(string inputString, object previousValue = null) =>
            Execute(new qConsoleContext(inputString, previousValue));

        /// <summary>Executes a string asynchronously.</summary>
        /// <param name="inputString">Command input that will be parsed and executed.</param>
        /// <param name="previousValue">Previously returned value by this method.</param>
        /// <returns>Returns a task with the returned value from executing. This will either be a value of a command or a 
        /// <see cref="CommandPrompt"/> that's used to make commands interactive. Make sure to store this value and pass 
        /// it in <paramref name="previousValue"/> when executing this method again.</returns>
        public async Task<object> ExecuteAsync(string inputString, object previousValue = null) =>
            await ExecuteAsync(new qConsoleContext(inputString, previousValue));

        /// <summary>Executes a string.</summary>
        /// <param name="context">Context used by the console to run commands.</param>
        /// <returns>Passes the returned value from executing. This will either be a value of a command, a <see cref="Task"/>
        /// that is executing asynchronous commands or a <see cref="CommandPrompt"/> that's used to make commands interactive. 
        /// Make sure to store this value and pass it in <paramref name="previousValue"/> when executing this method again.</returns>
        public object Execute(qConsoleContext context)
        {
            if (!PreprocessConsoleContext(context))
                return context.previousValue;

            var returnedValue = CommandParser.ExecuteParser(context);
            return PostProcessConsoleContext(context, returnedValue);
        }

        /// <summary>Executes a string asynchronously.</summary>
        /// <param name="context">Context used by the console to run commands.</param>
        /// <returns>Passes the returned value from executing. This will either be a value of a command, a <see cref="Task"/>
        /// that is executing asynchronous commands or a <see cref="CommandPrompt"/> that's used to make commands interactive. 
        /// Make sure to store this value and pass it in <paramref name="previousValue"/> when executing this method again.</returns>
        public async Task<object> ExecuteAsync(qConsoleContext context)
        {
            if (!PreprocessConsoleContext(context))
                return context.previousValue;

            var returnedValue = await CommandParser.ExecuteParserAsync(context);
            return PostProcessConsoleContext(context, returnedValue);
        }

        /// <summary>Invoked before executing an input string in <see cref="Execute(qConsoleContext)"/> and 
        /// <see cref="ExecuteAsync(qConsoleContext)"/>.</summary>
        /// <param name="context">Context from the afformentioned methods.</param>
        /// <returns>Returns whenever the string should be executed.</returns>
        protected virtual bool PreprocessConsoleContext(qConsoleContext context)
        {
            context.Console = this;

            if (context.previousValue is CommandPrompt prompt &&
                prompt.ParserData is qConsoleParserData parserData)
            {
                context.ParserData = parserData;
            }

            //Assign Logs to context
            context.Logs ??= context.ParserData?.Logs;
            if (context.Logs == null)
            {
                context.Logs = new qLogManager();
                Logs.Register(context.Logs);
            }

            //Assign Variables to context
            context.Variables = context.ParserData?.Variables ?? new qConsoleVariableList(Variables);

            LogUserInput(context);
            return true;
        }

        /// <summary>Invoked after executing an input string in <see cref="Execute(qConsoleContext)"/> and 
        /// <see cref="ExecuteAsync(qConsoleContext)"/>.</summary>
        /// <param name="context">Context from the afformentioned methods.</param>
        /// <param name="returnedValue">Returned value from running the string.</param>
        /// <returns>Returns <paramref name="returnedValue"/>. This will be used as the returned value by
        /// the afformentioned methods.</returns>
        protected virtual object PostProcessConsoleContext(qConsoleContext context, object returnedValue) =>
            returnedValue;
        #endregion

        private void LogUserInput(qConsoleContext context)
        {
            if (!(context.previousValue is CommandPrompt))
                context.Logs.Log(qLog.CreateNow(context.inputString, LogType.User, "user_input"));
        }

        #region ExecuteCode
        /// <summary>Executes command code.</summary>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="command">Code to execute.</param>
        /// <param name="logOutput">When true, it will log the output value to <paramref name="logs"/>.</param>
        public object ExecuteCode(string commandName, Func<object> command, qLogManager logs = null, bool logOutput = true)
        {
            try
            {
                var output = command.Invoke();
                if (logOutput && output != null && !(output is CommandPrompt) && !(output is Task))
                    logs?.Log(output.ToString());

                return output;
            }
            catch (Exception e)
            {
                var logMsg = LogMessage_Exception(commandName, e);
                if (logMsg != null)
                    logs?.LogError(logMsg);
            }

            return null;
        }

        public async Task<object> ExecuteCodeAsync(string commandName, Func<Task> command, qLogManager logs = null, bool logOutput = true) =>
            await ExecuteCodeAsync(commandName, command.Invoke(), logs, logOutput);

        /// <summary>Executes command code asynchronously.</summary>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="command">Task to execute.</param>
        /// <param name="logOutput">When true, it will log the output value to <paramref name="logs"/>.</param>
        public async Task<object> ExecuteCodeAsync(string commandName, Task task, qLogManager logs = null, bool logOutput = true)
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
            catch (Exception e)
            {
                var logMsg = LogMessage_Exception(commandName, e);
                if (logMsg != null)
                    logs?.LogError(logMsg);
            }

            return null;
        }
        #endregion

        #region Execute Command
        /// <summary>Executes a command.</summary>
        /// <param name="context">Command arguments.</param>
        public object ExecuteCommand(qConsoleCommandContext context)
        {
            //Before
            if (!PreprocessCommandContext(context))
                return null;

            //Executing
            var returnedValue = ExecuteCode(context.command.CommandName, () => context.command.Run(context), context.Logs);

            //After
            return PostprocessCommandContext(context, returnedValue);
        }

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="context">Command arguments.</param>
        public async Task<object> ExecuteCommandAsync(qConsoleCommandContext context)
        {
            //Before
            if (!PreprocessCommandContext(context))
                return null;

            //Executing
            var returnedValue = ExecuteCode(context.command.CommandName, () => context.command.Run(context), context.Logs);
            if (returnedValue is Task task)
                returnedValue = await ExecuteCodeAsync(context.command.CommandName, task, context.Logs);

            //After
            return PostprocessCommandContext(context, returnedValue);
        }

        protected virtual bool PreprocessCommandContext(qConsoleCommandContext context)
        {
            context.Parser = CommandParser.ValueParser;

            //Prompt
            if (context.prompt != null)
            {
                if (!context.prompt.CanExecute(context))
                    return false;

                context.prompt.Prepare(context);
                return true;
            }

            //Standard
            if (CommandList == null)
                throw new Exception("Cannot execute commands with no command list!");

            context.LogOutput = true;
            if (context.Logs == null)
            {
                context.Logs = new qLogManager();
                Logs.Register(context.Logs);
            } 

            if (!CommandList.TryGetCommand(context.commandName, out var command))
            {
                var logMsg = LogMessage_InvalidCommand(context);
                if (logMsg != null)
                    context.Logs.LogError(logMsg);
                
                context.Logs.ForceClose();
                return false;
            }

            context.command = command;

            return true;
        }

        protected virtual object PostprocessCommandContext(qConsoleCommandContext context, object returnedValue)
        {
            if (returnedValue is CommandPrompt prompt)
            {
                prompt.CommandContext = context;
                context.prompt = prompt;
                return returnedValue;
            }

            if (context.CleanupLogger && !(returnedValue is Task))
            {
                context.Logs.StartClosing();
            }

            context.Logs = null;
            return returnedValue;
        }
        #endregion

        #region Error ToString
        /// <summary>Creates a message string used in an error log when the user is trying to run a command that doesn't exist.</summary>
        /// <param name="context">Context for the invalid command.</param>
        /// <returns>Returns a string containing a message to the user about their error. If the string is null, the message will not be logged.</returns>
        protected virtual string LogMessage_InvalidCommand(qConsoleCommandContext context) =>
            $"Command {context.commandName} doesn't exist!";

        /// <summary>Creates a message string used in an error log when a command throws an exception.</summary>
        /// <param name="commandName">Name of the command that threw the exception.</param>
        /// <param name="e">The exception.</param>
        /// <returns>Returns a string containing information about the thrown exception. If the string is null, the message will not be logged.</returns>
        protected virtual string LogMessage_Exception(string commandName, Exception e)
        {
            if (e is qCommandException commandException)
                return commandException.ToString(IncludeStackTraceInCommandExceptions);

            return IncludeStackTraceInUnknownCommandExceptions ?
                $"There was an error while executing command '{commandName}': {e}" :
                $"There was an error while executing command '{commandName}'.";
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
                        Logs.Register(_instance);
                        break;
                    case false:
                        Logs.Unregister(_instance);
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