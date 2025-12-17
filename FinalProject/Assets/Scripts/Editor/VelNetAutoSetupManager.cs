#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelNet;

public static class VelNetAutoSetupEditor
{
	// ------------- TOP LEVEL MENU ITEMS -------------

	[MenuItem("VelNet/Setup/Scene/Setup Selected Scene Objects")]
	private static void SetupSelectedSceneObjects()
	{
		var selection = Selection.gameObjects;
		if (selection == null || selection.Length == 0)
		{
			Debug.LogWarning("[VelNetAutoSetup] No scene GameObjects selected.");
			return;
		}

		HashSet<int> usedIds;
		int nextId;
		CollectSceneIds(out usedIds, out nextId);

		foreach (var go in selection)
		{
			if (PrefabUtility.IsPartOfPrefabAsset(go)) continue; // skip prefab assets here
			ConfigureHierarchy(go, isSceneObject: true, usedIds: usedIds, nextSceneIdRef: ref nextId);
			EditorSceneManager.MarkSceneDirty(go.scene);
		}

		Debug.Log("[VelNetAutoSetup] Finished setting up selected scene objects.");
	}

	[MenuItem("VelNet/Setup/Scene/Setup ALL Scene Objects In Active Scene")]
	private static void SetupAllSceneObjectsInActiveScene()
	{
		Scene scene = SceneManager.GetActiveScene();
		if (!scene.IsValid() || !scene.isLoaded)
		{
			Debug.LogError("[VelNetAutoSetup] No valid active scene loaded.");
			return;
		}

		HashSet<int> usedIds;
		int nextId;
		CollectSceneIds(out usedIds, out nextId);

		foreach (var root in scene.GetRootGameObjects())
		{
			ConfigureHierarchy(root, isSceneObject: true, usedIds: usedIds, nextSceneIdRef: ref nextId);
		}

		EditorSceneManager.MarkSceneDirty(scene);
		Debug.Log("[VelNetAutoSetup] Finished setting up all scene objects in active scene.");
	}

	[MenuItem("VelNet/Setup/Prefabs/Setup Selected Prefab Assets")]
	private static void SetupSelectedPrefabs()
	{
		Object[] selection = Selection.objects;
		if (selection == null || selection.Length == 0)
		{
			Debug.LogWarning("[VelNetAutoSetup] No prefab assets selected.");
			return;
		}

		int processed = 0;
		foreach (Object obj in selection)
		{
			string path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path)) continue;
			if (!path.StartsWith("Assets/")) continue;        // skip Packages/ etc
			if (!IsPrefabPath(path)) continue;

