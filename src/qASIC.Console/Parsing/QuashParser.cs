using System.Collections.Generic;
using System.Linq;
using System.Text;
using qASIC.Parsing;

namespace qASIC.Console.Parsing
{
    public class QuashParser : ArgumentsParser
    {
        public override object Execute(string text)
        {
            var q = new Queue<char>(text.Replace("\r\n", "\n"));

            while (q.Count > 0)
            {
                ReadCommand(q, out var cmd, out var args);
                Console.Execute();
            }

            return null;
        }

        private void ReadCommand(Queue<char> q, out string commandName, out List<QuashArgument> args)
        {
            //WHITE SPACE
            while (q.TryPeek(out var c) && char.IsWhiteSpace(c))
                q.Dequeue();

            //COMMAND NAME
            var cmd = new StringBuilder();
            while (q.TryPeek(out var c) && !char.IsWhiteSpace(c))
            {
                cmd.Append(q.Dequeue());
            }

            commandName = cmd.ToString();

            //ARGS
            args = new List<QuashArgument>();

            string whiteBefore;
            var whiteAfter = new StringBuilder();

            //Pre arg white space
            while (q.TryPeek(out var c) && char.IsWhiteSpace(c))
                whiteAfter.Append(q.Dequeue());

            var finish = false;
            //Read all arguments
            while (q.Count > 0 && !finish)
            {
                whiteBefore = whiteAfter.ToString();
                whiteAfter.Clear();
                var arg = new StringBuilder();
                var inQuotes = false;

                //Read single argument
                while (q.TryDequeue(out var c))
                {
                    if (char.IsWhiteSpace(c))
                    {
                        if (inQuotes)
                        {
                            arg.Append(c);
                            continue;
                        }

                        //Finish argument
                        do
                        {
                            whiteAfter.Append(q.Dequeue());
                        }
                        while (q.TryPeek(out c) && char.IsWhiteSpace(c));
                        break;
                    }

                    if (c == '\\')
                    {
                        if (q.TryDequeue(out c))
                            arg.Append(c);

                        continue;
                    }

                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                        continue;
                    }

                    if (c == '\n')
                    {
                        if (inQuotes)
                        {
                            arg.Append(c);
                            continue;
                        }

                        //End of arguments
                        finish = true;
                        break;
                    }
                }


                args.Add(new QuashArgument(ValueParser, arg.ToString())
                {
                    WhiteBefore = whiteBefore,
                    WhiteAfter = whiteAfter.ToString(),
                });
            }
        }

        public override string ParseCommandName(string cmd)
        {
            cmd = cmd.Trim();

            var commandName = new StringBuilder();
            foreach (var c in cmd)
            {
                if (char.IsWhiteSpace(c)) break;
                commandName.Append(c);
            }

            return commandName.ToString();
        }

        public override qCommandArgument[] ParseArguments(string cmd)
        {
            var args = new List<QuashArgument>();

            var readCommand = false;
            var complex = false;
            var currentString = new StringBuilder();
            var empty = new StringBuilder();

            cmd = cmd.Trim();

            for (int i = 0; i < cmd.Length; i++)
            {
                //CASE: reading command name
                //Ignore rest until done
                if (!readCommand)
                {
                    if (char.IsWhiteSpace(cmd[i]))
                        readCommand = true;

                    continue;
                }

                //CASE: surrounded by quotation marks
                if (complex)
                {
                    if (cmd[i] == '"' &&
                        (cmd.Length <= i + 1 || char.IsWhiteSpace(cmd[i + 1])))
                    {
                        complex = false;
                        continue;
                    }

                    currentString.Append(cmd[i]);
                    continue;
                }

                //CASE: not that


                //CASE: whitespace
                //finish creating argument
                if (char.IsWhiteSpace(cmd[i]) && currentString.Length > 0)
                {
                    if (currentString.Length > 0)
                    {
                        args.Add(new QuashArgument(ValueParser, currentString.ToString())
                        {
                            IsComplex = complex,
                            WhiteBefore = empty.ToString(),
                        });

                        currentString.Clear();
                        empty.Clear();
                    }

                    empty.Append(cmd[i]);
                    continue;
                }

                //CASE: quotation mark after white space
                if (cmd[i] == '"' &&
                    (i != 0 && char.IsWhiteSpace(cmd[i - 1]) || i == 0) &&
                    currentString.Length == 0)
                {
                    complex = true;
                    continue;
                }

                currentString.Append(cmd[i]);
            }

            if (currentString.Length > 0)
                args.Add(new QuashArgument(ValueParser, currentString.ToString())
                {
                    IsComplex = complex,
                });

            return args.ToArray();
        }

        public override CmdCharacterInfo GetCharacterInfo(string cmd, int characterIndex)
        {
            //Prefixes and postfixes
            var prefix = cmd.Substring(0, cmd.Length - cmd.TrimStart().Length);
            var postfixStartIndex = cmd.TrimEnd().Length;
            var postfix = cmd.Substring(postfixStartIndex, cmd.Length - postfixStartIndex);

            //Normalize parameters
            characterIndex -= prefix.Length;
            cmd = cmd.Trim();

            //Parsed info
            var commandName = ParseCommandName(cmd);
            var args = ParseArguments(cmd);

            //Final info
            var info = new CmdCharacterInfo(prefix, postfix, commandName, args);

            //If it's before the command name
            if (characterIndex < 0)
                return info.WithScope(CmdCharacterInfo.Scope.CommandName, characterIndex);

            var quashArgs = args.Select(x => x as QuashArgument)
                .ToArray();

            //If it's between command name and first argument
            if (characterIndex < commandName.Length + 1)
                return info.WithScope(CmdCharacterInfo.Scope.CommandName, characterIndex);

            //Looking for the target argument
            var argIndex = 0;
            while (argIndex < quashArgs.Length)
            {
                var argLength = quashArgs[argIndex].arg.Length + (quashArgs[argIndex].WhiteBefore ?? string.Empty).Length;
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