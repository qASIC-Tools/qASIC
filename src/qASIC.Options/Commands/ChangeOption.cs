using qASIC.CmdAutocomplete;
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

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish()
            .AddVariant().AddType<string>("option name").Finish()
            .AddVariant().AddType<string>("option name").AddType<object>("value").Finish();

        KeyPrompt navigationPrompt = new KeyPrompt();
        TextPrompt valuePrompt = new TextPrompt();

        qLog listLog;
        int index = 0;
        Options.OptionsList.ListItem targetOption;

        List<Options.OptionsList.ListItem> items;

        public override object Run(qCommandContext context)
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

                var value = targetOption.value;
                if (!(value is string))
                {
                    try
                    {
                        value = Convert.ChangeType(valuePrompt.Text, targetOption.value?.GetType());
                    }
                    catch
                    {
                        throw new qCommandParseException(targetOption.value?.GetType(), valuePrompt.Text);
                    }
                }

                Manager.SetOption(targetOption.name, value);

                return null;
            }

            context.CheckArgumentCount(0, 2);

            //changeoption
            if (context.Length == 0)
            {
                listLog = null;
                items = Manager.OptionsList.Select(x => x.Value)
                    .ToList();
                UpdateLog(context);
                return navigationPrompt;
            }

            //changeoption [option name]
            if (context.Length == 1)
            {
                targetOption = GetOption(context[0].arg);
                return AskForValue(context);
            }

            //changeoption [option name] [value]
            targetOption = GetOption(context[0].arg);
            var settType = targetOption.value?.GetType();
            var val = settType == null ?
                context[1].values.FirstOrDefault() ?? context[1].GetValue(context[1].Parser.Parsers.Select(x => x.ValueType).FirstOrDefault(x => context[1].CanGetValue(x))):
                context[1].GetValue(settType);

            Manager.SetOption(targetOption.name, val);
            return null;
        }

        object AskForValue(qCommandContext context)
        {
            context.Logs.Log("Enter value...");
            return valuePrompt;
        }

        void UpdateLog(qCommandContext context, bool final = false, bool cancelled = false)
        {
            if (listLog == null)
                listLog = qLog.CreateNow("");

            StringBuilder txt = new StringBuilder(final ? (cancelled ? "Cancelled" : "Setting Selected") : "Select Setting");
            for (int i = 0; i < items.Count; i++)
            {
                txt.Append("\n");
                txt.Append(i == index ? (final ? "]" : ">") : " ");
                txt.Append($" {items[i].name}: {items[i].value} (default value:{items[i].defaultValue})");
            }

            listLog.message = txt.ToString();
            context.Logs.Log(listLog);
        }

        Options.OptionsList.ListItem GetOption(string settingName)
        {
            if (!Manager.OptionsList.ContainsKey(settingName))
                throw new qCommandException($"Setting '{settingName}' does not exist!");

            return Manager.OptionsList[settingName];
        }
    }
}