			if (SetupPrefabAtPath(path)) processed++;
		}

		Debug.Log($"[VelNetAutoSetup] Finished setting up {processed} prefab(s).");
	}

	[MenuItem("VelNet/Setup/Prefabs/Setup ALL Prefab Assets In Project (under Assets/)")]
	private static void SetupAllPrefabsInProject()
	{
		string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
		int processed = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!IsPrefabPath(path)) continue;
			if (SetupPrefabAtPath(path)) processed++;
		}

		Debug.Log($"[VelNetAutoSetup] Finished setting up {processed} prefab(s) in project.");
	}

	[MenuItem("VelNet/Setup/VelNetManager/Refresh Prefab List From Project")]
	private static void RefreshVelNetManagerPrefabList()
	{
		VelNetManager manager = Object.FindFirstObjectByType<VelNetManager>();
		if (manager == null)
		{
			Debug.LogError("[VelNetAutoSetup] No VelNetManager found in open scenes. Open your bootstrap scene first.");
			return;
		}

		string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
		List<NetworkObject> allNetPrefabs = new List<NetworkObject>();

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!IsPrefabPath(path)) continue;

			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null) continue;

			NetworkObject net = prefab.GetComponent<NetworkObject>();
			if (net != null)
			{
				allNetPrefabs.Add(net);
			}
		}

		Undo.RecordObject(manager, "Refresh VelNetManager prefab list");
		manager.prefabs = allNetPrefabs;
		EditorUtility.SetDirty(manager);
		Debug.Log($"[VelNetAutoSetup] VelNetManager.prefabs updated with {allNetPrefabs.Count} NetworkObject prefab(s).");
	}

	// ------------- CORE HELPERS -------------

	private static void CollectSceneIds(out HashSet<int> usedIds, out int nextId)
	{
		usedIds = new HashSet<int>();
		int maxId = 0;

		NetworkObject[] existing = Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
		foreach (var n in existing)
		{
			if (n.isSceneObject && n.sceneNetworkId > 0)
			{
				usedIds.Add(n.sceneNetworkId);
				if (n.sceneNetworkId > maxId) maxId = n.sceneNetworkId;
			}
		}

		nextId = maxId + 1;
		if (nextId <= 0) nextId = 1;
	}

	private static bool IsPrefabPath(string path)
	{
		return path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase);
	}

	private static bool SetupPrefabAtPath(string path)
	{
		// Avoid immutable folders (Packages, etc.)
		if (!path.StartsWith("Assets/")) return false;

		GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
		if (prefabRoot == null)
		{
			Debug.LogWarning($"[VelNetAutoSetup] Could not load prefab at path: {path}");
			return false;
		}

		// For prefabs we do NOT treat them as scene objects
		HashSet<int> dummy = new HashSet<int>();
		int dummyNextId = 1;
		ConfigureHierarchy(prefabRoot, isSceneObject: false, usedIds: dummy, nextSceneIdRef: ref dummyNextId, fromPrefab: true);

		PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
		PrefabUtility.UnloadPrefabContents(prefabRoot);
		return true;
	}

	/// <summary>
	/// Configure a hierarchy (root + children) either as scene objects or prefab assets.
	/// </summary>
	private static void ConfigureHierarchy(GameObject root, bool isSceneObject, HashSet<int> usedIds, ref int nextSceneIdRef, bool fromPrefab = false)
	{
		// We only assign scene IDs to objects that are actually in a scene, not prefab assets.
		bool treatAsScene = isSceneObject && !EditorUtility.IsPersistent(root);

		var transforms = root.GetComponentsInChildren<Transform>(true);
		foreach (var t in transforms)
		{
			GameObject go = t.gameObject;
			ConfigureSingleGameObject(go, treatAsScene, usedIds, ref nextSceneIdRef, fromPrefab);
		}
	}

	private static void ConfigureSingleGameObject(GameObject go, bool isSceneObject, HashSet<int> usedIds, ref int nextSceneIdRef, bool fromPrefab)
	{
		// Skip stuff inside Packages, just in case
		if (EditorUtility.IsPersistent(go))
		{
			string assetPath = AssetDatabase.GetAssetPath(go);
			if (!string.IsNullOrEmpty(assetPath) && !assetPath.StartsWith("Assets/"))
			{
				return; // immutable folder
			}
		}

		NetworkObject net = go.GetComponent<NetworkObject>();
		if (net == null)
		{
			net = Undo.AddComponent<NetworkObject>(go);
		}

		// Scene vs prefab config
		if (isSceneObject)
		{
			net.isSceneObject = true;

			// Assign unique sceneNetworkId
			if (net.sceneNetworkId <= 0 || usedIds.Contains(net.sceneNetworkId))
			{
				while (usedIds.Contains(nextSceneIdRef) || nextSceneIdRef <= 0)
				{
					nextSceneIdRef++;
				}

				net.sceneNetworkId = nextSceneIdRef;
				usedIds.Add(net.sceneNetworkId);
				nextSceneIdRef++;
			}
		}
		else
		{
			// Prefab config
			net.isSceneObject = false;
			net.sceneNetworkId = 0;

			if (fromPrefab)
			{
				// Use root name as prefabName; this is what VelNetManager.NetworkInstantiate expects
				if (go.transform.parent == null) // only set on root
				{
					net.prefabName = go.name;
				}
			}
		}

		// Ensure SyncTransform exists
		SyncTransform syncTransform = go.GetComponent<SyncTransform>();
		if (syncTransform == null)
		{
			syncTransform = Undo.AddComponent<SyncTransform>(go);
		}

		// Optionally add SyncRigidbody if a Rigidbody is present (or create a safe default)
		Rigidbody rb = go.GetComponent<Rigidbody>();
		if (rb == null && !EditorUtility.IsPersistent(go))
		{
			// For scene objects we can safely add a kinematic rigidbody if none exists
			// Comment this out if you prefer to add rigidbodies manually.
			rb = Undo.AddComponent<Rigidbody>(go);
			rb.useGravity = false;
			rb.isKinematic = true;
		}

		if (rb != null)
		{
			SyncRigidbody syncRb = go.GetComponent<SyncRigidbody>();
			if (syncRb == null)
			{
				syncRb = Undo.AddComponent<SyncRigidbody>(go);
			}
		}

		// Wire NetworkObject.syncedComponents and NetworkComponent.networkObject
		FixSyncedComponents(net, go);
		EditorUtility.SetDirty(go);
	}

	private static void FixSyncedComponents(NetworkObject net, GameObject go)
	{
		// Find all NetworkComponents on this GameObject
		NetworkComponent[] comps = go.GetComponents<NetworkComponent>();

		if (net.syncedComponents == null)
		{
			net.syncedComponents = new List<NetworkComponent>();
		}
		else
		{
			net.syncedComponents.Clear();
		}

		foreach (var c in comps)
		{
			if (c == null) continue;
			c.networkObject = net;
			net.syncedComponents.Add(c);
		}
	}
}
#endif
