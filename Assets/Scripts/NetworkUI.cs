using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [Header("Butonlar")]
    [SerializeField] public Button hostButton;
    [SerializeField] public Button clientButton;
    [SerializeField] public Button disconnectButton;
    [SerializeField] public Button startGameButton;
    [SerializeField] public Button backToMenuButton;

    [Header("Input")]
    [SerializeField] public TMP_InputField ipInputField;

    [Header("Metinler")]
    [SerializeField] public TextMeshProUGUI statusText;
    [SerializeField] public TextMeshProUGUI playerCountText;

    [Header("Paneller")]
    [SerializeField] public GameObject mainMenuPanel;
    [SerializeField] public GameObject lobbyPanel;
    [SerializeField] public GameObject hudPanel;
    
    [Header("Main Menu Reference")]
    [SerializeField] public MenuManager menuManager;

    private bool isConnected = false;
    private bool lobbyVisible = false;

    void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);
        
        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);
        
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(Disconnect);
        
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMainMenu);

        // Check if coming from MultiplayerPanel (auto-started by MultiplayerManager)
        if (GameModeManager.IsMultiplayer)
        {
            Debug.Log("[NetworkUI] Multiplayer mode detected - waiting for network start...");
            StartCoroutine(WaitForNetworkStart());
        }
        else
        {
            ShowMainMenuPanel();
        }
        
        Debug.Log("[NetworkUI] Initialized");
    }
    
    System.Collections.IEnumerator WaitForNetworkStart()
    {
        // Wait a few frames for MultiplayerManager to start the network
        float timeout = 2f;
        float waited = 0f;
        
        while (waited < timeout)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                // Network is running - go directly to HUD
                isConnected = true;
                ShowHUDPanel();
                Debug.Log("[NetworkUI] Network auto-started - showing HUD!");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }
        
        // Timeout - show main menu panel as fallback
        Debug.LogWarning("[NetworkUI] Network did not start in time - showing main menu");
        ShowMainMenuPanel();
    }
    
    void ShowHUDPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[NetworkUI] Showing HUD Panel - Game active!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isConnected)
        {
            ToggleLobbyPanel();
        }

        if (isConnected && NetworkManager.Singleton != null)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            if (playerCountText != null)
            {
                playerCountText.text = "AJANLAR: " + playerCount + "/4";
            }
        }
    }

    void ToggleLobbyPanel()
    {
        lobbyVisible = !lobbyVisible;
        
        if (lobbyVisible)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void StartGame()
    {
        Debug.Log("[NetworkUI] Starting game!");
        lobbyVisible = false;
        
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkUI] NetworkManager not found!");
            return;
        }

        NetworkManager.Singleton.StartHost();
        isConnected = true;
        lobbyVisible = true;
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "> SUNUCU AKTIF...";

        Debug.Log("[NetworkUI] Host started!");
    }

    void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkUI] NetworkManager not found!");
            return;
        }

        string ip = "127.0.0.1";
        if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
        {
            ip = ipInputField.text;
        }

        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
        }

        NetworkManager.Singleton.StartClient();
        isConnected = true;
        lobbyVisible = true;
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "> BAGLANILIYOR...";

        Debug.Log("[NetworkUI] Client started! IP: " + ip);
    }

    public void Disconnect()
    {
        Debug.Log("[NetworkUI] Disconnecting!");
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        isConnected = false;
        lobbyVisible = false;
        
        ShowMainMenuPanel();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowMainMenuPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void BackToMainMenu()
    {
        Debug.Log("[NetworkUI] Back to main menu");
        
        GameModeManager.Reset();
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        
        if (menuManager != null)
        {
            menuManager.OpenSelectionMenu();
        }
        else
        {
            Debug.LogWarning("[NetworkUI] MenuManager reference not set!");
        }
    }
}
