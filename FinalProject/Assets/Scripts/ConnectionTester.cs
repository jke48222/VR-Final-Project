using UnityEngine;
using VelNet;
using System.Linq;

public class ConnectionTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private float testInterval = 5f;
    
    void Start()
    {
        if (runOnStart)
        {
            TestConnectionNow();
        }
        
        // Optional: Test periodically
        if (testInterval > 0)
        {
            InvokeRepeating(nameof(TestConnectionNow), testInterval, testInterval);
        }
    }
    
    [ContextMenu("Test Connection Now")]
    public void TestConnectionNow()
    {
        Debug.Log("=== VELNET CONNECTION TEST ===");
        
        // Test 1: Check if VelNetManager exists
        var manager = FindFirstObjectByType<VelNetManager>();
        if (manager == null)
        {
            Debug.LogError("❌ No VelNetManager found in scene!");
            return;
        }
        
        Debug.Log($"✅ Found VelNetManager: {manager.gameObject.name}");
        
        // Test 2: Show current configuration
        Debug.Log($"Current Configuration:");
        Debug.Log($"- Host: '{manager.host}'");
        Debug.Log($"- Port: {manager.port}");
        Debug.Log($"- Auto Switch Offline: {manager.autoSwitchToOfflineMode}");
        Debug.Log($"- Auto Reconnect: {manager.autoReconnect}");
        Debug.Log($"- Keep Alive Interval: {manager.keepAliveInterval}s");
        Debug.Log($"- User ID: {manager.userid}");
        Debug.Log($"- Connected: {manager.connected}");
        Debug.Log($"- UDP Connected: {manager.udpConnected}");

        // Test 3: Test TCP connection to server
        TestTcpConnection(manager.host, manager.port);
        
        // Test 4: Check if in room
        if (VelNetManager.InRoom)
        {
            Debug.Log($"✅ In Room: '{VelNetManager.Room}'");
            Debug.Log($"✅ Player Count: {VelNetManager.PlayerCount}");
            
            // Show all players in room
            var players = VelNetManager.Players;
            if (players != null)
            {
                Debug.Log($"Players in room ({players.Count}):");
                foreach (var player in players)
                {
                    if (player != null)
                    {
                        Debug.Log($"- Player {player.userid} (Local: {player.isLocal})");
                    }
                }
            }
        }
        else
        {
            Debug.Log("⚠️ Not in a room");
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    private async void TestTcpConnection(string host, int port)
    {
        if (string.IsNullOrEmpty(host))
        {
            Debug.LogError("❌ Host is empty!");
            return;
        }
        
        if (port <= 0 || port > 65535)
        {
            Debug.LogError($"❌ Invalid port: {port}");
            return;
        }
        
        Debug.Log($"Testing TCP connection to {host}:{port}...");
        
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = System.Threading.Tasks.Task.Delay(2000); // 2 second timeout
                
                if (await System.Threading.Tasks.Task.WhenAny(connectTask, timeoutTask) == connectTask)
                {
                    if (client.Connected)
                    {
                        Debug.Log($"✅ TCP Connection SUCCESS to {host}:{port}");
                        client.Close();
                    }
                    else
                    {
                        Debug.LogError($"❌ TCP Connection FAILED (client not connected)");
                    }
                }
                else
                {
                    Debug.LogError($"❌ TCP Connection TIMEOUT after 2 seconds");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ TCP Connection ERROR: {e.Message}");
        }
    }
    
    // Editor helper method - FIXED VERSION
    [ContextMenu("Find All VelNet Components")]
    private void FindAllVelNetComponents()
    {
        Debug.Log("=== FINDING ALL VELNET COMPONENTS ===");
        
        // Find VelNetManager (Unity Object)
        var managers = FindObjectsByType<VelNetManager>(FindObjectsSortMode.None);
        Debug.Log($"Found {managers.Length} VelNetManager(s):");
        foreach (var manager in managers)
        {
            Debug.Log($"- {manager.gameObject.name} (Host: {manager.host}, Port: {manager.port})");
            
            // Show players from the manager
            if (manager.players != null && manager.players.Count > 0)
            {
                Debug.Log($"  Players in manager: {manager.players.Count}");
                foreach (var kvp in manager.players)
                {
                    Debug.Log($"  - Player ID: {kvp.Key}, IsLocal: {kvp.Value?.isLocal}");
                }
            }
        }
        
        // Find NetworkObject components (Unity Objects)
        var networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        Debug.Log($"Found {networkObjects.Length} NetworkObject(s)");
        foreach (var netObj in networkObjects.Take(5)) // Show first 5
        {
            Debug.Log($"- {netObj.name} (ID: {netObj.networkId}, Owner: {netObj.owner?.userid})");
        }
        if (networkObjects.Length > 5)
        {
            Debug.Log($"- ... and {networkObjects.Length - 5} more");
        }
        
        Debug.Log("=== SEARCH COMPLETE ===");
    }
    
    // Additional helper: Check VelNet connectivity status
    [ContextMenu("Check VelNet Status")]
    private void CheckVelNetStatus()
    {
        Debug.Log("=== VELNET STATUS CHECK ===");
        
        if (VelNetManager.instance == null)
        {
            Debug.LogError("VelNetManager.instance is null!");
            return;
        }
        
        Debug.Log($"Is Connected: {VelNetManager.IsConnected}");
        Debug.Log($"In Room: {VelNetManager.InRoom}");
        Debug.Log($"Room Name: {VelNetManager.Room}");
        Debug.Log($"Player Count: {VelNetManager.PlayerCount}");
        Debug.Log($"Local Player: {VelNetManager.LocalPlayer?.userid}");
        Debug.Log($"Offline Mode: {VelNetManager.OfflineMode}");
        
        // Check if we can send a ping
        if (VelNetManager.IsConnected)
        {
            Debug.Log("✅ VelNet appears to be properly connected!");
        }
        else if (VelNetManager.OfflineMode)
        {
            Debug.Log("⚠️ VelNet is in offline mode");
        }
        else
        {
            Debug.Log("❌ VelNet is not connected and not in offline mode");
        }
        
        Debug.Log("=== STATUS CHECK COMPLETE ===");
    }
}