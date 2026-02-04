using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ECHOES - Multiplayer Panel
/// Ana menude multiplayer secenekleri: Host Ol, Katil, Geri
/// Settings Panel ile ayni tarzd?.
/// </summary>
public class MultiplayerPanel : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button backButton;
    
    [Header("IP Input")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private GameObject ipInputContainer;
    
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("References")]
    [SerializeField] private MenuManager menuManager;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Echoes";
    
    private bool showingJoinOptions = false;

    void Start()
    {
        SetupButtons();
        HideJoinOptions();
        UpdateStatus("Multiplayer modu secin");
    }
    
    void SetupButtons()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);
        
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);
        
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    void OnHostClicked()
    {
        Debug.Log("[MultiplayerPanel] HOST selected");
        UpdateStatus("Host baslatiliyor...");
        
        // Set multiplayer mode
        GameModeManager.StartMultiplayer();
        
        // Save that we want to be host
        PlayerPrefs.SetInt("MP_IsHost", 1);
        PlayerPrefs.Save();
        
        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }
    
    void OnJoinClicked()
    {
        Debug.Log("[MultiplayerPanel] JOIN selected");
        
        if (!showingJoinOptions)
        {
            ShowJoinOptions();
        }
        else
        {
            HideJoinOptions();
        }
    }
    
    void OnConnectClicked()
    {
        string ip = "127.0.0.1";
        
        if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
        {
            ip = ipInputField.text.Trim();
        }
        
        Debug.Log("[MultiplayerPanel] Connecting to: " + ip);
        UpdateStatus("Baglaniliyor: " + ip);
        
        // Set multiplayer mode
        GameModeManager.StartMultiplayer();
        
        // Save connection info
        PlayerPrefs.SetInt("MP_IsHost", 0);
        PlayerPrefs.SetString("MP_ServerIP", ip);
        PlayerPrefs.Save();
        
        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }
    
    void OnBackClicked()
    {
        Debug.Log("[MultiplayerPanel] BACK clicked");
        HideJoinOptions();
        
        if (menuManager != null)
        {
            menuManager.ShowMainMenu();
        }
        else
        {
            // Try to find it
            menuManager = FindObjectOfType<MenuManager>();
            if (menuManager != null)
                menuManager.ShowMainMenu();
        }
    }
    
    void ShowJoinOptions()
    {
        showingJoinOptions = true;
        
        if (ipInputContainer != null)
            ipInputContainer.SetActive(true);
        
        if (connectButton != null)
            connectButton.gameObject.SetActive(true);
        
        if (ipInputField != null)
        {
            ipInputField.text = "127.0.0.1";
            ipInputField.Select();
        }
        
        UpdateStatus("Sunucu IP adresini girin");
    }
    
    void HideJoinOptions()
    {
        showingJoinOptions = false;
        
        if (ipInputContainer != null)
            ipInputContainer.SetActive(false);
        
        if (connectButton != null)
            connectButton.gameObject.SetActive(false);
        
        UpdateStatus("Multiplayer modu secin");
    }
    
    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
    
    void OnEnable()
    {
        HideJoinOptions();
    }
}
