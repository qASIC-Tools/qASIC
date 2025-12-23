using System.Xml.Serialization;
using qASIC.Console.Parsing;

using SysConsole = System.Console;

namespace Tests.Console;

public class QuashParserTests()
{
    public const string TEST_SCRIPT = @"command argument1 argument2 argument3
escape hello\ world\
\ and\ another

wrapping ""argument 1"" 'argument 2' `argument 3`
unusual_wrapping arg""ument ""1 ""argument ""2 argument"" 3""
selective_wrapping ""argument' argument ` '`""

VARIABLE=3 command
command $VARIABLE Inside\ $VARIABLE\ inside ""Wrapped $VARIABLE ""

# This is a comment
command

@ask_for_input
dont_ask
^force_input
^^arg1 ""arg2 arg2"" arg3
@
";

    public List<QuashParser.LexItem> ParseVisually(string txt)
    {
        SysConsole.WriteLine("Parsing script:");
        SysConsole.WriteLine(txt);
        SysConsole.WriteLine("END OF SCRIPT");
        SysConsole.WriteLine();

        var result = QuashParser.Lex(new(txt));

        SysConsole.WriteLine("Lexed:");

        foreach (var item in result)
        {
            SysConsole.ResetColor();
            switch (item)
            {
                case QuashParser.LexCodeText:
                    break;
                case QuashParser.LexEnd:
                    SysConsole.BackgroundColor = ConsoleColor.DarkRed;
                    SysConsole.Write('&');
                    SysConsole.ResetColor();
                    break;
                case QuashParser.LexSpecialToken:
                    SysConsole.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case QuashParser.LexWhiteSpace:
                    SysConsole.BackgroundColor = ConsoleColor.DarkBlue;
                    break;
            }

            SysConsole.Write(item.readString);
        }

        SysConsole.ResetColor();

        SysConsole.WriteLine();
        SysConsole.WriteLine("Parsed:");
        foreach (var item in QuashParser.ParseFromLexer(result).items)
        {
            switch (item)
            {
                case QuashParser.ParsedCommand cmd:
                    PrintTypeText("COMMAND", cmd.commandName);
                    PrintArgs(cmd.arguments);
                    break;
                case QuashParser.ParsedVariableSet varSet:
                    PrintTypeText("VAR SET", $"{varSet.variableName}");
                    PrintArgs([varSet.argument]);
                    break;
                case QuashParser.ParsedInputAsk:
                    PrintTypeText("INPUT ASK", "");
                    break;
                case QuashParser.ParsedPromptArgsResponse argResponse:
                    PrintTypeText("RESPONSE ARG", " ");
                    PrintArgs(argResponse.arguments);
                    break;
                case QuashParser.ParsedPromptLineResponse lineResponse:
                    PrintTypeText("RESPONSE LINE", lineResponse.line);
                    break;

            }
        }
        
        return result;


        void PrintTypeText(string type, string text)
        {
            SysConsole.ForegroundColor = ConsoleColor.DarkGray;
            SysConsole.Write($"{type}{(string.IsNullOrEmpty(text) ? "" : ": ")}");
            SysConsole.ResetColor();
            SysConsole.WriteLine(text);
        }

        void PrintArgs(List<QuashParser.ParsedCommand.Argument> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                SysConsole.ForegroundColor = ConsoleColor.DarkGray;
                SysConsole.Write($"  {i}: ");
                SysConsole.ResetColor();
                foreach (var part in arguments[i].parts)
                {
                    switch (part)
                    {
                        case QuashParser.ParsedCommand.Argument.TextPart textPart:
                            SysConsole.Write(textPart.text);
                            break;
                        case QuashParser.ParsedCommand.Argument.VariablePart varPart:
                            SysConsole.BackgroundColor = ConsoleColor.DarkGreen;
                            SysConsole.Write($"${varPart.variableName}");
                            break;
                    }

                    SysConsole.ResetColor();
                }

                SysConsole.ResetColor();
                SysConsole.WriteLine();
            }
        }
    }
}
