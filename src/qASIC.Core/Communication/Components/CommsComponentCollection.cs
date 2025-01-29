using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

namespace qASIC.Communication.Components
{
    public class CommsComponentCollection
    {
        private List<CommsComponent> components = new List<CommsComponent>();

        private Dictionary<Type, CommsComponent> componentsDictionary = new Dictionary<Type, CommsComponent>();

        public static CommsComponentCollection GetStandardCollection()
        {
            var comms = new CommsComponentCollection()
                .AddComponent<CC_ConnectData>()
                .AddComponent<CC_Disconnect>()
                .AddComponent<CC_Ping>()
                .AddComponent<CC_Debug>();

            return comms;
        }

        public CommsComponentCollection AddComponent<T>() where T : CommsComponent, new() =>
            AddComponent(new T());

        public CommsComponentCollection AddComponent<T>(T component) where T : CommsComponent
        {
            var type = typeof(T);
            components.Add(component);
            if (!componentsDictionary.ContainsKey(type))
                componentsDictionary.Add(type, component);

            return this;
        }

        public T GetComponent<T>() where T : CommsComponent =>
            GetComponent(typeof(T)) as T;

        public CommsComponent GetComponent(Type type)
        {
            if (!componentsDictionary.ContainsKey(type))
                return null;

            return componentsDictionary[type];
        }

        public T[] GetComponents<T>() where T : CommsComponent =>
            (T[])GetComponents(typeof(T));

        public CommsComponent[] GetComponents(Type type) =>
            componentsDictionary
                .Select(x => x.Value)
                .Where(x => type.IsAssignableFrom(x.GetType()))
                .ToArray();

        /// <summary>Determines after how many more messages a message will be marked as lost.</summary>
        public uint MaxMessageAge { get; set; } = 2048u;

        uint nextMessageId = 0;
        Dictionary<qServer.Client, Dictionary<uint, MessageData>> serverMessages = new Dictionary<qServer.Client, Dictionary<uint, MessageData>>();
        Dictionary<uint, MessageData> clientMessages = new Dictionary<uint, MessageData>();

        public qPacket[] FinalizePacket(qPacket packet)
        {
            //The amount of sections that will be created
            //(amount of bytes in packet - amount of bytes for secitonCount at start) / (buffer size - message id for section - id of section)
            var sectionCount = (packet.bytes.Count + sizeof(int) - 1) / (Constants.BUFFER_SIZE - sizeof(uint) - sizeof(int)) + 1;

            qPacket[] packets = new qPacket[sectionCount];
            packet.bytes.InsertRange(0, new qPacket().Write(sectionCount));
            for (int i = 0; i < sectionCount; i++)
            {
                packet.bytes.InsertRange(i * Constants.BUFFER_SIZE, new qPacket().Write(nextMessageId).Write(i));
                packets[i] = new qPacket(packet.bytes.GetRange(i * Constants.BUFFER_SIZE, Math.Min(Constants.BUFFER_SIZE, packet.bytes.Count - Constants.BUFFER_SIZE * i)));
            }

            nextMessageId++;
            return packets;
        }

        public void HandlePacketForServer(qServer server, qServer.Client serverClient, qPacket packet)
        {
            var messageId = packet.ReadUInt();
            var oldId = messageId - MaxMessageAge;

            if (!serverMessages.TryGetValue(serverClient, out var messages))
            {
                messages = new Dictionary<uint, MessageData>();
                serverMessages.Add(serverClient, messages);
            }

            if (messages.ContainsKey(oldId))
            {
                server.Logs.LogWarning($"Message id '{oldId}' seems to have been lost.");
                messages.Remove(oldId);
            }

            if (!messages.TryGetValue(messageId, out var data))
            {
                data = new MessageData();
                messages.Add(messageId, data);
            }

            //Return if there was an error
            if (!data.HandlePacket(packet, server.Logs))
                return;
            
            //If we don't have all packets, wait for the rest to arrive
            if (!data.CanCreateFinal())
                return;


            //Process full message
            var finalPacket = data.CreateFinalPacket();
            messages.Remove(messageId);

            var compId = finalPacket.ReadString();

            var targetComp = components
                .Where(x => x.GetId() == compId)
                .FirstOrDefault();

            if (targetComp == null)
            {
                server.Logs.LogError($"Communication Component of id '{compId}' does not exist");
                return;
            }

            var args = new CommsComponentArgs(PacketType.Server, finalPacket)
            {
                server = server,
                targetServerClient = serverClient,
            };

            try
            {
                targetComp.Read(args);
            }
            catch (Exception e)
            {
                server.Logs.LogError($"There was an error while reading packet from client {serverClient}: {e}");
            }
        }

