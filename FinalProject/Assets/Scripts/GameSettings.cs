using UnityEngine;

/// <summary>
/// Global game settings shared across scenes.
/// This static class persists automatically because it does not rely on a scene GameObject.
/// It holds player selections such as: game mode, skin index, player name, and optional overrides.
/// </summary>
public static class GameSettings
{
    // ---------------------------------------------------------
    // GAME MODE
    // ---------------------------------------------------------
    /// <summary>
    /// 0 = Single Player
    /// 1 = Multiplayer
    /// </summary>
    private static int _gameMode = 0;

    public static int gameMode
    {
        get => _gameMode;
        set
        {
            int clamped = Mathf.Clamp(value, 0, 1);
            if (clamped != value)
            {
                Debug.LogWarning($"[GameSettings] gameMode value '{value}' is invalid. Clamped to '{clamped}'.");
            }

            _gameMode = clamped;
            Debug.Log($"[GameSettings] gameMode set to {_gameMode}");
        }
    }

    // ---------------------------------------------------------
    // CHARACTER SKIN SELECTION
    // ---------------------------------------------------------

    /// <summary>
    /// Index of the selected character skin (matches AvatarSkinController.skins array).
    /// </summary>
    private static int _selectedSkinIndex = 0;

    public static int selectedSkinIndex
    {
        get => _selectedSkinIndex;
        set
        {
            int clamped = value < 0 ? 0 : value;

            if (clamped != value)
            {
                Debug.LogWarning($"[GameSettings] selectedSkinIndex '{value}' invalid. Using '{clamped}' instead.");
            }

            _selectedSkinIndex = clamped;
            Debug.Log($"[GameSettings] selectedSkinIndex updated to {_selectedSkinIndex}");
        }
    }

    // ---------------------------------------------------------
    // PLAYER NAME (optional but very useful for scoring)
    // ---------------------------------------------------------

    private static string _playerName = "Player";

    public static string playerName
    {
        get => _playerName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Debug.LogWarning("[GameSettings] Attempted to set empty playerName. Keeping previous value.");
                return;
            }

            _playerName = value.Trim();
            Debug.Log($"[GameSettings] playerName set to '{_playerName}'");
        }
    }

    // ---------------------------------------------------------
    // DEBUG / OPTIONAL OVERRIDES
    // ---------------------------------------------------------

    /// <summary>
    /// If >= 0, forces the local player's spawn index (useful for testing alone).
    /// If -1, the system auto-assigns based on VelNet player ordering.
    /// </summary>
    private static int _forcedStationIndex = -1;

    public static int forcedStationIndex
    {
        get => _forcedStationIndex;
        set
        {
            _forcedStationIndex = value;
            Debug.Log($"[GameSettings] forcedStationIndex set to {_forcedStationIndex} ( -1 = disabled )");
        }
    }

    /// <summary>
    /// If not null/empty, overrides which avatar prefab to spawn in multiplayer.
    /// Otherwise defaults to whatever PlayerSpawnManager is set to use.
    /// </summary>
    private static string _avatarPrefabOverride = "";

    public static string avatarPrefabOverride
    {
        get => _avatarPrefabOverride;
        set
        {
            _avatarPrefabOverride = value ?? "";
            Debug.Log($"[GameSettings] avatarPrefabOverride set to '{_avatarPrefabOverride}'");
        }
    }
}
