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

        private ArgumentsParser _commandParser;
        public ArgumentsParser CommandParser
        {
            get => _commandParser;
            set
            {
                if (_commandParser != null)
                    _commandParser.Console = null;

                _commandParser = value;

                if (_commandParser != null)
                    _commandParser.Console = this;
            }
        }

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

        /// <summary>Can the console execute commands using <see cref="Execute(string)"/>.</summary>
        public bool CanParseAndExecute =>
            CommandList != null && CommandParser != null;

        /// <summary>Can the console execute commands using <see cref="Execute(qCommandArgument[])"/>.</summary>
        public bool CanExecute =>
            CommandList != null;

        #region Execute
        /// <summary>Executes a command.</summary>
        /// <param name="cmd">Command text that will be parsed and executed.</param>
        public object Execute(string cmd, object previousValue = null) =>
            Execute(new qConsoleContext()
            {
                inputString = cmd,
                previousValue = previousValue,
            });

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="cmd">Command text that will be parsed and executed.</param>
        public async Task<object> ExecuteAsync(string cmd, object previousValue = null) =>
            await ExecuteAsync(new qConsoleContext()
            {
                inputString = cmd,
                previousValue = previousValue,
            });

        public object Execute(qConsoleContext context)
        {
            PreprocessConsoleContext(context);
            return CommandParser.ExecuteParser(context);
        }

        public async Task<object> ExecuteAsync(qConsoleContext context)
        {
            PreprocessConsoleContext(context);
            return await CommandParser.ExecuteParserAsync(context);
        }

        private void PreprocessConsoleContext(qConsoleContext context)
        {
            context.Console = this;
            context.Logs ??= context.ParserData?.logs ?? new qLogManager();
            Logs.RegisterManager(context.Logs);

            LogUserInput(context);
        }
        #endregion

        private void LogUserInput(qConsoleContext context)
        {
            if (!(context.previousValue is CommandPrompt))
                context.Logs.Log(qLog.CreateNow(context.inputString, LogType.User, "user_input"));
        }

        #region ExecuteCode
        /// <summary>Executes a command.</summary>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="command">Command code to execute.</param>
        /// <param name="logOutput">When true, it will log the output value to the console.</param>
        public object ExecuteCode(string commandName, Func<object> command, qLogManager logs = null, bool logOutput = true)
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

        public async Task<object> ExecuteCodeAsync(string commandName, Func<Task> command, qLogManager logs = null, bool logOutput = true) =>
            await ExecuteCodeAsync(commandName, command.Invoke(), logs, logOutput);

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="command">Command task to execute.</param>
        /// <param name="logOutput">When true, it will log the output value to the console.</param>
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
            context.parser = CommandParser.ValueParser;

            //Prompt
            if (context.prompt != null)
            {
                if (!context.prompt.CanExecute(context))
                    return false;

                context.args = context.prompt.Prepare(context);
                return true;
            }

            //Standard
            if (CommandList == null)
                throw new Exception("Cannot execute commands with no command list!");

            context.LogOutput = true;
            context.Logs ??= new qLogManager();

            if (!CommandList.TryGetCommand(context.commandName, out var command))
            {
                context.Logs.LogError($"Command {context.commandName} doesn't exist");
                context.Logs.Close();
                return false;
            }

            context.command = command;

            return true;
        }

        protected virtual object PostprocessCommandContext(qConsoleCommandContext context, object returnedValue)
        {
            if (returnedValue is CommandPrompt prompt)
            {
                prompt.context = context;
                return returnedValue;
            }

            if (context.CleanupLogger && !(returnedValue is Task))
            {
                context.Logs.Close();
            }

            context.Logs = null;
            return returnedValue;
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