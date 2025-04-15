using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace qASIC.Communication.Discovery
{
    public class DiscoveryServer
    {
        /// <param name="targetServer">Server to be broadcasted.</param>
        /// <param name="port">Port used for discovery. It has to be the same as on the <see cref="DiscoveryClient"/>!</param>
        public DiscoveryServer(qServer targetServer, int port = Constants.DEFAULT_DISCOVERY_PORT)
        {
            TargetServer = targetServer;
            Port = port;
        }

        private Thread _thread;
        private CancellationTokenSource _cancel;

        /// <summary>How long does an update loop take in miliseconds.</summary>
        public int UpdateFrequency { get; set; } = 200;

        /// <summary>Is the server active?</summary>
        public bool IsActive { get; private set; }

        /// <summary>Port used for discovery. It has to be the same as on the <see cref="DiscoveryClient"/>!</summary>
        public int Port { get; private set; }
        /// <param name="targetServer">Server to be broadcasted.</param>
        public qServer TargetServer { get; private set; }

        /// <summary>Should broadcast using the IPv4 protocol?</summary>
        public bool UseIPv4 { get; set; } = true;
        /// <summary>Should broadcast using the IPv6 protocol?</summary>
        public bool UseIPv6 { get; set; } = false;

        /// <summary>Changes <see cref="Port"/> when the server is inactive.</summary>
        /// <param name="port">The new port.</param>
        /// <exception cref="Exception">Gets thrown when the server is already active.</exception>
        public void ChangePort(int port)
        {
            if (IsActive)
                throw new Exception("Cannot change port, the discovery server is already active!");

            Port = port;
        }

        /// <summary>Starts broadcasting server information.</summary>
        /// <exception cref="Exception">Gets thrown when the server is already active.</exception>
        public void Start()
        {
            if (IsActive)
                throw new Exception("Cannot start discovery server, server is already active!");

            Sockets = new Dictionary<IPAddress, Socket>();
            Ip6Link = IPAddress.Parse("ff02::1");
            EndPoint4 = new IPEndPoint(IPAddress.Broadcast, Port);
            EndPoint6 = new IPEndPoint(Ip6Link, Port);

            IsActive = true;
            _thread = new Thread(async () => await UpdateLoop());
            _cancel = new CancellationTokenSource();
            _thread.Start();
        }

        /// <summary>Stops broadcasting server information.</summary>
        /// <exception cref="Exception">Gets thrown when the server is already inactive.</exception>
        public void Stop()
        {
            if (!IsActive)
                throw new Exception("Cannot stop discovery server, server is already inactive!");

            IsActive = false;
            _cancel.Cancel();
            _thread.Join();
            _cancel = null;
            _thread = null;

            Sockets = null;
            Ip6Link = null;
            EndPoint4 = null;
            EndPoint6 = null;
        }

        Dictionary<IPAddress, Socket> Sockets { get; set; }
        IPAddress Ip6Link { get; set; }
        IPEndPoint EndPoint4 { get; set; }
        IPEndPoint EndPoint6 { get; set; }

        async Task UpdateLoop()
        {
            var cancel = _cancel;

            var stopwatch = new Stopwatch();
            bool serverState = false;

            while (!cancel.IsCancellationRequested)
            {
                stopwatch.Reset();
                stopwatch.Start();

                Process();

                if (!TargetServer.IsActive && serverState)
                {
                    foreach (var item in Sockets.Values)
                        item.Dispose();

                    Sockets.Clear();
                }

                serverState = TargetServer.IsActive;

                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds < UpdateFrequency)
                    await Task.Delay(UpdateFrequency - (int)stopwatch.ElapsedMilliseconds);
            }
        }

        void Process()
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up || x.OperationalStatus == OperationalStatus.Unknown)
                .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                .Select(x => x.Address)
                .Where(x => (UseIPv4 && x.AddressFamily == AddressFamily.InterNetwork) ||
                    (UseIPv6 && x.AddressFamily == AddressFamily.InterNetworkV6));

            var added = addresses.Except(Sockets.Select(x => x.Key));
            var removed = Sockets.Select(x => x.Key).Except(addresses);

            foreach (var item in removed)
            {
                Sockets[item].Dispose();
                Sockets.Remove(item);
            }

            foreach (var item in added)
            {
                var is6 = item.AddressFamily == AddressFamily.InterNetworkV6;

                var socket = new Socket(item.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false,
                };

                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                if (is6)
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(Ip6Link));

                socket.Bind(new IPEndPoint(item, Port));

                Sockets.Add(item, socket);
            }

            var identity = new qPacket()
                .Write(TargetServer.Port)
                .Write(TargetServer.AppInfo)
                .ToArray();

            foreach (var item in Sockets)
            {
                try
                {
                    var is6 = item.Key.AddressFamily == AddressFamily.InterNetworkV6;
                    item.Value.SendTo(identity, is6 ? EndPoint6 : EndPoint4);
                }
                catch { }
            }
        }
    }
}