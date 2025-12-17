using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Grab;

public class CuttableItem : MonoBehaviour
{
    [Header("Cut Setup")]
    [Tooltip("Prefab for the first half after cutting.")]
    public GameObject halfPrefab;

    [Tooltip("Optional prefab for the second half. If null, halfPrefab is reused.")]
    public GameObject secondHalfPrefab;

    [Tooltip("If true, the second half will be rotated 180 degrees around Y.")]
    public bool rotateSecondHalf = true;

    [Tooltip("World space distance between the two halves along the object's local right axis.")]
    public float halfOffset = 0.05f;

    [Header("Board State")]
    [Tooltip("True when this item is currently resting on a cutting board snap point.")]
    public bool isOnCuttingBoard;

    [Tooltip("Reference to the cutting board this item is snapped to, if any.")]
    public CuttingBoardSnap currentBoard;

    [Header("Debug")]
    [Tooltip("True after the item has been cut once.")]
    public bool hasBeenCut;

    // Original scale so halves match pantry scaling.
    private Vector3 originalLocalScale;

    private void Awake()
    {
        originalLocalScale = transform.localScale;
        Debug.Log($"[CuttableItem] Awake on '{name}'. Initial scale: {originalLocalScale}");
    }

    /// <summary>
    /// Attempts to cut this item. Will only succeed if it is on a cutting board and not already cut.
    /// </summary>
    public void TryCut()
    {
        if (hasBeenCut)
        {
            Debug.Log($"[CuttableItem] '{name}' has already been cut. Ignoring TryCut.");
            return;
        }

        if (!isOnCuttingBoard)
        {
            Debug.Log($"[CuttableItem] '{name}' is not on a cutting board. Cut ignored.");
            return;
        }

        if (halfPrefab == null)
        {
            Debug.LogError($"[CuttableItem] '{name}' has no halfPrefab assigned. Cannot perform cut.");
            return;
        }

        hasBeenCut = true;
        Debug.Log($"[CuttableItem] Performing cut on '{name}'.");
        PerformCut();
    }

    /// <summary>
    /// Spawns two halves with appropriate transforms and interaction setup, then destroys the original.
    /// </summary>
    private void PerformCut()
    {
        Transform t = transform;
        Vector3 right = t.right;

        // Compute positions for halves, separated along the local right axis.
        Vector3 posA = t.position - right * halfOffset * 0.5f;
        Vector3 posB = t.position + right * halfOffset * 0.5f;

        Quaternion rotA = t.rotation;
        Quaternion rotB = rotateSecondHalf
            ? Quaternion.Euler(rotA.eulerAngles + new Vector3(0f, 180f, 0f))
            : rotA;

        GameObject prefabA = halfPrefab;
        GameObject prefabB = secondHalfPrefab != null ? secondHalfPrefab : halfPrefab;

        GameObject halfA = Instantiate(prefabA, posA, rotA);
        GameObject halfB = Instantiate(prefabB, posB, rotB);

        // Match pantry scaling.
        halfA.transform.localScale = originalLocalScale;
        halfB.transform.localScale = originalLocalScale;

        // Configure physics and grab interaction for each half.
        SetupHalfForInteraction(halfA);
        SetupHalfForInteraction(halfB);

        Debug.Log($"[CuttableItem] '{name}' cut into '{halfA.name}' and '{halfB.name}'.");

        Destroy(gameObject);
    }

    /// <summary>
    /// Configures collider, rigidbody and Meta Interaction SDK grab components for a spawned half.
    /// </summary>
    private void SetupHalfForInteraction(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("[CuttableItem] SetupHalfForInteraction called with null GameObject.");
            return;
        }

        // Match layer and tag so interactors behave consistently with the original item.
        obj.layer = gameObject.layer;
        obj.tag = gameObject.tag;

        // 1. Ensure a non trigger collider exists.
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.isTrigger = false;
            col = box;
            Debug.Log($"[CuttableItem] Added BoxCollider to '{obj.name}'.");
        }
        else
        {
            // Make sure it can participate in physics.
            if (col.isTrigger)
            {
                col.isTrigger = false;
            }

            if (col is MeshCollider meshCol)
            {
                // In case raw meshes cause issues. Adjust if you need convex.
                meshCol.convex = true;
                Debug.Log($"[CuttableItem] Using convex MeshCollider on '{obj.name}'.");
            }
        }

        // 2. Ensure a Rigidbody exists.
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            Debug.Log($"[CuttableItem] Added Rigidbody to '{obj.name}'.");
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 3. Ensure a Grabbable (Meta Interaction SDK) exists.
        Grabbable grabbable = obj.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = obj.AddComponent<Grabbable>();
            Debug.Log($"[CuttableItem] Added Grabbable to '{obj.name}'.");
        }

        // 4. Ensure a GrabFreeTransformer exists for free movement.
        GrabFreeTransformer freeTransformer = obj.GetComponent<GrabFreeTransformer>();
        if (freeTransformer == null)
        {
            freeTransformer = obj.AddComponent<GrabFreeTransformer>();
            Debug.Log($"[CuttableItem] Added GrabFreeTransformer to '{obj.name}'.");
        }

        // 5. Ensure a GrabInteractable exists for hand grab interactions.
        GrabInteractable grabInteractable = obj.GetComponent<GrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = obj.AddComponent<GrabInteractable>();
            Debug.Log($"[CuttableItem] Added GrabInteractable to '{obj.name}'.");
        }

        // 6. Explicit wiring of Interaction SDK components.

        // Grabbable uses this Rigidbody and the free transformer for one and two hand grabs.
        grabbable.InjectOptionalRigidbody(rb);
        grabbable.InjectOptionalOneGrabTransformer(freeTransformer);
        grabbable.InjectOptionalTwoGrabTransformer(freeTransformer);

        // GrabInteractable uses the Grabbable as its PointableElement.
        grabInteractable.InjectOptionalPointableElement(grabbable);

        Debug.Log($"[CuttableItem] Configured '{obj.name}' as Meta Interaction SDK grabbable half.");
    }
}
