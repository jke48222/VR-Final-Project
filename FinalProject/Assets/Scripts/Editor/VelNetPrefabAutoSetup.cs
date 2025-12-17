#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VelNet;

/// <summary>
/// Editor utility to automatically add and configure VelNet components on prefabs.
/// Adds NetworkObject, SyncTransform, SyncRigidbody, and Rigidbody if missing.
/// Sets prefabName and isSceneObject on NetworkObject.
/// Populates NetworkObject.syncedComponents and backreferences on each NetworkComponent.
/// Only processes prefabs inside the Assets folder (avoids immutable Packages folder).
/// </summary>
public static class VelNetPrefabAutoSetup
{
    [MenuItem("VelNet/Setup Selected Prefabs")]
    private static void SetupSelectedPrefabs()
    {
        Object[] selection = Selection.objects;

        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("[VelNetPrefabAutoSetup] No assets selected.");
            return;
        }

        int processed = 0;
        foreach (Object obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            // Only process valid prefabs inside the Assets folder
            if (string.IsNullOrEmpty(path) ||
                !path.EndsWith(".prefab") ||
                !path.StartsWith("Assets/"))
            {
                continue;
            }

            if (SetupPrefabAtPath(path))
            {
                processed++;
            }
        }

        Debug.Log($"[VelNetPrefabAutoSetup] Processed {processed} prefab(s) from selection.");
    }

    [MenuItem("VelNet/Setup All Prefabs In Project")]
    private static void SetupAllPrefabsInProject()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        if (prefabGuids == null || prefabGuids.Length == 0)
        {
            Debug.LogWarning("[VelNetPrefabAutoSetup] No prefabs found in project.");
            return;
        }

        int processed = 0;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

            // Skip immutable folders such as Packages
            if (!path.StartsWith("Assets/"))
            {
                continue;
            }

            if (SetupPrefabAtPath(path))
            {
                processed++;
            }

            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar(
                    "VelNet Prefab Setup",
                    $"Processing prefabs... ({i + 1}/{prefabGuids.Length})",
                    (float)(i + 1) / prefabGuids.Length
                );
            }
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"[VelNetPrefabAutoSetup] Processed {processed} prefab(s) in project.");
    }

    private static bool SetupPrefabAtPath(string prefabPath)
    {
        // Safety filter
        if (string.IsNullOrEmpty(prefabPath) ||
            !prefabPath.EndsWith(".prefab") ||
            !prefabPath.StartsWith("Assets/"))
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"[VelNetPrefabAutoSetup] Failed to load prefab at path: {prefabPath}");
            return false;
        }

        bool changed = SetupPrefabRoot(root);

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(root);
        return changed;
    }

    private static bool SetupPrefabRoot(GameObject root)
    {
        bool changed = false;

        NetworkObject netObj = root.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            netObj = root.AddComponent<NetworkObject>();
            changed = true;
        }

        netObj.isSceneObject = false;
        netObj.prefabName = root.name;

        SyncTransform syncTransform = root.GetComponent<SyncTransform>();
        if (syncTransform == null)
        {
            syncTransform = root.AddComponent<SyncTransform>();
            changed = true;
        }

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            changed = true;
        }

        SyncRigidbody syncRigidbody = root.GetComponent<SyncRigidbody>();
        if (syncRigidbody == null)
        {
            syncRigidbody = root.AddComponent<SyncRigidbody>();
            changed = true;
        }

        NetworkComponent[] allNetworkComponents = root.GetComponentsInChildren<NetworkComponent>(true);

        SerializedObject so = new SerializedObject(netObj);
        SerializedProperty listProp = so.FindProperty("syncedComponents");

        listProp.ClearArray();

        if (allNetworkComponents != null && allNetworkComponents.Length > 0)
        {
            for (int i = 0; i < allNetworkComponents.Length; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = allNetworkComponents[i];

                SerializedObject soComponent = new SerializedObject(allNetworkComponents[i]);
                SerializedProperty netObjProp = soComponent.FindProperty("networkObject");
                if (netObjProp != null)
                {
                    netObjProp.objectReferenceValue = netObj;
                    soComponent.ApplyModifiedProperties();
                }
            }

            so.ApplyModifiedProperties();
            changed = true;
        }
        else
        {
            listProp.ClearArray();
            so.ApplyModifiedProperties();
        }

        if (changed)
        {
            Debug.Log($"[VelNetPrefabAutoSetup] Setup VelNet components on prefab root: {root.name}", root);
        }

        return changed;
    }
}
#endif
