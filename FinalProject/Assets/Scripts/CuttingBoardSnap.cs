using UnityEngine;

public class CuttingBoardSnap : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Tag required on objects that can be snapped to this cutting board.")]
    public string cuttableTag = "Cuttable";

    [Tooltip("World space location and rotation where the item should rest on the board.")]
    public Transform snapPoint;

    [Tooltip("If true, items become kinematic while on the board.")]
    public bool makeKinematicOnBoard = true;

    [Header("Debug")]
    [Tooltip("The cuttable item currently snapped to this board, if any.")]
    public CuttableItem currentItem;

    private void Awake()
    {
        if (snapPoint == null)
        {
            Debug.LogWarning($"[CuttingBoardSnap] No snapPoint assigned on '{name}'. Using board transform as fallback.");
            snapPoint = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(cuttableTag))
        {
            return;
        }

        var cuttable = other.GetComponentInParent<CuttableItem>();
        if (cuttable == null)
        {
            Debug.LogWarning($"[CuttingBoardSnap] Object with tag '{cuttableTag}' entered but has no CuttableItem component in parents.");
            return;
        }

        // If there is already an item snapped that is not this one, ignore the new arrival.
        if (currentItem != null && currentItem != cuttable)
        {
            Debug.Log($"[CuttingBoardSnap] Board '{name}' already has an item '{currentItem.name}'. Ignoring '{cuttable.name}'.");
            return;
        }

        currentItem = cuttable;
        cuttable.isOnCuttingBoard = true;
        cuttable.currentBoard = this;

        // Snap in world space, do not parent to the board so scale is preserved.
        Transform t = cuttable.transform;
        t.position = snapPoint.position;
        t.rotation = snapPoint.rotation;

        // Freeze physics while on the board if requested.
        var rb = t.GetComponent<Rigidbody>();
        if (rb != null && makeKinematicOnBoard)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Debug.Log($"[CuttingBoardSnap] Snapped '{t.name}' onto board '{name}' without parenting. Scale preserved.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentItem == null)
        {
            return;
        }

        var cuttable = other.GetComponentInParent<CuttableItem>();
        if (cuttable == null)
        {
            return;
        }

        if (cuttable != currentItem)
        {
            // Different cuttable leaving, not the one this board owns.
            return;
        }

        // Item is leaving this board.
        cuttable.isOnCuttingBoard = false;
        cuttable.currentBoard = null;

        var rb = cuttable.GetComponent<Rigidbody>();
        if (rb != null && makeKinematicOnBoard)
        {
            rb.isKinematic = false;
        }

        Debug.Log($"[CuttingBoardSnap] '{cuttable.name}' left board '{name}'.");
        currentItem = null;
    }
}
