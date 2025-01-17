using System.Text;
using System;
using System.Linq;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Help : GameCommand
    {
        public override string CommandName => "help";
        public override string Description => "Displays a list of all avaliable commands.";

        public bool MultiplePages { get; set; } = true;
        public bool AllowDetailedDescription { get; set; } = true;
        public int PageCommandLimit { get; set; } = 16;

        public Func<GameCommandContext, ICommand, bool> CanShowCommand;

        public override object Run(GameCommandContext context)
        {
            //Ignore page argument if multipage and detailed description is off
            if (!MultiplePages)
                context.CheckArgumentCount(0);

            context.CheckArgumentCount(0, 1);

            string targetCommand = null;
            int index = 0;

            //help <index>
            if (context.Length == 2)
            {
                switch (context[0].CanGetValue<int>())
                {
                    case true:
                        index = context[0].GetValue<int>();
                        break;
                    case false:
                        targetCommand = context[0].arg;
                        break;
                }
            }

            var commandList = context.console.CommandList;
            var commands = commandList
                .Where(x => CanShowCommand?.Invoke(context, x) ?? true)
                .ToList();

            if (targetCommand != null)
            {
                if (!commandList.TryGetCommand(targetCommand, out ICommand command) || command == null)
                    throw new CommandException($"Command '{targetCommand}' does not exist!");

                if (command.DetailedDescription == null && command.Description == null)
                {
                    context.Logs.Log($"No detailed help avaliable for command '{targetCommand}'");
                    return null;
                }

                context.Logs.Log($"Help for command '{command.CommandName}': {command.DetailedDescription ?? command.Description}", "info");
                return null;
            }

            var startIndex = PageCommandLimit * index;

            if (startIndex >= commands.Count)
                throw new CommandException("Page index out of range");

            StringBuilder stringBuilder = new StringBuilder(MultiplePages ? 
                $"List of avaliable commands, page: {index} \n" :
                "List of avaliable commands \n");

            for (int i = index * PageCommandLimit; i < Math.Max(index * (PageCommandLimit + 1), commands.Count); i++)
                stringBuilder.AppendLine($"{commands[i].CommandName} - {commands[i].Description ?? "No description"}");

            context.Logs.Log(stringBuilder.ToString(), "info");

            return null;
        }
    }
}