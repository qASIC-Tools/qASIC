using System.Net;
using System.Net.Sockets;
using qASIC.Communication.Components;
using System.Collections.Generic;
using System;
using System.Linq;
using qASIC.Logging;
using System.Data;
using Microsoft.VisualBasic;
using System.Reflection.Metadata;

namespace qASIC.Communication
{
    public class qServer : qPeer
    {
        public qServer(CommsComponentCollection components) : this(components, Constants.DEFAULT_PORT) { }

        public qServer(CommsComponentCollection components, int port)
        {
            Components = components;

            Port = port;
        }

        public NetworkServerInfo AppInfo { get; set; } = new NetworkServerInfo();

        public int Port { get; private set; }

        /// <summary>If discovery should be limited to just this maschine.</summary>
        public bool LocalOnly { get; set; } = true;

        public List<Client> Clients { get; private set; } = new List<Client>();

        public Socket socket;

        public Action<Client> OnClientConnect;
        public event Action<Client> OnClientDisconnect;
        public event Action OnStart;
        public event Action OnStop;

        int nextClientId;
        public bool logPackets = false;

        public void Start()
        {
            if (IsActive)
                throw new Exception("Cannot start server, server is already active!");

            PrepareStart();
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                SendBufferSize = Constants.BUFFER_SIZE,
                ReceiveBufferSize = Constants.BUFFER_SIZE,
                NoDelay = true,
            };

            var endPoint = new IPEndPoint(LocalOnly ? IPAddress.Loopback : IPAddress.IPv6Any, Port);

            socket.Bind(endPoint);
            socket.Listen();

            Port = ((IPEndPoint)socket.LocalEndPoint).Port;
            
            Logs.Log($"Starting server on port {Port}...");

            nextClientId = 0;
            IsActive = true;

            Heartbeat();
            SendLoop();

            Logs.Log("Server is now active!");

            OnStart?.Invoke();
        }

        public override void OnUpdate()
        {
            AcceptConnections();

            foreach (var item in Clients)
                item.Read();
        }

        public qServer WithUpdateLoop(int milisecondsPerUpdate = 50)
        {
            StartUpdateLoop(milisecondsPerUpdate);
            return this;
        }

        void Heartbeat()
        {
            foreach (var item in Clients)
                Send(item, new CC_Ping().CreateEmptyComponentPacket());

            ExecuteLater(1000, Heartbeat);
        }

        private void AcceptConnections()
        {
            if (socket.Poll(0, SelectMode.SelectRead))
            {
                var accepted = socket.Accept();
                var endPoint = (IPEndPoint)accepted.RemoteEndPoint;
                if (!Clients.Any(x => x.Socket.RemoteEndPoint == endPoint))
                {
                    var conn = new Client(nextClientId++, accepted, HandleDataReceive);
                    Logs.Log($"Connection received, creating client id: {conn.id}");
                    Clients.Add(conn);
                    Logs.Register(conn);
                    conn.Initialize();
                }
                else
                    accepted.Close();
            }
        }

        public void Stop(bool notifyClients = true)
        {
            if (!IsActive)
                throw new Exception("Server is already stopped!");

            //Send disconnect message to clients
            switch (notifyClients)
            {
                case true:
                    while (Clients.Count > 0)
                        DisconnectClient(Clients[0]);

                    break;
                case false:
                    while (Clients.Count > 0)
                        DisconnectClientLocal(Clients[0]);

                    break;
            }

            Logs.Log("Stopping server...");
            socket.Close();

            PrepareStop();
            IsActive = false;

            Logs.Log("Stopped server");

            OnStop?.Invoke();
        }

        public void DisconnectClient(Client client)
        {
            Send(client, new CC_Disconnect().CreateEmptyComponentPacket());
            ExecuteLater(MilisecondsPerSend, () => DisconnectClientLocal(client));
        }

        public void DisconnectClientLocal(Client client)
        {
            client.DisconnectLocal();
            Clients.Remove(client);
            Logs.Unregister(client);

            OnClientDisconnect?.Invoke(client);
        }

