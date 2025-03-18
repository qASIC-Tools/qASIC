using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using qASIC.Communication.Components;
using System.Linq;

namespace qASIC.Communication
{
    public class qClient : qPeer
    {
        public qClient(CommsComponentCollection components, int maxConnectionAttempts = 8) :
            this(components, IPAddress.Parse("127.0.0.1"), Constants.DEFAULT_PORT, maxConnectionAttempts)
        { }

        public qClient(CommsComponentCollection components, IPAddress address, int port, int maxConnectionAttempts = 8)
        {
            Components = components;

            Address = address;
            Port = port;

            this.maxConnectionAttempts = maxConnectionAttempts;
        }

        public enum DisconnectReason
        {
            /// <summary>Client disconnected voluntarily</summary>
            None,
            /// <summary>Server was shut down</summary>
            ServerShutdown,
            /// <summary>Couldn't connect to the server in time</summary>
            FailedToEstablishConnection,
            /// <summary>Couldn't receive network server info in time</summary>
            FailedToReceiveConnectionInformation,
            /// <summary>Client didn't receive a pong signal in time</summary>
            NoResponse,
            /// <summary>General error</summary>
            Error,
            SendError,
        }

        public enum State
        {
            Offline,
            Connecting,
            Pending,
            Connected,
        }

        public NetworkServerInfo AppInfo { get; set; } = new NetworkServerInfo();

        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        public State CurrentState { get; internal set; } = State.Offline;
        public override bool IsActive => CurrentState != State.Offline;

        public int maxConnectionAttempts;
        private int connectionAttempts = 0;

        public TcpClient Socket { get; private set; }
        public NetworkStream Stream { get; private set; }

        public Action OnStart;
        public Action OnConnect;
        public Action<DisconnectReason> OnDisconnect;
        public Func<qPacket, NetworkServerInfo> ProcessAppInfo = null;

        public int MaxMissedPings { get; set; } = 3;

        private byte[] buffer = new byte[0];
        public qPacket currentRead = null;
        public int readLength;

        internal int missedPings;

        public bool logPackets = false;

        public qClient WithUpdateLoop(int milisecondsPerUpdate = 10)
        {
            StartUpdateLoop(milisecondsPerUpdate);
            return this;
        }

        public void Connect() =>
            Connect(Address, Port);

        public void Connect(IPAddress address, int port)
        {
            if (IsActive)
                throw new Exception("Cannot connect client, client is already active!");

            Address = address;
            Port = port;

            OnStart?.Invoke();
            Logs.Log("Starting client...");

            try
            {
                PrepareStart();
                connectionAttempts = 0;

                Socket = new TcpClient()
                {
                    ReceiveBufferSize = Constants.BUFFER_SIZE,
                    SendBufferSize = Constants.BUFFER_SIZE,
                    NoDelay = false,
                };

                buffer = new byte[Constants.BUFFER_SIZE];
                IAsyncResult result = Socket.BeginConnect(Address, Port, null, null);

                CurrentState = State.Connecting;
                Logs.Log($"Client is active, connecting to {Address}:{Port}...");
                SendLoop();
                Heartbeat(result);

            }
            catch (Exception e)
            {
                Logs.LogError($"Failed to connect client: {e}");
                Disconnect(DisconnectReason.Error);
            }
        }

