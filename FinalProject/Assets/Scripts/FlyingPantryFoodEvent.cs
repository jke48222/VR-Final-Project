using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Chaos/Flying Pantry Food")]
public class FlyingPantryFoodEvent : ChaosEvent
{
    [Header("Targeting")]
    [Tooltip("Optional explicit list of pantry rigidbodies. If empty, falls back to tag search.")]
    public List<Rigidbody> pantryItems = new List<Rigidbody>();

    [Tooltip("Tag used to find pantry items when the list is empty.")]
    public string pantryTag = "PantryItem";

    [Header("Forces")]
    public float launchForce = 4f;
    public float torqueForce = 2f;

    private readonly List<Rigidbody> _active = new List<Rigidbody>();
    private readonly Dictionary<Rigidbody, bool> _originalUseGravity = new Dictionary<Rigidbody, bool>();
    private readonly Dictionary<Rigidbody, RigidbodyConstraints> _originalConstraints = new Dictionary<Rigidbody, RigidbodyConstraints>();

    public override void StartEvent(ChaosManager manager)
    {
        _active.Clear();
        _originalUseGravity.Clear();
        _originalConstraints.Clear();

        List<Rigidbody> targets = new List<Rigidbody>();

        foreach (Rigidbody rb in pantryItems)
        {
            if (rb != null && !targets.Contains(rb))
            {
                targets.Add(rb);
            }
        }

        if (targets.Count == 0 && !string.IsNullOrWhiteSpace(pantryTag))
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(pantryTag);
            foreach (GameObject go in tagged)
            {
                if (go == null) continue;
                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null && !targets.Contains(rb))
                {
                    targets.Add(rb);
                }
            }
        }

        if (targets.Count == 0)
        {
            Debug.Log("[FlyingPantryFoodEvent] No pantry items found.");
            return;
        }

        Debug.Log($"[FlyingPantryFoodEvent] Affecting {targets.Count} pantry items.");

        foreach (Rigidbody rb in targets)
        {
            if (rb == null) continue;

            _active.Add(rb);
            _originalUseGravity[rb] = rb.useGravity;
            _originalConstraints[rb] = rb.constraints;

            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            Vector3 dir = Random.onUnitSphere;
            if (dir.y < 0f) dir.y = -dir.y; // bias upward a bit

            rb.AddForce(dir * launchForce, ForceMode.VelocityChange);
            rb.AddTorque(Random.onUnitSphere * torqueForce, ForceMode.VelocityChange);
        }
    }

    public override void EndEvent(ChaosManager manager)
    {
        foreach (Rigidbody rb in _active)
        {
            if (rb == null) continue;

            if (_originalUseGravity.TryGetValue(rb, out bool g))
            {
                rb.useGravity = g;
            }

            if (_originalConstraints.TryGetValue(rb, out RigidbodyConstraints c))
            {
                rb.constraints = c;
            }
        }

        _active.Clear();
        _originalUseGravity.Clear();
        _originalConstraints.Clear();
    }
}
