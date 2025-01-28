using qASIC.Communication;

namespace qASIC.Console.Comms
{
    public class CC_ExecuteCommand : ConsoleCommsComponent
    {
        public override void ReadForConsole(CommsComponentArgs args, GameConsole console)
        {
            if (args.packetType != PacketType.Server)
                return;

            console.Execute(args.packet.ReadString());
        }

        public qPacket BuildPacket(GameConsole console, string input) =>
            CreateEmptyPacketForConsole(console)
            .Write(input);
    }
}