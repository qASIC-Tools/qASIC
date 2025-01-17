using qASIC.CommandPrompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace qASIC.Options.Commands
{
    public class ChangeOption : OptionsCommand
    {
        public ChangeOption(OptionsManager manager) : base(manager) { }

        public override string CommandName => "changeoption";
        public override string[] Aliases => new string[] { "setoption", "changesetting", "setsetting" };
        public override string Description => "Changed the value of an option.";

        KeyPrompt navigationPrompt = new KeyPrompt();
        TextPrompt valuePrompt = new TextPrompt();

        qLog listLog;
        int index = 0;
        Options.OptionsList.ListItem targetOption;

        List<Options.OptionsList.ListItem> items;

        public override object Run(CommandContext context)
        {
            if (context.prompt == navigationPrompt)
            {
                switch (navigationPrompt.Key)
                {
                    case KeyPrompt.NavigationKey.Cancel:
                        UpdateLog(context, true, true);
                        return null;
                    case KeyPrompt.NavigationKey.Up:
                        index = Math.Max(index - 1, 0);
                        break;
                    case KeyPrompt.NavigationKey.Down:
                        index = Math.Min(index + 1, items.Count - 1);
                        break;
                    case KeyPrompt.NavigationKey.Confirm:
                        UpdateLog(context, true);
                        targetOption = items[index];
                        return AskForValue(context);
                }

                UpdateLog(context);
                return navigationPrompt;
            }

            if (context.prompt == valuePrompt)
            {
                //Set

                var value = targetOption.Value;
                if (!(value is string))
                {
                    try
                    {
                        value = Convert.ChangeType(valuePrompt.Text, targetOption.Value?.GetType());
                    }
                    catch
                    {
                        throw new CommandParseException(targetOption.Value?.GetType(), valuePrompt.Text);
                    }
                }

                Manager.SetOption(targetOption.Name, value);

                return null;
            }

            context.CheckArgumentCount(0, 2);

            //No args
            if (context.Length == 1)
            {
                listLog = null;
                items = Manager.OptionsList.Select(x => x.Value)
                    .ToList();
                UpdateLog(context);
                return navigationPrompt;
            }

            //Option name
            if (context.Length == 2)
            {
                targetOption = GetOption(context[1].arg);
                return AskForValue(context);
            }

            //All args
            targetOption = GetOption(context[1].arg);
            var settType = targetOption.Value?.GetType();
            var val = settType == null ?
                context[2].values.First() :
                context[2].GetValue(settType);

            Manager.SetOption(targetOption.Name, val);
            return null;
        }

        object AskForValue(CommandContext context)
        {
            context.Logs.Log("Enter value...");
            return valuePrompt;
        }

        void UpdateLog(CommandContext context, bool final = false, bool cancelled = false)
        {
            if (listLog == null)
                listLog = qLog.CreateNow("");

            StringBuilder txt = new StringBuilder(final ? (cancelled ? "Cancelled" : "Setting Selected") : "Select Setting");
            for (int i = 0; i < items.Count; i++)
            {
                txt.Append("\n");
                txt.Append(i == index ? (final ? "]" : ">") : " ");
                txt.Append($" {items[i].Name}: {items[i].Value} (default value:{items[i].DefaultValue})");
            }

            listLog.message = txt.ToString();
            context.Logs.Log(listLog);
        }

        Options.OptionsList.ListItem GetOption(string settingName)
        {
            if (!Manager.OptionsList.ContainsKey(settingName))
                throw new CommandException($"Setting '{settingName}' does not exist!");

            return Manager.OptionsList[settingName];
        }
    }
}