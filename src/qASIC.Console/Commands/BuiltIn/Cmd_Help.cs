using qASIC.CmdAutocomplete;
using qASIC.Console.Autocomplete;
using qASIC.qARK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Help : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "help";
        protected override string DefaultDescription => "Displays a list of all available commands.";

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish()
            .AddVariantIf(MultiplePages).AddType<int>("pageIndex").Finish()
            .AddVariantIf(AllowDetailedDescription).AddArgument(new ACCommandArgument("command")).Finish();


        public bool MultiplePages { get; set; } = true;
        public bool AllowDetailedDescription { get; set; } = true;
        public int PageCommandLimit { get; set; } = 16;
        public bool SortCommands { get; set; } = true;
        public string PageOutOfRangeMessage { get; set; } = "Page index out of range";
        public string CommandNotFoundMessage { get; set; } = "Command '$0' does not exist";
        public bool PageStartAtOne { get; set; } = true;

        public Func<qConsoleCommandContext, ICommandLogic, bool> CanShowCommand;

        private CommandComparer comparer = new CommandComparer();

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
                        if (PageStartAtOne)
                            index--;
                        break;
                    case false:
                        targetCommand = context[0].arg;
                        break;
                }
            }

            var commandList = context.Console.CommandList;
            var commands = commandList
                .Where(x => CanShowCommand?.Invoke(context, x) ?? true)
                .ToList();

            if (SortCommands)
                commands.Sort(comparer);

            //help <command>
            if (targetCommand != null)
            {
                if (!commandList.TryGetCommand(targetCommand, out ICommandLogic command) ||
                    command == null ||
                    (CanShowCommand != null && !CanShowCommand(context, command)))
                    throw new qCommandException(string.Format(CommandNotFoundMessage, CommandName));

                var txt = CreateDetailedInfoForCommand(context, command);
                if (txt != null)
                    context.Logs.Log(txt, "info");
                return null;
            }

            //help<index>
            {
                var startIndex = PageCommandLimit * index;

                if (startIndex >= commands.Count ||
                    startIndex < 0)
                    throw new qCommandException(PageOutOfRangeMessage);

                var txt = CreateCommandPage(commands,
                    index + 1,
                    (int)MathF.Ceiling((float)commands.Count / PageCommandLimit),
                    index * PageCommandLimit,
                    Math.Min((index + 1) * PageCommandLimit, commands.Count));

                if (txt != null)
                    context.Logs.Log(txt, "info");
            }

            return null;
        }

        protected virtual string CreateCommandPage(List<ICommandLogic> commands, int pageIndex, int maxPages, int cmdStartIndex, int cmdEndIndex)
        {
            StringBuilder txt = new StringBuilder(MultiplePages ?
                $"List of avaliable commands, page {pageIndex} out of {maxPages} \n" :
                "List of avaliable commands \n");

            for (int i = cmdStartIndex; i < cmdEndIndex; i++)
                txt.AppendLine($"{commands[i].CommandName} - {commands[i].Description ?? "No description"}");

            return txt.ToString();
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
                        txt.Append(context.Console.CommandParser.ConvertToString(cmd.CommandName, args));
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
            PageCommandLimit = data.GetValue("pageCommandLimit", 16);
            AllowDetailedDescription = data.GetValue("allowDetailedDescription", true);
            SortCommands = data.GetValue("SortCommands", true);
            PageOutOfRangeMessage = data.GetValue("PageOutOfRangeMessage", "Page index out of range");
            CommandNotFoundMessage = data.GetValue("CommandNotFoundMessage", "Command '$0' does not exist");
            PageStartAtOne = data.GetValue("PageStartAtOne", true);
        }

        public override qARKDocument CreateConfig() =>
            base.CreateConfig()
                .AddSpace()
                .AddEntry("multiplePages", MultiplePages)
                .AddEntry("pageCommandLimit", PageCommandLimit)
                .AddEntry("allowDetailedDescription", AllowDetailedDescription)
                .AddEntry("SortCommands", SortCommands)
                .AddEntry("PageOutOfRangeMessage", PageOutOfRangeMessage)
                .AddEntry("CommandNotFoundMessage", CommandNotFoundMessage)
                .AddEntry("PageStartAtOne", PageStartAtOne);

        private class CommandComparer : IComparer<ICommandLogic>
        {
            public int Compare(ICommandLogic x, ICommandLogic y)
            {
                return string.Compare(x.CommandName, y.CommandName);
            }
        }
    }
}