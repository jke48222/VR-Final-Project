using UnityEngine;

[DisallowMultipleComponent]
public class KnifeBlockController : MonoBehaviour
{
    [Header("Knife References")]
    [Tooltip("Knife object that lives in the block.")]
    public GameObject knife;

    [Tooltip("Slot position inside the block where the knife rests.")]
    public Transform knifeInBlockPoint;

    [Tooltip("Position in front of the block where the knife appears for grabbing.")]
    public Transform knifeOutPoint;

    [Header("Settings")]
    [Tooltip("Maximum distance from the slot to allow snapping the knife back into the block.")]
    public float returnDistance = 0.4f;

    private bool isKnifeInBlock = true;

    private void Awake()
    {
        if (knife == null)
        {
            Debug.LogWarning($"[KnifeBlockController] Knife reference is not assigned on '{name}'.");
        }

        if (knifeInBlockPoint == null)
        {
            Debug.LogWarning($"[KnifeBlockController] knifeInBlockPoint is not assigned on '{name}'.");
        }

        if (knifeOutPoint == null)
        {
            Debug.LogWarning($"[KnifeBlockController] knifeOutPoint is not assigned on '{name}'.");
        }
    }

    private void Start()
    {
        // Initialize knife in the block if everything is wired.
        if (knife != null && knifeInBlockPoint != null)
        {
            MoveKnifeTo(knifeInBlockPoint, inBlock: true);
            isKnifeInBlock = true;
            Debug.Log($"[KnifeBlockController] Initialized knife '{knife.name}' in block position.");
        }
    }

    /// <summary>
    /// Toggles the knife between "in the block" and "out for grabbing".
    /// When placing back, only snaps if the knife is close enough to the block slot.
    /// Intended to be called from a clickable / UI event (e.g., KnifeBlockClickable).
    /// </summary>
    public void ToggleKnife()
    {
        if (knife == null || knifeInBlockPoint == null || knifeOutPoint == null)
        {
            Debug.LogWarning("[KnifeBlockController] ToggleKnife called but one or more references are missing.");
            return;
        }

        if (isKnifeInBlock)
        {
            // Move knife out so it can be grabbed by the player.
            MoveKnifeTo(knifeOutPoint, inBlock: false);
            isKnifeInBlock = false;
            Debug.Log($"[KnifeBlockController] Moved knife '{knife.name}' out of block for grabbing.");
        }
        else
        {
            // Only snap back if knife is near the slot position.
            float dist = Vector3.Distance(knife.transform.position, knifeInBlockPoint.position);
            if (dist > returnDistance)
            {
                Debug.Log($"[KnifeBlockController] Knife '{knife.name}' is too far ({dist:F2} m) to snap back (threshold {returnDistance:F2} m).");
                return;
            }

            MoveKnifeTo(knifeInBlockPoint, inBlock: true);
            isKnifeInBlock = true;
            Debug.Log($"[KnifeBlockController] Snapped knife '{knife.name}' back into the block.");
        }
    }

    /// <summary>
    /// Moves the knife to the specified target transform and configures physics
    /// for either in-block (kinematic) or out-of-block (dynamic) behavior.
    /// </summary>
    private void MoveKnifeTo(Transform target, bool inBlock)
    {
        if (knife == null || target == null)
        {
            Debug.LogWarning("[KnifeBlockController] MoveKnifeTo called with null knife or target.");
            return;
        }

        knife.SetActive(true);
        knife.transform.SetParent(target);
        knife.transform.position = target.position;
        knife.transform.rotation = target.rotation;

        var rb = knife.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Reset velocities to avoid unexpected motion when snapping.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // In block: knife is fixed. Out of block: allow physics and grabbing.
            rb.isKinematic = inBlock;
            rb.useGravity = !inBlock;
        }
        else
        {
            Debug.LogWarning($"[KnifeBlockController] Knife '{knife.name}' has no Rigidbody. Physics behavior will be limited.");
        }
    }
}
