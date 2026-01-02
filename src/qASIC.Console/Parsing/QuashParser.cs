using System;
using System.Collections;
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

public sealed class QuashParser : ConsoleParser
{
    #region Entry point
    public override object ExecuteParser(qConsoleContext context)
    {
        // When executing for the first time
        if (context.ParserData is not QuashData data)
        {
            data = new();
            data.scopeStack.Push((ParseFromLexer(Lex(new Queue<char>(context.inputString))), 0));
            context.ParserData = data;
        }

        // If command returned a prompt, continue from prompt
        if (context.previousValue is CommandPrompts.CommandPrompt prompt &&
            prompt.CommandContext is qConsoleCommandContext promptContext)
        {
            context.previousValue = context.Console.ExecuteCommand(promptContext);
            if (context.previousValue is CommandPrompts.CommandPrompt)
                return context.previousValue;
        }

        return ExecuteLoop(context, data);
    }

    public override async Task<object> ExecuteParserAsync(qConsoleContext context)
    {
        // When executing for the first time
        if (context.ParserData is not QuashData data)
        {
            data = new();
            data.scopeStack.Push((ParseFromLexer(Lex(new Queue<char>(context.inputString))), 0));
            context.ParserData = data;
        }

        // If command returned a prompt, continue from prompt
        if (context.previousValue is CommandPrompts.CommandPrompt prompt &&
            prompt.CommandContext is qConsoleCommandContext promptContext)
        {
            promptContext.inputString = context.inputString;
            context.previousValue = await context.Console.ExecuteCommandAsync(promptContext);
            if (context.previousValue is CommandPrompts.CommandPrompt)
                return context.previousValue;
        }
        
        return await ExecuteLoopAsync(context, data);
    }
    #endregion

    #region #Executing
    private object ExecuteLoop(qConsoleContext context, QuashData data)
    {
        while (TryGetNextCommandFromLoop(context, data, out var commandContext))
        {
            context.previousValue = context.Console.ExecuteCommand(commandContext);
            
            if (context.previousValue is Task task)
                return SwitchToAsync(task, context, data);
        }

        return context.previousValue;
    }
    
    private async Task<object> ExecuteLoopAsync(qConsoleContext context, QuashData data)
    {
        while (TryGetNextCommandFromLoop(context, data, out var commandContext))
            context.previousValue = await context.Console.ExecuteCommandAsync(commandContext);
        
        return context.previousValue;
    }

    private object SwitchToAsync(Task task, qConsoleContext context, QuashData data)
    {
        var switchTask = SwitchTask(task, context, data);
        Task.Run(() => switchTask);
        return switchTask;
        

        async Task<object> SwitchTask(Task task, qConsoleContext context, QuashData data)
        {
            await task;
            return await ExecuteLoopAsync(context, data);
        }
    }

    private bool TryGetNextCommandFromLoop(qConsoleContext context, QuashData data, out qConsoleCommandContext commandContext)
    {
        commandContext = null;

        while (data.scopeStack.TryPop(out var item))
        {
            (var scope, var i) = item;
            for (; i < scope.items.Count; i++)
            {
                switch (scope.items[i])
                {
                    case ParsedCommand command:
                        commandContext = context.CreateCommandContext();
                        commandContext.commandName = command.commandName;
                        commandContext.inputString = command.GetInputString(ValueParser, data.Variables);
                        commandContext.args = command.GetArguments(ValueParser, data.Variables);

                        data.scopeStack.Push((scope, i+1));
                        return true;
                    case ParsedInputAsk:
                        if (context.previousValue is not CommandPrompts.CommandPrompt) break;

                        data.scopeStack.Push((scope, i+1));
                        return false;
                    case ParsedPromptArgsResponse argsResponse:
                        if (context.previousValue is not CommandPrompts.CommandPrompt argsPrompt ||
                            argsPrompt.CommandContext is not qConsoleCommandContext argsPromptContext) break;


                        commandContext = argsPromptContext;
                        commandContext.args = argsResponse.GetArguments(ValueParser, context.Variables);
                        argsPrompt.Prepare(commandContext);
                        data.scopeStack.Push((scope, i+1));
                        return true;
                    case ParsedPromptLineResponse lineResponse:
                        if (context.previousValue is not CommandPrompts.CommandPrompt linePrompt ||
                            linePrompt.CommandContext is not qConsoleCommandContext linePromptContext) break;

                        commandContext = linePromptContext;
                        commandContext.inputString = lineResponse.line;
                        linePrompt.Prepare(commandContext);
                        data.scopeStack.Push((scope, i+1));
                        return true;
                }
            }
        }

        return false;
    }
    #endregion

