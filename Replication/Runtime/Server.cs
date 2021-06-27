using System.Collections.Generic;
using Cube.Transport;
using UnityEngine;

namespace Cube.Replication {
    [AddComponentMenu("Cube/Server")]
    public class Server : MonoBehaviour {
        public ServerReplicaManagerSettings ReplicaManagerSettings;

        public IServerNetworkInterface NetworkInterface {
            get;
            internal set;
        }
        public ServerReactor Reactor {
            get;
            internal set;
        }
        public IServerReplicaManager ReplicaManager {
            get;
            internal set;
        }
        public GameObject World {
            get;
            internal set;
        }
        public List<Connection> connections {
            get;
            internal set;
        }


        void Start() {
            connections = new List<Connection>();
            World = new GameObject("Server World");

            var transport = GetComponent<ITransport>();
            if (transport == null) {
                Debug.Log("Missing ITransport", gameObject);
                return;
            }

            NetworkInterface = transport.CreateServerNetworkInterface(60000);
            NetworkInterface.ApproveConnection += bs => new ApprovalResult() { Approved = true };

            NetworkInterface.NewConnectionEstablished += conn => {
                Debug.Log($"[Server] New connection {conn}");

                var viewGO = new GameObject($"ReplicaView {conn}");
                var replicaView = viewGO.AddComponent<ReplicaView>();
                replicaView.Connection = conn;

                ReplicaManager.AddReplicaView(replicaView);
            };
            NetworkInterface.NewConnectionEstablished += OnNewConnectionEstablished;
            NetworkInterface.DisconnectNotification += OnDisconnectNotification;

            Reactor = new ServerReactor(NetworkInterface);
            ReplicaManager = new ServerReplicaManager(this, ReplicaManagerSettings);
        }

        void Update() {
            ReplicaManager.Update();
            NetworkInterface.Update();
        }

        void OnDestroy() {
            NetworkInterface.Shutdown();

            ReplicaManager = null;
            Reactor = null;
            NetworkInterface = null;
        }

        void OnNewConnectionEstablished(Connection connection) {
            connections.Add(connection);
        }

        void OnDisconnectNotification(Connection connection) {
            connections.Remove(connection);
        }
    }
}