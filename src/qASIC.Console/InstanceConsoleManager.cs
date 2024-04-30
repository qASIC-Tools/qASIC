﻿using qASIC.Communication;
using qASIC.Console.Comms;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

using static qASIC.Console.InstanceConsoleManager;

namespace qASIC.Console
{
    public class InstanceConsoleManager : IEnumerable<RegisteredConsole>, IEnumerable
    {
        public InstanceConsoleManager(Client client) : this(client as IPeer) { }
        public InstanceConsoleManager(Server server) : this(server as IPeer) { }
        public InstanceConsoleManager(IPeer peer)
        {
            Peer = peer;
            Peer.Components
                .AddComponent(CC_Log = new CC_ConsoleLog() { ConsoleManager = this })
                .AddComponent(new CC_ExecuteCommand() { ConsoleManager = this })
                .AddComponent(new CC_ConsoleRegister() { ConsoleManager = this })
                .AddComponent(new CC_ConsoleDeregister() { ConsoleManager = this });

            if (Peer is Server server)
            {
                server.OnClientConnect += (Server.Client client) =>
                {
                    foreach (var item in RegisteredConsoles)
                    {
                        server.Send(client, CC_ConsoleRegister.CreatePacket(item.Value.Console));
                        _ = 1;
                    }
                };
            }

            if (Peer is Client client)
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

        public event Action<GameConsole> OnConsoleRegister;

        public void RegisterConsole(GameConsole console)
        {
            RegisteredConsoles.Add(console.Name, new RegisteredConsole(console)
            {
                manager = this,
            });

            console.OnLog += (log) => Console_OnLog(console, log);

            if (Peer is Server server)
                server.SendToAll(CC_ConsoleRegister.CreatePacket(console));

            OnConsoleRegister?.Invoke(console);
        }

        public void DeregisterConsole(GameConsole console) =>
            DeregisterConsole(console.Name);

        public void DeregisterConsole(string name)
        {
            var console = RegisteredConsoles[name].Console;
            RegisteredConsoles.Remove(name);
            console.OnLog -= (log) => Console_OnLog(console, log);

            if (Peer is Server server)
                server.SendToAll(new CC_ConsoleDeregister().CreateEmptyPacketForConsole(console));
        }


        public bool ConsoleRegistered(GameConsole console) =>
            ConsoleRegistered(console.Name);

        public bool ConsoleRegistered(string name) =>
            RegisteredConsoles.ContainsKey(name);


        public RegisteredConsole Get(string name) =>
            RegisteredConsoles.TryGetValue(name, out var console) ? console : null;

        private void Console_OnLog(GameConsole console, qLog log)
        {
            if (Peer is Server server)
                server.SendToAll(CC_ConsoleLog.BuildPacket(console, log));
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
            public RegisteredConsole(GameConsole console)
            {
                Console = console;
            }

            public GameConsole Console { get; private set; }
            internal InstanceConsoleManager manager;

            public void SendCommand(string cmd)
            {
                if (!(manager.Peer is Client client))
                    throw new Exception("Only clients can send commands!");

                var packet = CC_ExecuteCommand.BuildPacket(Console, cmd);
                client.Send(packet);
            }
        }
    }
}