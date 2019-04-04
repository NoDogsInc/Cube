﻿using System;
using System.Collections.Generic;

namespace Cube.Networking.Transport.Tests {
#if CLIENT && SERVER
    public class LocalServerInterface : IServerNetworkInterface {
        class Message {
            public Connection connection;
            public BitStream bs;
        }

        ulong nextConnectionId = 0;
        public List<LocalClientInterface> clients = new List<LocalClientInterface>();

        Queue<Message> messageQueue = new Queue<Message>();

        BitStreamPool _bitStreamPool = new BitStreamPool();
        public BitStreamPool bitStreamPool {
            get { return _bitStreamPool; }
        }

        public bool isRunning {
            get { return true; }
        }

        public Connection[] GetConnections() {
            var connections = new Connection[clients.Count];

            for (int i = 0; i < connections.Length; i++)
                connections[i] = clients[i].connection;

            return connections;
        }

        public void Update() {
            bitStreamPool.FrameReset();
        }

        public BitStream Receive(out Connection connection) {
            connection = Connection.Invalid;

            if (messageQueue.Count == 0)
                return null;

            var msg = messageQueue.Dequeue();
            connection = msg.connection;
            return msg.bs;
        }

        public void Send(BitStream bs, PacketPriority priority, PacketReliability reliablity, Connection connection) {
            LocalClientInterface client = null;

            foreach(var tmp in clients) {
                if(tmp.connection == connection) {
                    client = tmp;
                    break;
                }
            }

            if (client == null)
                throw new Exception("Client not found.");

            client.EnqueueMessage(bs);
        }

        public void Shutdown() {
            throw new Exception("Not required.");
        }

        public void Start() {
            throw new Exception("Not required.");
        }

#region TestInterface

        public void AddClient(LocalClientInterface client) {
            client.connection = new Connection(nextConnectionId);
            nextConnectionId++;

            clients.Add(client);
        }

        public void EnqueueMessage(Connection connection, BitStream bs) {
            var message = new Message();
            message.connection = connection;
            message.bs = bs;
            messageQueue.Enqueue(message);
        }

#endregion

    }
#endif
}