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

        /// <summary>Gives information about a character in an input string.</summary>
        /// <param name="inputString">The input string.</param>
        /// <param name="characterIndex">Index of the character.</param>
        /// <returns>Returns information about the specified character.</returns>
        public abstract CmdCharacterInfo GetCharacterInfo(string inputString, int characterIndex);
        
        /// <summary>Finalizes executing of an input string.</summary>
        /// <param name="context">The context.</param>
        /// <param name="returnedValue">The last returned value by a command.</param>
        protected void FinishExecuting(qConsoleContext context, object returnedValue = null)
        {
            if (!(returnedValue is CommandPrompt))
                context.ParserData.logs.StartClosing();
        }
    }
}