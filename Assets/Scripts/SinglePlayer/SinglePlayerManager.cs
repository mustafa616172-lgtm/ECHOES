using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ECHOES - Single Player Manager
/// Single Player modda oyunu yonetir, player spawn eder.
/// SADECE oyun sahnesinde calisir (MainMenu'de calismaz!)
/// </summary>
public class SinglePlayerManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0, 2, 0);
    
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private GameObject currentPlayer;
    private bool initialized = false;

    void Awake()
    {
        // Check if we're in the main menu - if so, don't do anything!
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene.Contains("MainMenu") || currentScene.Contains("Menu"))
        {
            Debug.Log("[SinglePlayerManager] We're in MainMenu - disabling!");
            gameObject.SetActive(false);
            return;
        }
    }

    void Start()
    {
        // Double check - don't spawn in MainMenu
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.Contains("MainMenu") || currentScene.Contains("Menu"))
        {
            Debug.Log("[SinglePlayerManager] MainMenu detected - not spawning!");
            return;
        }
        
        // Only spawn if we're in Single Player mode
        if (!GameModeManager.IsSinglePlayer)
        {
            Debug.Log("[SinglePlayerManager] Not in Single Player mode - checking...");
            
            // If no mode is set and we're in a game scene, default to single player
            if (!GameModeManager.IsModeSet)
            {
                Debug.Log("[SinglePlayerManager] No mode set - defaulting to Single Player");
                GameModeManager.StartSinglePlayer();
            }
            else
            {
                Debug.Log("[SinglePlayerManager] Multiplayer mode - disabling");
                gameObject.SetActive(false);
                return;
            }
        }
        
        Debug.Log("[SinglePlayerManager] Spawning player...");
        SpawnPlayerNow();
    }
    
    void SpawnPlayerNow()
    {
        if (initialized) return;
        initialized = true;
        
        // Disable ALL existing cameras first
        Camera[] existingCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in existingCameras)
        {
            Debug.Log("[SinglePlayerManager] Disabling existing camera: " + cam.gameObject.name);
            cam.enabled = false;
        }
        
        // Also disable any AudioListeners
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        foreach (AudioListener listener in listeners)
        {
            listener.enabled = false;
        }
        
        // Get spawn position
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : defaultSpawnPosition;
        
        // Create player
        CreatePlayer(spawnPos);
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Hide any MainMenu UI that might be lingering
        HideMainMenuUI();
        
        Debug.Log("[SinglePlayerManager] === PLAYER SPAWNED - READY TO PLAY! ===");
    }
    
    void HideMainMenuUI()
    {
        // Find and disable MenuManager if present
        MenuManager menuManager = FindObjectOfType<MenuManager>();
        if (menuManager != null)
        {
            Debug.Log("[SinglePlayerManager] Hiding MenuManager canvas");
            Canvas canvas = menuManager.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.gameObject.SetActive(false);
            menuManager.gameObject.SetActive(false);
        }
        
        // Find and disable any MainMenu related objects
        GameObject mainMenuPanel = GameObject.Find("MainMenuPanel");
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            Debug.Log("[SinglePlayerManager] Hid MainMenuPanel");
        }
        
        // Hide NetworkUI panels if present
        NetworkUI networkUI = FindObjectOfType<NetworkUI>();
        if (networkUI != null)
        {
            if (networkUI.mainMenuPanel != null) networkUI.mainMenuPanel.SetActive(false);
            if (networkUI.lobbyPanel != null) networkUI.lobbyPanel.SetActive(false);
            Debug.Log("[SinglePlayerManager] Hid NetworkUI panels");
        }
        
        // Disable any other canvases that look like menus
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject.name.ToLower().Contains("menu") || 
                c.gameObject.name.ToLower().Contains("main"))
            {
                c.gameObject.SetActive(false);
                Debug.Log("[SinglePlayerManager] Disabled menu canvas: " + c.gameObject.name);
            }
        }
    }
    
    void CreatePlayer(Vector3 position)
    {
        // Check if playerPrefab is assigned
        if (playerPrefab != null)
        {
            Debug.Log("[SinglePlayerManager] Spawning assigned player prefab: " + playerPrefab.name);
            
            // Instantiate the assigned prefab
            currentPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            currentPlayer.name = "Player";
            
            // Make sure it has the necessary components
            if (currentPlayer.GetComponent<PlayerController>() == null)
            {
                Debug.LogWarning("[SinglePlayerManager] Player prefab doesn't have PlayerController! Adding it...");
                PlayerController controller = currentPlayer.AddComponent<PlayerController>();
                controller.walkSpeed = 5f;
                controller.runSpeed = 8f;
                controller.jumpForce = 7f;
                controller.mouseSensitivity = 2f;
            }
            
            // Make sure it has pause menu
            if (currentPlayer.GetComponent<SinglePlayerPauseMenu>() == null)
            {
                currentPlayer.AddComponent<SinglePlayerPauseMenu>();
            }
            
            Debug.Log("[SinglePlayerManager] Player prefab spawned successfully!");
        }
        else
        {
            Debug.LogWarning("[SinglePlayerManager] No player prefab assigned! Creating default player...");
            CreateDefaultPlayer(position);
        }
    }
    
    void CreateDefaultPlayer(Vector3 position)
    {
        // OLD FALLBACK CODE - Create player programmatically if no prefab assigned
        // Create player root
        currentPlayer = new GameObject("Player");
        currentPlayer.transform.position = position;
        
        // Add CharacterController
        CharacterController cc = currentPlayer.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1, 0);
        
        // Create visual body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(currentPlayer.transform);
        body.transform.localPosition = new Vector3(0, 1, 0);
        
        // Remove capsule collider
        CapsuleCollider col = body.GetComponent<CapsuleCollider>();
        if (col != null) DestroyImmediate(col);
        
        // Create camera holder
        GameObject camHolder = new GameObject("CameraHolder");
        camHolder.transform.SetParent(currentPlayer.transform);
        camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);
        camHolder.transform.localRotation = Quaternion.identity;
        
        // Create and configure camera
        Camera cam = camHolder.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        cam.fieldOfView = 70f;
        cam.depth = 100;
        cam.tag = "MainCamera";
        cam.enabled = true;
        
        // Add audio listener
        camHolder.AddComponent<AudioListener>();
        
        // Add player controller
        PlayerController controller = currentPlayer.AddComponent<PlayerController>();
        controller.walkSpeed = 5f;
        controller.runSpeed = 8f;
        controller.jumpForce = 7f;
        controller.mouseSensitivity = 2f;
        
        // Add pause menu for ESC functionality
        currentPlayer.AddComponent<SinglePlayerPauseMenu>();
        
        Debug.Log("[SinglePlayerManager] Default player created at: " + position);
    }
    
    public void RespawnPlayer()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer);
        
        initialized = false;
        SpawnPlayerNow();
    }
    
    public GameObject GetCurrentPlayer()
    {
        return currentPlayer;
    }
}
