using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qASIC.Parsing;

namespace qASIC.Console.Parsing
{
    public class QuashParser : ArgumentsParser
    {
        public static readonly char[] Char_End = new char[]
        {
            '\n',
            ';'
        };

        public override object ExecuteParser(qConsoleCommandContext context) =>
            ExecQ(CreateQ(context), context);

        public override Task<object> ExecuteParserAsync(qConsoleCommandContext context) =>
            ExecQAsync(CreateQ(context), context);

        private Queue<char> CreateQ(qConsoleCommandContext context) =>
            CreateQ(context.inputString);

        private Queue<char> CreateQ(string inputString) =>
            new Queue<char>(inputString.Replace("\r\n", "\n"));

        private object ExecQ(Queue<char> q, qConsoleCommandContext context, object returnedValue = null)
        {
            while (q.Count > 0)
            {
                ReadCommand(q, ref context, returnedValue);
                returnedValue = ExecuteInConsole(context);
                if (returnedValue is Task task)
                {
                    var switchTask = SwitchExecToAsync(q, context, task);
                    if (context.RunTaskResult)
                        Task.Run(() => switchTask);
                    return switchTask;
                }
            }

            return returnedValue;
        }

        private async Task<object> ExecQAsync(Queue<char> q, qConsoleCommandContext context, object returnedValue = null)
        {
            while (q.Count > 0)
            {
                ReadCommand(q, ref context, returnedValue);
                returnedValue = await ExecuteInConsoleAsync(context);
            }

            return returnedValue;
        }

        private async Task SwitchExecToAsync(Queue<char> q, qConsoleCommandContext context, Task task)
        {
            var returnedValue = await Console.ExecuteAsync(context.commandName, task, context.Logs);
            await ExecQAsync(q, context, returnedValue);
        }

        private void ReadCommand(Queue<char> q, ref qConsoleCommandContext context, object returnedValue)
        {
            ReadCommand(q, out var inputString, out var commandName, out var args);
            context.inputString = inputString;
            context.commandName = commandName;
            context.args = args.ToArray();
            Console.FillContext(ref context, returnedValue);
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
                var inQuotes = false;

                //Read single argument
                while (TryDequeue(out var c))
                {
                    if (char.IsWhiteSpace(c))
                    {
                        if (inQuotes)
                        {
                            arg.Append(c);
                            continue;
                        }

                        //Finish argument
                        while (q.TryPeek(out c) && char.IsWhiteSpace(c))
                        {
                            whiteAfter.Append(Dequeue());
                        }
                        break;
                    }

                    if (c == '\\')
                    {
                        if (TryDequeue(out c))
                            arg.Append(c);

                        continue;
                    }

                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                        continue;
                    }

                    if (Char_End.Contains(c))
                    {
                        if (inQuotes)
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

        public class QuashArgument : qCommandArgument
        {
            public QuashArgument(ModularParser parser, string arg, params object[] values) : base(parser, arg, values) { }

            public bool IsComplex { get; set; }
            public string WhiteBefore { get; set; }
            public string WhiteAfter { get; set; }
        }
    }
}