using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qASIC.Parsing;

// A short summary on how this parser works:
// 1. The command is being parsed. This is done using these methods:
//     - QToScope - converts a queue of characters into a ParsedCodeScope,
//                  containing all parsed commands, functions, etc.
//     - ReadToken - used by QToScope to read parts of the queue. The
//                   exact read behaviour is specified using the ReadTokenArgs
//                   which are defined in their own section.

namespace qASIC.Console.Parsing;

public class QuashParser : ConsoleParser
{
    #region Entry point
    public override object ExecuteParser(qConsoleContext context)
    {
        if (context.ParserData is not QuashData data)
        {
            data = new();
            context.ParserData = data;
        }

        if (data.scopeStack.Count == 0)
            data.scopeStack.Push((QToScope(new Queue<char>(context.inputString)), 0));

        return ExecuteLoop(context, data);
    }

    public override async Task<object> ExecuteParserAsync(qConsoleContext context)
    {
        if (context.ParserData is not QuashData data)
        {
            data = new();
            context.ParserData = data;
        }

        if (context.previousValue is CommandPrompts.CommandPrompt)
            data.scopeStack.Push((new ParsedCodeScope() { items = [new ParsedPromptInput() { input = context.inputString }] }, 0));

        if (data.scopeStack.Count == 0)
            data.scopeStack.Push((QToScope(new Queue<char>(context.inputString)), 0));
        
        return await ExecuteLoopAsync(context, data);
    }
    #endregion

    #region #Executing
    private object ExecuteLoop(qConsoleContext context, QuashData data)
    {
        bool first = true;
        while (TryGetNextCommandFromLoop(context, data, out var commandContext, ref first))
            context.previousValue = context.Console.ExecuteCommand(commandContext);

        return context.previousValue;
    }
    
    private async Task<object> ExecuteLoopAsync(qConsoleContext context, QuashData data)
    {
        bool first = true;
        while (TryGetNextCommandFromLoop(context, data, out var commandContext, ref first))
            context.previousValue = await context.Console.ExecuteCommandAsync(commandContext);
        
        return context.previousValue;
    }

    private bool TryGetNextCommandFromLoop(qConsoleContext context, QuashData data, out qConsoleCommandContext commandContext, ref bool first)
    {
        commandContext = null;
        var firstNow = first;
        first = false;

        while (data.scopeStack.TryPop(out var item))
        {
            (var scope, var i) = item;

            for (; i < scope.items.Count; i++)
            {
                if (scope.items[i] is not ParsedCommand and not ParsedPromptInput)
                    continue;

                // In case the previous command returned a prompt
                if (context.previousValue is CommandPrompts.CommandPrompt prompt)
                {
                    if (!firstNow && (!data.executePromptSelf || scope.items[i] is ParsedCommand { askForUserInput: true }))
                    {
                        data.scopeStack.Push((scope, i));
                        return false;
                    }

                    var args = scope.items[i] switch
                    {
                        ParsedCommand cmd => cmd.CreateQuashArgs(ValueParser, context.Variables),
                        ParsedPromptInput input => [new QuashArgument(ValueParser, input.input)],
                        _ => [],
                    };

                    var inputString = scope.items[i] switch
                    {
                        ParsedPromptInput input => input.input,
                        ParsedCommand cmd => ConvertToString(cmd.commandName, args),
                        _ => string.Empty,
                    };

                    if (!prompt.ParseArguments)
                        args = [new QuashArgument(ValueParser, inputString)];
                    
                    prompt.CommandContext.args = args;
                    prompt.CommandContext.inputString = inputString;
                    commandContext = prompt.CommandContext as qConsoleCommandContext;
                    return true;
                }

                // Set this to false when finished with executing from command prompts
                data.executePromptSelf = false;

                if (scope.items[i] is ParsedCommand parsedCommand)
                {
                    commandContext = context.CreateCommandContext();
                    commandContext.commandName = parsedCommand.commandName;
                    commandContext.args = parsedCommand.CreateQuashArgs(ValueParser, context.Variables);
                    commandContext.inputString = ConvertToString(commandContext.commandName, commandContext.args);
                    data.scopeStack.Push((scope, i + 1));
                    data.executePromptSelf = !parsedCommand.askForUserInput;
                    return true;
                }
            }
        }

        return false;
    }
    #endregion

