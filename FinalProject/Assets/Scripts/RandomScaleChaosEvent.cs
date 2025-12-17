using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Chaos/Random Scale Objects")]
public class RandomScaleChaosEvent : ChaosEvent
{
    [Header("Targets")]
    [Tooltip("Optional explicit list of objects that can be resized.")]
    public List<Transform> targets = new List<Transform>();

    [Tooltip("Tag used to find scalable objects when explicit list is empty.")]
    public string targetTag = "ChaosScale";

    [Header("Scale Range")]
    [Tooltip("Minimum random scale multiplier.")]
    public float minScaleMultiplier = 0.3f;

    [Tooltip("Maximum random scale multiplier.")]
    public float maxScaleMultiplier = 2.5f;

    [Tooltip("Maximum number of objects to affect at once. Zero or less means affect all.")]
    public int maxObjectsToAffect = 8;

    private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private readonly List<Transform> activeTransforms = new List<Transform>();

    public override void StartEvent(ChaosManager manager)
    {
        originalScales.Clear();
        activeTransforms.Clear();

        List<Transform> pool = new List<Transform>();

        foreach (Transform t in targets)
        {
            if (t != null && !pool.Contains(t))
            {
                pool.Add(t);
            }
        }

        if (pool.Count == 0 && !string.IsNullOrWhiteSpace(targetTag))
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(targetTag);
            foreach (GameObject go in tagged)
            {
                if (go != null && !pool.Contains(go.transform))
                {
                    pool.Add(go.transform);
                }
            }
        }

        if (pool.Count == 0)
        {
            Debug.Log("[RandomScaleChaosEvent] No targets found.");
            return;
        }

        int countToAffect = pool.Count;
        if (maxObjectsToAffect > 0)
        {
            countToAffect = Mathf.Min(countToAffect, maxObjectsToAffect);
        }

        // Shuffle for randomness
        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < countToAffect; i++)
        {
            Transform t = pool[i];
            if (t == null) continue;

            if (!originalScales.ContainsKey(t))
            {
                originalScales[t] = t.localScale;
            }

            float mult = Random.Range(minScaleMultiplier, maxScaleMultiplier);
            t.localScale = originalScales[t] * mult;
            activeTransforms.Add(t);
        }

        Debug.Log($"[RandomScaleChaosEvent] Rescaled {activeTransforms.Count} objects.");
    }

    public override void EndEvent(ChaosManager manager)
    {
        foreach (Transform t in activeTransforms)
        {
            if (t != null && originalScales.TryGetValue(t, out Vector3 s))
            {
                t.localScale = s;
            }
        }

        activeTransforms.Clear();
        originalScales.Clear();
    }
}
