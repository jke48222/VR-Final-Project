using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Input;

/// <summary>
/// Hand-level locomotion and state controller:
/// - Teleport and snap turning (thumbstick)
/// - GoGo arm extension
/// - Air grab locomotion
/// - Hand grab state for avatar fingers and networking
///
/// This script does not handle object grabbing directly. That is expected
/// to be managed by the Meta XR Interaction SDK (Grab Interactables, etc.).
/// </summary>
public class HandController : MonoBehaviour
{
    [Header("Rig References")]
    [SerializeField]
    [Tooltip("Head transform used for teleport and GoGo calculations.")]
    private Transform head;

    [SerializeField]
    [Tooltip("Transform of the physical controller used as the base for GoGo.")]
    private Transform controller;

    [SerializeField]
    [Tooltip("Root transform of the XR rig that moves when teleporting or air grabbing.")]
    private Transform rig;

    [SerializeField]
    [Tooltip("Network avatar state used to sync hand grab state across the network.")]
    private NetworkAvatarState avatarState;

    [Header("GoGo Settings")]
    [SerializeField]
    [Tooltip("If true, GoGo arm extension is applied to this hand.")]
    private bool useGoGo = true;

    [SerializeField]
    [Tooltip("Distance from head at which GoGo extension begins.")]
    private float goGoThreshold = 0.4f;

    [SerializeField]
    [Tooltip("Scale factor applied to the extended reach. Larger values reach farther.")]
    private float goGoScale = 0.5f;

    [Header("Hand Settings")]
    [SerializeField]
    [Tooltip("True if this is the left hand controller.")]
    private bool isLeftHand = true;

    [SerializeField]
    [Tooltip("Optional body follower used for hand pose syncing.")]
    private VRBodyBoneFollower bodyFollower;

    [Header("Grab Input")]
    [SerializeField]
    [Tooltip("Analog grab input action (typically grip).")]
    private InputAction grabAction;

    [SerializeField]
    [Tooltip("World space linear hand velocity from the input system (used for throws if needed).")]
    private InputAction handVelocity;

    [SerializeField]
    [Tooltip("World space angular hand velocity from the input system (used for throws if needed).")]
    private InputAction handAngularVelocity;

    [SerializeField]
    [Tooltip("Haptic output action for controller vibration.")]
    private InputAction vibration;

    [SerializeField]
    [Tooltip("Thumbstick input action (used for teleport and snap turn).")]
    private InputAction thumbstick;

    [SerializeField]
    [Tooltip("Threshold above which the grab value is considered an intentional grab.")]
    private float grabThreshold = 0.2f;

    [Header("Teleport Settings")]
    [SerializeField]
    [Tooltip("If true, thumbstick controls teleport and snap turn.")]
    private bool useTeleport = true;

    [SerializeField]
    [Tooltip("Dead zone for thumbstick teleport and snap recognition.")]
    private float stickDeadZone = 0.5f;

    [SerializeField]
    [Tooltip("Snap rotation degrees when rotating with the thumbstick.")]
    private float snapDegrees = 15f;

    [SerializeField]
    [Tooltip("Prefab for visual teleporter arc segments.")]
    private GameObject teleporterArcPrefab;

    [SerializeField]
    [Tooltip("Time step for projectile simulation used by the teleporter arc.")]
    private float teleporterDt = 0.1f;

    [SerializeField]
    [Tooltip("Vertical acceleration for the teleporter arc. Lower values create a more curved arc.")]
    private float teleporterGravity = -1f;

    [Header("Air Grab Settings")]
    [SerializeField]
    [Tooltip("If true, air grab locomotion is enabled.")]
    private bool useAirGrab = true;

    // Teleport state
    private bool teleportingActive;
    private bool snapActive;
    private Vector3 teleportingTarget;
    private bool teleportingValid;
    private GameObject[] arcPieces = new GameObject[50];

    // Air grab state
    private bool isAirGrabbing;
    private Vector3 airGrabStartPositionWorld;

    // Network / pose sync
    private bool wasGrabbingLastFrame;

    private void Awake()
    {
        if (bodyFollower == null)
        {
            bodyFollower = GetComponentInParent<VRBodyBoneFollower>();
        }

        if (head == null)
        {
            Debug.LogWarning("[HandController] Head reference is not assigned. Teleport and GoGo may behave incorrectly.");
        }

        if (controller == null)
        {
            Debug.LogWarning("[HandController] Controller reference is not assigned. GoGo calculations will be incorrect.");
        }

        if (rig == null)
        {
            Debug.LogWarning("[HandController] Rig reference is not assigned. Teleport and air grab cannot move the player.");
        }
    }