        void Heartbeat(IAsyncResult result)
        {
            if (!IsActive)
                return;

            try
            {
                switch (CurrentState)
                {
                    case State.Connecting:
                        if (Socket!.Connected)
                        {
                            Socket.EndConnect(result);
                            Stream = Socket.GetStream();
                            Stream.BeginRead(buffer, 0, Constants.BUFFER_SIZE, OnDataReceived, null);

                            Send(new CC_ConnectData().CreateClientConfirmationPacket());

                            CurrentState = State.Pending;
                            Logs.Log($"Connection established, waiting for connection confirmation");
                            break;
                        }

                        if (connectionAttempts >= maxConnectionAttempts)
                        {
                            Logs.Log($"Couldn't establish connection");
                            DisconnectLocal(DisconnectReason.FailedToEstablishConnection);
                            return;
                        }

                        Logs.Log($"Connection attempt: {connectionAttempts}");
                        connectionAttempts++;
                        break;
                    case State.Pending:
                        if (connectionAttempts >= maxConnectionAttempts)
                        {
                            Logs.Log($"Failed to receive connection confirmation.");
                            Disconnect(DisconnectReason.FailedToReceiveConnectionInformation);
                            return;
                        }

                        connectionAttempts++;
                        break;
                    case State.Connected:
                        if (missedPings > MaxMissedPings)
                        {
                            Logs.Log($"Server didn't respond, disconnecting...");
                            DisconnectLocal(DisconnectReason.NoResponse);
                            return;
                        }

                        missedPings++;
                        Send(new CC_Ping().CreateEmptyComponentPacket());
                        break;
                }
            }
            catch (Exception e)
            {
                Logs.LogError($"Failed to execute update loop: {e}");
            }

            ExecuteLater(1000, () => Heartbeat(result));
        }

        void OnDataReceived(IAsyncResult result)
        {
            if (!IsActive) return;

            missedPings = 0;

            try
            {
                if (Stream?.CanRead != true)
                {
                    Logs.LogError("Stream couldn't be read, disconnecting...");
                    DisconnectLocal();
                    return;
                }

                int streamLength = Stream.EndRead(result);

                if (streamLength == 0)
                {
                    Logs.LogError("Stream was empty, disconnecting...");
                    DisconnectLocal();
                    return;
                }

                if (logPackets)
                    Logs.Log($"Incomming data, length:{streamLength}");

                var packet = new qPacket();
                packet.bytes.AddRange(buffer.Take(streamLength));

                while (packet.bytes.Count > 0)
                {
                    if (currentRead == null)
                    {
                        currentRead = new qPacket();
                        readLength = packet.ReadInt();
                        packet.RemoveReadBytes();
                    }

                    var dataLength = Math.Min(packet.bytes.Count, readLength - currentRead.bytes.Count);
                    currentRead.WriteBytes(packet.ReadCurrentBytes(dataLength));
                    packet.RemoveReadBytes();

                    if (currentRead.bytes.Count == readLength)
                    {
                        Components.HandlePacketForClient(this, currentRead);
                        currentRead = null;
                        readLength = 0;
                    }
                }

                Array.Clear(buffer, 0, Constants.BUFFER_SIZE);

                if (IsActive)
                    Stream.BeginRead(buffer, 0, Constants.BUFFER_SIZE, OnDataReceived, null);
            }
            catch (Exception e)
            {
                Logs.LogError($"There was an error while processing data: {e}");
            }
        }

        Queue<qPacket> packetsToSend = new Queue<qPacket>();

        public override void Send(qPacket packet)
        {
            packet.bytes.InsertRange(0, new qPacket()
                .Write(packet.bytes.Count));

            //Enqueue packet to be send in send loop
            packetsToSend.Enqueue(packet);
        }

        private void SendLoop()
        {
            try
            {
                while (Stream?.CanWrite == true &&
                    packetsToSend.TryDequeue(out var packet))
                {
                    if (logPackets)
                        Logs.Log($"Sending packet - {packet}");

                    Stream?.Write(packet.ToArray(), 0, packet.bytes.Count);
                }
            }
            catch
            {
                Logs.LogError($"There was a problem while sending, disconecting...");
                DisconnectLocal(DisconnectReason.SendError);
            }

            ExecuteLater(MilisecondsPerSend, SendLoop);
        }

        public void Disconnect(DisconnectReason reason = DisconnectReason.None)
        {
            Send(new CC_Disconnect().CreateEmptyComponentPacket());
            ExecuteLater(MilisecondsPerSend, () => DisconnectLocal(reason));
        }

        public void DisconnectLocal(DisconnectReason reason = DisconnectReason.None)
        {
            CurrentState = State.Offline;

            try
            {
                Stream?.Close();
                Socket?.Close();

                PrepareStop();

                Logs.Log("Client disconnected");
                OnDisconnect?.Invoke(reason);
            }
            catch (Exception e)
            {
                Logs.LogError($"There was a problem while disconnecting. Please restart application! {e}");
            }
        }
    }
}