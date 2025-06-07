using qASIC.Communication;
using qASIC.Console.Comms;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

using static qASIC.Console.qConsoleInstanceManager;

namespace qASIC.Console
{
    public class qConsoleInstanceManager : IEnumerable<RegisteredConsole>, IEnumerable
    {
        public qConsoleInstanceManager(qClient client) : this(client as IPeer) { }
        public qConsoleInstanceManager(qServer server) : this(server as IPeer) { }
        public qConsoleInstanceManager(IPeer peer)
        {
            Peer = peer;
            Peer.Components
                .AddComponent(CC_Log = new CC_ConsoleLog() { ConsoleManager = this })
                .AddComponent(new CC_ExecuteCommand() { ConsoleManager = this })
                .AddComponent(new CC_ConsoleRegister() { ConsoleManager = this })
                .AddComponent(new CC_ConsoleDeregister() { ConsoleManager = this });

            if (Peer is qServer server)
            {
                server.OnClientConnect += (qServer.Client client) =>
                {
                    foreach (var item in RegisteredConsoles)
                    {
                        server.Send(client, new CC_ConsoleRegister().CreatePacket(item.Value.Console));
                    }
                };
            }

            if (Peer is qClient client)
            {
                client.OnDisconnect += _ =>
                {
                    RegisteredConsoles.Clear();
                };
            }
        }

        IPeer Peer { get; set; }
        internal Dictionary<string, RegisteredConsole> RegisteredConsoles { get; set; } = new Dictionary<string, RegisteredConsole>();

        public CC_ConsoleLog CC_Log { get; private set; }

        public event Action<qConsole> OnConsoleRegister;

        public void RegisterConsole(qConsole console)
        {
            RegisteredConsoles.Add(console.Name, new RegisteredConsole(console)
            {
                manager = this,
            });

            console.Logs.OnLog += (log) => Console_OnLog(console, log);
            console.Logs.OnUpdateLog += (log) => Console_OnUpdateLog(console, log);

            if (Peer is qServer server)
                server.Send(new CC_ConsoleRegister().CreatePacket(console));

            OnConsoleRegister?.Invoke(console);
        }

        public void DeregisterConsole(qConsole console) =>
            DeregisterConsole(console.Name);

        public void DeregisterConsole(string name)
        {
            var console = RegisteredConsoles[name].Console;
            RegisteredConsoles.Remove(name);
            console.Logs.OnLog -= (log) => Console_OnLog(console, log);
            console.Logs.OnUpdateLog -= (log) => Console_OnUpdateLog(console, log);

            if (Peer is qServer server)
                server.Send(new CC_ConsoleDeregister().CreateEmptyPacketForConsole(console));
        }


        public bool ConsoleRegistered(qConsole console) =>
            ConsoleRegistered(console.Name);

        public bool ConsoleRegistered(string name) =>
            RegisteredConsoles.ContainsKey(name);


        public RegisteredConsole Get(string name) =>
            RegisteredConsoles.TryGetValue(name, out var console) ? console : null;

        private void Console_OnLog(qConsole console, qLog log)
        {
            if (Peer is qServer server)
                server.Send(new CC_ConsoleLog().BuildPacket(console, log, false));
        }

        private void Console_OnUpdateLog(qConsole console, qLog log)
        {
            if (Peer is qServer server)
                server.Send(new CC_ConsoleLog().BuildPacket(console, log, true));
        }

        public IEnumerator<RegisteredConsole> GetEnumerator() =>
            RegisteredConsoles
            .Select(x => x.Value)
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            RegisteredConsoles
            .Select(x => x.Value)
            .GetEnumerator();

        public class RegisteredConsole
        {
            public RegisteredConsole(qConsole console)
            {
                Console = console;
            }

            public qConsole Console { get; private set; }
            internal qConsoleInstanceManager manager;

            public void SendCommand(string cmd)
            {
                if (!(manager.Peer is qClient client))
                    throw new Exception("Only clients can send commands!");

                var packet = new CC_ExecuteCommand().BuildPacket(Console, cmd);
                client.Send(packet);
            }
        }
    }
}