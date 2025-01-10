using System.Collections.Generic;
using System.Text;

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
            var args = new List<CommandArgument>();

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
                    args.Add(CreateCommandArgument(currentString.ToString()));
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
                args.Add(CreateCommandArgument(currentString.ToString()));
            
            return args.ToArray();
        }
    }
}