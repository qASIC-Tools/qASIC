using System.Linq;

namespace qASIC.Console.Autocomplete
{
    public class BlockAutocompleteEngine : AutocompleteEngine
    {
        public BlockAutocompleteEngine(GameConsole console) : base(console) { }

        public override (string, int) Autocomplete(string cmd, int cursorPosition)
        {
            var data = Console.CommandParser.GetCharacterInfo(cmd, cursorPosition);

            if (data.scope == Parsing.CmdCharacterInfo.Scope.CommandName &&
                data.scopedPosition - 1 < data.commandName.Length)
            {
                //If cursor is before command name
                if (data.scopedPosition < 0)
                    return (cmd, cursorPosition);

                var commandNames = Console.CommandList.GetSortedCommandNames().ToArray();
                for (int i = 0; i < commandNames.Length; i++)
                {
                    if (!commandNames[i].StartsWith(data.commandName)) continue;
                    if (commandNames[i] == data.commandName)
                        i = (i + 1) % commandNames.Length;

                    cmd = $"{data.prefix}{Console.CommandParser.ConvertToString(commandNames[i], data.arguments)}{data.postfix}";
                    cursorPosition += commandNames[i].Length - data.scopedPosition;
                    return (cmd, cursorPosition);
                }

                return (cmd, cursorPosition);
            }

            return (cmd, cursorPosition);
        }
    }
}