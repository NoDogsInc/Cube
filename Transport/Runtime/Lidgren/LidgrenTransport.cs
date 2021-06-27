using UnityEngine;

namespace Cube.Transport {
    [AddComponentMenu("Cube/Transport/Lidgren")]
    public class LidgrenTransport : MonoBehaviour, ITransport {
        public SimulatedLagSettings LagSettings;

        public IClientNetworkInterface CreateClientNetworkInterface() => new LidgrenClientNetworkInterface(LagSettings);
        public IServerNetworkInterface CreateServerNetworkInterface(ushort port) => new LidgrenServerNetworkInterface(port, LagSettings);
    }
}