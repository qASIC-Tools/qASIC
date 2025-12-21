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

VARIABLE=3 command
command $VARIABLE Inside\ $VARIABLE\ inside ""Wrapped $VARIABLE ""

# This is a comment
command

@ask_for_input
dont_ask
^force_input
@
";

    public QuashParser.ParsedCodeScope ParseVisually(string txt)
    {
        SysConsole.WriteLine("Parsing script:");
        SysConsole.WriteLine(txt);
        SysConsole.WriteLine("END OF SCRIPT");
        SysConsole.WriteLine();

        var result = QuashParser.QToScope(new(txt));

        SysConsole.WriteLine("Result:");    
        PrintVisually(result);
        return result;

        
        void PrintVisually(QuashParser.ParsedCodeScope scope, int indent = 0)
        {
            indent++;
            var scopeIndent = new string(' ', (indent - 1) * 2);
            var indentString = new string(' ', indent * 2);
            SysConsole.WriteLine($"{scopeIndent}SCOPE START");

            foreach (var item in scope.methods)
                PrintVisually(item, indent);

            foreach (var item in scope.items)
            {
                SysConsole.Write(indentString);
                SysConsole.ForegroundColor = ConsoleColor.DarkGray;
                switch (item)
                {
                    case QuashParser.ParsedCommand cmd:
                        SysConsole.Write($"{(cmd.askForUserInput ? "@" : "")}COMMAND");
                        break;
                    case QuashParser.ParsedComment:
                        SysConsole.Write("COMMENT");
                        break;
                    case QuashParser.ParsedEmpty:
                        SysConsole.Write("EMPTY");
                        break;
                    case QuashParser.ParsedPromptInput:
                        SysConsole.Write("INPUT");
                        break;
                    case QuashParser.ParsedVariableSet:
                        SysConsole.Write("SET");
                        break;
                    default:
                        SysConsole.Write("UNKNOWN");
                        break;
                }

                SysConsole.ResetColor();

                switch (item)
                {
                    case QuashParser.ParsedCommand cmd:
                        SysConsole.WriteLine($": {cmd.commandName}");
                        for (int i = 0; i < cmd.arguments.Count; i++)
                        {
                            SysConsole.Write($"{indentString}  {i}: ");
                            PrintArgumentContents(cmd.arguments[i]);
                            SysConsole.WriteLine();
                        }
                        
                        break;
                    case QuashParser.ParsedComment cmt:
                        SysConsole.WriteLine($": {cmt.comment}");
                        break;
                    case QuashParser.ParsedEmpty:
                        SysConsole.WriteLine();
                        break;
                    case QuashParser.ParsedPromptInput input:
                        SysConsole.WriteLine($": {input}");
                        break;
                    case QuashParser.ParsedVariableSet variable:
                        SysConsole.WriteLine($": {variable.variableName}=");
                        PrintArgumentContents(variable.value);
                        break;
                    default:
                        SysConsole.WriteLine($": {item}");
                        break;
                }
            }

            SysConsole.WriteLine($"{scopeIndent}SCOPE END");
        }

        void PrintArgumentContents(QuashParser.ParsedArgument argument)
        {
            foreach (var part in argument.parts)
            {
                switch (part)
                {
                    case QuashParser.ParsedArgument.ParsedVariable variable:
                        SysConsole.BackgroundColor = ConsoleColor.DarkGreen;
                        SysConsole.Write(variable.variableName);
                        SysConsole.ResetColor();
                        break;
                    default:
                        SysConsole.Write(part.ToArgPart(null, null));
                        break;
                }
            }
        }
    }     
}
