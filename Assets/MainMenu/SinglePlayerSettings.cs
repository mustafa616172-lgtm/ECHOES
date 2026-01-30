using UnityEngine;
using UnityEngine.UI; // Butonlarý kontrol etmek için
using UnityEngine.SceneManagement;

public class SingleplayerMenuManager : MonoBehaviour
{
    [Header("UI Elemanlarý")]
    public Button continueButton;
    public GameObject singleplayerPanel;
    public GameObject selectionPanel;

    [Header("Ayarlar")]
    public string gameSceneName = "GameScene";

    void OnEnable()
    {
        // Panel her açýldýðýnda kayýt kontrolü yap
        CheckSaveData();
    }

    public void CheckSaveData()
    {
        // "HasSavedGame" anahtarý veritabanýnda (PlayerPrefs) var mý kontrol et
        if (PlayerPrefs.HasKey("SaveExists") && PlayerPrefs.GetInt("SaveExists") == 1)
        {
            continueButton.interactable = true; // Butonu týklanabilir yap
            continueButton.gameObject.SetActive(true); // Veya tamamen görünür yap
        }
        else
        {
            continueButton.interactable = false; // Týklanamaz yap
            // continueButton.gameObject.SetActive(false); // Ýstersen tamamen gizleyebilirsin
        }
    }

    public void StartNewGame()
    {
        // Yeni oyun baþlarken eski kaydý temizleyebilir veya üzerine yazabiliriz
        PlayerPrefs.SetInt("SaveExists", 1); // Artýk bir kayýt olduðunu sisteme kaydet
        PlayerPrefs.DeleteKey("PlayerLevel"); // Örnek: Eski ilerlemeyi sil
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        // Kayýtlý verileri yükleyip sahneyi aç
        SceneManager.LoadScene(gameSceneName);
    }

    public void BackToSelection()
    {
        singleplayerPanel.SetActive(false);
        selectionPanel.SetActive(true);
    }
}