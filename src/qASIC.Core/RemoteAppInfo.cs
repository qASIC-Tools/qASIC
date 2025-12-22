using qASIC.Communication;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace qASIC;

public class RemoteAppInfo : NetworkServerInfo
{
    public string projectName = string.Empty;
    public string version = string.Empty;
    public string engine = string.Empty;
    public string engineVersion = string.Empty;

    public List<SystemInfo> systems = [];

    public void RegisterSystem(string systemInfo, string version)
    {
        if (!UsesSystem(systemInfo))
            systems.Add(new SystemInfo(systemInfo, version));
    }

    public bool UsesSystem(string systemName) =>
        systems.Any(x => x.name == systemName);

    public override qPacket Write(qPacket packet)
    {
        base.Write(packet)
            .Write(projectName)
            .Write(version)
            .Write(engine)
            .Write(engineVersion)
            .Write(systems.Count);

        foreach (var item in systems)
            packet.Write(item.name)
                .Write(item.version);

        return packet;
    }

    public override void Read(qPacket packet)
    {
        base.Read(packet);
        projectName = packet.ReadString();
        version = packet.ReadString();
        engine = packet.ReadString();
        engineVersion = packet.ReadString();

        systems.Clear();
        var systemCount = packet.ReadInt();
        for (int i = 0; i < systemCount; i++)
        {
            systems.Add(new SystemInfo()
            {
                name = packet.ReadString(),
                version = packet.ReadString(),
            });
        }
    }

    public override string ToString()
    {
        var txt = new StringBuilder("Remote App Info, ");

        switch (string.IsNullOrWhiteSpace(projectName), string.IsNullOrWhiteSpace(version))
        {
            case (false, false):
                txt.Append($" '{projectName}' v{version}");
                break;
            case (false, true):
                txt.Append($" project version: {version}");
                break;
            case (true, false):
                txt.Append($" '{projectName}'");
                break;
        }

        switch (string.IsNullOrWhiteSpace(engine), string.IsNullOrWhiteSpace(engineVersion))
        {
            case (false, false):
                txt.Append($" '{engine}' v{engineVersion}");
                break;
            case (false, true):
                txt.Append($" engine version: {engineVersion}");
                break;
            case (true, false):
                txt.Append($" '{engine}'");
                break;
        }

        txt.Append($" using protocol version {protocolVersion}");
        return txt.ToString();
    }

    public struct SystemInfo(string name, string version)
    {
        public string name = name;
        public string version = version;
    }
}

