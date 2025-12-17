using UnityEngine;
using VelNet;

public class VelNetConfigurator : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private string serverHost = "127.0.0.1";
    [SerializeField] private int serverPort = 8080;
    
    [Header("Fallback Options")]
    [SerializeField] private bool useOfflineModeWhenUnavailable = true;
    [SerializeField] private float connectionTimeout = 5f;
    
    void Start()
    {
        ConfigureVelNet();
        TestConnection();
    }
    
    private void ConfigureVelNet()
    {
        var velNetManager = FindAnyObjectByType<VelNetManager>();
        if (velNetManager != null)
        {
            Debug.Log($"[VelNetConfigurator] Setting server to {serverHost}:{serverPort}");
            
            // Set the server configuration
            velNetManager.host = serverHost;
            velNetManager.port = serverPort;
            
            // Enable offline mode fallback
            velNetManager.autoSwitchToOfflineMode = useOfflineModeWhenUnavailable;
            
            // Optional: Adjust keep-alive interval
            velNetManager.keepAliveInterval = 3f;
            velNetManager.autoReconnect = true;
        }
        else
        {
            Debug.LogError("[VelNetConfigurator] No VelNetManager found in scene!");
        }
    }
    
    private async void TestConnection()
    {
        Debug.Log("[VelNetConfigurator] Testing server connection...");
        
        bool connected = await TestServerConnection(serverHost, serverPort);
        
        if (connected)
        {
            Debug.Log($"✅ VelNet server is reachable at {serverHost}:{serverPort}");
        }
        else
        {
            Debug.LogWarning($"❌ Cannot connect to VelNet server at {serverHost}:{serverPort}");
            
            if (useOfflineModeWhenUnavailable)
            {
                Debug.Log("[VelNetConfigurator] Will use offline mode as fallback");
            }
        }
    }
    
    private async System.Threading.Tasks.Task<bool> TestServerConnection(string host, int port)
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = System.Threading.Tasks.Task.Delay((int)(connectionTimeout * 1000));
                
                var completedTask = await System.Threading.Tasks.Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask)
                {
                    return client.Connected;
                }
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VelNetConfigurator] Connection test failed: {e.Message}");
            return false;
        }
    }
}