    #region Read Token Args
    /// <summary>Used for reading blank space between other tokens (which includes newline).</summary>
    private static ReadTokenArgs Ta_WhiteSpace = ReadTokenArgs.ForWhiteSpace(';', '\n').WithEscapeCharacters();
    /// <summary>Used for detecting the # symbol indicating the beginning of a comment line.</summary>
    private static ReadTokenArgs Ta_CommentStart = ReadTokenArgs.ForSpecificTokens("#");
    /// <summary>Used for reading everything after the # symbol in a comment line until it reaches the end of the line. Cannot be extended with escape characters.</summary>
    private static ReadTokenArgs Ta_CommentEnd = new() { endChars = ['\n'] };
    /// <summary>Used for detecting the @ symbol in front of a command signifying that the command asks for user input.</summary>
    private static ReadTokenArgs Ta_CommandModifiers = ReadTokenArgs.ForSpecificTokens("@");
    /// <summary>Used for detecting the ^ symbol in front of a line signifying that it's meant to be parsed in it's entirety to be used for prompt input.</summary>
    private static ReadTokenArgs Ta_PromptInputStart = ReadTokenArgs.ForSpecificTokens("^");
    /// <summary>Used for reading everything after the ^ symbol (prompt input).</summary>
    private static ReadTokenArgs Ta_PromptInputEnd = new() { endChars = ['\n', ';'], useEscapeCharacters = true, };
    /// <summary>Used for reading the start of a command or the name of the variable that will be set.</summary>
    private static ReadTokenArgs Ta_CommandStart = ReadTokenArgs.ForNonWhite(';', '\n', '=').WithEscapeCharacters();
    /// <summary>Used for detecting the = symbol after the command name, used for setting a variable.</summary>
    private static ReadTokenArgs Ta_CommandVariableSet = ReadTokenArgs.ForSpecificTokens("=");
    /// <summary>Used for detecting a wrapping symbol in an argument.</summary>
    private static ReadTokenArgs Ta_CommandArgWrap = ReadTokenArgs.ForSpecificTokens("\"", "\'", "`");
    /// <summary>Used for checking for the $ symbol signifying a variable being used in an argument.</summary>
    private static ReadTokenArgs Ta_ArgVariableStart = ReadTokenArgs.ForSpecificTokens("$");
    /// <summary>Used for reading the variable name after a $ symbol that will be inserted in the argument.</summary>
    private static ReadTokenArgs Ta_ArgVariableName = ReadTokenArgs.ForWord(true, ['_']);
    /// <summary>Used for reading static contents of an argument, when the portion being read isn't wrapped.</summary>
    private static ReadTokenArgs Ta_CommandArgUnwrapped = ReadTokenArgs.ForNonWhite(';', '\n', '\"', '\'', '`', '$').WithEscapeCharacters();
    /// <summary>Used for reading static contents of an argument, when the portion being read is wrapped.</summary>
    private static ReadTokenArgs Ta_CommandArgWrapped = new() { endChars = ['\"', '\'', '`', '$'], useEscapeCharacters = true, };
    /// <summary>Used for detecting the end of a command line.</summary>
    private static ReadTokenArgs Ta_CommandEnd = ReadTokenArgs.ForSpecificTokens(";", "\n");
    #endregion

