using qASIC.Communication;
using qASIC.Communication.Components;
using qASIC.CommComponents;
using qASIC.Communication.Discovery;
using qASIC.Logging;

namespace qASIC;

public class qInstance : IHasLogs
{
    public qInstance(RemoteAppInfo appInfo = null)
    {
        RemoteInspectorComponents = CommsComponentCollection.GetStandardCollection()
            .AddComponent(cc_log);

        RemoteInspectorServer = new qServer(RemoteInspectorComponents).WithUpdateLoop();
        RemoteInspectorDiscoveryServer = new DiscoveryServer(RemoteInspectorServer);
        AppInfo = appInfo ?? new RemoteAppInfo();

        Services = new qServices(this);

        RegisteredObjects.OnObjectRegistered += a =>
        {
            if (a is IHasLogs loggable)
                Logs.Register(loggable);
        };

        RegisteredObjects.OnObjectDeregistered += a =>
        {
            if (a is IHasLogs loggable)
                Logs.Unregister(loggable);
        };
    }

    /// <summary>Main static instance of <see cref="qInstance"/> that was set using <see cref="SetAsMain"/>.</summary>
    public static qInstance Main { get; private set; }
    /// <summary>Sets this instance as main to make it accessible from property <see cref="Main"/>.</summary>
    /// <returns>Returns itself.</returns>
    public qInstance SetAsMain()
    {
        Main = this;
        return this;
    }

    public RemoteAppInfo AppInfo
    {
        get => (RemoteAppInfo)RemoteInspectorServer.AppInfo;
        set => RemoteInspectorServer.AppInfo = value;
    }

    public CommsComponentCollection RemoteInspectorComponents { get; private set; }
    public qServer RemoteInspectorServer { get; private set; }
    public DiscoveryServer RemoteInspectorDiscoveryServer { get; private set; }

    public qLogManager Logs { get; set; } = new qLogManager();

    public bool forwardDebugLogs = true;
    public bool autoStartRemoteInspectorServer = true;
    public bool useNetworkDiscovery = true;

    public readonly CC_Log cc_log = new CC_Log();

    public qServices Services;
    public qRegisteredObjects RegisteredObjects = new qRegisteredObjects();

    public void Start()
    {
        if (autoStartRemoteInspectorServer)
        {
            RemoteInspectorServer.Start();

            if (useNetworkDiscovery)
                RemoteInspectorDiscoveryServer.Start();
        }

        qDebug.OnLog += QDebug_OnLog;
    }

    private void QDebug_OnLog(qLog log)
    {
        if (!forwardDebugLogs) return;
        if (!RemoteInspectorServer.IsActive) return;
        RemoteInspectorServer.Send(CC_Log.BuildLogPacket(log));
    }

    public void Stop()
    {
        if (RemoteInspectorServer.IsActive)
            RemoteInspectorServer.Stop();

        if (RemoteInspectorDiscoveryServer.IsActive)
            RemoteInspectorDiscoveryServer.Stop();
    }
}
