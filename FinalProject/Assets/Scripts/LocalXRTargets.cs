using UnityEngine;

/// <summary>
/// Simple global reference holder for XR rig targets (head, left hand, right hand).
/// This class assigns static Transform references at runtime so they can be accessed anywhere.
/// </summary>
[DisallowMultipleComponent]
public class LocalXRTargets : MonoBehaviour
{
    public static Transform Head { get; private set; }
    public static Transform LeftHand { get; private set; }
    public static Transform RightHand { get; private set; }

    [Header("Assign from XR rig")]
    [Tooltip("Camera / HMD transform.")]
    public Transform headTransform;

    [Tooltip("Left hand controller transform.")]
    public Transform leftHandTransform;

    [Tooltip("Right hand controller transform.")]
    public Transform rightHandTransform;

    /// <summary>
    /// Returns true when all XR target references are assigned and ready.
    /// </summary>
    public static bool IsReady =>
        Head != null && LeftHand != null && RightHand != null;

    private void Awake()
    {
        AssignTargets();
    }

    /// <summary>
    /// Assigns static XR targets and warns about missing references.
    /// </summary>
    private void AssignTargets()
    {
        Head = headTransform;
        LeftHand = leftHandTransform;
        RightHand = rightHandTransform;

        if (Head == null)
        {
            Debug.LogWarning("[LocalXRTargets] Head transform is not assigned. Some features may fail.");
        }
        if (LeftHand == null)
        {
            Debug.LogWarning("[LocalXRTargets] Left hand transform is not assigned.");
        }
        if (RightHand == null)
        {
            Debug.LogWarning("[LocalXRTargets] Right hand transform is not assigned.");
        }

        Debug.Log($"[LocalXRTargets] Assigned XR targets. Ready={IsReady}");
    }

    private void OnDisable()
    {
        // Clear static references only if this instance was the one that set them.
        if (Head == headTransform) Head = null;
        if (LeftHand == leftHandTransform) LeftHand = null;
        if (RightHand == rightHandTransform) RightHand = null;

        Debug.Log("[LocalXRTargets] Cleared static XR target references.");
    }
}