    #region Parsing
    /// <summary>Converts a queue of characters to a parsed code scope.</summary>
    /// <param name="q">Queue of characters, typically made from a string containing the code.</param>
    /// <returns>Returns the parsed code scope.</returns>
    /// <exception cref="NotImplementedException">Should probably never be thrown :P</exception>
    public static ParsedCodeScope QToScope(Queue<char> q)
    {
        var scope = new ParsedCodeScope();
        while (q.Count > 0)
        {
            // Read white
            ReadToken(q, Ta_WhiteSpace, out _);

            // If immedietally ends after white space, end
            if (ReadToken(q, Ta_CommandEnd, out _).Length > 0)
            {
                scope.items.Add(new ParsedEmpty());
                continue;
            }
            
            // Comment
            if (ReadToken(q, Ta_CommentStart, out _).Length > 0)
            {
                scope.items.Add(new ParsedComment()
                {
                    comment = ReadToken(q, Ta_CommentEnd, out _).ToString(),
                });
                continue;
            }

            // Prompt input
            if (ReadToken(q, Ta_PromptInputStart, out _).Length > 0)
            {
                scope.items.Add(new ParsedPromptInput()
                {
                    input = ReadToken(q, Ta_PromptInputEnd, out _).ToString(),
                });
                continue;
            }

            // Command
            {
                var command = new ParsedCommand();

                // Command modifiers
                var modifierToken = ReadToken(q, Ta_CommandModifiers, out _);
                while (modifierToken.Length > 0)
                {
                    switch (modifierToken.ToString())
                    {
                        case "@":
                            command.askForUserInput = true;
                            break;
                        default:
                            throw new NotImplementedException("Read an unknown command modifier");
                    }

                    modifierToken = ReadToken(q, Ta_CommandModifiers, out _);
                }

                // Command name
                command.commandName = ReadToken(q, Ta_CommandStart, out _).ToString();

                // If this is a variable
                if (ReadToken(q, Ta_CommandVariableSet, out _).Length > 0)
                {
                    scope.items.Add(new ParsedVariableSet()
                    {
                        variableName = command.commandName,
                        value = ReadArgument(new(), out _),
                    });
                    continue;
                }

                // White space
                ReadToken(q, Ta_WhiteSpace, out var whiteBefore);

                while (q.Count > 0)
                {
                    // End if reached end
                    if (ReadToken(q, Ta_CommandEnd, out _).Length > 0)
                        break;

                    // Read arg
                    var argument = ReadArgument(whiteBefore, out var endCommand);
                    command.arguments.Add(argument);

                    // End if ended
                    if (endCommand)
                        break;

                    // Set next argument's white before to this argument's white after
                    whiteBefore = argument.whiteAfter;
                }

                // Add command and an empty space after it.
                scope.items.Add(command);
                scope.items.Add(new ParsedEmpty());
            }
        }

        return scope;


        ParsedArgument ReadArgument(StringBuilder whiteBefore, out bool endOfCommand)
        {
            endOfCommand = false;
            var argument = new ParsedArgument()
            {
                whiteBefore = whiteBefore,
                whiteAfter = new(),
            };

            char? wrap = null;
            while (q.Count > 0)
            {
                // Check for end
                if (wrap == null && ReadToken(q, Ta_WhiteSpace, out argument.whiteAfter).Length > 0) break;

                if (wrap == null && ReadToken(q, Ta_CommandEnd, out _).Length > 0)
                {
                    endOfCommand = true;
                    break;
                }

                // Check if it's a variable
                var varStartToken = ReadToken(q, Ta_ArgVariableStart, out var varStartString);
                if (varStartToken.Length > 0)
                {
                    var varNameToken = ReadToken(q, Ta_ArgVariableName, out var varNameString);
                    varStartString.Append(varNameString);

                    argument.parts.Add(new ParsedArgument.ParsedVariable(varNameToken.ToString(), varStartString));
                    continue;
                }

                // Check for wrapping
                var wrapToken = ReadToken(q, Ta_CommandArgWrap, out var wrapString);
                if (wrapToken.Length > 0)
                {
                    // Treat as normal text if the character used to wrap is different
                    if (wrap != null && wrap != wrapToken[0])
                    {
                        argument.parts.Add(new ParsedArgument.ParsedText(wrapToken.ToString(), wrapString));
                        continue;
                    }

                    argument.parts.Add(new ParsedArgument.ParsedText("", wrapString));
                    wrap = wrap == null ? wrapToken[0] : null;
                    continue;
                }
                
                // Add as normal text
                argument.parts.Add(new ParsedArgument.ParsedText(ReadToken(q, wrap == null ? Ta_CommandArgUnwrapped : Ta_CommandArgWrapped, out var argString).ToString(), argString));
            }

            return argument;
        }
    }

