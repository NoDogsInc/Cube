using LiteNetLib;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Cube.Transport {
    public sealed class LiteNetServerNetworkInterface : IServerNetworkInterface, INetEventListener {
        public Func<BitStream, ApprovalResult> ApproveConnection { get; set; }
        public Action<Connection> NewConnectionEstablished { get; set; }
        public Action NetworkError { get; set; }
        public Action<Connection> DisconnectNotification { get; set; }
        public Action<BitStream, Connection> ReceivedPacket { get; set; }

        public bool IsRunning => server.IsRunning;

        readonly NetManager server;

        public LiteNetServerNetworkInterface(ushort port) {
            server = new NetManager(this);
#if UNITY_EDITOR
            server.EnableStatistics = true;
#endif

            server.Start(port);
        }

        public void BroadcastBitStream(BitStream bs, PacketReliability reliablity, int sequenceChannel = 0) {
            server.SendToAll(bs.Data, 0, bs.Length, (byte)sequenceChannel, GetDeliveryMethod(reliablity));
        }

        public void SendBitStream(BitStream bs, PacketReliability reliablity, Connection connection, int sequenceChannel = 0) {
            var peer = server.GetPeerById((int)connection.id);
            peer.Send(bs.Data, 0, bs.Length, (byte)sequenceChannel, GetDeliveryMethod(reliablity));
        }

        public void Shutdown() {
            server.Stop();
        }

        public void Update() {
            server.PollEvents();
            BitStreamPool.FrameReset();

#if UNITY_EDITOR
            TransportDebugger.CycleFrame();

            {
                var f = server.Statistics.BytesSent / Time.time;
                f /= 1024; // b -> kb
                var f2 = Mathf.RoundToInt(f * 100) * 0.01f;

                TransportDebugger.ReportStatistic($"out {server.Statistics.PacketsSent} {f2}k/s");
            }
            {
                var f = server.Statistics.BytesReceived / Time.time;
                f /= 1024; // b -> kb
                var f2 = Mathf.RoundToInt(f * 100) * 0.01f;

                TransportDebugger.ReportStatistic($"in {server.Statistics.PacketsSent} {f2}k/s");
            }
#endif
        }

        public void OnPeerConnected(NetPeer peer) {
            NewConnectionEstablished.Invoke(new Connection((ulong)peer.Id));
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            DisconnectNotification(new Connection((ulong)peer.Id));
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
            NetworkError();
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
            var bs = BitStream.CreateWithExistingBuffer(reader.RawData,
                reader.UserDataOffset * 8,
                reader.RawDataSize * 8);

            ReceivedPacket(bs, new Connection((ulong)peer.Id));

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
        }

        public void OnConnectionRequest(ConnectionRequest request) {
            var bs = BitStream.CreateWithExistingBuffer(request.Data.RawData,
                request.Data.UserDataOffset * 8,
                request.Data.RawDataSize * 8);

            var approvalResult = ApproveConnection.Invoke(bs);
            if (!approvalResult.Approved) {
                Debug.Log($"[Server] Connection denied ({approvalResult.DenialReason})");
                var deniedBs = BitStreamPool.Create();
                deniedBs.Write(approvalResult.DenialReason);
                request.Reject(deniedBs.Data, 0, deniedBs.Length);
                return;
            }

            Debug.Log("[Server] Connection approved");
            request.Accept();
        }

        static DeliveryMethod GetDeliveryMethod(PacketReliability reliability) {
            return reliability switch {
                PacketReliability.Unreliable => DeliveryMethod.Unreliable,
                PacketReliability.UnreliableSequenced => DeliveryMethod.Sequenced,
                PacketReliability.ReliableUnordered => DeliveryMethod.ReliableUnordered,
                PacketReliability.ReliableOrdered => DeliveryMethod.ReliableOrdered,
                PacketReliability.ReliableSequenced => DeliveryMethod.ReliableSequenced,
                _ => throw new ArgumentException("reliability"),
            };
        }
    }
}
