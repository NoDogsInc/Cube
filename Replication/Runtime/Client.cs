using Cube.Transport;
using UnityEngine;

namespace Cube.Replication {
    [AddComponentMenu("Cube/Client")]
    public class Client : MonoBehaviour {
        public IClientNetworkInterface networkInterface {
            get;
            internal set;
        }
        public ClientReactor reactor {
            get;
            internal set;
        }
        public IClientReplicaManager replicaManager {
            get;
            internal set;
        }
        public GameObject world {
            get;
            internal set;
        }

        public bool IsInitialised {
            get;
            internal set;
        }

        public void InitClient() {
            if (IsInitialised)
                return;

            IsInitialised = true;

            world = new GameObject("Client World");

            var transport = GetComponent<ITransport>();
            if (transport == null) {
                Debug.Log("Missing ITransport", gameObject);
                return;
            }

            networkInterface = transport.CreateClientNetworkInterface();
            networkInterface.ConnectionRequestAccepted += () => Debug.Log($"[Client] Connection request accepted");

            reactor = new ClientReactor(networkInterface);
            replicaManager = new ClientReplicaManager(this, NetworkPrefabLookup.Instance);
        }

        void Update() {
            if (!IsInitialised)
                return;

            replicaManager.Update();
            networkInterface.Update();
        }

        void OnDestroy() {
            if (!IsInitialised)
                return;

            networkInterface.Shutdown(0);
        }
    }
}