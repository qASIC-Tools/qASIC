using qASIC.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_RemoteInfo : qConsoleCommand
    {
        public override string CommandName => "remote";
        public override string Description => "Shows information about the remote inspector server.";

        public qInstance Instance { get; set; }

        public override object Run(ConsoleCommandContext context)
        {
            context.CheckArgumentCount(0);

            var instance = Instance ?? context.console.Instance;
            if (instance == null)
                throw new CommandException("Unable to get remote inspector server info: no qInstance found. Neither this command nor this console has an instance of qASIC assigned.");

            var tree = TextTree.Fancy;
            var root = new TextTreeItem($"Remote inspector status: {(instance.RemoteInspectorServer.IsActive ? "active" : "offline")}");
            root.Add($"Address: {GetLocalAddress()?.ToString() ?? "UNKNOWN"}");
            root.Add($"Port: {instance.RemoteInspectorServer.Port}");
            root.Add($"Is local only: {instance.RemoteInspectorServer.LocalOnly}");
            root.Add($"Connected inspectors count: {instance.RemoteInspectorServer.Clients.Count}");
            var systems = new TextTreeItem("Registered systems:");
            foreach (var item in (instance.RemoteInspectorServer.AppInfo as RemoteAppInfo)?.systems ?? new List<RemoteAppInfo.SystemInfo>())
                systems.Add($"{item.name} v{item.version}");

            root.Add(systems);

            context.Logs.Log(tree.GenerateTree(root));
            return null;
        }

        static IPAddress GetLocalAddress() =>
            Dns.GetHostEntry(Dns.GetHostName())?.AddressList
                .Where(x => !x.IsIPv6LinkLocal)
                .FirstOrDefault();
    }
}
