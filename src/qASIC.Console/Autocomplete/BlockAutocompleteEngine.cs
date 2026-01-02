using System.Data;
using System.Linq;
using qASIC.CmdAutocomplete;

namespace qASIC.Console.Autocomplete;

public class BlockAutocompleteEngine(qConsole console) : AutocompleteEngine(console)
{
    public override (string, int) Autocomplete(string cmd, int cursorPosition)
    {
        var data = Console.CommandParser.GetCharacterInfo(cmd, cursorPosition);

        switch (data.scope)
        {
            case Parsing.CmdCharacterInfo.Scope.Nothing:
                break;
            case Parsing.CmdCharacterInfo.Scope.CommandName:
                {
                    var cmds = Console.CommandList.ToList();
                    var target = cmds.FirstOrDefault(x => x.CommandName.StartsWith(data.commandName));
                    if (target == null) break;
                    var newValue = target.CommandName;
                    if (data.commandName == newValue)
                        newValue = cmds[(cmds.IndexOf(target) + 1) % cmds.Count].CommandName;
                    
                    cmd = Console.CommandParser.ReplaceCharacterInfo(data, newValue, out var newPos, out var newLength);
                    cursorPosition = newPos + newLength;
                }
                break;
            case Parsing.CmdCharacterInfo.Scope.Argument:
                {
                    if (!Console.CommandList.TryGetCommand(data.commandName, out var command)) break;
                    if (command is not ISupportsAutocomplete ac) break;
                    if (!ac.CommandAutocomplete.Variants.IndexInRange(data.argumentIndex)) break;

                    // Oops, have to rewrite how we do this ://
                    // var variant = ac.CommandAutocomplete.Variants[data.argumentIndex];
                    // variant.
                }
                break;
        }
        

        return (cmd, cursorPosition);
    }
}
