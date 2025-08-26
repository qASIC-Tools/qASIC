using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qASIC.CommandPrompts;
using qASIC.Logging;
using qASIC.Parsing;

namespace qASIC.Console.Parsing
{
    public class QuashParser : ConsoleParser
    {
        public static readonly char[] Char_Escape = new char[]
        {
            '\\',
        };

        public static readonly char[] Char_End = new char[]
        {
            '\n',
            ';'
        };

        public static readonly char[] Char_Wrapping = new char[]
        {
            '\"',
            '\'',
            '`',
        };

        public static ReadTokenArgs TA_CommandName = new ReadTokenArgs()
        {
            useEscapeCharacters = true,
            endOnEndCharacters = true,
            endOnWhiteSpace = true,
        };

        public static ReadTokenArgs TA_Argument = new ReadTokenArgs()
        {
            useEscapeCharacters = true,
            useWrapping = true,
            endOnEndCharacters = true,
            endOnWhiteSpace = true,
        };

        #region Entry point
        public override object ExecuteParser(qConsoleContext context)
        {
            object returnedValue = null;
            if (TryPreparePromptContextForStart(context, out var promptContext))
            {
                returnedValue = context.Console.ExecuteCommand(promptContext);
                if (returnedValue is Task task)
                    return SwitchExecToAsync(CreateQAndPrepare(context), context, promptContext, task);
            }

            return ExecQ(CreateQAndPrepare(context), context, returnedValue);
        }

        public override async Task<object> ExecuteParserAsync(qConsoleContext context)
        {
            object returnedValue = null;
            if (TryPreparePromptContextForStart(context, out var promptContext))
                returnedValue = await context.Console.ExecuteCommandAsync(promptContext);

            return await ExecQAsync(CreateQAndPrepare(context), context, returnedValue);
        }
        #endregion

        #region Preparation
        protected bool TryPreparePromptContextForStart(qConsoleContext context, out qConsoleCommandContext promptContext)
        {
            promptContext = null;
            if (context.previousValue is CommandPrompt prompt &&
                prompt.CommandContext is qConsoleCommandContext c)
            {
                promptContext = c;
                promptContext.prompt = prompt;
                promptContext.inputString = context.inputString;

                if (prompt.ParseArguments)
                {
                    ReadCommand(new Queue<char>(promptContext.inputString), out promptContext.inputString, out promptContext.commandName, out var promptArgs);
                    promptContext.args = promptArgs.ToArray();
                }

                prompt.Prepare(promptContext);
                context.inputString = string.Empty;
                return true;
            }

            return false;
        }

        protected Queue<char> CreateQAndPrepare(qConsoleContext context)
        {
            if (!(context.ParserData is QuashData))
            {
                context.ParserData = new QuashData()
                {
                    Logs = context.Logs ?? new qLogManager(),
                    queue = CreateQ(context.inputString)
                };
            }

            return (context.ParserData as QuashData).queue;
        }

        protected Queue<char> CreateQ(string inputString) =>
            new Queue<char>(inputString.Replace("\r\n", "\n"));
        #endregion

        #region Executing loops
        private object ExecQ(Queue<char> q, qConsoleContext context, object returnedValue = null)
        {
            while (q.Count > 0)
            {
                var cmdContext = ReadCommand(q, context, returnedValue);
                returnedValue = context.Console.ExecuteCommand(cmdContext);
                if (returnedValue is Task task)
                    return SwitchExecToAsync(q, context, cmdContext, task);
            }

            FinishExecuting(context);
            return returnedValue;
        }

        private Task SwitchExecToAsync(Queue<char> q, qConsoleContext context, qConsoleCommandContext cmdContext, Task task)
        {
            var switchTask = SwitchExecToAsyncTask(q, context, cmdContext, task);
            Task.Run(() => switchTask);
            return switchTask;
        }

        private async Task SwitchExecToAsyncTask(Queue<char> q, qConsoleContext context, qConsoleCommandContext cmdContext, Task task)
        {
            var returnedValue = await context.Console.ExecuteCodeAsync(cmdContext.commandName, task, cmdContext.Logs);
            await ExecQAsync(q, context, returnedValue);
        }

