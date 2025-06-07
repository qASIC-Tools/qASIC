using qASIC.Console.Autocomplete;
using qASIC.qARK;
using System;
using System.Linq;
using System.Text;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Help : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "help";
        protected override string DefaultDescription => "Displays a list of all avaliable commands.";

        public override ACData CommandAutocomplete { get; protected set; } = new ACData()
            .AddVariant().Finish()
            .AddVariant().AddType<int>("pageIndex").Finish()
            .AddVariant().AddArgument(new ACCommandArgument("command")).Finish();


        public bool MultiplePages { get; set; } = true;
        public bool AllowDetailedDescription { get; set; } = true;
        public int PageCommandLimit { get; set; } = 16;

        public Func<qConsoleCommandContext, ICommandLogic, bool> CanShowCommand;

        public override object Run(qConsoleCommandContext context)
        {
            context.CheckArgumentCount(0, 1);

            string targetCommand = null;
            int index = 0;

            //help <index> or help <command>
            if (context.Length == 1)
            {
                switch (context[0].TryGetValue(out int pageIndex) && MultiplePages)
                {
                    case true:
                        index = pageIndex;
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
                if (!commandList.TryGetCommand(targetCommand, out ICommandLogic command) || command == null)
                    throw new qCommandException($"Command '{targetCommand}' does not exist!");

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
                throw new qCommandException("Page index out of range");

            StringBuilder stringBuilder = new StringBuilder(MultiplePages ? 
                $"List of avaliable commands, page: {index} \n" :
                "List of avaliable commands \n");

            for (int i = index * PageCommandLimit; i < Math.Max(index * (PageCommandLimit + 1), commands.Count); i++)
                stringBuilder.AppendLine($"{commands[i].CommandName} - {commands[i].Description ?? "No description"}");

            context.Logs.Log(stringBuilder.ToString(), "info");

            return null;
        }

        public override void LoadConfig(qARKHolder data)
        {
            base.LoadConfig(data);

            MultiplePages = data.GetValue("multiplePages", true);
            AllowDetailedDescription = data.GetValue("allowDetailedDescription", true);
            PageCommandLimit = data.GetValue("pageCommandLimit", 16);
        }

        public override qARKDocument CreateConfig() =>
            base.CreateConfig()
                .AddSpace()
                .AddEntry("multiplePages", MultiplePages)
                .AddEntry("allowDetailedDescription", AllowDetailedDescription)
                .AddEntry("pageCommandLimit", PageCommandLimit);
    }
}