using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

/// <summary>
/// ECHOES - In Game Menu (ESC Menu)
/// Oyun icinde ESC'ye basinca acilan menu.
/// Sunucu IP bilgisi, baglanti durumu ve cikis butonu.
/// </summary>
public class InGameMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI serverIPText;
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button resumeButton;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private bool isMenuOpen = false;
    private NetworkManager networkManager;
    private bool isInitialized = false;

    void Start()
    {
        // Use delayed init to handle runtime creation
        StartCoroutine(DelayedInit());
    }
    
    System.Collections.IEnumerator DelayedInit()
    {
        // Wait a frame for references to be set via reflection
        yield return null;
        
        // ONLY work in Multiplayer mode - disable completely otherwise
        if (!GameModeManager.IsMultiplayer)
        {
            Debug.Log("[InGameMenu] Not in Multiplayer mode - disabling InGameMenu");
            if (menuPanel != null)
                menuPanel.SetActive(false);
            enabled = false;
            yield break;
        }
        
        networkManager = NetworkManager.Singleton;
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
        
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        
        // Start with menu closed
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        isInitialized = true;
        Debug.Log("[InGameMenu] Initialized (Multiplayer mode)");
    }

    void Update()
    {
        // Don't process until initialized
        if (!isInitialized)
            return;
        
        // Double-check: only work in Multiplayer mode
        if (!GameModeManager.IsMultiplayer)
            return;
        
        // ESC key toggles menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[InGameMenu] ESC pressed - toggling menu");
            ToggleMenu();
        }
        
        // Update info while menu is open
        if (isMenuOpen)
        {
            UpdateMenuInfo();
        }
    }
    
    void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }
    
    public void OpenMenu()
    {
        isMenuOpen = true;
        
        if (menuPanel != null)
            menuPanel.SetActive(true);
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause time (optional)
        // Time.timeScale = 0f;
        
        UpdateMenuInfo();
        
        Debug.Log("[InGameMenu] Menu opened");
    }
    
    public void CloseMenu()
    {
        isMenuOpen = false;
        
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Resume time
        // Time.timeScale = 1f;
        
        Debug.Log("[InGameMenu] Menu closed");
    }
    
    void UpdateMenuInfo()
    {
        // Get network info
        if (networkManager != null && networkManager.IsListening)
        {
            // Server IP
            string ip = GetServerIP();
            if (serverIPText != null)
                serverIPText.text = "Sunucu IP: " + ip;
            
            // Connection status
            string status = GetConnectionStatus();
            if (connectionStatusText != null)
                connectionStatusText.text = "Durum: " + status;
            
            // Player count
            int playerCount = networkManager.ConnectedClients.Count;
            if (playerCountText != null)
                playerCountText.text = "Oyuncular: " + playerCount + "/4";
        }
        else
        {
            // Single player mode or not connected
            if (serverIPText != null)
                serverIPText.text = "Mod: Single Player";
            
            if (connectionStatusText != null)
                connectionStatusText.text = "Durum: Offline";
            
            if (playerCountText != null)
                playerCountText.text = "Oyuncu: 1";
        }
    }
    
    string GetServerIP()
    {
        if (networkManager == null) return "N/A";
        
        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            if (networkManager.IsHost || networkManager.IsServer)
            {
                // Get local IP
                string localIP = GetLocalIPAddress();
                return localIP + ":" + transport.ConnectionData.Port;
            }
            else
            {
                return transport.ConnectionData.Address + ":" + transport.ConnectionData.Port;
            }
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
                    }
                    else if (localIP == null)
                    {
                        localIP = ipStr;
                    }
                }
            }
            
            // Return Hamachi IP if found, otherwise local IP
            if (!string.IsNullOrEmpty(hamachiIP))
                return hamachiIP;
            if (!string.IsNullOrEmpty(localIP))
                return localIP;
        }
        catch (System.Exception) { }
        
        return "127.0.0.1";
    }
    
    string GetConnectionStatus()
    {
        if (networkManager == null) return "Offline";
        
        if (networkManager.IsHost)
            return "Host (Sunucu)";
        else if (networkManager.IsServer)
            return "Server";
        else if (networkManager.IsClient)
            return "Client (Bagli)";
        else
            return "Bagli Degil";
    }
    
    void OnExitClicked()
    {
        Debug.Log("[InGameMenu] Exit clicked - returning to main menu");
        
        // Shutdown network
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
        
        // Reset game mode
        GameModeManager.Reset();
        
        // Resume time
        Time.timeScale = 1f;
        
        // Load main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    void OnResumeClicked()
    {
        CloseMenu();
    }
    
    public bool IsMenuOpen => isMenuOpen;
}
