using UnityEngine;
using UnityEngine.Assertions;

namespace Cube.Networking.Replicas {
    public class NetworkBehaviour : MonoBehaviour {
        IReplicaWorld _world;
        /// <summary>
        /// World this object is part of. Can be downcast to your actual implementation.
        /// </summary>
        /// <example> 
        /// This sample shows how downcasting might look.
        /// <code>
        /// class Test :  NetworkBehaviour {
        ///     void Awake() {
        ///         var world = (World)replicaWorld;
        ///         // Use world
        ///     }
        /// }
        /// </code>
        /// </example>
        public IReplicaWorld replicaWorld {
            get {
                if (_world == null) {
                    _world = GetComponentInParent<IReplicaWorld>();
                }
                return _world;
            }
        }

#if CLIENT
        public IUnityClient client {
            get {
                return replicaWorld.client;
            }
        }
#endif

#if SERVER
        public IUnityServer server {
            get {
                return replicaWorld.server;
            }
        }
#endif

        public bool isServer {
            get {
#if SERVER
                return server != null;
#else
                return false;
#endif
            }
        }

        public bool isClient {
            get {
#if CLIENT
                return client != null;
#else
                return false;
#endif
            }
        }
    }
}