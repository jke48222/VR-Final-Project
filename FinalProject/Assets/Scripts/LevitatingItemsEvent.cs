using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Chaos/Levitate Items")]
public class LevitateItemsEvent : ChaosEvent
{
    private List<Rigidbody> affected = new List<Rigidbody>();

    public override void StartEvent(ChaosManager manager)
    {
        affected.Clear();

        // Find objects tagged as Cuttable
        GameObject[] cuttables = GameObject.FindGameObjectsWithTag("Cuttable");
        Debug.Log($"[LevitateItemsEvent] Found {cuttables.Length} Cuttable objects");

        foreach (var obj in cuttables)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            // Disable gravity
            rb.useGravity = false;

            // Add a little lift to start the levitation visually
            rb.AddForce(Vector3.up * 3f, ForceMode.VelocityChange);

            affected.Add(rb);
        }

        Debug.Log($"[LevitateItemsEvent] Levitation applied to {affected.Count} objects");
    }

    public override void EndEvent(ChaosManager manager)
    {
        foreach (var rb in affected)
        {
            if (rb != null)
                rb.useGravity = true;
        }

        Debug.Log("[LevitateItemsEvent] Levitation ended — gravity restored");
    }
}