        public void ChangePort(int port)
        {
            if (IsActive)
                throw new Exception("Cannot change credentials when server is active!");

            Port = port;
        }

        #region Callbacks
        private void HandleDataReceive(OnServerReceiveDataArgs args)
        {
            if (logPackets)
                Logs.Log($"Received packet from client '{args.client.id}' - {args.data}");

            Components.HandlePacketForServer(this, args.client, args.data);
        }
        #endregion

        #region Send
        public void Send(Client client, qPacket packet)
        {
            if (!client.Connected) return;

            packet.bytes.InsertRange(0, new qPacket()
                .Write(packet.bytes.Count));

            if (logPackets)
                Logs.Log($"Adding packets to send queue, client: {client.id}, count: {packet.bytes.Count}");

            client.packetsToSend.Enqueue(packet);
        }

        public override void Send(qPacket packet)
        {
            for (int i = 0; i < Clients.Count; i++)
                if (Clients[i] != null)
                    Send(Clients[i], packet);
        }

        private void SendLoop()
        {
            foreach (var client in Clients.ToArray())
            {
                try
                {
                    while (client.packetsToSend.TryDequeue(out qPacket packet))
                    {
                        if (logPackets)
                            Logs.Log($"Sending packet to client '{client.id}' - {packet}");

                        client.Socket.Send(packet.ToArray(), 0, packet.bytes.Count, SocketFlags.None);
                    }
                }
                catch
                {
                    Logs.LogError($"There was an error while sending data to client '{client.id}', removing...");
                    DisconnectClientLocal(client);
                }
            }

            ExecuteLater(MilisecondsPerSend, SendLoop);
        }
        #endregion


        public class Client : IHasLogs
        {
            public Client(int id, Socket socket, Action<OnServerReceiveDataArgs> onDataReceive)
            {
                this.id = id;

                Socket = socket;
                buffer = new byte[Constants.BUFFER_SIZE];

                OnDataReceive = onDataReceive;
            }

            public int id;
            public bool IsActive { get; private set; }
            public bool Connected { get; set; }

            public Socket Socket { get; private set; }

            public qLogManager Logs { get; set; } = new qLogManager();
            public event Action<OnServerReceiveDataArgs> OnDataReceive;

            public Queue<qPacket> packetsToSend = new Queue<qPacket>();

            private byte[] buffer;
            private qPacket readPacket = null;
            private int readLength;

            public void Initialize()
            {
                IsActive = true;
            }

            public void DisconnectLocal()
            {
                Socket.Close();

                IsActive = false;
                Logs.Log($"Client id: {id} has been disconnected locally");
            }

            public void Read()
            {
                try
                {
                    //FIXME: when the server stops client id:0 IsActive is still set to true,
                    //even though it was changed in the stop method. If you disconnect and
                    //reconnect, which assigns a new id, the error doesn't appear
                    if (!IsActive)
                        return;

                    if (Socket.Available == 0)
                        return;

                    int streamLength = Socket.Receive(buffer, Constants.BUFFER_SIZE, SocketFlags.None);
                    if (streamLength <= 0)
                    {
                        Logs.LogError($"Couldn't process data for client id '{id}'");
                        return;
                    }

                    var packet = new qPacket();
                    packet.bytes.AddRange(buffer.Take(streamLength));

                    while (packet.position < packet.bytes.Count)
                    {
                        if (readPacket == null)
                        {
                            readPacket = new qPacket();
                            readLength = packet.ReadInt();
                        }

                        var dataLength = Math.Min(packet.bytes.Count - packet.position, readLength - readPacket.bytes.Count);
                        readPacket.WriteBytes(packet.ReadCurrentBytes(dataLength));

                        if (readPacket.bytes.Count == readLength)
                        {
                            OnDataReceive?.Invoke(new OnServerReceiveDataArgs(this, readPacket));
                            readPacket = null;
                            readLength = 0;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logs.LogError($"Unexpected error while processing data: {e}");
                }
            }

            public override string ToString() =>
                $"Server Client (id: {id})";
        }
    }
}