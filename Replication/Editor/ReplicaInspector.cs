﻿#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;

namespace Cube.Replication.Editor {
    [CustomEditor(typeof(Replica))]
    [CanEditMultipleObjects]
    public class ReplicaInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            if (targets.Length != 1)
                return;

            var replica = target as Replica;

            if (PrefabUtility.GetPrefabAssetType(replica) == PrefabAssetType.NotAPrefab) {
                EditorGUILayout.LabelField("Prefab Id", replica.prefabIdx.ToString());
            }

            if (EditorApplication.isPlaying) {
                EditorGUILayout.LabelField("Replica Id", replica.ReplicaId.data.ToString());
            }

            var idxStr = replica.sceneIdx != 0 ? replica.sceneIdx.ToString() : "-";
            EditorGUILayout.LabelField("Scene Idx", idxStr);

            if (EditorApplication.isPlaying) {
                if (GUILayout.Button("Find " + (replica.isClient ? "Server" : "Client") +  " Replica")) {
                    ApplyToCorrespondingReplica(replica, cr => EditorGUIUtility.PingObject(cr.transform.gameObject));
                }
                if (GUILayout.Button("Select " + (replica.isClient ? "Server" : "Client") + " Replica")) {
                    ApplyToCorrespondingReplica(replica, cr => Selection.activeGameObject = cr.transform.gameObject);
                }
            }
        }

        void ApplyToCorrespondingReplica(Replica replica, Action<Replica> func) {
            Action<GameObject> impl = (go) => {
                var correspondingReplicas = go.GetComponentsInChildren<Replica>();
                foreach (var correspondingReplica in correspondingReplicas) {
                    if (correspondingReplica.ReplicaId == replica.ReplicaId) {
                        func(correspondingReplica);
                        break;
                    }
                }
            };

            if (replica.isClient) {
                foreach (var replicaManager in ServerReplicaManager.all) {
                    var otherReplica = replicaManager.GetReplicaById(replica.ReplicaId);
                    if (otherReplica == null)
                        continue;

                    impl(otherReplica.gameObject);
                    break;
                }
            }

            if (replica.isServer) {
                foreach (var replicaManager in ClientReplicaManager.all) {
                    var otherReplica = replicaManager.GetReplicaById(replica.ReplicaId);
                    if (otherReplica == null)
                        continue;

                    impl(otherReplica.gameObject);
                    break;
                }
            }
        }
    }
}
#endif
