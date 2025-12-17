using System.Linq;
using UnityEngine;
using VelNet;

/// <summary>
/// Spawns the local player's avatar at a station based on their index in the room.
/// Uses VelNet's player list so every client makes the same decision.
/// Also moves the local XR rig (Meta XR camera rig) to the same spawn point.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Avatar Prefab")]
    [Tooltip("Name of the networked avatar prefab as registered in VelNetManager.prefabs (e.g., 'ExampleAvatar').")]
    public string avatarPrefabName = "ExampleAvatar";

    [Header("Spawn Points")]
    [Tooltip("Spawn positions for players. Index 0 = first player, 1 = second player, etc.")]
    public Transform[] spawnPoints;

    [Header("Local XR Rig")]
    [Tooltip("Root transform of the local XR rig (Meta XR Origin / Camera Rig). If not set, will try to find LocalXRTargets in the scene.")]
    public Transform xrRigRoot;

    private bool _spawned;

    private void OnEnable()
    {
        VelNetManager.OnJoinedRoom += HandleJoinedRoom;
    }

    private void OnDisable()
    {
        VelNetManager.OnJoinedRoom -= HandleJoinedRoom;
    }

    private void Start()
    {
        // If we already joined the room in an earlier scene (MP_Loading), OnJoinedRoom may
        // have already fired before this script existed. In that case, just spawn now.
        if (VelNetManager.InRoom)
        {
            TrySpawnLocalPlayer();
        }

        // Try to auto assign XR rig if not set.
        if (xrRigRoot == null)
        {
            var localTargets = FindAnyObjectByType<LocalXRTargets>();
            if (localTargets != null)
            {
                xrRigRoot = localTargets.transform;
                Debug.Log("[PlayerSpawnManager] Auto assigned xrRigRoot from LocalXRTargets.");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnManager] xrRigRoot is not assigned and no LocalXRTargets found. " +
                                 "Camera rig will not be repositioned.");
            }
        }
    }

    private void HandleJoinedRoom(string roomName)
    {
        // This will fire if the player joins the room while this scene is loaded.
        TrySpawnLocalPlayer();
    }

    private void TrySpawnLocalPlayer()
    {
        if (_spawned)
        {
            return;
        }

        if (!VelNetManager.InRoom || VelNetManager.LocalPlayer == null)
        {
            Debug.LogWarning("[PlayerSpawnManager] Not in a room or LocalPlayer is null. Cannot spawn yet.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawnManager] No spawnPoints configured.");
            return;
        }

        // Sort players by userid so every client agrees on the order.
        var orderedPlayers = VelNetManager.Players
            .OrderBy(p => p.userid)
            .ToList();

        int index = orderedPlayers.FindIndex(p => p.isLocal);
        if (index < 0)
        {
            Debug.LogError("[PlayerSpawnManager] LocalPlayer not found in player list.");
            return;
        }

        // Clamp to available spawn points
        if (index >= spawnPoints.Length)
        {
            Debug.LogWarning(
                $"[PlayerSpawnManager] Not enough spawn points for player index {index}. " +
                "Using the last spawn point as a fallback."
            );
            index = spawnPoints.Length - 1;
        }

        Transform spawn = spawnPoints[index];
        if (spawn == null)
        {
            Debug.LogError($"[PlayerSpawnManager] Spawn point at index {index} is null.");
            return;
        }

        Debug.Log($"[PlayerSpawnManager] Spawning local player at index {index} at '{spawn.name}'.");

        // Move the local XR rig so the camera starts at the correct station.
        if (xrRigRoot != null)
        {
            xrRigRoot.position = spawn.position;
            xrRigRoot.rotation = spawn.rotation;
            Debug.Log("[PlayerSpawnManager] Moved XR rig to spawn point.");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawnManager] xrRigRoot is null. Camera rig was not moved.");
        }

        // Instantiate the networked avatar so others see it.
        NetworkObject avatar = VelNetManager.NetworkInstantiate(
            avatarPrefabName,
            spawn.position,
            spawn.rotation
        );

        if (avatar == null)
        {
            Debug.LogError("[PlayerSpawnManager] NetworkInstantiate returned null. Check prefab name and VelNetManager.prefabs.");
        }
        else
        {
            Debug.Log($"[PlayerSpawnManager] Spawned avatar instance '{avatar.name}'.");
        }

        _spawned = true;
    }
}
