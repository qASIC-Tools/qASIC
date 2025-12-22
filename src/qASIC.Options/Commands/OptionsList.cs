using System.Text;

namespace qASIC.Options.Commands;

public class OptionsList(OptionsManager manager) : OptionsCommand(manager)
{
    public override string CommandName => "optionslist";
    public override string[] Aliases => ["settingslist", "listoptions", "listsettings"];
    public override string Description => "Shows a list of options.";

    public override object Run(qCommandContext context)
    {
        context.CheckArgumentCount(0);

        var txt = new StringBuilder("List of options:");

        foreach (var item in Manager.OptionsList)
            txt.Append($"\n- {item.Key}:{item.Value.value} (default: {item.Value.defaultValue})");

        context.Logs.Log(txt.ToString());
        return null;
    }
}
