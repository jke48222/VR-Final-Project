using UnityEngine;

public class AvatarSkinController : MonoBehaviour
{
    [Header("Character Skins")]
    [Tooltip("One root GameObject per character skin (Panda, Rabbit_Bald, etc.). Each element should be a full prefab root.")]
    public GameObject[] skins;

    private int currentIndex = 0;

    /// <summary>
    /// Index of the currently active skin.
    /// </summary>
    public int CurrentIndex => currentIndex;

    /// <summary>
    /// Returns the active skin root GameObject or null if invalid.
    /// </summary>
    public GameObject CurrentSkinRoot
    {
        get
        {
            if (skins == null || skins.Length == 0)
            {
                Debug.LogWarning("[AvatarSkinController] Skin array is empty or missing.");
                return null;
            }

            if (currentIndex < 0 || currentIndex >= skins.Length)
            {
                Debug.LogWarning($"[AvatarSkinController] CurrentIndex out of range: {currentIndex}");
                return null;
            }

            return skins[currentIndex];
        }
    }

    private void Awake()
    {
        // Ensure only the selected skin is active at runtime.
        if (skins == null || skins.Length == 0)
        {
            Debug.LogError("[AvatarSkinController] No skins assigned.");
            return;
        }

        Debug.Log($"[AvatarSkinController] Initializing with {skins.Length} skins. Activating index {currentIndex}.");
        ApplySkin(currentIndex);
    }

    /// <summary>
    /// Applies the specified skin index by activating that root object and disabling all others.
    /// </summary>
    public void ApplySkin(int index)
    {
        if (skins == null || skins.Length == 0)
        {
            Debug.LogError("[AvatarSkinController] Cannot apply skin. Skin array is empty.");
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, skins.Length - 1);

        if (clampedIndex != index)
        {
            Debug.LogWarning($"[AvatarSkinController] Requested skin index {index} is out of range. Clamped to {clampedIndex}.");
        }

        currentIndex = clampedIndex;

        Debug.Log($"[AvatarSkinController] Applying skin index {currentIndex}.");

        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] == null)
            {
                Debug.LogWarning($"[AvatarSkinController] Skin at index {i} is null.");
                continue;
            }

            bool isActive = i == currentIndex;
            skins[i].SetActive(isActive);

            if (isActive)
                Debug.Log($"[AvatarSkinController] Activated skin: {skins[i].name}");
        }
    }
}
