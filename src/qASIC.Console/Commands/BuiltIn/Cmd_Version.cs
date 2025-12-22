using qASIC.CmdAutocomplete;
using System;
using System.Text;

namespace qASIC.Console.Commands.BuiltIn;

public class Cmd_Version : qBuiltinCommandLogic
{
    protected override string DefaultCommandName => "version";
    protected override string DefaultDescription => "Displays current project version.";
    protected override string[] DefaultAliases => ["info", "about"];

    public override ACData CommandAutocomplete => new ACData()
        .AddVariant().Finish();

    public event Func<RemoteAppInfo, string> GetInfoString = (a) =>
    {
        var txt = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(a.projectName))
            txt.Append($"{a.projectName} ");

        if (!string.IsNullOrWhiteSpace(a.version))
            txt.Append($"v{a.version} ");

        if (!string.IsNullOrWhiteSpace(a.engine))
            txt.Append($", made with {a.engine} ");

        if (!string.IsNullOrWhiteSpace(a.engineVersion))
            txt.Append($"v{a.engineVersion} ");

        return txt.ToString().TrimStart(',').Trim();
    };

    public override object Run(qConsoleCommandContext context)
    {
        context.CheckArgumentCount(0);

        var appInfo = context.Console.Instance?.AppInfo;

        if (appInfo == null)
        {
            context.Console.LogError("No version information is supplied.");
            return null;
        }

        context.Console.Log(GetInfoString(appInfo));
        return null;
    }
}
