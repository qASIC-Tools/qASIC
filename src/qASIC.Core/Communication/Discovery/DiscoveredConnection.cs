using System.Net;

namespace qASIC.Communication.Discovery;

/// <param name="address">Address of the discovered server.</param>
/// <param name="port">Packet containing identity information of the discovered server.</param>
/// <param name="identity">Packet containing identity information of the discovered server.</param>
public class DiscoveredConnection(IPAddress address, int port, qPacket identity)
{
    /// <summary>Address of the discovered server.</summary>
    public IPAddress Address { get; set; } = address;
    /// <summary>Port of the discovered server.</summary>
    public int Port { get; set; } = port;
    /// <summary>Packet containing identity information of the discovered server.</summary>
    public qPacket Identity { get; set; } = identity;

    /// <summary>Amount of pings missed from the discovered server.</summary>
    public int MissedPings { get; set; }
}
