﻿using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Cube.Networking.Replicas {
    public class NetworkObjectLookupGenerator {
        [DidReloadScripts]
        [MenuItem("Cube/Generated/Generate NetworkObjectLookup")]
        public static void ForceRefreshCode() {
            var networkObjects = new List<NetworkObject>();

            var assetGuids = AssetDatabase.FindAssets("t:NetworkObject");
            foreach (var assetGuid in assetGuids) {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                var asset = AssetDatabase.LoadAssetAtPath<NetworkObject>(assetPath);
                if (asset == null) {
                    Debug.LogError("LoadAssetAtPath failed (path=" + assetPath + ")");
                    continue;
                }

                networkObjects.Add(asset);
            }

            var lookup = ScriptableObject.CreateInstance<NetworkObjectLookup>();
            lookup.entries = networkObjects.ToArray();

            //
            var newLookupPath = "Assets/Generated/Cube/Networking/Resources/NetworkObjectLookup.asset";

            var oldLookup = AssetDatabase.LoadAssetAtPath<NetworkObjectLookup>(newLookupPath);
            if (lookup == oldLookup)
                return;

            //
            Directory.CreateDirectory(Application.dataPath + "/Generated/Cube/Networking/Resources");
            AssetDatabase.CreateAsset(lookup, newLookupPath);
        }
    }
}