using UnityEngine;

public class AvatarSkinInitializer : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Controls which character skin is shown on this avatar.")]
    public AvatarSkinController skinController;

    [Tooltip("Network state class that synchronizes avatar skin selection.")]
    public NetworkAvatarState avatarState;

    private void Start()
    {
        // Auto-assign components if not set.
        if (skinController == null)
        {
            skinController = GetComponent<AvatarSkinController>();
            if (skinController == null)
                Debug.LogError("[AvatarSkinInitializer] Missing AvatarSkinController component.");
            else
                Debug.Log("[AvatarSkinInitializer] Auto-assigned AvatarSkinController.");
        }

        if (avatarState == null)
        {
            avatarState = GetComponent<NetworkAvatarState>();
            if (avatarState == null)
                Debug.LogWarning("[AvatarSkinInitializer] No NetworkAvatarState found. Avatar will not sync on network.");
            else
                Debug.Log("[AvatarSkinInitializer] Auto-assigned NetworkAvatarState.");
        }

        int index = GameSettings.selectedSkinIndex;
        Debug.Log($"[AvatarSkinInitializer] Applying initial skin index: {index}");

        // Apply skin locally.
        if (skinController != null)
        {
            skinController.ApplySkin(index);
        }
        else
        {
            Debug.LogError("[AvatarSkinInitializer] Cannot apply skin. skinController is null.");
        }

        // If this is the local player, record the selection to network state.
        if (avatarState != null)
        {
            if (avatarState.IsLocalPlayer)
            {
                avatarState.SetSkinIndex(index);
                Debug.Log($"[AvatarSkinInitializer] Local player detected. Network skin index set to {index}.");
            }
            else
            {
                Debug.Log("[AvatarSkinInitializer] Not local player, network skin will be set externally.");
            }
        }
    }
}