    private void OnEnable()
    {
        if (grabAction != null) grabAction.Enable();
        if (handVelocity != null) handVelocity.Enable();
        if (handAngularVelocity != null) handAngularVelocity.Enable();
        if (vibration != null) vibration.Enable();
        if (thumbstick != null) thumbstick.Enable();
    }

    private void OnDisable()
    {
        if (grabAction != null) grabAction.Disable();
        if (handVelocity != null) handVelocity.Disable();
        if (handAngularVelocity != null) handAngularVelocity.Disable();
        if (vibration != null) vibration.Disable();
        if (thumbstick != null) thumbstick.Disable();
    }

    private void Update()
    {
        HandleTeleportAndSnapTurn();
        HandleGoGo();
        HandleGrabStateOnly();
        HandleAirGrab();
        SyncNetworkGrabState();
    }

    #region Teleport and Snap Turn

    /// <summary>
    /// Handles teleport arc visual, teleport activation, and snap turning using the thumbstick.
    /// </summary>
    private void HandleTeleportAndSnapTurn()
    {
        if (!useTeleport || thumbstick == null || rig == null || head == null)
        {
            return;
        }

        Vector2 stick = thumbstick.ReadValue<Vector2>();

        // Reset snap flag when stick returns to center
        if (Mathf.Abs(stick.x) < stickDeadZone * 0.9f && snapActive)
        {
            snapActive = false;
        }

        // Snap turn
        if (Mathf.Abs(stick.x) > stickDeadZone && !snapActive)
        {
            snapActive = true;

            Vector3 footWorld = ProjectToRigFloor(head.position);
            rig.Rotate(0f, Mathf.Sign(stick.x) * snapDegrees, 0f, Space.World);
            Vector3 footWorldNew = ProjectToRigFloor(head.position);
            rig.Translate(footWorld - footWorldNew, Space.World);

            Debug.Log($"[HandController] Snap turn {(stick.x > 0f ? "right" : "left")} by {snapDegrees} degrees.");
        }

        // Teleport release
        if (stick.y < stickDeadZone * 0.9f && teleportingActive)
        {
            teleportingActive = false;

            foreach (GameObject part in arcPieces)
            {
                if (part != null)
                {
                    part.SetActive(false);
                }
            }

            if (teleportingValid)
            {
                Vector3 currentFoot = ProjectToRigFloor(head.position);
                rig.Translate(teleportingTarget - currentFoot, Space.World);
                Debug.Log($"[HandController] Teleported to {teleportingTarget}.");
            }
            else
            {
                Debug.Log("[HandController] Teleport invalid. No movement performed.");
            }
        }

        // Teleport start
        if (stick.y > stickDeadZone && !teleportingActive)
        {
            teleportingActive = true;
            Debug.Log("[HandController] Teleport aiming started.");
        }

        // Teleport arc simulation
        if (teleportingActive)
        {
            SimulateTeleportArc();
        }
    }

    /// <summary>
    /// Simulates a projectile arc from the hand and builds a visual arc with raycasts to find a teleport target.
    /// </summary>
    private void SimulateTeleportArc()
    {
        if (teleporterArcPrefab == null)
        {
            if (teleportingValid)
            {
                teleportingValid = false;
            }

            Debug.LogWarning("[HandController] Teleporter arc prefab is not assigned. Teleport arcs will not render.");
            return;
        }

        Vector3 p = transform.position;
        Vector3 v = transform.forward;
        Vector3 a = new Vector3(0f, teleporterGravity, 0f);

        teleportingValid = false;

        for (int i = 0; i < arcPieces.Length; i++)
        {
            if (arcPieces[i] == null)
            {
                arcPieces[i] = Instantiate(teleporterArcPrefab);
            }
            else
            {
                arcPieces[i].SetActive(true);
            }

            arcPieces[i].transform.position = p;

            Vector3 pNext = p + v * teleporterDt;
            Vector3 r = pNext - p;

            arcPieces[i].transform.forward = r.normalized;
            arcPieces[i].transform.localScale = new Vector3(1f, 1f, r.magnitude);

            if (Physics.Raycast(p, r.normalized, out RaycastHit hitInfo, r.magnitude))
            {
                if (hitInfo.normal.y > 0.7f)
                {
                    teleportingValid = true;
                    teleportingTarget = hitInfo.point;

                    arcPieces[i].transform.localScale = new Vector3(1f, 1f, hitInfo.distance);

                    for (int j = i + 1; j < arcPieces.Length; j++)
                    {
                        if (arcPieces[j] != null)
                        {
                            arcPieces[j].SetActive(false);
                        }
                    }

                    break;
                }
            }

            v += a * teleporterDt;
            p = pNext;
        }
    }

