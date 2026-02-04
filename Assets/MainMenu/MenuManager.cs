using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ECHOES - Menu Manager
/// Ana menu sistemi - panel gecisleri ve oyun baslama.
/// MainMenu, SelectionMenu, Settings, Multiplayer panellerini yonetir.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("UI Panelleri")]
    public GameObject mainMenuPanel;
    public GameObject selectionPanel;  // Single Player / Multiplayer / Back
    public GameObject settingsPanel;
    public GameObject multiplayerPanel;
    public GameObject loadingPanel;

    [Header("Ayarlar")]
    public string gameSceneName = "Echoes";

    void Start()
    {
        ShowMainMenu();
        
        // Cursor'u ac
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[MenuManager] Initialized");
    }
    
    void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        Debug.Log("[MenuManager] Showing MainMenu");
    }
    
    // START GAME butonu bunu cagiriyor
    public void OpenSelectionMenu()
    {
        HideAllPanels();
        if (selectionPanel != null) 
        {
            selectionPanel.SetActive(true);
            Debug.Log("[MenuManager] Showing SelectionMenu");
        }
        else
        {
            Debug.LogWarning("[MenuManager] SelectionPanel not assigned! Please assign in Inspector.");
        }
    }
    
    public void CloseSelectionMenu()
    {
        ShowMainMenu();
    }

    public void OpenSettings()
    {
        HideAllPanels();
        if (settingsPanel != null) settingsPanel.SetActive(true);
        Debug.Log("[MenuManager] Showing Settings");
    }

    public void CloseSettings()
    {
        ShowMainMenu();
    }
    
    public void OpenMultiplayer()
    {
        HideAllPanels();
        if (multiplayerPanel != null) multiplayerPanel.SetActive(true);
        Debug.Log("[MenuManager] Showing Multiplayer");
    }
    
    public void CloseMultiplayer()
    {
        // Multiplayer'dan cikinca Selection'a don
        OpenSelectionMenu();
    }

    // Selection Menu'deki SINGLE PLAYER butonu
    public void StartSingleplayer()
    {
        Debug.Log("[MenuManager] Starting Singleplayer");
        GameModeManager.StartSinglePlayer();
        
        HideAllPanels();
        if (loadingPanel != null) loadingPanel.SetActive(true);
        
        SceneManager.LoadScene(gameSceneName);
    }

    // Selection Menu'deki MULTIPLAYER butonu
    public void StartMultiplayer()
    {
        Debug.Log("[MenuManager] Opening Multiplayer Menu");
        OpenMultiplayer();
    }

    public void QuitGame()
    {
        Debug.Log("[MenuManager] Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    // Eski uyumluluk methodlari
    public void GoBackToMainMenu()
    {
        ShowMainMenu();
    }
}

