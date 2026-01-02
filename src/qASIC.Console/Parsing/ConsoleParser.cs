using qASIC.Parsing;
using System.Threading.Tasks;
using qASIC.CommandPrompts;

namespace qASIC.Console.Parsing;

public abstract class ConsoleParser
{
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
    public virtual CmdCharacterInfo GetCharacterInfo(string inputString, int characterIndex) =>
        new()
        {
            scope = CmdCharacterInfo.Scope.Nothing,
        };

    public virtual string ReplaceCharacterInfo(CmdCharacterInfo info, string newValue, out int position, out int length)
    {
        position = 0;
        length = 0;
        return string.Empty;
    }
}
