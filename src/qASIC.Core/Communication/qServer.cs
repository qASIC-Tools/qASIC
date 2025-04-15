using System.Net;
using System.Net.Sockets;
using qASIC.Communication.Components;
using System.Collections.Generic;
using System;
using System.Linq;

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

        public TcpListener Listener { get; private set; }

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
            Listener = new TcpListener(LocalOnly ? IPAddress.Loopback : IPAddress.Any, Port);
            Listener.Start();
            Port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            Logs.Log($"Starting server on port {Port}...");
            Listener.BeginAcceptTcpClient(new AsyncCallback(HandleClientConnect), null);

            nextClientId = 0;
            IsActive = true;

            Heartbeat();
            SendLoop();

            Logs.Log("Server is now active!");

            OnStart?.Invoke();
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
            Listener.Stop();

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
            Logs.UnregisterLoggable(client);

            OnClientDisconnect?.Invoke(client);
        }

        public void ChangePort(int port)
        {
            if (IsActive)
                throw new Exception("Cannot change credentials when server is active!");

            Port = port;
        }

        #region Callbacks
        private void HandleClientConnect(IAsyncResult result)
        {
            if (!IsActive)
                return;

            try
            {
                var clientSocket = Listener.EndAcceptTcpClient(result);
                clientSocket.NoDelay = false;
                clientSocket.ReceiveBufferSize = Constants.BUFFER_SIZE;
                clientSocket.SendBufferSize = Constants.BUFFER_SIZE;

                Listener.BeginAcceptTcpClient(new AsyncCallback(HandleClientConnect), null);

                Client newClient = new Client(nextClientId, clientSocket, HandleDataReceive);
                Logs.RegisterLoggable(newClient);
                Clients.Add(newClient);
                newClient.Initialize();

                nextClientId++;

                Logs.Log($"Connection received, creating client id: {newClient.id}");
            }
            catch (Exception e)
            {
                Logs.LogError($"There was an error while connecting client: {e}");
            }
        }

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
                    while (client.Stream?.CanWrite == true &&
                        client.packetsToSend.TryDequeue(out qPacket packet))
                    {
                        if (logPackets)
                            Logs.Log($"Sending packet to client '{client.id}' - {packet}");

                        client.Stream.Write(packet.ToArray(), 0, packet.bytes.Count);
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
            public Client(int id, TcpClient socket, Action<OnServerReceiveDataArgs> onDataReceive)
            {
                this.id = id;

                Socket = socket;
                Stream = socket.GetStream();
                buffer = new byte[Constants.BUFFER_SIZE];

                OnDataReceive = onDataReceive;
            }

            public int id;
            public bool IsActive { get; private set; }
            public bool Connected { get; set; }

            public TcpClient Socket { get; private set; }
            public NetworkStream Stream { get; private set; }

            public LogManager Logs { get; set; } = new LogManager();
            public event Action<OnServerReceiveDataArgs> OnDataReceive;

            public Queue<qPacket> packetsToSend = new Queue<qPacket>();

            private byte[] buffer;
            private qPacket readPacket = null;
            private int readLength;

            public void Initialize()
            {
                IsActive = true;
                Stream.BeginRead(buffer, 0, Constants.BUFFER_SIZE, HandleReceiveData, null);
            }

            public void DisconnectLocal()
            {
                Stream.Close();
                Socket.Close();

                IsActive = false;
                Logs.Log($"Client id: {id} has been disconnected locally");
            }

            private void HandleReceiveData(IAsyncResult result)
            {
                try
                {
                    //FIXME: when the server stops client id:0 IsActive is still set to true,
                    //even though it was changed in the stop method. If you disconnect and
                    //reconnect, which assigns a new id, the error doesn't appear
                    if (!IsActive || !Stream.CanRead)
                        return;

                    int streamLength = Stream.EndRead(result);
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

                    if (IsActive)
                        Stream.BeginRead(buffer, 0, Constants.BUFFER_SIZE, HandleReceiveData, null);
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