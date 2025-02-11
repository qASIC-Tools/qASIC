using qASIC.Parsing;

namespace qASIC.Console.Parsing
{
    public abstract class ArgumentsParser
    {
        public ArgumentsParser() { }

        public ModularParser ValueParser { get; set; } = new ModularParser();

        /// <summary>Gets the command name from a console input string.</summary>
        /// <param name="cmd">The console input string.</param>
        /// <returns>Returns the parsed command name.</returns>
        public abstract string ParseCommandName(string cmd);

        /// <summary>Gets command arguments from a console input string.</summary>
        /// <param name="cmd">The console input string.</param>
        /// <returns>Returns a list of command arguments.</returns>
        public abstract CommandArgument[] ParseArguments(string cmd);

        /// <summary>Converts output back into a string</summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="arguments">Array of command arguments.</param>
        /// <returns>Returns a console input string.</returns>
        public abstract string ConvertToString(string commandName, CommandArgument[] arguments);

        public abstract CmdCharacterInfo GetCharacterInfo(string cmd, int characterIndex);
    }
}