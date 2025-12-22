using System.Data;
using System.Linq;
using qASIC.CmdAutocomplete;

namespace qASIC.Console.Autocomplete;

public class BlockAutocompleteEngine(qConsole console) : AutocompleteEngine(console)
{
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

        if (data.scope == Parsing.CmdCharacterInfo.Scope.Argument &&
            data.argumentIndex >= data.arguments.Length - 1 &&
            Console.CommandList.TryGetCommand(data.commandName, out var command) &&
            command is ISupportsAutocomplete ac)
        {
            if (data.argumentIndex == data.arguments.Length)
                data.arguments = data.arguments
                    .Append(new qCommandArgument(Console.CommandParser.ValueParser, ""))
                    .ToArray();

            var acData = ac.CommandAutocomplete;
            if (acData != null)
            {
                var variants = acData.GetValidVariants(data.arguments);
                var values = variants.SelectMany(x => x.Arguments[data.argumentIndex].GetAvaliableValues(this))
                    .OrderBy(x => x)
                    .ToArray();

                if (values.Length != 0)
                {
                    int i = 0;
                    
                    if (!string.IsNullOrWhiteSpace(data.arguments[data.argumentIndex].arg))
                    {
                        while (i < values.Length && 
                            !values[i].StartsWith(data.arguments[data.argumentIndex].arg))
                            i++;

                        if (i == values.Length)
                            return (cmd, cursorPosition);
                    }


                    if (values[i] == data.arguments[data.argumentIndex].arg)
                        i = (i + 1) % values.Length;

                    data.arguments[data.argumentIndex].arg = values[i];
                    cmd = Console.CommandParser.ConvertToString(data.commandName, data.arguments);
                    cursorPosition = cmd.Length;
                }
            }

        }

        return (cmd, cursorPosition);
    }
}