    /// <summary>Reads a part of the queue.</summary>
    /// <param name="q">Queue of characters, typically made from a string containing the code.</param>
    /// <param name="args">Arguments specifying how much should be read.</param>
    /// <param name="readString">Contains all characters removed from the queue.</param>
    /// <returns>Returns the read token.</returns>
    public static StringBuilder ReadToken(Queue<char> q, ReadTokenArgs args, out StringBuilder readString)
    {
        var result = new StringBuilder();

        //We have to do this, because csharp keeps crying about "out parameter inside a local method"
        readString = new StringBuilder();
        var read = readString;

        // In case we are looking only for specific tokens
        if (args.specificTokens != null && args.specificTokens.Length > 0)
        {
            var str = new string(q.ToArray());
            foreach (var item in args.specificTokens)
            {
                if (str.Length < item.Length) continue;
                if (!str.StartsWith(item)) continue;
                for (int i = 0; i < item.Length; i++)
                    result.Append(Dequeue());
                
                break;
            }

            return result;
        }

        // Main read loop
        while (q.TryPeek(out var c))
        {
            //Ending
            if (args.endChars != null && args.endChars.Contains(c))
                break;

            if (args.endOnWhite && char.IsWhiteSpace(c))
                break;
            
            if ((args.validChars != null && !args.validChars.Contains(c)) || (args.endOnNonWhite && !char.IsWhiteSpace(c)) || (args.endOnNonLetter && !char.IsLetterOrDigit(c)))
                break;

            //Escape characters
            if (args.useEscapeCharacters && c == '\\')
            {
                Dequeue();
                if (q.Count > 0)
                    result.Append(Dequeue());

                continue;
            }

            result.Append(Dequeue());
        }

        return result;


        char Dequeue()
        {
            var c = q.Dequeue();
            read.Append(c);
            return c;
        }
    }
    #endregion

    #region Other overrides
    public override CmdCharacterInfo GetCharacterInfo(string cmd, int characterIndex)
    {
        //Parsed info
        // var q = CreateQ(cmd);
        // var inputString = string.Empty;
        // var commandName = string.Empty;
        // var args = new List<QuashArgument>();
        // while (cmd.Length - q.Count <= characterIndex && q.Count > 0)
        // {
        //     ReadCommand(q, out inputString, out commandName, out args);
        // }

        // if (q.Count == 0 && cmd.Length > 0 && Char_End.Contains(cmd.Last()))
        // {
        //     inputString = string.Empty;
        //     commandName = string.Empty;
        //     args = new List<QuashArgument>();
        // }

        // //Prefixes and postfixes
        // var prefixEndIndex = cmd.Length - q.Count - inputString.TrimStart().Length;

        // var postfixStartIndex = cmd.Length - q.Count;
        // if (inputString.Length > 0 && Char_End.Contains(cmd[postfixStartIndex - 1]))
        // {
        //     prefixEndIndex--;
        //     postfixStartIndex--;
        // }

        // var prefix = cmd.Substring(0, prefixEndIndex);
        // var postfix = cmd.Substring(postfixStartIndex, cmd.Length - postfixStartIndex);

        // var argsArray = args.ToArray();

        // //Normalize parameters
        // characterIndex -= prefix.Length;

        // //Final info
        // var info = new CmdCharacterInfo(prefix, postfix, commandName, argsArray);

        // //If it's before the command name
        // if (characterIndex < 0)
        //     return info.WithScope(CmdCharacterInfo.Scope.CommandName, characterIndex);

        // //If it's between command name and first argument
        // if (characterIndex < commandName.Length + 1)
        //     return info.WithScope(CmdCharacterInfo.Scope.CommandName, characterIndex);

        // //Looking for the target argument
        // var argIndex = 0;
        // while (argIndex < args.Count)
        // {
        //     var argLength = args[argIndex].arg.Length + (args[argIndex].WhiteBefore ?? string.Empty).Length;
        //     if (characterIndex > argLength) break;
        //     characterIndex -= argLength;
        //     argIndex++;
        // }

        // return info.WithScope(CmdCharacterInfo.Scope.Argument, characterIndex, argIndex);
        return new CmdCharacterInfo();
    }

    public override string ConvertToString(string commandName, qCommandArgument[] arguments)
    {
        var txt = new StringBuilder(commandName);

        var lastWhite = string.Empty;

        foreach (var arg in arguments)
        {
            if (arg is QuashArgument quashArg)
            {
                txt.Append(quashArg.WhiteBefore);
                txt.Append(quashArg.UnparsedArgString);
                lastWhite = quashArg.WhiteAfter;
                continue;
            }

            lastWhite = "";

            if (arg.arg.Any(x => char.IsWhiteSpace(x)))
            {
                txt.Append($" \"{arg.arg.Replace("\"", "\"\"")}\"");
                continue;
            }

