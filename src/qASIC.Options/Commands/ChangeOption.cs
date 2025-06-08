using qASIC.CmdAutocomplete;
using qASIC.CommandPrompts;
using System;
using System.Linq;
using qASIC.Text;

namespace qASIC.Options.Commands
{
    public class ChangeOption : OptionsCommand
    {
        public ChangeOption(OptionsManager manager) : base(manager) { }

        public override string CommandName => "changeoption";
        public override string[] Aliases => new string[] { "setoption", "changesetting", "setsetting" };
        public override string Description => "Changes the value of an option.";

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish()
            .AddVariant().AddType<string>("option name").Finish()
            .AddVariant().AddType<string>("option name").AddType<object>("value").Finish();

        qLog listLog;
        Options.OptionsList.ListItem targetOption;
        TextMenu<Options.OptionsList.ListItem> menu;

        public override object Run(qCommandContext context)
        {
            //Prompts
            if (context.prompt is KeyPrompt key)
            {
                var obj = key.UseTextMenu(menu);
                UpdateLog();
                return obj;
            }

            if (context.prompt is TextPrompt text)
            {
                //Set
                var value = targetOption.value;
                if (!context.parser.TryParse(targetOption.value?.GetType(), text.Text, out value))
                    throw new qCommandParseException(targetOption.value?.GetType(), text.Text);

                Manager.SetOption(targetOption.name, value);
                return null;
            }

            //Standard
            context.CheckArgumentCount(0, 2);

            //changeoption
            if (context.Length == 0)
            {
                listLog = null;
                CreateMenu(context.Logs);
                UpdateLog();
                return new KeyPrompt();
            }

            //changeoption [option name]
            if (context.Length == 1)
            {
                targetOption = GetOption(context[0].arg);
                return AskForValue(context.Logs);
            }

            //changeoption [option name] [value]
            targetOption = GetOption(context[0].arg);
            var val = GetValueFromArg(context[1], targetOption.value?.GetType());

            Manager.SetOption(targetOption.name, val);
            return null;


            void UpdateLog()
            {
                listLog ??= qLog.CreateNow("");
                listLog.message = menu.GenerateMenu();
                context.Logs.Log(listLog);
            }
        }

        void CreateMenu(qLogManager logs)
        {
            menu = new TextMenu<Options.OptionsList.ListItem>("Select Setting", Manager.OptionsList
                .Select(x => new TextMenuItem<Options.OptionsList.ListItem>($"{x.Value.name}: {x.Value.value} (default: {x.Value.defaultValue})", x.Value, _ =>
                {
                    targetOption = x.Value;
                    menu.Header = "Setting Selected";
                    return AskForValue(logs);
                })));

            menu.CanCancel += () =>
            {
                menu.Header = "Cancelled";
                return true;
            };
        }

        object GetValueFromArg(qCommandArgument arg, Type type)
        {
            if (type != null)
                return arg.GetValue(type);

            if (arg.values.Length > 0)
                return arg.values[0];

            type = arg.Parser.Parsers
                .Select(x => x.ValueType)
                .FirstOrDefault(arg.CanGetValue);

            return arg.GetValue(type);
        }

        object AskForValue(qLogManager logs)
        {
            logs.Log("Enter value...");
            return new TextPrompt();
        }

        Options.OptionsList.ListItem GetOption(string settingName)
        {
            if (!Manager.OptionsList.ContainsKey(settingName))
                throw new qCommandException($"Setting '{settingName}' does not exist!");

            return Manager.OptionsList[settingName];
        }
    }
}