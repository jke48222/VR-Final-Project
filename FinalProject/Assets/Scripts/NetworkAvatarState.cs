using System.IO;
using UnityEngine;
using VelNet;

/// <summary>
/// Syncs VR avatar state over VelNet:
/// - head / left / right transforms
/// - left/right grab flags
/// - selected skin index
/// Works with VRBodyBoneFollower and AvatarSkinController.
/// </summary>
[DisallowMultipleComponent]
public class NetworkAvatarState : SyncState
{
    [Header("Avatar Systems")]
    [Tooltip("Drives the bones of the avatar based on IK targets.")]
    public VRBodyBoneFollower bodyFollower;

    [Tooltip("Controls which character skin is active on this avatar.")]
    public AvatarSkinController skinController;

    [Header("Remote IK Targets (children of avatar)")]
    [Tooltip("Target for the remote head transform.")]
    public Transform remoteHeadTarget;

    [Tooltip("Target for the remote left hand transform.")]
    public Transform remoteLeftHandTarget;

    [Tooltip("Target for the remote right hand transform.")]
    public Transform remoteRightHandTarget;

    [Header("Smoothing")]
    [Tooltip("How quickly remote IK targets follow the networked positions and rotations.")]
    public float remoteFollowSpeed = 15f;

    // Local XR targets (for the owner)
    private Transform localHead;
    private Transform localLeft;
    private Transform localRight;

    // Networked state
    private Vector3 targetHeadPos, targetLeftPos, targetRightPos;
    private Quaternion targetHeadRot, targetLeftRot, targetRightRot;
    private bool netLeftGrabbed;
    private bool netRightGrabbed;
    private int netSkinIndex;

    // Local flags
    private bool localLeftGrabbed;
    private bool localRightGrabbed;
    private bool initializedLocalRig;

    /// <summary>
    /// Exposes ownership to other scripts.
    /// </summary>
    public bool IsLocalPlayer => IsMine;

    protected override void Awake()
    {
        base.Awake();

        if (bodyFollower == null)
        {
            bodyFollower = GetComponent<VRBodyBoneFollower>();
        }

        if (skinController == null)
        {
            skinController = GetComponent<AvatarSkinController>();
        }

        Debug.Log($"[NetworkAvatarState] Awake on '{name}'. IsMine={IsMine}");
    }

    private void Start()
    {
        if (!IsMine)
        {
            // Remote avatar: drive bones from the remote targets.
            if (bodyFollower != null)
            {
                bodyFollower.headTarget = remoteHeadTarget;
                bodyFollower.leftHandTarget = remoteLeftHandTarget;
                bodyFollower.rightHandTarget = remoteRightHandTarget;

                Debug.Log($"[NetworkAvatarState] Configured remote IK targets for '{name}'.");
            }
            else
            {
                Debug.LogWarning($"[NetworkAvatarState] No VRBodyBoneFollower on remote avatar '{name}'.");
            }
        }
        else
        {
            // Local: we will hook into LocalXRTargets once they are ready.
            Debug.Log($"[NetworkAvatarState] Local avatar '{name}' waiting for LocalXRTargets.");
        }

        if (skinController == null)
        {
            Debug.LogWarning($"[NetworkAvatarState] No AvatarSkinController assigned on '{name}'. Skin sync will not work.");
        }
    }

    private void Update()
    {
        if (IsMine)
        {
            HandleLocalRigBinding();
            // Local visual updates happen in other scripts (hand controllers, menus).
        }
        else
        {
            HandleRemoteFollow();
        }
    }

    /// <summary>
    /// Binds this avatar to the local XR rig targets when they become available.
    /// </summary>
    private void HandleLocalRigBinding()
    {
        if (initializedLocalRig)
        {
            return;
        }

        if (!LocalXRTargets.IsReady)
        {
            return;
        }

        localHead = LocalXRTargets.Head;
        localLeft = LocalXRTargets.LeftHand;
        localRight = LocalXRTargets.RightHand;

        if (bodyFollower != null)
        {
            bodyFollower.headTarget = localHead;
            bodyFollower.leftHandTarget = localLeft;
            bodyFollower.rightHandTarget = localRight;

            Debug.Log($"[NetworkAvatarState] Local rig bound for '{name}'.");
        }
        else
        {
            Debug.LogWarning($"[NetworkAvatarState] Local rig ready but VRBodyBoneFollower is missing on '{name}'.");
        }

        initializedLocalRig = true;
    }

    /// <summary>
    /// Smoothly follows the networked target transforms on remote avatars.
    /// </summary>
    private void HandleRemoteFollow()
    {
        float t = remoteFollowSpeed * Time.deltaTime;

        if (remoteHeadTarget != null)
        {
            remoteHeadTarget.position =
                Vector3.Lerp(remoteHeadTarget.position, targetHeadPos, t);
            remoteHeadTarget.rotation =
                Quaternion.Slerp(remoteHeadTarget.rotation, targetHeadRot, t);
        }

        if (remoteLeftHandTarget != null)
        {
            remoteLeftHandTarget.position =
                Vector3.Lerp(remoteLeftHandTarget.position, targetLeftPos, t);
            remoteLeftHandTarget.rotation =
                Quaternion.Slerp(remoteLeftHandTarget.rotation, targetLeftRot, t);
        }

        if (remoteRightHandTarget != null)
        {
            remoteRightHandTarget.position =
                Vector3.Lerp(remoteRightHandTarget.position, targetRightPos, t);
            remoteRightHandTarget.rotation =
                Quaternion.Slerp(remoteRightHandTarget.rotation, targetRightRot, t);
        }

        if (bodyFollower != null)
        {
            bodyFollower.SetHandGrabbed(true, netLeftGrabbed);
            bodyFollower.SetHandGrabbed(false, netRightGrabbed);
        }
    }

