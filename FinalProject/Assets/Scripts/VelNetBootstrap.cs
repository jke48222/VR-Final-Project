using UnityEngine;
using UnityEngine.SceneManagement;
using VelNet;

/// <summary>
/// Persistent bootstrapper for VelNet and scene initialization.
/// - Lives in the very first scene only (your loading or intro scene).
/// - Keeps VelNetManager alive across all scene loads.
/// - Connects to the VelNet server, waits for login, then loads MainMenu.
/// </summary>
[DisallowMultipleComponent]
public class VelNetBootstrap : MonoBehaviour
{
    [Header("Startup Scene")]
    [Tooltip("Name of the scene to load after VelNet login succeeds.")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("VelNet Server Settings")]
    [Tooltip("Hostname or IP of the VelNet server.")]
    [SerializeField] private string serverHost = "vn.ugavel.com";

    [Tooltip("Port for the VelNet server.")]
    [SerializeField] private int serverPort = 5002;

    [Header("Behavior")]
    [Tooltip("If true, only load the menu after a successful VelNet login.")]
    [SerializeField] private bool waitForLoginBeforeMenu = true;

    private bool _sceneLoaded;
    private bool _subscribed;

    private void Awake()
    {
        // Make this object (and any attached VelNetManager) persistent.
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("[VelNetBootstrap] menuSceneName is empty. Scene loading will fail.");
        }

        // Ensure there is a VelNetManager in this scene.
        if (VelNetManager.instance == null)
        {
            Debug.LogError("[VelNetBootstrap] No VelNetManager instance found in the scene. " +
                           "Add VelNetManager to this GameObject or another object in the loading scene.");
        }
    }

    private void OnEnable()
    {
        // Subscribe to VelNet callbacks once.
        if (_subscribed) return;
        _subscribed = true;

        VelNetManager.OnConnectedToServer += HandleConnected;
        VelNetManager.OnFailedToConnectToServer += HandleFailedToConnect;
        VelNetManager.OnDisconnectedFromServer += HandleDisconnected;
        VelNetManager.OnLoggedIn += HandleLoggedIn;
    }

    private void OnDisable()
    {
        if (!_subscribed) return;
        _subscribed = false;

        VelNetManager.OnConnectedToServer -= HandleConnected;
        VelNetManager.OnFailedToConnectToServer -= HandleFailedToConnect;
        VelNetManager.OnDisconnectedFromServer -= HandleDisconnected;
        VelNetManager.OnLoggedIn -= HandleLoggedIn;
    }

    private void Start()
    {
        // Configure the server at startup.
        if (VelNetManager.instance != null)
        {
            Debug.Log($"[VelNetBootstrap] Setting VelNet server to {serverHost}:{serverPort}.");
            VelNetManager.SetServer(serverHost, serverPort);
        }

        // VelNetManager.ConnectToServer is already called in its OnEnable,
        // so usually you do not need to call it here.
        // If you disabled auto connect in the inspector, you can force it:
        // VelNetManager.ConnectToServer();

        if (!waitForLoginBeforeMenu)
        {
            // Old behavior: go straight to MainMenu regardless of VelNet.
            LoadMenuSceneIfNeeded();
        }
    }

    #region VelNet Callbacks

    private void HandleConnected()
    {
        Debug.Log("[VelNetBootstrap] Connected to VelNet server. Waiting for login...");
        // If VelNetManager.autoLogin is true in the inspector, login happens automatically.
    }

    private void HandleFailedToConnect()
    {
        Debug.LogWarning("[VelNetBootstrap] Failed to connect to VelNet server.");

        if (!waitForLoginBeforeMenu)
        {
            // You chose not to block on networking, so still go to menu.
            LoadMenuSceneIfNeeded();
        }
        else
        {
            // Optional: you could show a "Play offline" popup here.
            Debug.LogWarning("[VelNetBootstrap] Waiting for login is enabled. " +
                             "You may want to fall back to offline mode here.");
        }
    }

    private void HandleDisconnected()
    {
        Debug.LogWarning("[VelNetBootstrap] Disconnected from VelNet server.");
        // You can keep the user in the current scene, or show a reconnect UI.
    }

    private void HandleLoggedIn()
    {
        Debug.Log("[VelNetBootstrap] Logged in to VelNet. Loading menu scene.");
        // Once we know we have a userId, it is safe to join rooms from later scripts.
        if (waitForLoginBeforeMenu)
        {
            LoadMenuSceneIfNeeded();
        }
    }

    #endregion

    private void LoadMenuSceneIfNeeded()
    {
        if (_sceneLoaded)
        {
            return;
        }

        _sceneLoaded = true;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("[VelNetBootstrap] Cannot load menu scene, name is empty.");
            return;
        }

        Debug.Log($"[VelNetBootstrap] Bootstrapping complete, loading scene '{menuSceneName}'.");
        SceneManager.LoadScene(menuSceneName);
    }
}
