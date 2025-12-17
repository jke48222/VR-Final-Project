using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelNet;

[DisallowMultipleComponent]
public class CombinedNetworkGameManager : MonoBehaviour
{
	[Header("Room Config")]
	[Tooltip("Name of the VelNet room all players should join.")]
	public string roomName = "ChaoticKitchen";

	[Header("Minimum players before game starts")]
	[Tooltip("Number of players required in the room before loading the game scene.")]
	public int requiredPlayers = 2;

	[Header("Start Delay")]
	[Tooltip("Delay (in seconds) after room is ready before loading the game scene.")]
	public float startDelaySeconds = 2f;

	[Header("Game Scene")]
	[Tooltip("Name of the multiplayer kitchen gameplay scene.")]
	public string gameSceneName = "ChaoticKitchen_Multiplayer";

	[Header("Avatar")]
	[Tooltip("If true, will spawn the selected avatar prefab once in the game scene.")]
	public bool spawnAvatarInGameScene = true;

	private bool joinedRoom;
	private bool loadingStarted;
	private NetworkObject avatarInstance;

	private void Awake()
	{
		if (requiredPlayers < 1)
		{
			Debug.LogWarning("[CombinedNetworkGameManager] requiredPlayers was less than 1. Clamping to 1.");
			requiredPlayers = 1;
		}

		if (string.IsNullOrWhiteSpace(roomName))
		{
			Debug.LogWarning("[CombinedNetworkGameManager] roomName is empty. JoinRoom will fail.");
		}

		if (string.IsNullOrWhiteSpace(gameSceneName))
		{
			Debug.LogWarning("[CombinedNetworkGameManager] gameSceneName is empty. Scene loading will fail.");
		}
	}

	private void OnEnable()
	{
		VelNetManager.OnLoggedIn += HandleLoggedIn;
		VelNetManager.OnJoinedRoom += HandleJoinedRoom;
		VelNetManager.OnPlayerJoined += HandlePlayerJoined;
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDisable()
	{
		VelNetManager.OnLoggedIn -= HandleLoggedIn;
		VelNetManager.OnJoinedRoom -= HandleJoinedRoom;
		VelNetManager.OnPlayerJoined -= HandlePlayerJoined;
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	private void Start()
	{
		// If VelNetManager already logged in (because it survives scenes), skip waiting
		VelNetManager manager = FindFirstObjectByType<VelNetManager>();
		if (manager == null)
		{
			Debug.LogError("[CombinedNetworkGameManager] No VelNetManager found in scene. Multiplayer cannot start.");
			return;
		}

		if (manager.userid != -1)
		{
			Debug.Log("[CombinedNetworkGameManager] Already logged in to VelNet. Proceeding to JoinRoom.");
			HandleLoggedIn();
		}
		else
		{
			Debug.Log("[CombinedNetworkGameManager] Waiting for VelNet login...");
		}
	}

	// ------------- VELNET CALLBACKS -------------

	private void HandleLoggedIn()
	{
		if (joinedRoom) return;

		joinedRoom = true;

		if (string.IsNullOrWhiteSpace(roomName))
		{
			Debug.LogError("[CombinedNetworkGameManager] Cannot join room. roomName is empty.");
			return;
		}

		Debug.Log($"[CombinedNetworkGameManager] Logged in. Joining room '{roomName}'...");
		VelNetManager.JoinRoom(roomName);
	}

	private void HandleJoinedRoom(string room)
	{
		Debug.Log($"[CombinedNetworkGameManager] Joined room '{room}'. Current players: {VelNetManager.Players.Count}");

		// We might already be in the game scene if you skip a loading scene
		TryStartGame();
		TrySpawnAvatarIfReady();
	}

	private void HandlePlayerJoined(VelNetPlayer player, bool wasAlreadyInRoom)
	{
		Debug.Log($"[CombinedNetworkGameManager] Player joined. " +
		          $"Id={player.userid}, " +
		          $"WasAlreadyInRoom={wasAlreadyInRoom}. " +
		          $"Total players now: {VelNetManager.Players.Count}");

		TryStartGame();
	}

	// ------------- SCENE LOADING AND AVATAR SPAWN -------------

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// When the multiplayer gameplay scene loads, spawn the avatar if conditions are met
		if (scene.name == gameSceneName)
		{
			Debug.Log($"[CombinedNetworkGameManager] Scene loaded: {scene.name}. Checking avatar spawn.");
			TrySpawnAvatarIfReady();
		}
	}

	/// <summary>
	/// Checks whether we have enough players and, if so, schedules loading the game scene.
	/// This is called when we join the room or when players join.
	/// </summary>
	private void TryStartGame()
	{
		if (loadingStarted) return;

		int currentPlayers = VelNetManager.Players.Count;
		if (currentPlayers < requiredPlayers)
		{
			Debug.Log($"[CombinedNetworkGameManager] Not enough players yet ({currentPlayers}/{requiredPlayers}). Waiting...");
			return;
		}

		loadingStarted = true;
		Debug.Log($"[CombinedNetworkGameManager] Required players reached ({currentPlayers}/{requiredPlayers}). " +
		          $"Starting game in {startDelaySeconds:F1} seconds.");

		StartCoroutine(LoadGameAfterDelay());
	}

	private IEnumerator LoadGameAfterDelay()
	{
		if (startDelaySeconds > 0f)
		{
			yield return new WaitForSeconds(startDelaySeconds);
		}

		if (string.IsNullOrWhiteSpace(gameSceneName))
		{
			Debug.LogError("[CombinedNetworkGameManager] Cannot load game scene. gameSceneName is empty.");
			yield break;
		}

		Debug.Log($"[CombinedNetworkGameManager] Loading multiplayer game scene '{gameSceneName}'...");
		SceneManager.LoadScene(gameSceneName);
	}

	/// <summary>
	/// Spawns the avatar once, after:
	/// - We are in the target game scene
	/// - VelNetManager is in a room
	/// </summary>
	private void TrySpawnAvatarIfReady()
	{
		if (!spawnAvatarInGameScene) return;
		if (avatarInstance != null) return;

		Scene activeScene = SceneManager.GetActiveScene();
		if (activeScene.name != gameSceneName)
		{
			// Not in the gameplay scene yet
			return;
		}

		if (!VelNetManager.InRoom)
		{
			// Not in a room yet
			Debug.Log("[CombinedNetworkGameManager] In game scene but not in a room yet. Waiting for join.");
			return;
		}

		string avatarPrefabName = AvatarRegistry.GetPrefabName(GameSettings.selectedSkinIndex);
		Debug.Log($"[CombinedNetworkGameManager] Spawning avatar prefab '{avatarPrefabName}'.");

		avatarInstance = VelNetManager.NetworkInstantiate(avatarPrefabName);
		if (avatarInstance == null)
		{
			Debug.LogError("[CombinedNetworkGameManager] Failed to instantiate avatar. " +
			               "Check that the prefab is in VelNetManager.prefabs and has NetworkObject.");
		}
	}
}

// Keep your existing registry as is
public static class AvatarRegistry
{
	private static readonly string[] prefabNames =
	{
		"ChefSkin0",
		"ChefSkin1",
		"ChefSkin2",
		"ChefSkin3",
		"ChefSkin4"
	};

	public static string GetPrefabName(int index)
	{
		if (prefabNames == null || prefabNames.Length == 0)
		{
			Debug.LogError("[AvatarRegistry] No avatar prefab names configured.");
			return "ChefSkin0";
		}

		if (index < 0 || index >= prefabNames.Length)
		{
			index = 0;
		}

		return prefabNames[index];
	}
}
