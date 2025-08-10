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
    public class QuashParser : ArgumentsParser
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

        public static readonly char[] Char_Grouping = new char[]
        {
            '\"',
            '\'',
            '`',
        };

        #region Entry point
        public override object ExecuteParser(qConsoleContext context)
        {
            object returnedValue = null;
            if (TryPreparePromptContextForStart(context, out var promptContext))
            {
                returnedValue = Console.ExecuteCommand(promptContext);
                if (returnedValue is Task task)
                    return SwitchExecToAsync(CreateQAndPrepare(context), context, promptContext, task);
            }

            return ExecQ(CreateQAndPrepare(context), context, returnedValue);
        }

        public override async Task<object> ExecuteParserAsync(qConsoleContext context)
        {
            object returnedValue = null;
            if (TryPreparePromptContextForStart(context, out var promptContext))
                returnedValue = await Console.ExecuteCommandAsync(promptContext);

            return await ExecQAsync(CreateQAndPrepare(context), context, returnedValue);
        }
        #endregion

        #region Preparation
        protected bool TryPreparePromptContextForStart(qConsoleContext context, out qConsoleCommandContext promptContext)
        {
            promptContext = null;
            if (context.previousValue is CommandPrompt prompt &&
                prompt.Context is qConsoleCommandContext)
            {
                promptContext = prompt.Context as qConsoleCommandContext;
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
                    logs = context.Logs ?? new qLogManager(),
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
                returnedValue = Console.ExecuteCommand(cmdContext);
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
            var returnedValue = await Console.ExecuteCodeAsync(cmdContext.commandName, task, cmdContext.Logs);
            await ExecQAsync(q, context, returnedValue);
        }

        private async Task<object> ExecQAsync(Queue<char> q, qConsoleContext context, object returnedValue = null)
        {
            while (q.Count > 0)
            {
                var cmdContext = ReadCommand(q, context, returnedValue);
                returnedValue = await Console.ExecuteCommandAsync(cmdContext);
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
                prompt.Context is qConsoleCommandContext promptContext)
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

        private void ReadCommand(Queue<char> q, out string inputString, out string commandName, out List<QuashArgument> args)
        {
            var input = new StringBuilder();

            //WHITE SPACE
            while (q.TryPeek(out var c) && char.IsWhiteSpace(c))
                Dequeue();

            {
                //If reached end character before command name
                if (q.TryPeek(out var c) && Char_End.Contains(c))
                {
                    q.Dequeue();
                    inputString = input.ToString();
                    commandName = string.Empty;
                    args = new List<QuashArgument>();
                    return;
                }
            }

            //COMMAND NAME
            var cmd = new StringBuilder();
            while (q.TryPeek(out var c) && !char.IsWhiteSpace(c) && !Char_End.Contains(c))
            {
                cmd.Append(Dequeue());
            }

            commandName = cmd.ToString();

            //ARGS
            args = new List<QuashArgument>();

            string whiteBefore;
            var whiteAfter = new StringBuilder();

            //Pre arg white space
            while (q.TryPeek(out var c) && char.IsWhiteSpace(c))
                whiteAfter.Append(Dequeue());

            var finish = false;
            //Read all arguments
            while (q.Count > 0 && !finish)
            {
                whiteBefore = whiteAfter.ToString();
                whiteAfter.Clear();
                var arg = new StringBuilder();
                char? inGrouping = null;

                //Read single argument
                while (TryDequeue(out var c))
                {
                    if (char.IsWhiteSpace(c))
                    {
                        if (inGrouping != null)
                        {
                            arg.Append(c);
                            continue;
                        }

                        //Finish argument
                        while (q.TryPeek(out c) && char.IsWhiteSpace(c))
                            whiteAfter.Append(Dequeue());

                        break;
                    }

                    if (Char_Escape.Contains(c))
                    {
                        if (TryDequeue(out c))
                            arg.Append(c);

                        continue;
                    }

                    //Start grouping
                    if (inGrouping == null && inGrouping == c)
                    {
                        inGrouping = c;
                        continue;
                    }

                    //End grouping
                    if (Char_Grouping.Contains(c))
                    {
                        inGrouping = null;
                        continue;
                    }

                    //Ending
                    if (Char_End.Contains(c))
                    {
                        if (inGrouping != null)
                        {
                            arg.Append(c);
                            continue;
                        }

                        //End of arguments
                        finish = true;
                        input.Remove(input.Length - 1, 1);
                        break;
                    }

                    arg.Append(c);
                }

                if (arg.Length > 0)
                {
                    args.Add(new QuashArgument(ValueParser, arg.ToString())
                    {
                        WhiteBefore = whiteBefore,
                        WhiteAfter = whiteAfter.ToString(),
                    });
                }
            }

            inputString = input.ToString();


            char Dequeue()
            {
                var c = q.Dequeue();
                input.Append(c);
                return c;
            }

            bool TryDequeue(out char c)
            {
                if (q.TryDequeue(out c))
                {
                    input.Append(c);
                    return true;
                }

                return false;
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
    }
}