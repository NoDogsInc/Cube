using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cube.Transport {
    public interface ITransport {
        IServerNetworkInterface CreateServerNetworkInterface(ushort port);
        IClientNetworkInterface CreateClientNetworkInterface();
    }
}