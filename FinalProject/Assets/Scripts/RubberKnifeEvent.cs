using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Chaos/Rubber Knife")]
public class RubberKnifeEvent : ChaosEvent
{
    [Header("Targeting")]
    [Tooltip("Tag to find all knife objects in the scene.")]
    public string knifeTag = "Knife";

    [Header("Rubber Settings")]
    public float scaleMultiplier = 1.5f;
    public float floppyAngularDrag = 0.05f;
    public float normalAngularDrag = 0.5f;

    private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Rigidbody, float> originalAngularDrag = new Dictionary<Rigidbody, float>();

    public override void StartEvent(ChaosManager manager)
    {
        originalScales.Clear();
        originalAngularDrag.Clear();

        GameObject[] knives = GameObject.FindGameObjectsWithTag(knifeTag);
        if (knives.Length == 0)
        {
            Debug.Log("[RubberKnifeEvent] No knives found.");
            return;
        }

        Debug.Log($"[RubberKnifeEvent] Found {knives.Length} knives.");

        foreach (GameObject knife in knives)
        {
            if (knife == null) continue;

            Transform t = knife.transform;
            if (!originalScales.ContainsKey(t))
            {
                originalScales[t] = t.localScale;
            }

            t.localScale = originalScales[t] * scaleMultiplier;

            Rigidbody rb = knife.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!originalAngularDrag.ContainsKey(rb))
                {
                    originalAngularDrag[rb] = rb.angularDamping;
                }

                rb.angularDamping = floppyAngularDrag;
            }
        }
    }

    public override void EndEvent(ChaosManager manager)
    {
        foreach (KeyValuePair<Transform, Vector3> kvp in originalScales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localScale = kvp.Value;
            }
        }

        foreach (KeyValuePair<Rigidbody, float> kvp in originalAngularDrag)
        {
            if (kvp.Key != null)
            {
                kvp.Key.angularDamping = kvp.Value;
            }
        }

        originalScales.Clear();
        originalAngularDrag.Clear();
    }
}
