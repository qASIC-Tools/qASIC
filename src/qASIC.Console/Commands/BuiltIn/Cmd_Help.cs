using qASIC.CmdAutocomplete;
using qASIC.Console.Autocomplete;
using qASIC.qARK;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Help : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "help";
        protected override string DefaultDescription => "Displays a list of all avaliable commands.";

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish()
            .AddVariantIf(MultiplePages).AddType<int>("pageIndex").Finish()
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

            //help <command>
            if (targetCommand != null)
            {
                if (!commandList.TryGetCommand(targetCommand, out ICommandLogic command) || command == null)
                    throw new qCommandException($"Command '{targetCommand}' does not exist!");

                context.Logs.Log(CreateDetailedInfoForCommand(context, command), "info");
                return null;
            }

            //help<index>
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

        protected virtual string CreateDetailedInfoForCommand(qConsoleCommandContext context, ICommandLogic cmd)
        {
            var txt = new StringBuilder($"Help for command '{cmd.CommandName}':")
                .Append($"\n\nCOMMAND NAME\n  {cmd.CommandName}");

            if (cmd.Aliases.Length > 0)
                txt.Append($"\n\nALIASES\n  {string.Join(", ", cmd.Aliases)}");

            var description = cmd.DetailedDescription;

            if (string.IsNullOrWhiteSpace(description))
                description = cmd.Description;

            if (!string.IsNullOrWhiteSpace(description))
                txt.Append($"\n\nDESCRIPTION\n  {description}");

            if (cmd is ISupportsAutocomplete ac)
            {
                var acData = ac.CommandAutocomplete;
                if (acData != null && acData.Variants.Count > 0)
                {
                    txt.Append("\n\nUSAGE");
                    foreach (var item in acData.Variants)
                    {
                        var args = item.Arguments
                            .Select(x => new qCommandArgument($"[{x.name}]"))
                            .ToArray();

                        txt.Append("\n  ");
                        txt.Append(context.console.CommandParser.ConvertToString(cmd.CommandName, args));
                    }
                }
            }

            txt.Append('\n');

            return txt.ToString();
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