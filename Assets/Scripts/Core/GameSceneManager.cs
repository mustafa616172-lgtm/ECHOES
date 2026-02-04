using UnityEngine;
using Unity.Netcode;

/// <summary>
/// ECHOES - Game Scene Manager
/// Oyun sahnesinde moda gore yoneticileri aktif eder.
/// Single Player: NetworkManager KAPATILIR, player offline spawn edilir
/// Multiplayer: NetworkManager ACIK kalir, network player beklenir
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private SinglePlayerManager singlePlayerManager;
    [SerializeField] private GameObject multiplayerManagerObject;
    
    [Header("Network")]
    [SerializeField] private bool autoFindNetworkManager = true;
    [SerializeField] private bool disableNetworkInSinglePlayer = true;
    
    private NetworkManager networkManager;

    void Awake()
    {
        Debug.Log("[GameSceneManager] Awake - Current Mode: " + GameModeManager.CurrentMode);
        
        if (autoFindNetworkManager)
            networkManager = FindObjectOfType<NetworkManager>();
        
        // Auto-find managers if not assigned
        if (singlePlayerManager == null)
            singlePlayerManager = FindObjectOfType<SinglePlayerManager>();
        
        if (multiplayerManagerObject == null)
        {
            var mpm = FindObjectOfType<MultiplayerManager>();
            if (mpm != null) multiplayerManagerObject = mpm.gameObject;
        }
    }

    void Start()
    {
        Debug.Log("[GameSceneManager] Starting setup...");
        SetupForGameMode();
    }
    
    void SetupForGameMode()
    {
        // If no mode is set, default to Single Player
        if (!GameModeManager.IsModeSet)
        {
            Debug.LogWarning("[GameSceneManager] No mode set - defaulting to Single Player");
            GameModeManager.StartSinglePlayer();
        }
        
        if (GameModeManager.IsSinglePlayer)
        {
            SetupSinglePlayer();
        }
        else if (GameModeManager.IsMultiplayer)
        {
            SetupMultiplayer();
        }
    }
    
    void SetupSinglePlayer()
    {
        Debug.Log("========================================");
        Debug.Log("[GameSceneManager] SINGLE PLAYER MODE");
        Debug.Log("========================================");
        
        // Disable NetworkManager
        if (disableNetworkInSinglePlayer && networkManager != null)
        {
            // Shutdown if running
            if (networkManager.IsClient || networkManager.IsServer || networkManager.IsHost)
            {
                Debug.Log("[GameSceneManager] Shutting down NetworkManager");
                networkManager.Shutdown();
            }
            
            // Disable the GameObject entirely
            networkManager.enabled = false;
            networkManager.gameObject.SetActive(false);
            Debug.Log("[GameSceneManager] NetworkManager DISABLED");
        }
        
        // Enable SinglePlayerManager
        if (singlePlayerManager != null)
        {
            singlePlayerManager.gameObject.SetActive(true);
            singlePlayerManager.enabled = true;
            Debug.Log("[GameSceneManager] SinglePlayerManager ENABLED");
        }
        else
        {
            // Create one if it doesn't exist
            Debug.Log("[GameSceneManager] Creating SinglePlayerManager");
            GameObject go = new GameObject("SinglePlayerManager");
            singlePlayerManager = go.AddComponent<SinglePlayerManager>();
        }
        
        // Disable MultiplayerManager
        if (multiplayerManagerObject != null)
        {
            multiplayerManagerObject.SetActive(false);
            Debug.Log("[GameSceneManager] MultiplayerManager DISABLED");
        }
        
        Debug.Log("[GameSceneManager] Single Player setup COMPLETE");
    }
    
    void SetupMultiplayer()
    {
        Debug.Log("========================================");
        Debug.Log("[GameSceneManager] MULTIPLAYER MODE");
        Debug.Log("========================================");
        
        // Enable NetworkManager
        if (networkManager != null)
        {
            networkManager.gameObject.SetActive(true);
            networkManager.enabled = true;
            Debug.Log("[GameSceneManager] NetworkManager ENABLED");
        }
        else
        {
            Debug.LogError("[GameSceneManager] NetworkManager not found!");
            return;
        }
        
        // Disable SinglePlayerManager
        if (singlePlayerManager != null)
        {
            singlePlayerManager.enabled = false;
            singlePlayerManager.gameObject.SetActive(false);
            Debug.Log("[GameSceneManager] SinglePlayerManager DISABLED");
        }
        
        // Find or create MultiplayerManager
        if (multiplayerManagerObject == null)
        {
            var existingMM = FindObjectOfType<MultiplayerManager>();
            if (existingMM != null)
            {
                multiplayerManagerObject = existingMM.gameObject;
            }
            else
            {
                // Create new MultiplayerManager
                Debug.Log("[GameSceneManager] Creating new MultiplayerManager");
                multiplayerManagerObject = new GameObject("MultiplayerManager");
                multiplayerManagerObject.AddComponent<MultiplayerManager>();
            }
        }
        
        // Enable MultiplayerManager
        multiplayerManagerObject.SetActive(true);
        
        // Ensure the component is enabled and Start gets called
        var mmComponent = multiplayerManagerObject.GetComponent<MultiplayerManager>();
        if (mmComponent != null)
        {
            mmComponent.enabled = true;
            Debug.Log("[GameSceneManager] MultiplayerManager ENABLED and ready");
        }
        
        // Ensure InGameMenu exists for ESC functionality
        EnsureInGameMenu();
        
        Debug.Log("[GameSceneManager] Multiplayer setup COMPLETE");
    }
    
    void EnsureInGameMenu()
    {
        // Check if InGameMenu already exists
        InGameMenu existingMenu = FindObjectOfType<InGameMenu>();
        if (existingMenu != null)
        {
            Debug.Log("[GameSceneManager] InGameMenu already exists");
            return;
        }
        
        Debug.Log("[GameSceneManager] Creating InGameMenu at runtime...");
        
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
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
        GameObject ipText = CreateMenuText(menuPanel.transform, "ServerIPText", "Sunucu IP: -", 18, new Vector2(0, 70));
        
        // Status
        GameObject statusText = CreateMenuText(menuPanel.transform, "StatusText", "Durum: -", 18, new Vector2(0, 40));
        
        // Player Count
        GameObject countText = CreateMenuText(menuPanel.transform, "PlayerCountText", "Oyuncular: -", 18, new Vector2(0, 10));
        
        // Resume Button
        GameObject resumeBtn = CreateMenuButton(menuPanel.transform, "ResumeButton", "[ DEVAM ET ]", new Vector2(0, -50));
        
        // Exit Button
        GameObject exitBtn = CreateMenuButton(menuPanel.transform, "ExitButton", "[ ANA MENU ]", new Vector2(0, -120));
        
        // Connect references via reflection (since we can't use SerializedObject at runtime)
        var igmType = typeof(InGameMenu);
        SetPrivateField(igm, "menuPanel", menuPanel);
        SetPrivateField(igm, "serverIPText", ipText.GetComponent<TMPro.TextMeshProUGUI>());
        SetPrivateField(igm, "connectionStatusText", statusText.GetComponent<TMPro.TextMeshProUGUI>());
        SetPrivateField(igm, "playerCountText", countText.GetComponent<TMPro.TextMeshProUGUI>());
        SetPrivateField(igm, "resumeButton", resumeBtn.GetComponent<UnityEngine.UI.Button>());
        SetPrivateField(igm, "exitButton", exitBtn.GetComponent<UnityEngine.UI.Button>());
        
        menuPanel.SetActive(false);
        
        Debug.Log("[GameSceneManager] InGameMenu created! Press ESC to open.");
    }
    
    void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
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
    
    // Public method to switch modes at runtime (for testing)
    public void SwitchToSinglePlayer()
    {
        GameModeManager.StartSinglePlayer();
        SetupSinglePlayer();
    }
    
    public void SwitchToMultiplayer()
    {
        GameModeManager.StartMultiplayer();
        SetupMultiplayer();
    }
}
