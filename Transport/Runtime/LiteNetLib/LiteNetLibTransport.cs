using UnityEngine;

namespace Cube.Transport {
    [AddComponentMenu("Cube/Transport/LiteNetLib")]
    public class LiteNetLibTransport : MonoBehaviour, ITransport {
        public IClientNetworkInterface CreateClientNetworkInterface() => new LiteNetClientNetworkInterface();

        public IServerNetworkInterface CreateServerNetworkInterface(ushort port) => new LiteNetServerNetworkInterface(port);
    }
}