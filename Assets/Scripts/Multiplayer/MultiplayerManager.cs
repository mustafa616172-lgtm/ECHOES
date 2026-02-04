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
        
        // Create InGameMenu for ESC functionality
        EnsureInGameMenu();
        
        isInitialized = true;
        Debug.Log("[MultiplayerManager] Initialization complete!");
    }
    
    void EnsureInGameMenu()
    {
        // Check if InGameMenu already exists
        InGameMenu existingMenu = FindObjectOfType<InGameMenu>();
        if (existingMenu != null)
        {
            Debug.Log("[MultiplayerManager] InGameMenu already exists");
            return;
        }
        
        Debug.Log("[MultiplayerManager] Creating InGameMenu...");
        
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Make sure it's on top
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add EventSystem if not present
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        
        // Create InGameMenu parent
        GameObject menuObj = new GameObject("InGameMenu");
        menuObj.transform.SetParent(canvas.transform, false);
        InGameMenu igm = menuObj.AddComponent<InGameMenu>();
        
        // Create menu panel
        GameObject menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(menuObj.transform, false);
        
        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 350);
        
        UnityEngine.UI.Image panelBg = menuPanel.AddComponent<UnityEngine.UI.Image>();
        panelBg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        
        // Title
        CreateMenuText(menuPanel.transform, "Title", "OYUN MENUSU", 32, new Vector2(0, 130));
        
        // Server IP
        GameObject ipText = CreateMenuText(menuPanel.transform, "ServerIPText", "Sunucu IP: " + GetServerIP(), 18, new Vector2(0, 70));
        
        // Status
        GameObject statusText = CreateMenuText(menuPanel.transform, "StatusText", "Durum: Host", 18, new Vector2(0, 40));
        
        // Player Count
        GameObject countText = CreateMenuText(menuPanel.transform, "PlayerCountText", "Oyuncular: 1/4", 18, new Vector2(0, 10));
        
        // Resume Button
        GameObject resumeBtn = CreateMenuButton(menuPanel.transform, "ResumeButton", "[ DEVAM ET ]", new Vector2(0, -50));
        
        // Exit Button
        GameObject exitBtn = CreateMenuButton(menuPanel.transform, "ExitButton", "[ ANA MENU ]", new Vector2(0, -120));
        
        // Connect references via reflection
        SetPrivateField(igm, "menuPanel", menuPanel);
        SetPrivateField(igm, "serverIPText", ipText.GetComponent<TMPro.TextMeshProUGUI>());
        SetPrivateField(igm, "connectionStatusText", statusText.GetComponent<TMPro.TextMeshProUGUI>());
        SetPrivateField(igm, "playerCountText", countText.GetComponent<TMPro.TextMeshProUGUI>());
        SetPrivateField(igm, "resumeButton", resumeBtn.GetComponent<UnityEngine.UI.Button>());
        SetPrivateField(igm, "exitButton", exitBtn.GetComponent<UnityEngine.UI.Button>());
        
        menuPanel.SetActive(false);
        
        Debug.Log("[MultiplayerManager] InGameMenu created! Press ESC to open.");
    }
    
    void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }
    
    GameObject CreateMenuText(Transform parent, string name, string text, int fontSize, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(380, 40);
        rect.anchoredPosition = position;
        
        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        return textObj;
    }
    
    GameObject CreateMenuButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 50);
        rect.anchoredPosition = position;
        
        UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        btn.colors = colors;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.2f, 0.8f, 0.4f, 1f);
        
        return btnObj;
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
            string hamachiIP = null;
            string localIP = null;
            
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    
                    // Hamachi IPs start with 25.x.x.x - prefer these
                    if (ipStr.StartsWith("25."))
                    {
                        hamachiIP = ipStr;
                        Debug.Log("[MultiplayerManager] Found Hamachi IP: " + ipStr);
                    }
                    // Keep first local IP as fallback
                    else if (localIP == null)
                    {
                        localIP = ipStr;
                    }
                }
            }
            
            // Return Hamachi IP if found, otherwise local IP
            if (!string.IsNullOrEmpty(hamachiIP))
            {
                return hamachiIP;
            }
            if (!string.IsNullOrEmpty(localIP))
            {
                return localIP;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MultiplayerManager] Failed to get IP: " + e.Message);
        }
        return "127.0.0.1";
    }
}
