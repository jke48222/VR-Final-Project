using System;
using UnityEngine;

/// <summary>
/// Drives a humanoid character mesh from VR targets.
/// For the local player, targets are the XR rig (LocalXRTargets).
/// For remote players, targets are the "remote" transforms that NetworkAvatarState drives.
/// </summary>
[DisallowMultipleComponent]
public class VRBodyBoneFollower : MonoBehaviour
{
    [Header("Targets (set by NetworkAvatarState)")]
    [Tooltip("Target transform for the head (HMD).")]
    public Transform headTarget;

    [Tooltip("Target transform for the left hand controller.")]
    public Transform leftHandTarget;

    [Tooltip("Target transform for the right hand controller.")]
    public Transform rightHandTarget;

    [Header("Bones (from the active skin)")]
    [Tooltip("Bone that should follow the head target.")]
    public Transform headBone;

    [Tooltip("Bone that should follow the left hand target.")]
    public Transform leftHandBone;

    [Tooltip("Bone that should follow the right hand target.")]
    public Transform rightHandBone;

    [Tooltip("Optional root or hips bone used to align the body under the head.")]
    public Transform bodyRootBone;

    [Header("Offsets")]
    [Tooltip("Local position offset applied to the head bone relative to the head target.")]
    public Vector3 headPositionOffset = Vector3.zero;

    [Tooltip("Local rotation offset (degrees) applied to the head bone relative to the head target.")]
    public Vector3 headRotationOffsetEuler = Vector3.zero;

    [Tooltip("Local position offset for the left hand relative to its target.")]
    public Vector3 leftHandPositionOffset = Vector3.zero;

    [Tooltip("Local rotation offset (degrees) for the left hand relative to its target.")]
    public Vector3 leftHandRotationOffsetEuler = Vector3.zero;

    [Tooltip("Local position offset for the right hand relative to its target.")]
    public Vector3 rightHandPositionOffset = Vector3.zero;

    [Tooltip("Local rotation offset (degrees) for the right hand relative to its target.")]
    public Vector3 rightHandRotationOffsetEuler = Vector3.zero;

    [Header("Body Alignment")]
    [Tooltip("If true, bodyRootBone will be placed under the head on the horizontal plane.")]
    public bool alignBodyToHead = true;

    [Tooltip("How quickly the body root moves to follow the head (position).")]
    public float bodyFollowPositionSpeed = 15f;

    [Tooltip("How quickly the body root turns to face the same direction as the head (yaw only).")]
    public float bodyFollowRotationSpeed = 15f;

    [Header("Animation")]
    [Tooltip("Optional Animator used for hand grab states and other parameters.")]
    public Animator animator;

    [Tooltip("Animator bool parameter for left hand grab state.")]
    public string leftGrabParam = "IsLeftGrabbing";

    [Tooltip("Animator bool parameter for right hand grab state.")]
    public string rightGrabParam = "IsRightGrabbing";

    [Header("Debug")]
    [Tooltip("If true, logs when bones are refreshed from the active skin.")]
    public bool logRebinds = false;

    private Quaternion _headRotOffset;
    private Quaternion _leftHandRotOffset;
    private Quaternion _rightHandRotOffset;

    private void Awake()
    {
        _headRotOffset = Quaternion.Euler(headRotationOffsetEuler);
        _leftHandRotOffset = Quaternion.Euler(leftHandRotationOffsetEuler);
        _rightHandRotOffset = Quaternion.Euler(rightHandRotationOffsetEuler);

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        FollowHead();
        FollowHands();
        FollowBodyRoot();
    }

    #region Public API used by NetworkAvatarState

