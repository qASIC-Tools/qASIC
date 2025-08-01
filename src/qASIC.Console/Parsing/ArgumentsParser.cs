using qASIC.Parsing;
using System;
using System.Threading.Tasks;
using qASIC.Logging;
using qASIC.CommandPrompts;

namespace qASIC.Console.Parsing
{
    public abstract class ArgumentsParser
    {
        public ArgumentsParser() { }

        public qConsole Console { get; set; }
        public ModularParser ValueParser { get; set; } = new ModularParser();

        public abstract object ExecuteParser(qConsoleCommandContext context);
        public abstract Task<object> ExecuteParserAsync(qConsoleCommandContext context);

        /// <summary>Converts output back into a string</summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="arguments">Array of command arguments.</param>
        /// <returns>Returns a console input string.</returns>
        public abstract string ConvertToString(string commandName, qCommandArgument[] arguments);

        public abstract CmdCharacterInfo GetCharacterInfo(string cmd, int characterIndex);

        #region Executing
        /// <summary>Executes a command.</summary>
        /// <param name="context">Command arguments.</param>
        protected object ExecuteInConsole(qConsoleCommandContext context)
        {
            //Before
            if (!PreprocessContext(context))
                return null;

            //Executing
            var returnedValue = Console.Execute(context.command.CommandName, () => context.command.Run(context), context.Logs);

            //After
            return PostprocessContext(context, returnedValue);
        }

        /// <summary>Executes a command asynchronously.</summary>
        /// <param name="context">Command arguments.</param>
        protected async Task<object> ExecuteInConsoleAsync(qConsoleCommandContext context)
        {
            //Before
            if (!PreprocessContext(context))
                return null;

            //Executing
            var returnedValue = Console.Execute(context.command.CommandName, () => context.command.Run(context), context.Logs);
            if (returnedValue is Task task)
                returnedValue = await Console.ExecuteAsync(context.command.CommandName, task, context.Logs);

            //After
            return PostprocessContext(context, returnedValue);
        }

        protected virtual bool PreprocessContext(qConsoleCommandContext context)
        {
            //Prompt
            if (context.prompt != null)
            {
                context.parser = ValueParser;

                if (!context.prompt.CanExecute(context))
                    return false;

                context.args = context.prompt.Prepare(context);
                return true;
            }

            //Normal
            if (Console.CommandList == null)
                throw new Exception("Cannot execute commands with no command list!");

            context.LogOutput = true;
            context.Logs = new qLogManager();
            context.ParserData.logs.RegisterManager(context.Logs);

            if (!Console.CommandList.TryGetCommand(context.commandName, out var command))
            {
                context.Logs.LogError($"Command {context.commandName} doesn't exist");
                context.Logs.Close();
                return false;
            }

            context.command = command;

            return true;
        }

        protected virtual object PostprocessContext(qConsoleCommandContext context, object returnedValue)
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

        protected void FinishExecuting(qConsoleCommandContext context)
        {
            if (context.ParserData.cleanupLogger)
            {
                if (context.ParserData.logs.RegisteredManagers.Count > 0)
                    context.ParserData.logs.AutoClose = true;
                else
                    context.ParserData.logs.Close();
            }

            context.ParserData = null;
        }
        #endregion
    }
}