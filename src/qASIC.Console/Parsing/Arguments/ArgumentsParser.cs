using qASIC.Parsing;
using System.Collections.Generic;

namespace qASIC.Console.Parsing.Arguments
{
    public abstract class ArgumentsParser
    {
        public ArgumentsParser() { }

        public List<ValueParser> ValueParsers { get; set; } = new List<ValueParser>(ValueParser.CreateStandardParserArray());

        /// <summary>Gets the command name from a console input string.</summary>
        /// <param name="cmd">The console input string.</param>
        /// <returns>Returns the parsed command name.</returns>
        public abstract string ParseCommandName(string cmd);
        
        /// <summary>Gets command arguments from a console input string.</summary>
        /// <param name="cmd">The console input string.</param>
        /// <returns>Returns a list of command arguments.</returns>
        public abstract CommandArgument[] ParseArguments(string cmd);

        protected CommandArgument CreateCommandArgument(string arg)
        {
            var parsedArgs = new List<object>();
            foreach (var parser in ValueParsers)
                if (parser.TryParse(arg, out object result) && result != null)
                    parsedArgs.Add(result);

            return new CommandArgument(arg, parsedArgs.ToArray());
        }
    }
}