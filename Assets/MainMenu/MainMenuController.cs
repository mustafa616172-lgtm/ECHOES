using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Button References")]
    public Button startGameButton;
    public Button quitButton;
    
    [Header("Game Settings")]
    public string gameSceneName = "Echoes";

    void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            Debug.Log("[MainMenu] Start Game button connected");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("[MainMenu] Quit button connected");
        }
    }

    public void OnStartGameClicked()
    {
        Debug.Log("[MainMenu] Starting game: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
