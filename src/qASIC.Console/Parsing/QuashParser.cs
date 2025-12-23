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

        // if (data.scopeStack.Count == 0)
        //     data.scopeStack.Push((QToScope(new Queue<char>(context.inputString)), 0));

        return ExecuteLoop(context, data);
    }

    public override async Task<object> ExecuteParserAsync(qConsoleContext context)
    {
        if (context.ParserData is not QuashData data)
        {
            data = new();
            context.ParserData = data;
        }

        // if (context.previousValue is CommandPrompts.CommandPrompt)
        //     data.scopeStack.Push((new ParsedCodeScope() { items = [new ParsedPromptInput() { input = context.inputString }] }, 0));

        // if (data.scopeStack.Count == 0)
        //     data.scopeStack.Push((QToScope(new Queue<char>(context.inputString)), 0));
        
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
            // (var scope, var i) = item;

            // for (; i < scope.items.Count; i++)
            // {
            //     if (scope.items[i] is not ParsedCommand and not ParsedPromptInput)
            //         continue;

            //     // In case the previous command returned a prompt
            //     if (context.previousValue is CommandPrompts.CommandPrompt prompt)
            //     {
            //         if (!firstNow && (!data.executePromptSelf || scope.items[i] is ParsedCommand { askForUserInput: true }))
            //         {
            //             data.scopeStack.Push((scope, i));
            //             return false;
            //         }

            //         var args = scope.items[i] switch
            //         {
            //             ParsedCommand cmd => cmd.CreateQuashArgs(ValueParser, context.Variables),
            //             ParsedPromptInput input => [new QuashArgument(ValueParser, input.input)],
            //             _ => [],
            //         };

            //         var inputString = scope.items[i] switch
            //         {
            //             ParsedPromptInput input => input.input,
            //             ParsedCommand cmd => ConvertToString(cmd.commandName, args),
            //             _ => string.Empty,
            //         };

            //         if (!prompt.ParseArguments)
            //             args = [new QuashArgument(ValueParser, inputString)];
                    
            //         prompt.CommandContext.args = args;
            //         prompt.CommandContext.inputString = inputString;
            //         commandContext = prompt.CommandContext as qConsoleCommandContext;
            //         return true;
            //     }

            //     // Set this to false when finished with executing from command prompts
            //     data.executePromptSelf = false;

            //     if (scope.items[i] is ParsedCommand parsedCommand)
            //     {
            //         commandContext = context.CreateCommandContext();
            //         commandContext.commandName = parsedCommand.commandName;
            //         commandContext.args = parsedCommand.CreateQuashArgs(ValueParser, context.Variables);
            //         commandContext.inputString = ConvertToString(commandContext.commandName, commandContext.args);
            //         data.scopeStack.Push((scope, i + 1));
            //         data.executePromptSelf = !parsedCommand.askForUserInput;
            //         return true;
            //     }
            // }
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

    #region Lexer
    /// <summary>Converts a queue of characters to a parsed code scope.</summary>
    /// <param name="q">Queue of characters, typically made from a string containing the code.</param>
    /// <returns>Returns the parsed code scope.</returns>
    public static List<LexItem> Lex(Queue<char> q)
    {
        var items = Lex_Stage1(q);
        items = Lex_Stage2(items);

        return items;
    }

    /// <summary>Does the initial lexing - converts a queue of characters to a parserd code scope.</summary>
    /// <param name="q"></param>
    /// <returns></returns>
    protected static List<LexItem> Lex_Stage1(Queue<char> q)
    {
        var items = new List<LexItem>();
        while (q.Count > 0)
        {
            // Read white
            ReadToken(q, Ta_WhiteSpace, out var readWhite);
            if (readWhite.Length > 0)
            {
                items.Add(new LexWhiteSpace(readWhite));
                continue;
            }

            // If immedietally ends after white space, end
            ReadToken(q, Ta_CommandEnd, out var readEnd);
            if (readEnd.Length > 0)
            {
                items.Add(new LexEnd(readEnd));
                continue;
            }

            // Try read token for arguments
            var argsToken = ReadToken(q, ReadTokenArgs.ForSpecificTokens("^^"), out var readArgsToken);
            if (readArgsToken.Length > 0)
            {
                items.Add(new LexSpecialToken(argsToken.ToString(), readArgsToken));
                while (TryReadArg()) { }
                continue;
            }

            // Try read token for reading the entire line
            var tokenLine = ReadToken(q, ReadTokenArgs.ForSpecificTokens("#", "^"), out var readTokenLine);
            if (readTokenLine.Length > 0)
            {
                items.Add(new LexSpecialToken(tokenLine.ToString(), readTokenLine));
                items.Add(new LexCodeText(ReadToken(q, Ta_CommentEnd, out var readLine).ToString(), readLine));
                continue;
            }

            // Try read token for reading only arguments
            bool readCommandName = true;

            // Reading command name
            if (readCommandName)
            {
                var tokenCommand = ReadToken(q, ReadTokenArgs.ForSpecificTokens("@"), out var readTokenCommand);
                if (readTokenCommand.Length > 0)
                    items.Add(new LexSpecialToken(tokenCommand.ToString(), readTokenCommand));

                var command = ReadToken(q, Ta_CommandStart, out var readCommand);
                if (readCommand.Length > 0)
                    items.Add(new LexCodeText(command.ToString(), readCommand));
                
                ReadToken(q, Ta_WhiteSpace, out var readCommandWhite);
                if (readCommandWhite.Length > 0)
                    items.Add(new LexWhiteSpace(readCommandWhite));

                var varSet = ReadToken(q, ReadTokenArgs.ForSpecificTokens("="), out var readVarSet);
                if (readVarSet.Length > 0)
                {
                    items.Add(new LexSpecialToken(varSet.ToString(), readVarSet));
                    TryReadArg();
                    continue;
                }
            }

            // White after
            ReadToken(q, Ta_WhiteSpace, out var readPostWhite);
            if (readPostWhite.Length > 0)
                items.Add(new LexWhiteSpace(readPostWhite));

            // Reading arguments
            while (TryReadArg()) { }
        }

        return items;


        bool TryReadArg()
        {
            char? curWrap = null;
            while (q.Count > 0)
            {
                var ta = curWrap == null ? 
                    ReadTokenArgs.ForNonWhite(['\n', ';', '$', '"', '\'', '`',]) :
                    new() { endChars = ['$', '"', '\'', '`'] };

                // Argument
                var argument = ReadToken(q, ta.WithEscapeCharacters(), out var readArgument);
                if (readArgument.Length > 0)
                    items.Add(new LexCodeText(argument.ToString(), readArgument));
                
                // Wrap character
                var wrapToken = ReadToken(q, ReadTokenArgs.ForSpecificTokens("\"", "'", "`"), out var readWrapToken);
                if (readWrapToken.Length > 0)
                {
                    items.Add(curWrap == null || wrapToken[0] == curWrap ?
                        new LexSpecialToken(wrapToken.ToString(), readWrapToken) :
                        new LexCodeText(wrapToken.ToString(), readWrapToken));
                    
                    curWrap = curWrap == null ? wrapToken[0] : (curWrap == wrapToken[0] ? null : curWrap);
                    continue;
                }

                // Variable
                var varToken = ReadToken(q, ReadTokenArgs.ForSpecificTokens("$"), out var readVarToken);
                if (readVarToken.Length > 0)
                {
                    var varName = ReadToken(q, ReadTokenArgs.ForWord(true, ['_']), out var readVarName);
                    items.Add(new LexSpecialToken(varToken.ToString(), readVarToken));
                    items.Add(new LexCodeText(varName.ToString(), readVarName));
                    items.Add(new LexBreak());
                    continue;
                }

                if (curWrap == null)
                {
                    // White after
                    ReadToken(q, Ta_WhiteSpace, out var readPostWhite);
                    if (readPostWhite.Length > 0)
                    {
                        items.Add(new LexWhiteSpace(readPostWhite));
                        return true;
                    }
                }

                ReadToken(q, ReadTokenArgs.ForSpecificTokens("\n", ";"), out var readEndToken);
                if (readEndToken.Length > 0)
                {
                    items.Add(new LexEnd(readEndToken));
                    return false;
                }
            }

            return false;
        }
    }

    protected static List<LexItem> Lex_Stage2(List<LexItem> items)
    {
        for (int i = 1; i < items.Count; i++)
        {
            switch (items[i-1], items[i])
            {
                case (LexCodeText code1, LexCodeText code2):
                    code1.readString.Append(code2.readString);
                    code1.text += code2.text;
                    items.RemoveAt(i);
                    i--;
                    break;
                case (LexWhiteSpace space1, LexWhiteSpace space2):
                    space1.readString.Append(space2.readString);
                    items.RemoveAt(i);
                    i--;
                    break;
            }
        }

        return items;
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
            
            if ((args.validChars != null || args.endOnNonWhite || args.endOnNonLetter) && (args.validChars == null || !args.validChars.Contains(c)) && (!args.endOnNonWhite || !char.IsWhiteSpace(c)) && (!args.endOnNonLetter || !char.IsLetterOrDigit(c)))
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

    #region Parser
    public static ParsedCodeScope ParseFromLexer(List<LexItem> items)
    {
        var scope = new ParsedCodeScope();
        for (int i = 0; i < items.Count; i++)
            scope.items.AddRange(ReadForI(ref i));

        return scope;


        List<ParsedItem> ReadForI(ref int i)
        {
            var list = new List<ParsedItem>();
            var varSetList = new List<ParsedVariableSet>();
            switch (items[i])
            {
                case LexSpecialToken token:
                    if (token.token == "@")
                    {
                        bool success = TryReadCode(ref i, out var cmd);

                        // If the thing is in front of a command,
                        // have the command first, then the ask
                        if (cmd is ParsedCommand)
                        {
                            list.Add(cmd);
                            list.Add(new ParsedInputAsk());
                            break;
                        }

                        if (cmd is ParsedVariableSet varSet)
                            varSetList.Add(varSet);
                        
                        // Otherwise, just ask for input
                        list.Add(new ParsedInputAsk());

                        // And if we read something, add it after
                        if (success)
                            list.Add(cmd);
                        
                        break;
                    }

                    if (token.token == "^")
                    {
                        var response = new ParsedPromptLineResponse();
                        if (i+1 < items.Count && items[i+1] is LexCodeText line)
                        {
                            i++;
                            response.line = line.text;
                            response.inputString = line.readString;
                        }

                        response.inputString ??= new();
                        list.Add(response);
                    }

                    if (token.token == "^^")
                    {
                        var response = new ParsedPromptArgsResponse();
                        if (i+1 < items.Count && items[i+1] is LexWhiteSpace space)
                        {
                            response.whiteBefore = space.readString;
                            i++;
                        }
                        
                        response.whiteBefore ??= new();
                        response.arguments = ReadArguments(ref i);
                        list.Add(response);
                    }

                    if (token.token == "#")
                    {
                        if (i+1 < items.Count && items[i+1] is LexCodeText)
                            i++;
                    }
                    break;
                case LexCodeText:
                    {
                        if (TryReadCode(ref i, out var code))
                            list.Add(code);
                    }
                    break;
            }
            
            return list;
        }

        bool TryReadCode(ref int i, out ParsedItem item)
        {
            item = null;
            if (items[i] is not LexCodeText cmdText) return false;

            var cmd = new ParsedCommand()
            {
                commandName = cmdText.text,
            };

            item = cmd;

            i++;

            if (i < items.Count && items[i] is LexWhiteSpace cmdWhite)
            {
                cmd.whiteAfter = cmdWhite.readString;
                i++;
            }

            if (i < items.Count && items[i] is LexSpecialToken token && token.token == "=")
            {
                ReadSingleArgument(ref i, out var varSetArg);
                var varSet = new ParsedVariableSet()
                {
                    variableName = cmd.commandName,
                    argument = varSetArg,
                };

                item = varSet;
                return true;
            }
            
            cmd.whiteAfter ??= new();
            cmd.arguments = ReadArguments(ref i);
            return true;
        }

        List<ParsedCommand.Argument> ReadArguments(ref int i)
        {
            var list = new List<ParsedCommand.Argument>();
            ParsedCommand.Argument arg;
            while (ReadSingleArgument(ref i, out arg))
                list.Add(arg);
            
            if (arg != null)
                list.Add(arg);

            return list;
        }

        bool ReadSingleArgument(ref int i, out ParsedCommand.Argument arg)
        {
            arg = null;
            while (i < items.Count)
            {
                switch (items[i])
                {
                    case LexCodeText codeText:
                        arg ??= new();
                        arg.parts.Add(new ParsedCommand.Argument.TextPart(codeText.text, codeText.readString));
                        break;
                    case LexSpecialToken token:
                        arg ??= new();

                        if (token.token == "$" && i+1 < items.Count && items[i+1] is LexCodeText varName)
                        {
                            arg.parts.Add(new ParsedCommand.Argument.VariablePart(varName.text));
                            i++;
                            break;
                        }

                        arg.parts.Add(new ParsedCommand.Argument.TextPart(string.Empty, token.readString));
                        break;
                    case LexWhiteSpace white:
                        if (arg != null)
                        {
                            arg.whiteAfter = white.readString;
                            return true;
                        }
                        break;
                    case LexEnd:
                        if (arg != null)
                        {
                            arg.whiteAfter = new();
                            return true;
                        }

                        return false;
                }

                i++;
            }

            return false;
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

    public class QuashArgument(ModularParser parser, string arg, params object[] values) : qCommandArgument(parser, arg, values)
    {
        public QuashArgument(ModularParser parser, string arg, string unparsedArgString, string whiteBefore, string whiteAfter, params object[] values) : this(parser, arg, values)
        {
            UnparsedArgString = unparsedArgString;
            WhiteBefore = whiteBefore;
            WhiteAfter = whiteAfter;
        }

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

    #region Lexed Objects
    public abstract class LexItem(StringBuilder readString)
    {
        public readonly StringBuilder readString = readString;
    }

    public class LexSpecialToken(string token, StringBuilder readString) : LexItem(readString)
    {
        public string token = token;
    }

    public class LexWhiteSpace(StringBuilder readString) : LexItem(readString) { }

    public class LexCodeText(string text, StringBuilder readString) : LexItem(readString)
    {
        public string text = text;
    }

    public class LexEnd(StringBuilder readString) : LexItem(readString) { }

    public class LexBreak() : LexItem(new()) { }
    #endregion

    public class ParsedCodeScope
    {
        public readonly List<ParsedItem> items = [];
    }

    public abstract class ParsedItem
    {
        
    }

    public class ParsedCommand : ParsedItem
    {
        public string commandName;
        public StringBuilder whiteAfter;
        public List<Argument> arguments = [];

        public QuashArgument[] GetArguments(ModularParser parser, qConsoleVariableList variables)
        {
            var args = new QuashArgument[arguments.Count];

            for (int i = 0; i < arguments.Count; i++)
            {
                var whiteBefore = (i == 0 ? this.whiteAfter : arguments[i-1].whiteAfter).ToString();
                var whiteAfter = arguments[i].whiteAfter.ToString();

                var arg = new StringBuilder();
                var inputString = new StringBuilder();

                foreach (var item in arguments[i].parts)
                {
                    arg.Append(item.GetArg(parser, variables));
                    inputString.Append(item.GetInputString(parser, variables));
                }

                var objs = Array.Empty<object>();

                if (arguments[i].parts.Count == 1 && arguments[i].parts[0].TryGetObj(parser, variables, out object obj))
                    objs = [obj];

                args[i] = new QuashArgument(parser, arg.ToString(), inputString.ToString(), whiteBefore, whiteAfter, objs);
            }

            return args;
        }
    
        public class Argument
        {
            public StringBuilder whiteAfter;
            public List<ArgumentPart> parts = [];

            public abstract class ArgumentPart
            {
                public abstract string GetArg(ModularParser parser, qConsoleVariableList variables);
                public abstract string GetInputString(ModularParser parser, qConsoleVariableList variables);
                public virtual bool TryGetObj(ModularParser parser, qConsoleVariableList variables, out object obj)
                {
                    obj = null;
                    return false;
                }
            }

            public class TextPart(string text, StringBuilder inputString) : ArgumentPart
            {
                public string text = text;
                public StringBuilder inputString = inputString;

                public override string GetArg(ModularParser parser, qConsoleVariableList variables) => text;
                public override string GetInputString(ModularParser parser, qConsoleVariableList variables) => inputString.ToString();
            }

            public class VariablePart(string variableName) : ArgumentPart
            {
                public string variableName = variableName;

                public override string GetArg(ModularParser parser, qConsoleVariableList variables) => variables.Get(variableName).ToString();
                public override string GetInputString(ModularParser parser, qConsoleVariableList variables) => $"${variables.Get(variableName)}";
                public override bool TryGetObj(ModularParser parser, qConsoleVariableList variables, out object obj)
                {
                    obj = variables.Get(variableName);
                    return true;
                }
            }
        }
    }

    public class ParsedVariableSet : ParsedItem
    {
        public string variableName;
        public ParsedCommand.Argument argument;
    }

    public class ParsedInputAsk : ParsedItem { }

    public class ParsedPromptLineResponse : ParsedItem
    {
        public StringBuilder inputString;
        public string line;
    }

    public class ParsedPromptArgsResponse : ParsedItem
    {
        public StringBuilder whiteBefore;
        public List<ParsedCommand.Argument> arguments = [];
    }
}
