using UnityEngine;

/// <summary>
/// ECHOES - Game Mode Manager
/// Oyun modunu yonetir: SinglePlayer veya Multiplayer
/// Sahneler arasi geciste korunur (DontDestroyOnLoad)
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        None,
        SinglePlayer,
        Multiplayer
    }
    
    // Singleton
    private static GameModeManager _instance;
    public static GameModeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameModeManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameModeManager");
                    _instance = go.AddComponent<GameModeManager>();
                }
            }
            return _instance;
        }
    }
    
    // Current game mode
    private static GameMode _currentMode = GameMode.None;
    public static GameMode CurrentMode
    {
        get { return _currentMode; }
        private set
        {
            _currentMode = value;
            Debug.Log("[GameMode] Mode set to: " + _currentMode);
        }
    }
    
    // Easy check properties
    public static bool IsSinglePlayer => _currentMode == GameMode.SinglePlayer;
    public static bool IsMultiplayer => _currentMode == GameMode.Multiplayer;
    public static bool IsModeSet => _currentMode != GameMode.None;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameModeManager] Initialized");
    }
    
    public static void StartSinglePlayer()
    {
        var _ = Instance;
        CurrentMode = GameMode.SinglePlayer;
        Debug.Log("[GameMode] SINGLE PLAYER MODE - Network: DISABLED");
    }
    
    public static void StartMultiplayer()
    {
        var _ = Instance;
        CurrentMode = GameMode.Multiplayer;
        Debug.Log("[GameMode] MULTIPLAYER MODE - Network: ENABLED");
    }
    
    public static void Reset()
    {
        CurrentMode = GameMode.None;
        Debug.Log("[GameMode] Mode reset to None");
    }
}