    #region Lexer Read Token Args
    // Random
    private static ReadTokenArgs ta_end = ReadTokenArgs.ForSpecificTokens(";", "\n");
    private static ReadTokenArgs ta_whiteSpace = ReadTokenArgs.ForWhiteSpace(';', '\n').WithEscapeCharacters();

    // Reading entire line
    private static ReadTokenArgs ta_lineReadToken = ReadTokenArgs.ForSpecificTokens("#", "^");
    private static ReadTokenArgs ta_lineRead = new() { endChars = ['\n'] };

    // Related to command name
    private static ReadTokenArgs ta_commandName = ReadTokenArgs.ForNonWhite(';', '\n', '=').WithEscapeCharacters();
    private static ReadTokenArgs ta_inputAskToken = ReadTokenArgs.ForSpecificTokens("@");
    
    // Argument related
    private static ReadTokenArgs ta_argsReadToken = ReadTokenArgs.ForSpecificTokens("^^");
    private static ReadTokenArgs ta_argsReadUnwrappd = ReadTokenArgs.ForNonWhite(['\n', ';', '$', '"', '\'', '`',]).WithEscapeCharacters();
    private static ReadTokenArgs ta_argsReadWrappd = new() { endChars = ['$', '"', '\'', '`'], useEscapeCharacters = true, };
    private static ReadTokenArgs ta_wrapToken = ReadTokenArgs.ForSpecificTokens("\"", "'", "`");

    // Variable related
    private static ReadTokenArgs ta_varSetToken = ReadTokenArgs.ForSpecificTokens("=");
    private static ReadTokenArgs ta_varGetToken = ReadTokenArgs.ForSpecificTokens("$");
    private static ReadTokenArgs ta_varGetName = ReadTokenArgs.ForWord(true, ['_']);
    #endregion

    #region Lexer
    /// <summary>Fully lexes a string queue (calls all stages).</summary>
    /// <param name="q">Queue of characters, typically made from a string containing the code.</param>
    /// <returns>Returns a list of lexed items.</returns>
    public static List<LexItem> Lex(Queue<char> q)
    {
        var items = Lex_Stage1(q);
        items = Lex_Stage2(items);
        items = Lex_Stage3(items);

        return items;
    }

