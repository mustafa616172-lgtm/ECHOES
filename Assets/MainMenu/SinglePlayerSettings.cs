using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Single Player panel kontrolleri.
/// Yeni oyun ve devam et butonlarini yonetir.
/// </summary>
public class SinglePlayerSettings : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject singleplayerPanel;
    public GameObject selectionPanel;
    
    [Header("Butonlar")]
    public Button newGameButton;
    public Button continueButton;
    
    [Header("Ayarlar")]
    public string gameSceneName = "Echoes";
    
    void Start()
    {
        // Buton baglantilari
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
            
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
    }
    
    void OnEnable()
    {
        // Sadece kayit kontrolu yap - OTOMATIK BASLATMA
        CheckSaveData();
    }
    
    void CheckSaveData()
    {
        // Kayitli veri var mi kontrol et
        bool hasSave = PlayerPrefs.HasKey("HasSaveData");
        
        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
        }
        
        Debug.Log("[SinglePlayerSettings] Save data check: " + (hasSave ? "Found" : "Not found"));
    }
    
    public void StartNewGame()
    {
        Debug.Log("[SinglePlayerSettings] Starting new game...");
        
        // Eski kaydi temizle
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // Oyunu baslat
        LoadGame();
    }
    
    public void ContinueGame()
    {
        Debug.Log("[SinglePlayerSettings] Continuing game...");
        
        // Kayitli verilerle devam et
        LoadGame();
    }
    
    void LoadGame()
    {
        // GameMode ayarla
        GameModeManager.StartSinglePlayer();
        
        // Sahne yukle
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void BackToSelection()
    {
        if (singleplayerPanel != null)
            singleplayerPanel.SetActive(false);
            
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }
}
