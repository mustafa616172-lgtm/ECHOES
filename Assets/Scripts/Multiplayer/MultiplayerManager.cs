using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// ECHOES - Multiplayer Manager
/// Multiplayer modda network baglanti yonetimi.
/// PlayerPrefs'ten Host/Client bilgisini okur ve otomatik baslatir.
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private string defaultIP = "127.0.0.1";
    
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;
    
    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool isInitialized = false;

    void Awake()
    {
        Debug.Log("[MultiplayerManager] Awake called - Mode: " + GameModeManager.CurrentMode);
    }

    void Start()
    {
        // Use Start instead of OnEnable for more reliable timing
        TryInitialize();
    }

    void OnEnable()
    {
        // Also try on enable in case object was disabled then enabled
        TryInitialize();
    }

    void TryInitialize()
    {
        // Check if we're in multiplayer mode
        if (!GameModeManager.IsMultiplayer)
        {
            Debug.Log("[MultiplayerManager] Not in Multiplayer mode - disabling");
            gameObject.SetActive(false);
            return;
        }
        
        // Only initialize once
        if (isInitialized) return;
        
        Debug.Log("[MultiplayerManager] Initializing...");
        
        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[MultiplayerManager] NetworkManager not found! Waiting...");
            StartCoroutine(WaitForNetworkManager());
            return;
        }
        
        CompleteInitialization();
    }

    System.Collections.IEnumerator WaitForNetworkManager()
    {
        float timeout = 5f;
        float waited = 0f;
        
        while (NetworkManager.Singleton == null && waited < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }
        
        if (NetworkManager.Singleton != null)
        {
            networkManager = NetworkManager.Singleton;
            CompleteInitialization();
        }
        else
        {
            Debug.LogError("[MultiplayerManager] NetworkManager not found after waiting!");
        }
    }

    void CompleteInitialization()
    {
        if (isInitialized) return;
        
        transport = networkManager.GetComponent<UnityTransport>();
        
        // Register callbacks
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        
        // Auto-start based on PlayerPrefs
        AutoStartNetwork();
        
        isInitialized = true;
        Debug.Log("[MultiplayerManager] Initialization complete!");
    }
    
    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    
    void AutoStartNetwork()
    {
        // Read from PlayerPrefs (set by MultiplayerPanel)
        int isHost = PlayerPrefs.GetInt("MP_IsHost", 1);
        string serverIP = PlayerPrefs.GetString("MP_ServerIP", defaultIP);
        
        if (isHost == 1)
        {
            Debug.Log("[MultiplayerManager] Auto-starting as HOST");
            StartAsHost();
        }
        else
        {
            Debug.Log("[MultiplayerManager] Auto-starting as CLIENT to: " + serverIP);
            StartAsClient(serverIP);
        }
    }
    
    public void StartAsHost()
    {
        if (networkManager == null) return;
        
        if (transport != null)
        {
            transport.ConnectionData.Port = defaultPort;
        }
        
        Debug.Log("[MultiplayerManager] Starting Host on port: " + defaultPort);
        networkManager.StartHost();
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void StartAsClient(string ip)
    {
        if (networkManager == null) return;
        
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = defaultPort;
        }
        
        Debug.Log("[MultiplayerManager] Connecting to: " + ip + ":" + defaultPort);
        networkManager.StartClient();
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void Disconnect()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            Debug.Log("[MultiplayerManager] Disconnecting...");
            networkManager.Shutdown();
        }
    }
    
    void OnClientConnected(ulong clientId)
    {
        Debug.Log("[MultiplayerManager] Client connected: " + clientId);
        
        if (networkManager.IsHost && clientId == networkManager.LocalClientId)
        {
            Debug.Log("[MultiplayerManager] Host connected - local client ID: " + clientId);
        }
    }
    
    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("[MultiplayerManager] Client disconnected: " + clientId);
    }
    
    // Public getters
    public bool IsHost => networkManager != null && networkManager.IsHost;
    public bool IsClient => networkManager != null && networkManager.IsClient;
    public bool IsConnected => networkManager != null && networkManager.IsListening;
    
    public string GetServerIP()
    {
        if (transport != null)
        {
            if (IsHost)
                return GetLocalIPAddress() + ":" + transport.ConnectionData.Port;
            else
                return transport.ConnectionData.Address + ":" + transport.ConnectionData.Port;
        }
        return "N/A";
    }
    
    string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception) { }
        return "127.0.0.1";
    }
}