    /// <summary>
    /// Called by NetworkAvatarState when the active skin changes or on spawn.
    /// Tries to rebind the head and hand bones based on the current skin root.
    /// </summary>
    public void RefreshBonesFromActiveSkin(AvatarSkinController skinController)
    {
        if (skinController == null)
        {
            Debug.LogWarning("[VRBodyBoneFollower] RefreshBonesFromActiveSkin called with null skinController.");
            return;
        }

        GameObject root = skinController.CurrentSkinRoot;
        if (root == null)
        {
            Debug.LogWarning("[VRBodyBoneFollower] CurrentSkinRoot is null, cannot refresh bones.");
            return;
        }

        // Try to find bones by common humanoid names.
        // You can adjust these if ithappy uses different names.
        headBone = headBone ?? FindChildRecursive(root.transform, "Head");
        if (headBone == null) headBone = FindChildRecursive(root.transform, "head");

        leftHandBone = leftHandBone ?? FindChildRecursive(root.transform, "LeftHand");
        if (leftHandBone == null) leftHandBone = FindChildRecursive(root.transform, "Hand_L");
        if (leftHandBone == null) leftHandBone = FindChildRecursive(root.transform, "Left_Hand");

        rightHandBone = rightHandBone ?? FindChildRecursive(root.transform, "RightHand");
        if (rightHandBone == null) rightHandBone = FindChildRecursive(root.transform, "Hand_R");
        if (rightHandBone == null) rightHandBone = FindChildRecursive(root.transform, "Right_Hand");

        if (bodyRootBone == null)
        {
            // Try some common options for hips or root.
            bodyRootBone = FindChildRecursive(root.transform, "Hips");
            if (bodyRootBone == null) bodyRootBone = FindChildRecursive(root.transform, "Pelvis");
            if (bodyRootBone == null) bodyRootBone = root.transform;
        }

        if (logRebinds)
        {
            Debug.Log(
                $"[VRBodyBoneFollower] Bones refreshed from skin '{root.name}'. " +
                $"Head={NameOrNull(headBone)}, LeftHand={NameOrNull(leftHandBone)}, " +
                $"RightHand={NameOrNull(rightHandBone)}, BodyRoot={NameOrNull(bodyRootBone)}"
            );
        }
    }

    /// <summary>
    /// Called by NetworkAvatarState on both local and remote avatars when the grab state changes.
    /// You can drive animator parameters here.
    /// </summary>
    public void SetHandGrabbed(bool isLeft, bool grabbed)
    {
        if (animator == null)
        {
            return;
        }

        if (isLeft && !string.IsNullOrEmpty(leftGrabParam))
        {
            animator.SetBool(leftGrabParam, grabbed);
        }
        else if (!isLeft && !string.IsNullOrEmpty(rightGrabParam))
        {
            animator.SetBool(rightGrabParam, grabbed);
        }
    }

    #endregion

    #region Follow logic

    private void FollowHead()
    {
        if (headTarget == null || headBone == null)
        {
            return;
        }

        // Directly follow the head target with optional offsets.
        headBone.position = headTarget.TransformPoint(headPositionOffset);
        headBone.rotation = headTarget.rotation * _headRotOffset;
    }

    private void FollowHands()
    {
        if (leftHandTarget != null && leftHandBone != null)
        {
            leftHandBone.position = leftHandTarget.TransformPoint(leftHandPositionOffset);
            leftHandBone.rotation = leftHandTarget.rotation * _leftHandRotOffset;
        }

        if (rightHandTarget != null && rightHandBone != null)
        {
            rightHandBone.position = rightHandTarget.TransformPoint(rightHandPositionOffset);
            rightHandBone.rotation = rightHandTarget.rotation * _rightHandRotOffset;
        }
    }

    private void FollowBodyRoot()
    {
        if (!alignBodyToHead || bodyRootBone == null || headTarget == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Move body root toward the head on the horizontal plane.
        Vector3 bodyPos = bodyRootBone.position;
        Vector3 headPos = headTarget.position;

        Vector3 targetBodyPos = new Vector3(headPos.x, bodyPos.y, headPos.z);
        bodyRootBone.position = Vector3.Lerp(bodyPos, targetBodyPos, bodyFollowPositionSpeed * dt);

        // Rotate body to face same yaw as head.
        Vector3 forward = headTarget.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
            bodyRootBone.rotation = Quaternion.Slerp(
                bodyRootBone.rotation,
                targetRot,
                bodyFollowRotationSpeed * dt
            );
        }
    }

    #endregion

    #region Helpers

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static string NameOrNull(Transform t)
    {
        return t == null ? "null" : t.name;
    }

    #endregion
}