        private async Task<object> ExecQAsync(Queue<char> q, qConsoleContext context, object returnedValue = null)
        {
            while (q.Count > 0)
            {
                var cmdContext = ReadCommand(q, context, returnedValue);
                returnedValue = await context.Console.ExecuteCommandAsync(cmdContext);
            }

            FinishExecuting(context);
            return returnedValue;
        }
        #endregion

        #region Reading
        private void ReadLine(Queue<char> q, out string line)
        {
            var txt = new StringBuilder();
            while (q.TryDequeue(out var c))
            {
                if (Char_Escape.Contains(c))
                {
                    if (q.TryDequeue(out c))
                        txt.Append(c);

                    continue;
                }

                if (Char_End.Contains(c))
                    break;

                txt.Append(c);
            }

            line = txt.ToString();
        }

        private qConsoleCommandContext ReadCommand(Queue<char> q, qConsoleContext context, object returnedValue)
        {
            if (returnedValue is CommandPrompt prompt &&
                prompt.CommandContext is qConsoleCommandContext promptContext)
            {
                switch (prompt.ParseArguments)
                {
                    case true:
                        ReadCommand(q, out promptContext.inputString, out promptContext.commandName, out var promptArgs);
                        promptContext.args = promptArgs.ToArray();
                        break;
                    case false:
                        ReadLine(q, out var line);
                        promptContext.inputString = line;
                        break;
                }

                return promptContext;
            }

            var cmdContext = context.CreateCommandContext();
            cmdContext.Logs ??= new qLogManager();
            context.Logs.Register(cmdContext.Logs);

            ReadCommand(q, out cmdContext.inputString, out cmdContext.commandName, out var args);
            cmdContext.args = args.ToArray();
            return cmdContext;
        }

        public void ReadCommand(Queue<char> q, out string inputString, out string commandName, out List<QuashArgument> args)
        {
            var input = new StringBuilder();

            //WHITE SPACE
            input.Append(ReadWhiteSpace(q));

            //COMMAND NAME
            commandName = ReadToken(q, TA_CommandName, out var readCmd).ToString();
            input.Append(readCmd);

            //ARGS
            args = new List<QuashArgument>();
            QuashArgument arg = null;

            while (q.Count > 0)
            {
                var white = ReadWhiteSpace(q);
                input.Append(white);

                if (arg != null)
                    arg.WhiteAfter = white.ToString();

                //Check can read more arguments
                if (q.TryPeek(out var c) && Char_End.Contains(c))
                    break;

                var argTxt = ReadToken(q, TA_Argument, out var readArg);
                input.Append(readArg);
                arg = new QuashArgument(ValueParser, argTxt.ToString())
                {
                    WhiteBefore = white.ToString(),
                };

                args.Add(arg);
            }

            arg.WhiteAfter ??= string.Empty;
            inputString = input.ToString();
        }

        public StringBuilder ReadWhiteSpace(Queue<char> q)
        {
            var txt = new StringBuilder();

            while (q.TryPeek(out var c))
            {
                //If it's an escape character for an empty character
                if (Char_Escape.Contains(c) && q.Count > 1 && char.IsWhiteSpace(q.ElementAt(1)))
                {
                    q.Dequeue();
                    txt.Append(q.Dequeue());
                    continue;
                }

                //Reached end of white spaces or end character
                if (!char.IsWhiteSpace(c) || Char_End.Contains(c))
                    break;

                txt.Append(q.Dequeue());
            }

            return txt;
        }

