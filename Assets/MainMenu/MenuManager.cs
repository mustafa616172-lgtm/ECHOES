using UnityEngine;
using UnityEngine.SceneManagement; // Sahne geçiþleri için

public class MenuManager : MonoBehaviour
{
    [Header("UI Panelleri")]
    public GameObject mainMenuPanel;
    public GameObject selectionPanel;
    public GameObject loadingPanel;

    [Header("Ayarlar")]
    public string gameSceneName = "GameScene";

    // Start butonuna basýldýðýnda çalýþacak
    public void OpenSelectionMenu()
    {
        mainMenuPanel.SetActive(false);
        selectionPanel.SetActive(true);
    }

    // Seçim menüsünden geri dönmek için
    public void BackToMainMenu()
    {
        selectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // Tek oyunculu baþlatma
    public void StartSingleplayer()
    {
        selectionPanel.SetActive(false);
        loadingPanel.SetActive(true);
        SceneManager.LoadScene(gameSceneName);
    }

    // Çok oyunculu baðlantý mantýðý (Örn: Photon veya Mirror için)
    public void StartMultiplayer()
    {
        selectionPanel.SetActive(false);
        loadingPanel.SetActive(true);
        
        Debug.Log("Sunucuya baðlanýlýyor...");
        // Burada Network Manager'ý tetikleyip Lobby sahnesine geçiþ yapmalýsýn.
    }
}