    /// <summary>Does the initial lexing - converts a queue of characters to a parserd code scope.</summary>
    /// <param name="q">Queue of characters, typically made from a string containing the code.</param>
    /// <returns>Returns a list of lexed items.</returns>
    private static List<LexItem> Lex_Stage1(Queue<char> q)
    {
        var items = new List<LexItem>();
        while (q.Count > 0)
        {
            // Read white
            ReadToken(q, ta_whiteSpace, out var readWhite);
            if (readWhite.Length > 0)
            {
                items.Add(new LexWhiteSpace(readWhite));
                continue;
            }

            // If immedietally ends after white space, end
            ReadToken(q, ta_end, out var readEnd);
            if (readEnd.Length > 0)
            {
                items.Add(new LexEnd(readEnd));
                continue;
            }

            // Try read token for arguments
            var argsToken = ReadToken(q, ta_argsReadToken, out var readArgsToken);
            if (readArgsToken.Length > 0)
            {
                items.Add(new LexSpecialToken(argsToken.ToString(), readArgsToken));
                while (TryReadArg()) { }
                continue;
            }

            // Try read token for reading the entire line
            var tokenLine = ReadToken(q, ta_lineReadToken, out var readTokenLine);
            if (readTokenLine.Length > 0)
            {
                items.Add(new LexSpecialToken(tokenLine.ToString(), readTokenLine));
                items.Add(new LexCodeText(ReadToken(q, ta_lineRead, out var readLine).ToString(), readLine));
                continue;
            }

            // Try read token for reading only arguments
            bool readCommandName = true;

            // Reading command name
            if (readCommandName)
            {
                var tokenCommand = ReadToken(q, ta_inputAskToken, out var readTokenCommand);
                if (readTokenCommand.Length > 0)
                    items.Add(new LexSpecialToken(tokenCommand.ToString(), readTokenCommand));

                var command = ReadToken(q, ta_commandName, out var readCommand);
                if (readCommand.Length > 0)
                    items.Add(new LexCodeText(command.ToString(), readCommand));
                
                ReadToken(q, ta_whiteSpace, out var readCommandWhite);
                if (readCommandWhite.Length > 0)
                    items.Add(new LexWhiteSpace(readCommandWhite));

                var varSet = ReadToken(q, ta_varSetToken, out var readVarSet);
                if (readVarSet.Length > 0)
                {
                    items.Add(new LexSpecialToken(varSet.ToString(), readVarSet));
                    TryReadArg();
                    continue;
                }
            }

            // White after
            ReadToken(q, ta_whiteSpace, out var readPostWhite);
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
                var ta = curWrap == null ? ta_argsReadUnwrappd : ta_argsReadWrappd;

                // Argument
                var argument = ReadToken(q, ta, out var readArgument);
                if (readArgument.Length > 0)
                    items.Add(new LexCodeText(argument.ToString(), readArgument));
                
                // Wrap character
                var wrapToken = ReadToken(q, ta_wrapToken, out var readWrapToken);
                if (readWrapToken.Length > 0)
                {
                    items.Add(curWrap == null || wrapToken[0] == curWrap ?
                        new LexSpecialToken(wrapToken.ToString(), readWrapToken) :
                        new LexCodeText(wrapToken.ToString(), readWrapToken));
                    
                    curWrap = curWrap == null ? wrapToken[0] : (curWrap == wrapToken[0] ? null : curWrap);
                    continue;
                }

                // Variable
                var varToken = ReadToken(q, ta_varGetToken, out var readVarToken);
                if (readVarToken.Length > 0)
                {
                    var varName = ReadToken(q, ta_varGetName, out var readVarName);
                    items.Add(new LexSpecialToken(varToken.ToString(), readVarToken));
                    items.Add(new LexCodeText(varName.ToString(), readVarName));
                    items.Add(new LexBreak());
                    continue;
                }

                if (curWrap == null)
                {
                    // White after
                    ReadToken(q, ta_whiteSpace, out var readPostWhite);
                    if (readPostWhite.Length > 0)
                    {
                        items.Add(new LexWhiteSpace(readPostWhite));
                        return true;
                    }
                }

                ReadToken(q, ta_end, out var readEndToken);
                if (readEndToken.Length > 0)
                {
                    items.Add(new LexEnd(readEndToken));
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>Merges consecutive lex items of same type.</summary>
    /// <param name="items">List of lexed items from stage 1.</param>
    /// <returns>Returns the supplied parameter <paramref name="items"/>.</returns>
    private static List<LexItem> Lex_Stage2(List<LexItem> items)
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

    /// <summary>Sets <see cref="LexCodeText.type"/> of <see cref="LexCodeText"/> items.</summary>
    /// <param name="items">List of lexed items from stage 2.</param>
    /// <returns>Returns the supplied parameter <paramref name="items"/>.</returns>
    private static List<LexItem> Lex_Stage3(List<LexItem> items)
    {
        var nextType = LexItem.Type.Command;
        for (int i = 0; i < items.Count; i++)
        {
            items[i].type = nextType;
            switch (items[i])
            {
                case LexBreak:
                    if (nextType is LexItem.Type.VarGet)
                    {
                        items[i].type = LexItem.Type.ArgumentPart;
                        nextType = LexItem.Type.ArgumentPart;
                    }
                    break;
                case LexWhiteSpace:
                    items[i].type = LexItem.Type.Space;

                    if (nextType is LexItem.Type.Command)
                        nextType = LexItem.Type.ArgumentPart;
                    break;
                case LexSpecialToken token:
                    switch (token.token)
                    {
                        case "=":   
                            token.type = LexItem.Type.VarSet;
                            if (i-1 > 0 && items[i-1] is LexCodeText varSet)
                                varSet.type = LexItem.Type.VarSet;
                            else if (i-2 > 0 && items[i-1] is LexWhiteSpace && items[i-2] is LexCodeText varSet2)
                                varSet2.type = LexItem.Type.VarSet;
                            break;
                        case "$":
                            nextType = nextType == LexItem.Type.ResponseArgsPart ?
                                LexItem.Type.ResponseArgsVarGet :
                                LexItem.Type.VarGet;
                            
                            token.type = nextType;
                            break;
                        case "#":
                            nextType = LexItem.Type.Comment;
                            token.type = nextType;
                            break;
                        case "^":
                            nextType = LexItem.Type.ResponseLine;
                            token.type = nextType;
                            break;
                        case "^^":
                            nextType = LexItem.Type.ResponseArgsPart;
                            token.type = nextType;
                            break;
                    }

                    break;
                case LexEnd:
                    items[i].type = LexItem.Type.End;
                    nextType = LexItem.Type.Command;
                    break;
            }
        }

        return items;
    }
    #endregion

    #region Token Reading
    /// <summary>Reads a part of the queue.</summary>
    /// <param name="q">Queue of characters, typically made from a string containing the code.</param>
    /// <param name="args">Arguments specifying how much should be read.</param>
    /// <param name="readString">Contains all characters removed from the queue.</param>
    /// <returns>Returns the read token.</returns>
    private static StringBuilder ReadToken(Queue<char> q, ReadTokenArgs args, out StringBuilder readString)
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
    
    private struct ReadTokenArgs
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
    #endregion

    #region Parser
    /// <summary>Parses lexed items into a code scope.</summary>
    /// <param name="items">List of lexed items.</param>
    /// <returns>Returns a parsed code scope.</returns>
    public static ParsedCodeScope ParseFromLexer(List<LexItem> items)
    {
        var q = new Queue<LexItem>(items);
        var scope = new ParsedCodeScope();
        while (q.TryDequeue(out var item))
        {
            switch (item)
            {
                case LexCodeText codeText:
                    switch (codeText.type)
                    {
                        case LexCodeText.Type.Command:
                            ReadSingleCommand(codeText);
                            break;
                        case LexCodeText.Type.VarSet:
                            ReadVarSet(codeText);
                            break;
                        case LexCodeText.Type.ResponseLine:
                            scope.items.Add(new ParsedPromptLineResponse()
                            {
                                line = codeText.text,
                                inputString = codeText.readString,
                            });
                            break;
                    }
                    break;
                case LexSpecialToken token:
                    if (token.token == "@")
                    {
                        if (q.TryPeek(out var nextItem) && nextItem is LexCodeText nextCode && nextCode.type == LexCodeText.Type.Command)
                            ReadSingleCommand(nextCode);
                        
                        scope.items.Add(new ParsedInputAsk());
                    }

                    if (token.token == "^^")
                    {
                        var response = new ParsedPromptArgsResponse();
                        if (q.TryPeek(out var nextItem) && nextItem is LexWhiteSpace whiteBefore)
                            response.whiteBefore = whiteBefore.readString;
                        
                        response.whiteBefore ??= new();
                        response.arguments = ReadArguments();
                        scope.items.Add(response);
                    }
                    break;
            }
        }

        return scope;


        void ReadSingleCommand(LexCodeText commandCode)
        {
            var cmd = new ParsedCommand()
            {
                commandName = commandCode.text,
            };

            if (q.TryPeek(out var cmdWhite) && cmdWhite is LexWhiteSpace)
            {
                q.Dequeue();
                cmd.whiteAfter = cmdWhite.readString;
            }

            cmd.whiteAfter ??= new();
            cmd.arguments = ReadArguments();

            scope.items.Add(cmd);
        }

        List<ParsedCommand.Argument> ReadArguments()
        {
            var list = new List<ParsedCommand.Argument>();

            while (TryReadSingleArgumentParts(out var arg))
                list.Add(arg);

            return list;
        }

        bool TryReadSingleArgumentParts(out ParsedCommand.Argument arg)
        {
            arg = null;
            while (q.TryPeek(out var item))
            {
                switch (item)
                {
                    case LexEnd:
                        arg?.whiteAfter ??= new();
                        return arg != null;
                    case LexCodeText code:
                        switch (code.type)
                        {
                            case LexCodeText.Type.ResponseArgsPart:
                            case LexCodeText.Type.ArgumentPart:
                                arg ??= new();
                                arg.parts.Add(new ParsedCommand.Argument.TextPart(code.text, code.readString));
                                break;
                            case LexCodeText.Type.ResponseArgsVarGet:
                            case LexCodeText.Type.VarGet:
                                arg ??= new();
                                arg.parts.Add(new ParsedCommand.Argument.VariablePart(code.text));
                                break;
                        }
                        break;
                    case LexWhiteSpace argWhite:
                        arg?.whiteAfter = argWhite.readString;
                        q.Dequeue();
                        return arg != null;
                }

                q.Dequeue();
            }

            arg?.whiteAfter ??= new();
            return arg != null;
        }
    
        void ReadVarSet(LexCodeText codeStart)
        {
            var varSet = new ParsedVariableSet()
            {
                variableName = codeStart.text,
                argument = TryReadSingleArgumentParts(out var arg) ? arg : new(),
            };
            
            scope.items.Add(varSet);
        }
    }
    #endregion

    #region Other overrides
    public override CmdCharacterInfo GetCharacterInfo(string cmd, int characterIndex)
    {
        var data = new CharacterInfoData(Lex(new(cmd)));
        var info = new CmdCharacterInfo()
        {
            ParserData = data,
            avaliableVariables = from item in data.lexedItems
                where item is LexCodeText { type: LexItem.Type.VarGet }
                select (item as LexCodeText).text
        };

        var charI = 0;

        var commandName = new StringBuilder();
        var argumentVal = new StringBuilder();
        var argIndex = -1;
        var varName = new StringBuilder();
        var argIsNothing = false;

        LexItem startItem = null;
        LexItem endItem = null;
        for (int i = 0; i < data.lexedItems.Count; i++)
        {
            var item = data.lexedItems[i];
            charI += item.readString.Length;
            
            startItem ??= item;
            if (startItem.type != item.type)
                startItem = item;

            switch (item, item.type)
            {
                default:
                    commandName.Clear();
                    argumentVal.Clear();
                    argIndex = -1;
                    break;
                case (_, LexItem.Type.Space):
                    argumentVal.Clear();
                    argIndex += 1;
                    argIsNothing = false;
                    continue;
                case (LexCodeText cmdCode, LexItem.Type.Command):
                    commandName.Append(cmdCode.text);
                    break;
                case (LexCodeText argCode, LexItem.Type.ArgumentPart):
                    argumentVal.Append(argCode.text);
                    break;
                case (LexCodeText varCode, LexItem.Type.VarGet):
                    varName.Clear();
                    varName.Append(varCode.text);
                    argIsNothing = true;
                    break;
                case (_, LexItem.Type.ArgumentPart or LexItem.Type.Command or LexItem.Type.VarGet):
                    break;
            }

            if (characterIndex <= charI)
            {
                endItem = item;
                break;
            }
        }

        // If we are at the end of a "line"
        // line meaning a command with arguments
        if (endItem == null)
        {
            info.scope = commandName.Length == 0 ?
                CmdCharacterInfo.Scope.CommandName :
                CmdCharacterInfo.Scope.Argument;
            
            info.commandName = commandName.ToString();
            info.argumentIndex = argIndex;
            return info;
        }

        var startIndex = data.lexedItems.IndexOf(startItem);
        var endIndex = data.lexedItems.IndexOf(endItem);
        for (; endIndex < data.lexedItems.Count-1; endIndex++)
        {
            // If the next thing after an argument part is a
            // var get -> stop trying to handle an argument
            if (endItem.type is LexItem.Type.ArgumentPart && data.lexedItems[endIndex+1].type is LexItem.Type.VarGet)
                argIsNothing = true;

            if (endItem.type != data.lexedItems[endIndex+1].type) break;
            endItem = data.lexedItems[endIndex+1];

            if (endItem is LexCodeText codeText)
            {
                var str = endItem.type switch
                {
                    LexItem.Type.ArgumentPart => argumentVal,
                    LexItem.Type.Command => commandName,
                    LexItem.Type.VarGet => varName,
                    _ => null,
                };

                str?.Append(codeText.text);
            }
        }

        data.index = startIndex;
        data.length = endIndex - startIndex + 1;

        if (endItem.type == LexItem.Type.ArgumentPart && !argIsNothing)
        {
            info.scope = CmdCharacterInfo.Scope.Argument;
            info.commandName = commandName.ToString();
            info.argument = argumentVal.ToString();
            info.argumentIndex = argIndex;
            return info;
        }

        if (endItem.type == LexItem.Type.Command)
        {
            info.scope = CmdCharacterInfo.Scope.CommandName;
            info.commandName = commandName.ToString();
            return info;
        }
        
        if (endItem.type == LexItem.Type.VarGet)
        {
            data.index++;
            data.length--;
            info.scope = CmdCharacterInfo.Scope.Variable;
            info.variableName = varName.ToString();
            return info;
        }

        info.scope = CmdCharacterInfo.Scope.Nothing;
        return info;
    }

    public override string ReplaceCharacterInfo(CmdCharacterInfo info, string newValue, out int position, out int length)
    {
        position = 0;
        length = 0;
        if (info.ParserData is not CharacterInfoData data) return string.Empty;
        var txt = new StringBuilder();

        var type = data.lexedItems[data.index].type;
        data.lexedItems.RemoveRange(data.index, data.length);

        LexItem target;
        
        switch (type)
        {
            case LexItem.Type.ArgumentPart:
                var inputStr = newValue.Replace(" ", "\\ ")
                    .Replace("\"", "\\\"")
                    .Replace("`", "\\`")
                    .Replace("'", "\\'");

                target = new LexCodeText(newValue, new(inputStr))
                {
                    type = type,
                };
                break;
            default:
                target = new LexCodeText(newValue, new(newValue))
                {
                    type = type,
                };
                break;
        }

        data.lexedItems.Insert(data.index, target);
        length = target.readString.Length;

        var addPos = true;
        foreach (var item in data.lexedItems)
        {
            if (item == target)
                addPos = false;
            
            if (addPos)
                position += item.readString.Length;
            
            txt.Append(item.readString);
        }

        return txt.ToString();
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

    #region Non parser classes
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
    
    public class CharacterInfoData(List<LexItem> lexedItems)
    {
        public List<LexItem> lexedItems = lexedItems;
        public int index;
        public int length;
    }
    #endregion

    #region Lexed Items
    public abstract class LexItem(StringBuilder readString)
    {
        public enum Type
        {
            Unknown,
            Space,
            End,
            Command,
            ArgumentPart,
            VarGet,
            VarSet,
            Comment,
            ResponseLine,
            ResponseArgsPart,
            ResponseArgsVarGet,
            Other,
        }

        public Type type;
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

    #region Parsed Items
    public class ParsedCodeScope
    {
        public readonly List<ParsedItem> items = [];
    }

    public abstract class ParsedItem { }

    public class ParsedCommand : ParsedItem
    {
        public string commandName;
        public StringBuilder whiteAfter;
        public List<Argument> arguments = [];

        public QuashArgument[] GetArguments(ModularParser parser, qConsoleVariableList variables) =>
            GetArguments(whiteAfter, arguments, parser, variables);
    
        public string GetInputString(ModularParser parser, qConsoleVariableList variables)
        {
            var txt = new StringBuilder()
                .Append(commandName)
                .Append(whiteAfter);
            
            foreach (var item in arguments)
                txt.Append(item.GetInputString(parser, variables));
            
            return txt.ToString();
        }

        public static QuashArgument[] GetArguments(StringBuilder startWhite, List<Argument> arguments, ModularParser parser, qConsoleVariableList variables)
        {
            var args = new QuashArgument[arguments.Count];

            for (int i = 0; i < arguments.Count; i++)
            {
                var whiteBefore = (i == 0 ? startWhite : arguments[i-1].whiteAfter).ToString();
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
            public List<ArgumentPart> parts = [];
            public StringBuilder whiteAfter;

            public StringBuilder GetInputString(ModularParser parser, qConsoleVariableList variables)
            {
                var txt = new StringBuilder();
                foreach (var item in parts)
                    txt.Append(item.GetInputString(parser, variables));
                
                txt.Append(whiteAfter);
                return txt;
            }

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

        public QuashArgument[] GetArguments(ModularParser parser, qConsoleVariableList variables) =>
        [
            new QuashArgument(parser, line, inputString.ToString(), new StringBuilder(), new StringBuilder(), Array.Empty<object>())
        ];
    }

    public class ParsedPromptArgsResponse : ParsedItem
    {
        public StringBuilder whiteBefore;
        public List<ParsedCommand.Argument> arguments = [];

        public QuashArgument[] GetArguments(ModularParser parser, qConsoleVariableList variables) =>
            ParsedCommand.GetArguments(whiteBefore, arguments, parser, variables);
    }
    #endregion
}