        public StringBuilder ReadToken(Queue<char> q, ReadTokenArgs args, out StringBuilder readString)
        {
            var result = new StringBuilder();

            //We have to do this, because csharp keeps crying about "out parameter inside a local method"
            readString = new StringBuilder();
            var read = readString;

            char? wrap = null;

            //Read single argument
            while (q.TryPeek(out var c))
            {
                //Ending
                if (args.endOnEndCharacters && Char_End.Contains(c))
                {
                    if (wrap != null)
                    {
                        result.Append(Dequeue());
                        continue;
                    }

                    //Reached the end
                    break;
                }

                //White space
                if (args.endOnWhiteSpace && char.IsWhiteSpace(c))
                {
                    if (wrap != null)
                    {
                        result.Append(Dequeue());
                        continue;
                    }

                    //Reached the end
                    break;
                }

                //Escape characters
                if (args.useEscapeCharacters && Char_Escape.Contains(c))
                {
                    Dequeue();
                    if (q.Count > 0)
                        result.Append(Dequeue());

                    continue;
                }

                //Start grouping
                if (args.useWrapping && wrap == null)
                {
                    Dequeue();
                    wrap = c;
                    continue;
                }

                //End grouping
                if (args.useWrapping && Char_Wrapping.Contains(c) && wrap == c)
                {
                    Dequeue();
                    wrap = null;
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
            var q = CreateQ(cmd);
            var inputString = string.Empty;
            var commandName = string.Empty;
            var args = new List<QuashArgument>();
            while (cmd.Length - q.Count <= characterIndex && q.Count > 0)
            {
                ReadCommand(q, out inputString, out commandName, out args);
            }

            if (q.Count == 0 && cmd.Length > 0 && Char_End.Contains(cmd.Last()))
            {
                inputString = string.Empty;
                commandName = string.Empty;
                args = new List<QuashArgument>();
            }

            //Prefixes and postfixes
            var prefixEndIndex = cmd.Length - q.Count - inputString.TrimStart().Length;

            var postfixStartIndex = cmd.Length - q.Count;
            if (inputString.Length > 0 && Char_End.Contains(cmd[postfixStartIndex - 1]))
            {
                prefixEndIndex--;
                postfixStartIndex--;
            }

            var prefix = cmd.Substring(0, prefixEndIndex);
            var postfix = cmd.Substring(postfixStartIndex, cmd.Length - postfixStartIndex);

            var argsArray = args.ToArray();

            //Normalize parameters
            characterIndex -= prefix.Length;

            //Final info
            var info = new CmdCharacterInfo(prefix, postfix, commandName, argsArray);

            //If it's before the command name
            if (characterIndex < 0)
                return info.WithScope(CmdCharacterInfo.Scope.CommandName, characterIndex);

            //If it's between command name and first argument
            if (characterIndex < commandName.Length + 1)
                return info.WithScope(CmdCharacterInfo.Scope.CommandName, characterIndex);

            //Looking for the target argument
            var argIndex = 0;
            while (argIndex < args.Count)
            {
                var argLength = args[argIndex].arg.Length + (args[argIndex].WhiteBefore ?? string.Empty).Length;
                if (characterIndex > argLength) break;
                characterIndex -= argLength;
                argIndex++;
            }

            return info.WithScope(CmdCharacterInfo.Scope.Argument, characterIndex, argIndex);
        }

        public override string ConvertToString(string commandName, qCommandArgument[] arguments)
        {
            var txt = new StringBuilder(commandName);

            foreach (var arg in arguments)
            {
                if (arg is QuashArgument quashArg)
                {
                    txt.Append(quashArg.IsComplex ? $"{quashArg.WhiteBefore}\"{quashArg.arg.Replace("\"", "\"\"")}\"" : $" {quashArg.arg}");
                    continue;
                }

                if (arg.arg.Any(x => char.IsWhiteSpace(x)))
                {
                    txt.Append($" \"{arg.arg.Replace("\"", "\"\"")}\"");
                    continue;
                }

                txt.Append($" {arg.arg}");
            }

            return txt.ToString().Trim();
        }
        #endregion

        public class QuashArgument : qCommandArgument
        {
            public QuashArgument(ModularParser parser, string arg, params object[] values) : base(parser, arg, values) { }

            public bool IsComplex { get; set; }
            public string WhiteBefore { get; set; }
            public string WhiteAfter { get; set; }
        }

        public class QuashData : qConsoleParserData
        {
            public Queue<char> queue;
        }

        public struct ReadTokenArgs
        {
            public bool useWrapping;
            public bool useEscapeCharacters;
            public bool endOnEndCharacters;
            public bool endOnWhiteSpace;
            public bool useVariables;
        }
    }
}