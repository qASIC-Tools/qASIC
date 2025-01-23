using System.Collections.Generic;
using System.Linq;
using System.Text;
using qASIC.Parsing;

namespace qASIC.Console.Parsing.Arguments
{
    public class QuashParser : ArgumentsParser
    {
        public string[] textForTrue = new string[] { "true", "on", "yes" };
        public string[] textForFalse = new string[] { "false", "off", "no" };

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

        public override CommandArgument[] ParseArguments(string cmd)
        {
            var args = new List<QuashArgument>();

            var readCommand = false;
            var complex = false;
            var currentString = new StringBuilder();

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
                        (cmd.Length <= i + 1 ||char.IsWhiteSpace(cmd[i + 1])))
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
                if (char.IsWhiteSpace(cmd[i]))
                {
                    args.Add(new QuashArgument(ValueParser, currentString.ToString())
                    {
                        IsComplex = complex,
                    });

                    currentString.Clear();
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

        public override string ConvertToString(string commandName, CommandArgument[] arguments)
        {
            var txt = new StringBuilder(commandName);

            foreach (var arg in arguments)
            {
                if (arg is QuashArgument quashArg)
                {
                    txt.Append(quashArg.IsComplex ? $" \"{quashArg.arg.Replace("\"", "\"\"")}\"" : $" {quashArg.arg}");
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

        public class QuashArgument : CommandArgument
        {
            public QuashArgument(ModularParser parser, string arg, params object[] values) : base(parser, arg, values) { }

            public bool IsComplex { get; set; }
        }
    }
}