            txt.Append($" {arg.arg}");
        }

        txt.Append(lastWhite);
        return txt.ToString().Trim();
    }
    #endregion

    public class QuashArgument : qCommandArgument
    {
        public QuashArgument(ModularParser parser, string arg, params object[] values) : base(parser, arg, values) { }

        public string UnparsedArgString { get; set; }
        public string WhiteBefore { get; set; }
        public string WhiteAfter { get; set; }
    }

    public class QuashData : qConsoleParserData
    {
        public bool executePromptSelf = false;
        public Stack<(ParsedCodeScope, int)> scopeStack = [];
    }

    public struct ReadTokenArgs
    {
        public string[] specificTokens;

        public bool endOnWhite;
        public bool endOnNonWhite;
        public bool endOnNonLetter;
        public bool endOnNonDigit;
        public char[] endChars;
        public char[] validChars;

        public bool useEscapeCharacters;

        public ReadTokenArgs WithEscapeCharacters()
        {
            useEscapeCharacters = true;
            return this;
        }

        public static ReadTokenArgs ForSpecificTokens(params string[] tokens) =>
            new() { specificTokens = tokens, };
        
        public static ReadTokenArgs ForWhiteSpace(params char[] otherEndChars) =>
            new() { endOnNonWhite = true, endChars = otherEndChars, };
        
        public static ReadTokenArgs ForNonWhite(params char[] otherEndChars) =>
            new() { endOnWhite = true, endChars = otherEndChars, };
        
        public static ReadTokenArgs ForWord(bool canWordsHaveDigits, char[] otherValidChars, params char[] otherEndChars) =>
            new() { endOnNonLetter = true, validChars = otherValidChars, endChars = otherEndChars, endOnNonDigit = canWordsHaveDigits, };
    }

    #region Parsed Objects
    public class ParsedCodeScope
    {
        public List<ParsedCodeScope> methods = new List<ParsedCodeScope>();
        public List<ParsedScopeItem> items = new List<ParsedScopeItem>();
    }

    public abstract class ParsedScopeItem
    {
        
    }

    public class ParsedCommand : ParsedScopeItem
    {
        public string commandName;
        public List<ParsedArgument> arguments = [];
        public bool askForUserInput;

        public QuashArgument[] CreateQuashArgs(ModularParser parser, qConsoleVariableList variables) =>
            arguments
                .Select(x => x.ToQuashArgument(parser, variables))
                .ToArray();
    }

    public class ParsedArgument
    {
        public StringBuilder whiteBefore;
        public StringBuilder whiteAfter;
        public List<ParsedPart> parts = [];

        public QuashArgument ToQuashArgument(ModularParser parser, qConsoleVariableList variables)
        {
            var readString = new StringBuilder();
            var arg = new StringBuilder();
            foreach (var item in parts)
            {
                readString.Append(item.readString);
                arg.Append(item.ToArgPart(parser, variables));
            }
            
            return new QuashArgument(parser, arg.ToString(), parts.Count == 1 && parts[0] is ParsedVariable var && variables != null ? [variables.Get(var.variableName)] : [])
            {
                UnparsedArgString = readString?.ToString(),
                WhiteBefore = whiteBefore.ToString(),
                WhiteAfter = whiteAfter.ToString(),
            };
        }


        public abstract class ParsedPart(StringBuilder readString)
        {
            public StringBuilder readString = readString;

            public abstract string ToArgPart(ModularParser parser, qConsoleVariableList variables);
        }
        
        public class ParsedText(string text, StringBuilder readString) : ParsedPart(readString)
        {
            public string text = text;

            public override string ToArgPart(ModularParser parser, qConsoleVariableList variables) =>
                text;
        }

        public class ParsedVariable(string variableName, StringBuilder readString) : ParsedPart(readString)
        {
            public string variableName = variableName;

            public override string ToArgPart(ModularParser parser, qConsoleVariableList variables)
            {
                if (variables == null)
                    return variableName;

                return parser?.ConvertToString(variables.Get(variableName)) ?? variables.Get(variableName).ToString();
            }
        }
    }

    public class ParsedPromptInput : ParsedScopeItem
    {
        public string input;
    }

    public class ParsedComment : ParsedScopeItem
    {
        public string comment;
    }

    public class ParsedVariableSet : ParsedScopeItem
    {
        public string variableName;
        public ParsedArgument value;
    }

    public class ParsedEmpty : ParsedScopeItem { }
    #endregion
}
