using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class SpatialAnchorManager : MonoBehaviour
{
    public enum AnchorMode
    {
        LocalPersistence,   // simple save/load of transforms to disk
        ARFoundation,       // requires AR Foundation packages (stubbed)
        AzureSpatialAnchors // requires Azure Spatial Anchors SDK (stubbed)
    }

    [Header("Mode")]
    public AnchorMode mode = AnchorMode.LocalPersistence;

    [Header("Local Persistence")]
    [Tooltip("Filename used to persist anchors when using LocalPersistence mode.")]
    public string anchorsFileName = "spatial_anchors.json";

    [Header("Visualization")]
    [Tooltip("Optional visual prefab instantiated at anchor load time (purely cosmetic).")]
    public GameObject anchorVisualPrefab;

    [Serializable]
    public class AnchorData
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string note; // optional user note
    }

    [Serializable]
    class AnchorList { public List<AnchorData> anchors = new List<AnchorData>(); }

    // In-memory cached anchors (populated by LoadAnchors)
    public List<AnchorData> anchors = new List<AnchorData>();

    string GetFilePath() => Path.Combine(Application.persistentDataPath, anchorsFileName);

    /// <summary>
    /// Create an anchor at the provided transform and persist it (local mode).
    /// Returns the anchor id (GUID).
    /// </summary>
    public string CreateAnchor(Transform atTransform, string optionalId = null, string note = null)
    {
        string id = string.IsNullOrEmpty(optionalId) ? Guid.NewGuid().ToString() : optionalId;

        if (mode == AnchorMode.LocalPersistence)
        {
            var d = new AnchorData
            {
                id = id,
                position = atTransform.position,
                rotation = atTransform.rotation,
                scale = atTransform.localScale,
                note = note
            };

            anchors.Add(d);
            SaveAnchorsToDisk();

            if (anchorVisualPrefab != null)
            {
                Instantiate(anchorVisualPrefab, d.position, d.rotation);
            }

            return id;
        }

        Debug.LogWarning("CreateAnchor: Non-local anchor creation must be implemented for ARFoundation/Azure modes.");
        return id;
    }

    /// <summary>
    /// Remove anchor by id (local mode).
    /// </summary>
    public bool RemoveAnchor(string id)
    {
        var idx = anchors.FindIndex(a => a.id == id);
        if (idx < 0) return false;
        anchors.RemoveAt(idx);
        SaveAnchorsToDisk();
        return true;
    }

    /// <summary>
    /// Saves anchors to disk (local persistence).
    /// </summary>
    public void SaveAnchorsToDisk()
    {
        try
        {
            var wrapper = new AnchorList { anchors = this.anchors };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(GetFilePath(), json);
            Debug.Log($"[SpatialAnchorManager] Saved {anchors.Count} anchors to {GetFilePath()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpatialAnchorManager] Failed to save anchors: {ex}");
        }
    }

    /// <summary>
    /// Loads anchors from disk (local persistence) and optionally instantiates visuals.
    /// </summary>
    public void LoadAnchorsFromDisk(bool instantiateVisuals = true)
    {
        anchors.Clear();
        try
        {
            string path = GetFilePath();
            if (!File.Exists(path))
            {
                Debug.Log($"[SpatialAnchorManager] No anchors file found at {path}");
                return;
            }

            string json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<AnchorList>(json);
            if (wrapper != null && wrapper.anchors != null)
            {
                anchors = wrapper.anchors;
                Debug.Log($"[SpatialAnchorManager] Loaded {anchors.Count} anchors from disk.");

                if (instantiateVisuals && anchorVisualPrefab != null)
                {
                    foreach (var a in anchors)
                    {
                        Instantiate(anchorVisualPrefab, a.position, a.rotation);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpatialAnchorManager] Failed to load anchors: {ex}");
        }
    }

    /// <summary>
    /// Attempt to apply a stored anchor transform to a target GameObject (local mode).
    /// Returns true if applied.
    /// </summary>
    public bool TryResolveAnchorToObject(string id, GameObject target)
    {
        if (target == null) return false;
        var a = anchors.Find(x => x.id == id);
        if (a == null) return false;
        target.transform.position = a.position;
        target.transform.rotation = a.rotation;
        target.transform.localScale = a.scale;
        return true;
    }

    // --- Stubs / integration notes for ARFoundation / Azure Spatial Anchors ---
    // These methods are intentionally left as stubs to keep this script package-free.
    // When you add AR Foundation or Azure Spatial Anchors to your project you can:
    // - Implement CreateAnchor / RemoveAnchor to call the platform API and persist cloud ids.
    // - Implement LoadAnchorsFromDisk to pull saved cloud anchor ids and resolve them on startup.
    // - Use the 'mode' enum to toggle between local development (LocalPersistence) and platform anchors.

    private void Awake()
    {
        // For demonstration we load anchors on Awake when in LocalPersistence mode.
        if (mode == AnchorMode.LocalPersistence)
        {
            LoadAnchorsFromDisk(instantiateVisuals: true);
        }
    }
}
