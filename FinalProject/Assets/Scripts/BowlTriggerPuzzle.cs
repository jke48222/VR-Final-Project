using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class BowlTriggerPuzzle : MonoBehaviour
{
    [Header("Puzzle Hook")]
    [Tooltip("Reference to the bowl recipe combiner that will handle ingredient logic.")]
    public BowlRecipeCombiner puzzle;

    [Header("Snap Settings")]
    [Tooltip("Center point inside the bowl where ingredients should rest.")]
    public Transform snapCenter;

    [Tooltip("Random radius around the snap center (X/Z only).")]
    public float snapRadius = 0.05f;

    [Tooltip("If true, items become kinematic while in the bowl so they stay put.")]
    public bool freezeInBowl = true;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[BowlTriggerPuzzle] Collider is missing even though RequireComponent is present.");
            return;
        }

        col.isTrigger = true;
        Debug.Log("[BowlTriggerPuzzle] Reset called. Collider set to trigger.");
    }

    private void Awake()
    {
        if (puzzle == null)
        {
            puzzle = GetComponentInParent<BowlRecipeCombiner>();
            if (puzzle != null)
            {
                Debug.Log("[BowlTriggerPuzzle] Auto-assigned BowlRecipeCombiner from parent.");
            }
            else
            {
                Debug.LogWarning("[BowlTriggerPuzzle] No BowlRecipeCombiner assigned or found in parents.");
            }
        }

        if (snapCenter == null)
        {
            Debug.LogWarning("[BowlTriggerPuzzle] snapCenter is not assigned. Using bowl transform position for snapping.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (puzzle == null)
        {
            Debug.LogWarning("[BowlTriggerPuzzle] OnTriggerEnter called but puzzle reference is null.");
            return;
        }

        var ingredient = other.GetComponentInParent<IngredientDescriptor>();
        if (ingredient == null)
        {
            // Not an ingredient, nothing to do.
            return;
        }

        // 1) Register with the bowl recipe logic.
        puzzle.OnIngredientEntered(ingredient, ingredient.gameObject);

        // 2) Snap the ingredient into the bowl, preserving scale.
        Transform t = ingredient.transform;

        // Base position for snapping.
        Vector3 basePos = snapCenter != null ? snapCenter.position : transform.position;

        // Small random offset so multiple items do not sit in the exact same spot.
        Vector2 offset2D = Random.insideUnitCircle * snapRadius;
        Vector3 snapPos = new Vector3(
            basePos.x + offset2D.x,
            basePos.y,           // keep Y consistent
            basePos.z + offset2D.y
        );

        t.position = snapPos;

        // Optionally align rotation to bowl interior.
        if (snapCenter != null)
        {
            t.rotation = snapCenter.rotation;
        }

        // Do not parent to bowl in order to avoid scale issues.
        // t.SetParent(null);

        // 3) Freeze physics so it does not bounce out.
        var rb = t.GetComponent<Rigidbody>();
        if (rb != null && freezeInBowl)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Debug.Log($"[BowlTriggerPuzzle] Snapped ingredient '{t.name}' into bowl '{name}'.");
    }

    private void OnTriggerExit(Collider other)
    {
        var ingredient = other.GetComponentInParent<IngredientDescriptor>();
        if (ingredient == null)
        {
            // Non ingredient objects leaving the trigger are ignored.
            return;
        }

        if (puzzle != null)
        {
            puzzle.OnIngredientExited(ingredient, ingredient.gameObject);
            Debug.Log($"[BowlTriggerPuzzle] Ingredient '{ingredient.name}' exited bowl '{name}'. Removed from combiner.");
        }

        // If an ingredient leaves the bowl, restore physics if it was frozen.
        var rb = ingredient.GetComponent<Rigidbody>();
        if (rb != null && freezeInBowl)
        {
            rb.isKinematic = false;
            Debug.Log($"[BowlTriggerPuzzle] Restored physics for ingredient '{ingredient.name}'.");
        }
    }
}