    #endregion

    #region GoGo Extension

    /// <summary>
    /// Handles GoGo arm extension based on distance from head to controller.
    /// </summary>
    private void HandleGoGo()
    {
        if (!useGoGo || head == null || controller == null)
        {
            return;
        }

        Vector3 headToController = controller.position - head.position;
        float d = headToController.magnitude;

        if (d >= goGoThreshold)
        {
            float e = d - goGoThreshold;
            transform.position = head.position + headToController.normalized * (d + goGoScale * e * e);
        }
        else
        {
            transform.localPosition = Vector3.zero;
        }
    }

    #endregion

    #region Grab State (no custom grabbables)

    /// <summary>
    /// Uses grab input purely as a state signal for avatar fingers and haptics.
    /// Object grabbing is expected to be handled by the Meta XR Interaction SDK.
    /// </summary>
    private void HandleGrabStateOnly()
    {
        if (grabAction == null)
        {
            return;
        }

        float grabValue = grabAction.ReadValue<float>();
        bool isGrabbingNow = grabValue > grabThreshold;

        // Update local finger pose
        if (bodyFollower != null)
        {
            bodyFollower.SetHandGrabbed(isLeftHand, isGrabbingNow);
        }
    }

    #endregion

    #region Air Grab Locomotion

    /// <summary>
    /// Handles air grab locomotion when enabled.
    /// This feature reuses the same grab input, so consider disabling it
    /// if it conflicts with interaction grabbing in your scene.
    /// </summary>
    private void HandleAirGrab()
    {
        if (!useAirGrab || grabAction == null || rig == null)
        {
            return;
        }

        float grabValue = grabAction.ReadValue<float>();

        // Begin air grab (only when we were not already air grabbing)
        if (grabValue > grabThreshold && !isAirGrabbing)
        {
            isAirGrabbing = true;
            airGrabStartPositionWorld = transform.position;
            Debug.Log("[HandController] Air grab started.");
        }

        // End air grab
        if (grabValue < grabThreshold * 0.9f && isAirGrabbing)
        {
            isAirGrabbing = false;
            Debug.Log("[HandController] Air grab ended.");
        }

        // While air grabbing, move rig opposite to hand motion
        if (isAirGrabbing)
        {
            Vector3 handMovement = transform.position - airGrabStartPositionWorld;
            rig.position -= handMovement;
        }
    }

    #endregion

    #region Networking / Haptics

    /// <summary>
    /// Notifies the NetworkAvatarState when grab state changes.
    /// </summary>
    private void SyncNetworkGrabState()
    {
        if (grabAction == null || avatarState == null)
        {
            return;
        }

        bool isGrabbingNow = grabAction.ReadValue<float>() > grabThreshold;

        if (isGrabbingNow != wasGrabbingLastFrame)
        {
            avatarState.SetGrabState(isLeftHand, isGrabbingNow);
        }

        wasGrabbingLastFrame = isGrabbingNow;
    }

    /// <summary>
    /// Triggers a simple haptic impulse on this controller.
    /// </summary>
    public void Rumble(float magnitude, float duration)
    {
        if (vibration == null || vibration.controls.Count == 0)
        {
            return;
        }

        (vibration.controls[0].device as XRControllerWithRumble)?.SendImpulse(magnitude, duration);
        // Alternative OpenXR haptics is intentionally disabled here:
        // OpenXRInput.SendHapticImpulse(vibration, 1, 0.01f);
    }

    #endregion

    #region Helpers and Setup

    /// <summary>
    /// Projects a world position onto the rig floor plane (y = 0 in rig local space).
    /// </summary>
    private Vector3 ProjectToRigFloor(Vector3 worldPos)
    {
        if (rig == null)
        {
            return worldPos;
        }

        Vector3 rigLocal = rig.InverseTransformPoint(worldPos);
        rigLocal.y = 0f;
        return rig.TransformPoint(rigLocal);
    }

    /// <summary>
    /// Convenience method intended to be called from NetworkAvatarState or setup code.
    /// It wires both hand controllers to share this component's NetworkAvatarState.
    /// </summary>
    public void AttachHands(HandController left, HandController right)
    {
        if (left != null)
        {
            left.avatarState = avatarState;
            left.isLeftHand = true;
        }

        if (right != null)
        {
            right.avatarState = avatarState;
            right.isLeftHand = false;
        }

        Debug.Log("[HandController] Hands attached to shared NetworkAvatarState.");
    }

    #endregion
}