        public void HandlePacketForClient(qClient client, qPacket packet)
        {
            var messageId = packet.ReadUInt();
            var oldId = messageId - MaxMessageAge;

            if (clientMessages.ContainsKey(oldId))
            {
                client.Logs.LogWarning($"Message id '{oldId}' seems to have been lost.");
                clientMessages.Remove(oldId);
            }

            if (!clientMessages.TryGetValue(messageId, out var data))
            {
                data = new MessageData();
                clientMessages.Add(messageId, data);
            }

            //Return if there was an error
            if (!data.HandlePacket(packet, client.Logs))
                return;

            //If we don't have all packets, wait for the rest to arrive
            if (!data.CanCreateFinal())
                return;

            
            //Process full message
            var finalPacket = data.CreateFinalPacket();
            clientMessages.Remove(messageId);
            
            var compId = finalPacket.ReadString();

            var targetComp = components
                .Where(x => x.GetId() == compId)
                .FirstOrDefault();

            if (targetComp == null)
            {
                client.Logs.LogError($"Communication Component of id '{compId}' does not exist");
                return;
            }

            var args = new CommsComponentArgs(PacketType.Client, finalPacket)
            {
                client = client,
            };

            try
            {
                targetComp.Read(args);
            }
            catch (Exception e)
            {
                client.Logs.LogError($"There was an error while reading packet from server: {e}");
            }
        }

        public void CleanupServerMessages()
        {
            serverMessages.Clear();
        }

        public void CleanupServerMessages(qServer.Client serverClient)
        {
            if (serverMessages.ContainsKey(serverClient))
                serverMessages.Remove(serverClient);
        }

        public void CleanupClientMessages()
        {
            clientMessages.Clear();
        }

        public class MessageData
        {
            public int packetAmount = 0;
            public Dictionary<int, qPacket> packets = new Dictionary<int, qPacket>();

            public bool HandlePacket(qPacket packet, LogManager logs)
            {
                var packetId = packet.ReadInt();

                if (packetId == 0)
                {
                    packetAmount = packet.ReadInt();
                    var errors = new Stack<int>();
                    foreach (var item in packets)
                    {
                        if (item.Key > packetAmount)
                        {
                            errors.Push(item.Key);
                            logs.LogError("There are packets saved that have a greater id than what is expected.");
                        }
                    }

                    foreach (var item in errors)
                        packets.Remove(item);
                }

                if (packetId < 0)
                {
                    logs.LogError("Error processing packet, id is less than zero (possible corruption of packet).");
                    return false;
                }

                if (packetAmount != 0 && packetId > packetAmount)
                {
                    logs.LogError("Error processing packet, packet id seems to be greater than the expected amount.");
                    return false;
                }

                packets.SetOrAdd(packetId, packet);

                return true;
            }

            public bool CanCreateFinal() =>
                packetAmount == packets.Count;

            public qPacket CreateFinalPacket()
            {
                if (!CanCreateFinal())
                    return null;

                var finalPacket = new qPacket();
                for (int i = 0; i < packetAmount; i++)
                {
                    var p = packets[i];
                    p.RemoveReadBytes();
                    finalPacket.WriteBytes(p.bytes);
                }

                return finalPacket;
            }
        }
    }
}