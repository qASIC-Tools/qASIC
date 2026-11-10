using qASIC.Communication;
using qASIC.Communication.Components;
using qASIC.CommComponents;
using qASIC.Communication.Discovery;
using qASIC.Logging;

namespace qASIC;

/// <summary>An instance of qASIC in your project. Contains all systems and manages communication with exterrnal tools.</summary>
public class qInstance : IHasLogs
{
    public qInstance(RemoteAppInfo appInfo = null)
    {
        RemoteInspectorComponents = CommsComponentCollection.GetStandardCollection()
            .AddComponent(new CC_Log());

        RemoteInspectorServer = new qServer(RemoteInspectorComponents).WithUpdateLoop();
        RemoteInspectorDiscoveryServer = new DiscoveryServer(RemoteInspectorServer);
        AppInfo = appInfo ?? new RemoteAppInfo();

        Services = new(this);

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

    /// <summary>Information about the running application instance used to identify it using external tools.</summary>
    public RemoteAppInfo AppInfo
    {
        get => (RemoteAppInfo)RemoteInspectorServer.AppInfo;
        set => RemoteInspectorServer.AppInfo = value;
    }

    /// <summary>Components of the remote inspector server.</summary>
    public CommsComponentCollection RemoteInspectorComponents { get; private set; }
    /// <summary>A server instance used by external applications.</summary>
    public qServer RemoteInspectorServer { get; private set; }
    /// <summary>A discovery server that makes the <see cref="RemoteInspectorServer"/> show up on the network.</summary>
    public DiscoveryServer RemoteInspectorDiscoveryServer { get; private set; }

    /// <inheritdoc/>
    public qLogManager Logs { get; set; } = new qLogManager();

    /// <summary>If true, messages from <see cref="qDebug"/> will be sent to the remote inspector server.</summary>
    public bool forwardDebugLogs = true;
    /// <summary>Wether to start the remote inspector server automatically.</summary>
    public bool autoStartRemoteInspectorServer = true;
    /// <summary>Wether to make the application discoverable over the network to make it easier to connect to using external tools.</summary>
    public bool useNetworkDiscovery = true;

    /// <summary>Colletion of qASIC services used by the instance.</summary>
    public qServices Services { get; }
    /// <summary>Collection of registered objects in the instance.</summary>
    public qRegisteredObjects RegisteredObjects { get; } = new();

    /// <summary>Start the instance.</summary>
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

    /// <summary>Stop the instance.</summary>
    public void Stop()
    {
        if (RemoteInspectorServer.IsActive)
            RemoteInspectorServer.Stop();

        if (RemoteInspectorDiscoveryServer.IsActive)
            RemoteInspectorDiscoveryServer.Stop();
        
        qDebug.OnLog -= QDebug_OnLog;
    }
}
