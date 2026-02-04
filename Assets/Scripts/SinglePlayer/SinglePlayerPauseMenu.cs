using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ECHOES - Single Player Pause Menu
/// Single Player modda ESC'ye basinca acilan duraklatma menusu.
/// </summary>
public class SinglePlayerPauseMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool pauseTimeWhenOpen = true;
    
    private bool isPaused = false;
    private bool isInitialized = false;
    
    void Start()
    {
        // Only run in Single Player mode
        if (!GameModeManager.IsSinglePlayer)
        {
            Debug.Log("[SinglePlayerPauseMenu] Not in Single Player mode - disabling");
            enabled = false;
            return;
        }
        
        // If no panel assigned, try to create one
        if (pauseMenuPanel == null)
        {
            CreatePauseMenuUI();
        }
        
        SetupButtons();
        
        // Start with menu closed
        ClosePauseMenu();
        isInitialized = true;
        
        Debug.Log("[SinglePlayerPauseMenu] Initialized");
    }
    
    void Update()
    {
        // Only check for ESC in Single Player mode
        if (!GameModeManager.IsSinglePlayer) return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }
    
    void SetupButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ClosePauseMenu);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }
    
    public void TogglePauseMenu()
    {
        if (isPaused)
            ClosePauseMenu();
        else
            OpenPauseMenu();
    }
    
    public void OpenPauseMenu()
    {
        isPaused = true;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause time
        if (pauseTimeWhenOpen)
            Time.timeScale = 0f;
        
        Debug.Log("[SinglePlayerPauseMenu] Menu opened");
    }
    
    public void ClosePauseMenu()
    {
        isPaused = false;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Resume time
        Time.timeScale = 1f;
        
        if (isInitialized)
            Debug.Log("[SinglePlayerPauseMenu] Menu closed");
    }
    
    void OpenSettings()
    {
        Debug.Log("[SinglePlayerPauseMenu] Settings clicked (not implemented)");
        // TODO: Show settings panel
    }
    
    void ReturnToMainMenu()
    {
        Debug.Log("[SinglePlayerPauseMenu] Returning to main menu");
        
        // Resume time
        Time.timeScale = 1f;
        
        // Reset game mode
        GameModeManager.Reset();
        
        // Load main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    void CreatePauseMenuUI()
    {
        Debug.Log("[SinglePlayerPauseMenu] Creating pause menu UI...");
        
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PauseMenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // On top of everything
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create panel with dark background
        pauseMenuPanel = new GameObject("PauseMenuPanel");
        pauseMenuPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = pauseMenuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelImage = pauseMenuPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create container for buttons
        GameObject container = new GameObject("ButtonContainer");
        container.transform.SetParent(pauseMenuPanel.transform, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(300, 200);
        
        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // Title
        CreateText(container.transform, "DURAKLATILDI", 36, Color.white);
        
        // Buttons
        resumeButton = CreateButton(container.transform, "DEVAM ET", new Color(0.2f, 0.6f, 0.2f));
        mainMenuButton = CreateButton(container.transform, "ANA MENÜ", new Color(0.6f, 0.2f, 0.2f));
        
        Debug.Log("[SinglePlayerPauseMenu] UI created");
    }
    
    GameObject CreateText(Transform parent, string text, int fontSize, Color color)
    {
        GameObject textObj = new GameObject("Title");
        textObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 50);
        
        return textObj;
    }
    
    Button CreateButton(Transform parent, string text, Color bgColor)
    {
        GameObject buttonObj = new GameObject(text + "Button");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 50);
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = bgColor;
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        return button;
    }
    
    public bool IsPaused => isPaused;
}
