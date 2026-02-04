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
        }
        
        // Disable SinglePlayerManager
        if (singlePlayerManager != null)
        {
            singlePlayerManager.enabled = false;
            singlePlayerManager.gameObject.SetActive(false);
            Debug.Log("[GameSceneManager] SinglePlayerManager DISABLED");
        }
        
        // Enable MultiplayerManager
        if (multiplayerManagerObject != null)
        {
            multiplayerManagerObject.SetActive(true);
            Debug.Log("[GameSceneManager] MultiplayerManager ENABLED");
        }
        
        Debug.Log("[GameSceneManager] Multiplayer setup COMPLETE");
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
