using qASIC.CmdAutocomplete;
using System;
using System.Threading.Tasks;

namespace qASIC.Console.Commands.BuiltIn;

public class Cmd_Sleep : qBuiltinCommandLogic
{
    protected override string DefaultCommandName => "sleep";
    protected override string[] DefaultAliases => ["wait"];
    protected override string DefaultDescription => "Does nothing for the specified amount of time.";
    public override ACData CommandAutocomplete => new ACData()
        .AddVariant().AddType<TimeSpan>("span").Finish();

    public override async Task<object> Run(qConsoleCommandContext context)
    {
        context.CheckArgumentCount(1);
        await Task.Delay(context[0].GetValue<TimeSpan>());
        return null;
    }
}
