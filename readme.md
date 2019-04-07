# Core

## Getting Started

### Connecting server and client in editor
Create new GameObject *ClientGame* in the scene and add the *Core/ClientGame* component. Create another new GameObject *ServerGame* and add the *Core/ServerGame* component. 
When you start the game now you should see log output of the client connecting to the server.

Note that the instant connection in ClientGame is just enabled in the Unity editor.

Now that we've got a connection we can start looking at replication.

### Replication
The hearth of Cube is a powerful replication system.

Create a new GameObject in the scene. Add the Cube/Replica component to mark it as an Replica. Add the Cube/ReplicaTransform component to synchronize their transforms.
Create a new prefab **TestReplica** from this GameObject.

Create a new script TestServerGame:
```C#
using Cube.Networking;
using Cube.Replication;
using Cube.Transport;
using UnityEngine;
using BitStream = Cube.Transport.BitStream;

public class TestServerGame : ServerGame {
    public GameObject prefab;

    protected override void OnNewIncomingConnection(Connection connection, BitStream bs) {
		// Create a new ReplicaView for this Connection
        var view = new GameObject();
        view.transform.parent = transform;

        var rw = view.AddComponent<ReplicaView>();
        rw.connection = connection;
        
        server.replicaManager.AddReplicaView(rw);

		// Instantiate some Replica
        server.replicaManager.InstantiateReplica(prefab);
    }
}
```
Replace the *ServerGame* component on the ServerGame scene GameObject. Assign the TestReplica prefab to the prefab field on ServerGame.

Start the game now and you should see the Replica prefab being replicated. Try to move around the server-side instance in the editor.

For the client to instante a different prefab, rename your prefab to **Server_TestReplica**
and create a new prefab variant **Client_TestReplica** (The name prefixes Server_ and Client_ are important).
Now you can for instance set a blue transparent material color on the server prefab.