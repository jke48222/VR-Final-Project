using System.IO;
using UnityEngine;
using VelNet;

/// <summary>
/// Networked avatar wrapper for ithappy characters.
/// Local owner:
///   - Uses existing movement scripts (CharacterMover, MovePlayerInput)
///   - Sends position and rotation over the network
/// Remote clients:
///   - Disable MovePlayerInput so they do not move locally
///   - Smoothly interpolate toward received position and rotation
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class Avatar : NetworkComponent, IPackState
{
    [Header("Interpolation")]
    [Tooltip("How fast the remote avatar lerps toward the networked pose.")]
    public float positionLerpSpeed = 10f;

    [Tooltip("How fast the remote avatar slerps rotation toward the networked pose.")]
    public float rotationLerpSpeed = 10f;

    // The networked pose received from the owner
    private Vector3 networkPosition;
    private Quaternion networkRotation = Quaternion.identity;

    // Cached components for enabling or disabling movement on non owned avatars
    private CharacterController characterController;
    private MonoBehaviour characterMover;
    private MonoBehaviour movePlayerInput;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // These types come from the ithappy pack, adjust names if needed
        characterMover = GetComponent("CharacterMover") as MonoBehaviour;
        movePlayerInput = GetComponent("MovePlayerInput") as MonoBehaviour;
    }

    private void Start()
    {
        // Initialize network pose to current pose
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (IsMine)
        {
            // Local player, keep movement scripts enabled
            SetMovementEnabled(true);
        }
        else
        {
            // Remote avatar, movement should be driven by network
            SetMovementEnabled(false);
        }
    }

    private void Update()
    {
        if (IsMine)
        {
            // Owned avatar, send pose updates
            SendPoseUpdate();
        }
        else
        {
            // Remote avatar, smooth toward networked pose
            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition,
                positionLerpSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                networkRotation,
                rotationLerpSpeed * Time.deltaTime
            );
        }
    }

    private void SetMovementEnabled(bool enabled)
    {
        // CharacterController usually stays enabled on both
        // The important ones are the mover and input
        if (characterMover != null)
        {
            characterMover.enabled = enabled;
        }

        if (movePlayerInput != null)
        {
            movePlayerInput.enabled = enabled;
        }
    }

    #region IPackState

    public byte[] PackState()
    {
        // This builds the payload that represents our current pose
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(transform.position);
        bw.Write(transform.rotation);

        return ms.ToArray();
    }

    public void UnpackState(byte[] state)
    {
        BinaryReader br = new BinaryReader(new MemoryStream(state));
        networkPosition = br.ReadVector3();
        networkRotation = br.ReadQuaternion();
    }

    #endregion

    #region Networking

    private void SendPoseUpdate()
    {
        // For simple cases this can be sent every frame
        // If you want, you can add a timer to throttle messages
        byte[] state = PackState();
        SendRPC(nameof(RPCUpdatePose), false, state);
    }

    public override void ReceiveBytes(byte[] message)
    {
        // Not used in this example, we use RPCs instead
    }

    [VelNetRPC]
    private void RPCUpdatePose(byte[] state)
    {
        UnpackState(state);
    }

    #endregion
}
