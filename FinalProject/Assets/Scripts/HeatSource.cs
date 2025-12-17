using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HeatSource : MonoBehaviour
{
    [Header("Heat Settings")]
    [Tooltip("Heat per second applied to items inside this trigger.")]
    public float heatPerSecond = 0.4f;

    [Tooltip("If false, this heat source does not apply heat.")]
    public bool isOn = true;

    [Header("Debug")]
    [Tooltip("Color used to visualize the heat volume in the scene view.")]
    public Color gizmoColor = new Color(1f, 0.5f, 0.1f, 0.25f);

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError("[HeatSource] Collider is missing even though RequireComponent is present.");
        }
        else
        {
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"[HeatSource] Collider on '{name}' was not a trigger. Setting isTrigger = true.");
                _collider.isTrigger = true;
            }
        }

        if (heatPerSecond < 0f)
        {
            Debug.LogWarning($"[HeatSource] heatPerSecond on '{name}' was negative. Clamping to 0.");
            heatPerSecond = 0f;
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
            Debug.Log("[HeatSource] Reset: collider set to trigger.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isOn || heatPerSecond <= 0f)
        {
            return;
        }

        var cookable = other.GetComponentInParent<CookableItem>();
        if (cookable == null)
        {
            return;
        }

        float heatThisFrame = heatPerSecond * Time.deltaTime;
        cookable.ApplyHeat(heatThisFrame);
        // If needed for debugging, uncomment:
        // Debug.Log($"[HeatSource] Applying {heatThisFrame:F3} heat to '{cookable.name}' this frame.");
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;

        // Use localToWorldMatrix so collider center and size are shown correctly.
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            // Simple capsule visualization: approximate with a box.
            Vector3 size = Vector3.one * capsule.radius * 2f;
            size[capsule.direction] = capsule.height;
            Gizmos.DrawCube(capsule.center, size);
        }
    }
}
