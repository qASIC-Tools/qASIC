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

        public abstract object ExecuteParser(qConsoleContext context);
        public abstract Task<object> ExecuteParserAsync(qConsoleContext context);

        /// <summary>Converts output back into a string</summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="arguments">Array of command arguments.</param>
        /// <returns>Returns a console input string.</returns>
        public abstract string ConvertToString(string commandName, qCommandArgument[] arguments);

        public abstract CmdCharacterInfo GetCharacterInfo(string cmd, int characterIndex);
        
        protected void FinishExecuting(qConsoleContext context)
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
    }
}