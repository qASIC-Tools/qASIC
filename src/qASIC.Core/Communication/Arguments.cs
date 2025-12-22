using qASIC.Logging;

namespace qASIC.Communication;

public class OnServerReceiveDataArgs(qServer.Client client, qPacket packet)
{
    public qServer.Client client = client;
    public qPacket data = packet;
}

public class CommsComponentArgs(PacketType packetType, qPacket packet)
{
    public PacketType packetType = packetType;
    public qPacket packet = packet;
    public qClient client;
    public qServer server;

    public qServer.Client targetServerClient;

    public qLogManager Logs
    {
        get
        {
            return packetType switch
            {
                PacketType.Server => server.Logs,
                PacketType.Client => client.Logs,
                _ => null,
            };
        }
    }
}