    #region API used by other scripts

    /// <summary>
    /// Called by AvatarSkinInitializer on spawn to set the skin index.
    /// This sets the local value and will be included in the next network send.
    /// </summary>
    public void SetSkinIndex(int index)
    {
        netSkinIndex = index;

        if (skinController != null)
        {
            skinController.ApplySkin(index);

            if (bodyFollower != null)
            {
                bodyFollower.RefreshBonesFromActiveSkin(skinController);
            }

            Debug.Log($"[NetworkAvatarState] '{name}' SetSkinIndex to {index} (local apply).");
        }
        else
        {
            Debug.LogWarning($"[NetworkAvatarState] SetSkinIndex called on '{name}' but skinController is null.");
        }
    }

    /// <summary>
    /// Called by HandController to report grip changes.
    /// </summary>
    public void SetGrabState(bool isLeft, bool grabbed)
    {
        if (isLeft)
        {
            localLeftGrabbed = grabbed;
            netLeftGrabbed = grabbed;
        }
        else
        {
            localRightGrabbed = grabbed;
            netRightGrabbed = grabbed;
        }

        if (bodyFollower != null && IsMine)
        {
            bodyFollower.SetHandGrabbed(isLeft, grabbed);
        }

        Debug.Log($"[NetworkAvatarState] '{name}' SetGrabState. IsLeft={isLeft}, Grabbed={grabbed}, IsMine={IsMine}");
    }

    #endregion

    #region SyncState implementation

    /// <summary>
    /// Owner packs state into a byte stream. Called automatically by SyncState.
    /// </summary>
    protected override void SendState(BinaryWriter writer)
    {
        // Only owners call this.
        Transform h = localHead ?? (bodyFollower != null ? bodyFollower.headTarget : transform);
        Transform l = localLeft ?? (bodyFollower != null ? bodyFollower.leftHandTarget : transform);
        Transform r = localRight ?? (bodyFollower != null ? bodyFollower.rightHandTarget : transform);

        if (h == null || l == null || r == null)
        {
            Debug.LogWarning($"[NetworkAvatarState] '{name}' SendState called but one or more transforms are null.");
            // Still write something so the stream stays consistent.
        }

        Vector3 hp = h != null ? h.position : transform.position;
        Quaternion hr = h != null ? h.rotation : transform.rotation;

        Vector3 lp = l != null ? l.position : transform.position;
        Quaternion lr = l != null ? l.rotation : transform.rotation;

        Vector3 rp = r != null ? r.position : transform.position;
        Quaternion rr = r != null ? r.rotation : transform.rotation;

        // Manually write Vector3 and Quaternion components in a fixed order.
        writer.Write(hp.x);
        writer.Write(hp.y);
        writer.Write(hp.z);
        writer.Write(hr.x);
        writer.Write(hr.y);
        writer.Write(hr.z);
        writer.Write(hr.w);

        writer.Write(lp.x);
        writer.Write(lp.y);
        writer.Write(lp.z);
        writer.Write(lr.x);
        writer.Write(lr.y);
        writer.Write(lr.z);
        writer.Write(lr.w);

        writer.Write(rp.x);
        writer.Write(rp.y);
        writer.Write(rp.z);
        writer.Write(rr.x);
        writer.Write(rr.y);
        writer.Write(rr.z);
        writer.Write(rr.w);

        // Pack grabs into a bitmask: bit 0 = left, bit 1 = right.
        byte flags = 0;
        if (localLeftGrabbed) flags |= 1;
        if (localRightGrabbed) flags |= 2;
        writer.Write(flags);

        // Skin index as a byte (0 to 255).
        writer.Write((byte)netSkinIndex);
    }

    /// <summary>
    /// Non owners receive state here and update their targets.
    /// </summary>
    protected override void ReceiveState(BinaryReader reader)
    {
        targetHeadPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        targetHeadRot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        targetLeftPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        targetLeftRot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        targetRightPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        targetRightRot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        // Grabs
        byte flags = reader.ReadByte();
        netLeftGrabbed = (flags & 1) != 0;
        netRightGrabbed = (flags & 2) != 0;

        // Skin
        int incomingSkin = reader.ReadByte();
        if (incomingSkin != netSkinIndex)
        {
            netSkinIndex = incomingSkin;

            if (skinController != null)
            {
                skinController.ApplySkin(netSkinIndex);

                if (bodyFollower != null)
                {
                    bodyFollower.RefreshBonesFromActiveSkin(skinController);
                }

                Debug.Log($"[NetworkAvatarState] '{name}' applied remote skin index {netSkinIndex}.");
            }
            else
            {
                Debug.LogWarning($"[NetworkAvatarState] '{name}' received skin index {netSkinIndex} but skinController is null.");
            }
        }
    }

    #endregion
}
