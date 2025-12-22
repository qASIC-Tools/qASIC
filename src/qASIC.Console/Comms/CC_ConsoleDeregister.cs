using qASIC.Communication;

namespace qASIC.Console.Comms;

public class CC_ConsoleDeregister : ConsoleCommsComponent
{
    public override void ReadForConsole(CommsComponentArgs args, qConsole console)
    {
        ConsoleManager?.DeregisterConsole(console);
    }
}
