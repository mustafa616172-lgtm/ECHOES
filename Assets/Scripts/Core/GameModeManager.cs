using UnityEngine;

/// <summary>
/// ECHOES - Game Mode Manager
/// Oyun modunu yonetir: SinglePlayer veya Multiplayer
/// Sahneler arasi geciste korunur (DontDestroyOnLoad + PlayerPrefs)
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        None,
        SinglePlayer,
        Multiplayer
    }
    
    private const string PREFS_KEY = "ECHOES_GameMode";
    
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
    
    // Current game mode - backed by PlayerPrefs for build persistence
    private static GameMode _currentMode = GameMode.None;
    public static GameMode CurrentMode
    {
        get
        {
            // If None, try to restore from PlayerPrefs
            if (_currentMode == GameMode.None)
            {
                int saved = PlayerPrefs.GetInt(PREFS_KEY, 0);
                if (saved == 1) _currentMode = GameMode.SinglePlayer;
                else if (saved == 2) _currentMode = GameMode.Multiplayer;
            }
            return _currentMode;
        }
        private set
        {
            _currentMode = value;
            // Save to PlayerPrefs for persistence
            int saveValue = 0;
            if (value == GameMode.SinglePlayer) saveValue = 1;
            else if (value == GameMode.Multiplayer) saveValue = 2;
            PlayerPrefs.SetInt(PREFS_KEY, saveValue);
            PlayerPrefs.Save();
            Debug.Log("[GameMode] Mode set to: " + _currentMode + " (saved to PlayerPrefs)");
        }
    }
    
    // Easy check properties
    public static bool IsSinglePlayer => CurrentMode == GameMode.SinglePlayer;
    public static bool IsMultiplayer => CurrentMode == GameMode.Multiplayer;
    public static bool IsModeSet => CurrentMode != GameMode.None;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Try to restore mode from PlayerPrefs on startup
        var _ = CurrentMode; // This triggers the getter which reads from PlayerPrefs
        Debug.Log("[GameModeManager] Initialized - Current Mode: " + _currentMode);
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
        PlayerPrefs.DeleteKey(PREFS_KEY);
        Debug.Log("[GameMode] Mode reset to None");
    }
}
