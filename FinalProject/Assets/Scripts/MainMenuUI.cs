using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class MainMenuUI : MonoBehaviour
{
    [Header("Mode UI")]
    [Tooltip("Text label that shows the current mode on the Play button.")]
    public TMP_Text modeLabel;

    [Header("Character UI")]
    [Tooltip("Image that displays the currently selected character skin.")]
    public Image skinImage;

    [Tooltip("Optional label that shows the name of the currently selected character.")]
    public TMP_Text skinNameLabel;

    [Header("Skins")]
    [Tooltip("Sprites for available character skins. Order MUST match AvatarSkinController.skins on PlayerAvatar.")]
    public Sprite[] skinSprites;

    [Tooltip("Optional display names for each skin, same order as skinSprites.")]
    public string[] skinNames;

    [Header("Scenes")]
    [Tooltip("Scene name for single player mode.")]
    public string singlePlayerScene = "ChaoticKitchenScene";

    [Tooltip("Scene name for multiplayer loading or lobby.")]
    public string multiplayerLoadingScene = "MP_Loading";

    // 0 = single player, 1 = multiplayer
    private int currentMode = 0;

    // Current skin index into skinSprites / ithappy avatar list
    private int currentSkinIndex = 0;

    private void Awake()
    {
        if (modeLabel == null)
        {
            Debug.LogWarning("[MainMenuUI] modeLabel is not assigned.");
        }

        if (skinImage == null)
        {
            Debug.LogWarning("[MainMenuUI] skinImage is not assigned.");
        }

        if (skinSprites == null || skinSprites.Length == 0)
        {
            Debug.LogWarning("[MainMenuUI] No skinSprites assigned. Character preview will be empty.");
        }

        if (string.IsNullOrWhiteSpace(singlePlayerScene))
        {
            Debug.LogWarning("[MainMenuUI] singlePlayerScene is empty.");
        }

        if (string.IsNullOrWhiteSpace(multiplayerLoadingScene))
        {
            Debug.LogWarning("[MainMenuUI] multiplayerLoadingScene is empty.");
        }
    }

    private void Start()
    {
        // Load last chosen settings
        currentMode = GameSettings.gameMode;
        currentSkinIndex = GameSettings.selectedSkinIndex;

        // Clamp skin index to the available sprite count
        int maxIndex = (skinSprites != null && skinSprites.Length > 0)
            ? skinSprites.Length - 1
            : 0;

        currentSkinIndex = Mathf.Clamp(currentSkinIndex, 0, maxIndex);

        UpdateModeLabel();
        UpdateSkinDisplay();

        string skinName = GetSkinName(currentSkinIndex);
        Debug.Log($"[MainMenuUI] Initialized. Mode={currentMode}, SkinIndex={currentSkinIndex}, SkinName='{skinName}'");
    }

    // -----------------------------------
    // MODE BUTTONS
    // -----------------------------------

    public void OnSinglePlayerClicked()
    {
        currentMode = 0;
        UpdateModeLabel();
        Debug.Log("[MainMenuUI] Mode set to Single Player.");
    }

    public void OnMultiplayerClicked()
    {
        currentMode = 1;
        UpdateModeLabel();
        Debug.Log("[MainMenuUI] Mode set to Multiplayer.");
    }

    // -----------------------------------
    // SKIN SELECTION
    // -----------------------------------

    public void OnPreviousSkinClicked()
    {
        if (skinSprites == null || skinSprites.Length == 0)
        {
            Debug.LogWarning("[MainMenuUI] OnPreviousSkinClicked but no skins are configured.");
            return;
        }

        currentSkinIndex--;
        if (currentSkinIndex < 0)
        {
            currentSkinIndex = skinSprites.Length - 1;
        }

        UpdateSkinDisplay();
        Debug.Log($"[MainMenuUI] Selected previous skin. New index={currentSkinIndex}, Name='{GetSkinName(currentSkinIndex)}'");
    }

    public void OnNextSkinClicked()
    {
        if (skinSprites == null || skinSprites.Length == 0)
        {
            Debug.LogWarning("[MainMenuUI] OnNextSkinClicked but no skins are configured.");
            return;
        }

        currentSkinIndex = (currentSkinIndex + 1) % skinSprites.Length;

        UpdateSkinDisplay();
        Debug.Log($"[MainMenuUI] Selected next skin. New index={currentSkinIndex}, Name='{GetSkinName(currentSkinIndex)}'");
    }

    // -----------------------------------
    // PLAY BUTTON
    // -----------------------------------

    public void OnPlayClicked()
    {
        // Save global settings so the PlayerAvatar can read them later
        GameSettings.gameMode = currentMode;
        GameSettings.selectedSkinIndex = currentSkinIndex;

        // Determine target scene
        string sceneToLoad = currentMode == 0
            ? singlePlayerScene
            : multiplayerLoadingScene;

        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogError("[MainMenuUI] Target scene name is empty. Cannot load.");
            return;
        }

        string skinName = GetSkinName(currentSkinIndex);
        Debug.Log($"[MainMenuUI] Play clicked. Mode={currentMode}, SkinIndex={currentSkinIndex}, SkinName='{skinName}', Loading='{sceneToLoad}'");

        SceneManager.LoadScene(sceneToLoad);
    }

    // -----------------------------------
    // UI Updates
    // -----------------------------------

    private void UpdateModeLabel()
    {
        if (modeLabel == null)
        {
            return;
        }

        modeLabel.text = currentMode == 0
            ? "Mode: Single Player"
            : "Mode: Multiplayer";
    }

    private void UpdateSkinDisplay()
    {
        if (skinSprites == null || skinSprites.Length == 0)
        {
            if (skinImage != null)
            {
                skinImage.sprite = null;
            }

            if (skinNameLabel != null)
            {
                skinNameLabel.text = string.Empty;
            }

            return;
        }

        currentSkinIndex = Mathf.Clamp(currentSkinIndex, 0, skinSprites.Length - 1);

        if (skinImage != null)
        {
            skinImage.sprite = skinSprites[currentSkinIndex];
        }

        if (skinNameLabel != null)
        {
            skinNameLabel.text = GetSkinName(currentSkinIndex);
        }
    }

    private string GetSkinName(int index)
    {
        if (skinNames == null || skinNames.Length == 0)
        {
            return $"Skin {index}";
        }

        if (index < 0 || index >= skinNames.Length)
        {
            return $"Skin {index}";
        }

        string name = skinNames[index];
        return string.IsNullOrWhiteSpace(name) ? $"Skin {index}" : name;
    }
}
