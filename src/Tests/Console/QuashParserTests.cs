using System.Text;
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

    public QuashParser Parser { get; } = new();

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
            SysConsole.ForegroundColor = item.type switch
            {
                QuashParser.LexItem.Type.Command => ConsoleColor.Blue,
                QuashParser.LexItem.Type.ArgumentPart => ConsoleColor.Green,
                QuashParser.LexItem.Type.VarGet => ConsoleColor.Yellow,
                QuashParser.LexItem.Type.VarSet => ConsoleColor.DarkYellow,
                QuashParser.LexItem.Type.ResponseArgsPart => ConsoleColor.Magenta,
                QuashParser.LexItem.Type.ResponseArgsVarGet => ConsoleColor.Yellow,
                QuashParser.LexItem.Type.ResponseLine => ConsoleColor.DarkBlue,
                QuashParser.LexItem.Type.Comment => ConsoleColor.DarkGray,
                QuashParser.LexItem.Type.End => ConsoleColor.Black,
                _ => SysConsole.ForegroundColor,
            };
            
            switch (item)
            {
                case QuashParser.LexEnd:
                    SysConsole.Write('&');
                    SysConsole.ResetColor();
                    break;
                case QuashParser.LexWhiteSpace:
                    SysConsole.BackgroundColor = ConsoleColor.Black;
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

    public void TestCharacterInfo()
    {
        var info = new CmdCharacterInfo();

        string txt = "";
        var i = 0;

        WriteInfo(false);

        var read = SysConsole.ReadKey();
        while (read.Key != ConsoleKey.Escape)
        {
            var clearEnd = false;
            if (char.IsWhiteSpace(read.KeyChar) || char.IsLetterOrDigit(read.KeyChar) || char.IsSymbol(read.KeyChar) || char.IsPunctuation(read.KeyChar))
            {
                txt = $"{txt[..i]}{read.KeyChar}{txt[i..]}";
                i++;
            }
            
            if (read.Key == ConsoleKey.Backspace && i > 0)
            {
                txt = $"{txt[..(i-1)]}{txt[i..]}";
                i--;
                clearEnd = true;
            }

            if (read.Key == ConsoleKey.Delete && i < txt.Length)
            {
                txt = $"{txt[..i]}{txt[(i+1)..]}";
                clearEnd = true;
            }
            
            if (read.Key == ConsoleKey.LeftArrow && i > 0) i--;
            if (read.Key == ConsoleKey.RightArrow && i < txt.Length) i++;

            info = Parser.GetCharacterInfo(txt, i);

            SysConsole.Write("\u001b[F");
            WriteInfo(clearEnd);
            read = SysConsole.ReadKey();
        }


        void WriteInfo(bool clearEnd)
        {
            var width = SysConsole.BufferWidth;
            var infoTxt = new StringBuilder();
            infoTxt.Append(info.scope switch
            {
                CmdCharacterInfo.Scope.Nothing => "NOTHING",
                CmdCharacterInfo.Scope.Argument => "ARG",
                CmdCharacterInfo.Scope.CommandName => "CMD",
                CmdCharacterInfo.Scope.Variable => "VAR",
                _ => "IDK",
            });

            infoTxt.Append(' ');

            infoTxt.Append(info.scope switch
            {
                CmdCharacterInfo.Scope.CommandName => info.commandName,
                CmdCharacterInfo.Scope.Argument => $"{info.commandName} {info.argumentIndex}:{info.argument}",
                CmdCharacterInfo.Scope.Variable => info.variableName,
                _ => ""
            });

            infoTxt.Append(new string(' ', Math.Max(0, width - infoTxt.Length)));

            SysConsole.WriteLine(infoTxt);
            SysConsole.Write(txt);

            if (clearEnd)
                SysConsole.Write(" \b");

            SysConsole.Write(new string('\b', txt.Length - i));
        }
    }
}
