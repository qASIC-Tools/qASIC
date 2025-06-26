using qASIC.Communication;
using System;
using System.Collections.Generic;
using qASIC.Console.Logging;

namespace qASIC.Console.Comms
{
    public class CC_ConsoleRegister : ConsoleCommsComponent
    {
        public override void Read(CommsComponentArgs args)
        {
            if (args.packetType != PacketType.Client) return;
            var consoleName = args.packet.ReadString();

            var logs = new List<qLog>();
            int logCount = args.packet.ReadInt();
            for (int i = 0; i < logCount; i++)
                logs.Add(args.packet.ReadNetworkSerializable<qLog>());

            var console = new qConsole(consoleName)
            {
                Logs = new qSavableLogManager(logs),
                Theme = args.packet.ReadNetworkSerializable<qConsoleTheme>(),
            };

            ConsoleManager?.RegisterConsole(console);
        }

        public override void ReadForConsole(CommsComponentArgs args, qConsole console) =>
            throw new NotImplementedException();

        public qPacket CreatePacket(qConsole console)
        {
            var packet = CreateEmptyPacketForConsole(console);

            packet.Write(console.Logs.Logs.Count);

            foreach (var log in console.Logs)
                packet.Write(log);

            packet.Write(console.Theme);

            return packet;
        }
    }
}
