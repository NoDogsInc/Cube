﻿using Cube.Transport;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using BitStream = Cube.Transport.BitStream;

namespace Cube.Replication {
    [AddComponentMenu("Cube/Replica")]
    [DisallowMultipleComponent]
    public class Replica : MonoBehaviour {
        public struct QueuedRpc {
            public RpcTarget target;
            public BitStream bs;
        }

        public static ReplicaSettings defaultReplicaSettings;
        public ReplicaSettings settings;
        public ReplicaSettings settingsOrDefault => settings != null ? settings : defaultReplicaSettings;

        public bool replicateOnlyToOwner;

        [HideInInspector]
        public ReplicaId Id = ReplicaId.Invalid;

        [HideInInspector]
        public ushort prefabIdx;
        [HideInInspector]
        public byte sceneIdx;

        public bool isSceneReplica => sceneIdx != 0;

        public ICubeServer server;
        public ICubeClient client;
        public IReplicaManager ReplicaManager => server != null ? (IReplicaManager)server.ReplicaManager : client.replicaManager;

        public bool isServer => server != null;
        public bool isClient => client != null;

        public Connection Owner {
            get;
            internal set;
        }

        public bool IsOwner {
            get;
            internal set;
        }

        ReplicaBehaviour[] _replicaBehaviours;

        /// <summary>
        /// Used on the client to remove Replicas which received no updates for a long time.
        /// </summary>
        [HideInInspector]
        public float lastUpdateTime;

        public List<QueuedRpc> queuedRpcs = new List<QueuedRpc>();

        static bool _applicationQuitting;

        public void AssignOwnership(Connection owner) {
            Assert.IsTrue(isServer);
            Assert.IsTrue(owner != Connection.Invalid);

            Owner = owner;
            IsOwner = false;
        }

        public void TakeOwnership() {
            Assert.IsTrue(isServer);

            Owner = Connection.Invalid;
            IsOwner = true;
        }

        public void ClientUpdateOwnership(bool owned) {
            Assert.IsTrue(owned != IsOwner);
            IsOwner = owned;
        }

        public bool IsRelevantFor(ReplicaView view) {
            Assert.IsTrue(isServer);

            if (!gameObject.activeInHierarchy)
                return false;

            if (replicateOnlyToOwner)
                return view.Connection == Owner;

            return true;
        }

        /// [0,1]
        public virtual float GetRelevance(ReplicaView view) {
            Assert.IsNotNull(view);
            Assert.IsTrue(isServer);

            if (Owner == view.Connection)
                return 1;

            var usePosition = (settings.priorityFlags & ReplicaPriorityFlag.IgnorePosition) == 0
                && !view.IgnoreReplicaPositionsForPriority;
            if (!usePosition)
                return 1;

            var diff = new Vector2(transform.position.x - view.transform.position.x,
                transform.position.z - view.transform.position.z);

            var sqrMaxDist = Mathf.Pow(settings.MaxViewDistance, 2);
            var sqrMagnitude = diff.sqrMagnitude;
            if (sqrMagnitude > sqrMaxDist)
                return 0; // No costly calculations

            var distanceRelevance = 1f - Mathf.Pow(sqrMagnitude / sqrMaxDist, 0.8f);


            var dotRelevance = Vector2.Dot(new Vector2(view.transform.forward.x, view.transform.forward.z).normalized,
                diff.normalized);
            dotRelevance = Mathf.Max(dotRelevance, 0.5f);

            return distanceRelevance * dotRelevance;
        }

        /// <summary>
        /// SERVER only. Removes the Replica instantly from replication, destroys the GameObject and sends a destroy message to the clients on the next update.
        /// </summary>
        public void Destroy() {
            if (!isServer)
                return;

            server.ReplicaManager.DestroyReplica(this);
        }

        /// <summary>
        /// SERVER only. Removes the Replica instantly from replication. Does NOT send any message to the clients.
        /// </summary>
        public void Remove() {
            if (!isServer)
                return;

            server.ReplicaManager.RemoveReplica(this);
        }

        public void Serialize(BitStream bs, ReplicaBehaviour.SerializeContext ctx) {
            foreach (var component in _replicaBehaviours) {
#if UNITY_EDITOR
                TransportDebugger.BeginScope(component.ToString());
                var startSize = bs.LengthInBits;
#endif

                component.Serialize(bs, ctx);

#if UNITY_EDITOR
                TransportDebugger.EndScope(bs.LengthInBits - startSize);
#endif
            }
        }

        public void Deserialize(BitStream bs) {
            foreach (var component in _replicaBehaviours) {
                component.Deserialize(bs);
            }
        }

        public void SerializeDestruction(BitStream bs, ReplicaBehaviour.SerializeContext ctx) {
            foreach (var component in _replicaBehaviours) {
                component.SerializeDestruction(bs, ctx);
            }
        }

        public void DeserializeDestruction(BitStream bs) {
            foreach (var component in _replicaBehaviours) {
                component.DeserializeDestruction(bs);
            }
        }

        public void RebuildCaches() {
            _replicaBehaviours = GetComponentsInChildren<ReplicaBehaviour>();

            byte idx = 0;
            foreach (var rb in _replicaBehaviours) {
                rb.Replica = this;
                rb.replicaComponentIdx = idx++;
            }
        }

        void Awake() {
            if (settings == null) {
                if (defaultReplicaSettings == null) {
                    defaultReplicaSettings = ScriptableObject.CreateInstance<ReplicaSettings>();
                }

                settings = defaultReplicaSettings;
            }

            RebuildCaches();
        }

        /// <summary>
        /// Removes the Replica from all global managers. Does NOT broadcast its destruction.
        /// </summary>
        void OnDestroy() {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return; // No need to remove this if not in play mode
#endif
            if (_applicationQuitting)
                return;

            if (isClient) {
                client.replicaManager.RemoveReplica(this);
            }
            if (isServer) {
                server.ReplicaManager.RemoveReplica(this);
            }
        }

        void OnApplicationQuit() {
            _applicationQuitting = true;
        }

        public void QueueServerRpc(BitStream bs, RpcTarget target) {
            var qrpc = new QueuedRpc() {
                bs = bs,
                target = target
            };
            queuedRpcs.Add(qrpc);
        }

        public void CallRpcServer(Connection connection, BitStream bs) {
            var isReplicaOwnedByCaller = Owner == connection;
            if (!isReplicaOwnedByCaller)
                return;

            ReplicaBehaviour.rpcConnection = connection;
            try {
                var componentIdx = bs.ReadByte();
                var methodId = bs.ReadByte();

                var replicaBehaviour = _replicaBehaviours[componentIdx];
                replicaBehaviour.DispatchRpc(methodId, bs);
            } finally {
                ReplicaBehaviour.rpcConnection = Connection.Invalid;
            }
        }

        public void CallRpcClient(BitStream bs) {
            var componentIdx = bs.ReadByte();
            var methodId = bs.ReadByte();

            var replicaBehaviour = _replicaBehaviours[componentIdx];
            replicaBehaviour.DispatchRpc(methodId, bs);
        }
    }
}
