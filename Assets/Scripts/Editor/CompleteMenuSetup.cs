using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// ECHOES - Complete Menu Setup Tool
/// Tum menu panellerini ve buton baglantilarini otomatik olusturur.
/// Video arka plan gorunur, seffaf paneller!
/// </summary>
public class CompleteMenuSetup : Editor
{
    // Renkler - SEFFAF arka plan, video gorunsun
    private static Color panelBgTransparent = new Color(0f, 0f, 0f, 0f); // Tamamen seffaf
    private static Color buttonBg = new Color(0f, 0f, 0f, 0.6f); // Yari seffaf buton
    private static Color buttonHover = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    private static Color textLight = new Color(0.9f, 0.9f, 0.85f, 1f);
    private static Color accentCyan = new Color(0.4f, 0.85f, 0.85f, 1f); // Turkuaz
    private static Color accentPink = new Color(0.9f, 0.5f, 0.7f, 1f); // Pembe

    [MenuItem("Tools/ECHOES/Menu Setup/1. TUM MENULER (Hepsini Kur)")]
    public static void CompleteMainMenuSetup()
    {
        var (canvas, menuManager) = EnsureBasicSetup();
        if (canvas == null) return;
        
        // Create all panels
        GameObject mainPanel = CreateMainMenuPanel(canvas.transform, menuManager);
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform, menuManager);
        GameObject multiplayerPanel = CreateMultiplayerPanel(canvas.transform, menuManager);
        
        // Connect panels to MenuManager
        SerializedObject mmSo = new SerializedObject(menuManager);
        mmSo.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
        mmSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        mmSo.FindProperty("multiplayerPanel").objectReferenceValue = multiplayerPanel;
        mmSo.ApplyModifiedProperties();
        
        CreateRequiredManagers();
        
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        
        Debug.Log("=== TUM MENULER KURULDU ===");
        EditorUtility.DisplayDialog("Kurulum Tamamlandi!", 
            "Tum menuler olusturuldu!\n\n" +
            "- MainMenuPanel\n" +
            "- SettingsPanel\n" +
            "- MultiplayerPanel\n\n" +
            "Sahneyi kaydetmeyi unutma!", "Tamam");
    }
    
    [MenuItem("Tools/ECHOES/Menu Setup/2. Sadece MAIN MENU")]
    public static void SetupOnlyMainMenu()
    {
        var (canvas, menuManager) = EnsureBasicSetup();
        if (canvas == null) return;
        
        GameObject mainPanel = CreateMainMenuPanel(canvas.transform, menuManager);
        
        SerializedObject mmSo = new SerializedObject(menuManager);
        mmSo.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
        mmSo.ApplyModifiedProperties();
        
        mainPanel.SetActive(true);
        
        Debug.Log("=== MAIN MENU KURULDU ===");
        EditorUtility.DisplayDialog("Main Menu Kuruldu!", 
            "MainMenuPanel yeniden olusturuldu!\n\n" +
            "Sahneyi kaydetmeyi unutma!", "Tamam");
    }
    
    [MenuItem("Tools/ECHOES/Menu Setup/3. Sadece SETTINGS")]
    public static void SetupOnlySettings()
    {
        var (canvas, menuManager) = EnsureBasicSetup();
        if (canvas == null) return;
        
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform, menuManager);
        
        SerializedObject mmSo = new SerializedObject(menuManager);
        mmSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        mmSo.ApplyModifiedProperties();
        
        settingsPanel.SetActive(true);
        
        Debug.Log("=== SETTINGS PANEL KURULDU ===");
        EditorUtility.DisplayDialog("Settings Kuruldu!", 
            "SettingsPanel yeniden olusturuldu!\n\n" +
            "Sahneyi kaydetmeyi unutma!", "Tamam");
    }
    
    [MenuItem("Tools/ECHOES/Menu Setup/4. Sadece MULTIPLAYER")]
    public static void SetupOnlyMultiplayer()
    {
        var (canvas, menuManager) = EnsureBasicSetup();
        if (canvas == null) return;
        
        GameObject multiplayerPanel = CreateMultiplayerPanel(canvas.transform, menuManager);
        
        SerializedObject mmSo = new SerializedObject(menuManager);
        mmSo.FindProperty("multiplayerPanel").objectReferenceValue = multiplayerPanel;
        mmSo.ApplyModifiedProperties();
        
        multiplayerPanel.SetActive(true);
        
        Debug.Log("=== MULTIPLAYER PANEL KURULDU ===");
        EditorUtility.DisplayDialog("Multiplayer Kuruldu!", 
            "MultiplayerPanel yeniden olusturuldu!\n\n" +
            "Sahneyi kaydetmeyi unutma!", "Tamam");
    }
    
    static (Canvas, MenuManager) EnsureBasicSetup()
    {
        // Check scene
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("MainMenu"))
        {
            if (!EditorUtility.DisplayDialog("Warning", "Bu MainMenu sahnesi degil gibi gorunuyor. Devam et?", "Evet", "Hayir"))
                return (null, null);
        }
        
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Find or create EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Find or create MenuManager
        MenuManager menuManager = FindObjectOfType<MenuManager>();
        if (menuManager == null)
        {
            GameObject mmObj = new GameObject("MenuManager");
            menuManager = mmObj.AddComponent<MenuManager>();
        }
        
        return (canvas, menuManager);
    }
    
    static GameObject CreateMainMenuPanel(Transform parent, MenuManager menuManager)
    {
        // Delete old if exists
        Transform old = parent.Find("MainMenuPanel");
        if (old != null) DestroyImmediate(old.gameObject);
        
        GameObject panel = CreateTransparentPanel(parent, "MainMenuPanel");
        
        // Title - Sag tarafa hizali (eski menu gibi)
        CreateStyledText(panel.transform, "Title", "ECHOES", 72, new Vector2(350, 150), accentCyan, TextAlignmentOptions.Right);
        CreateStyledText(panel.transform, "Subtitle", "FRAGMENTED", 28, new Vector2(350, 90), textLight, TextAlignmentOptions.Right);
        
        // Buttons - Sag tarafa hizali
        GameObject singleBtn = CreateStyledButton(panel.transform, "SinglePlayerButton", "START GAME", new Vector2(350, 0));
        GameObject settingsBtn = CreateStyledButton(panel.transform, "SettingsButton", "SETTINGS", new Vector2(350, -70));
        GameObject exitBtn = CreateStyledButton(panel.transform, "ExitButton", "EXIT", new Vector2(350, -140));
        
        // Connect button events
        ConnectButton(singleBtn, menuManager, "StartSingleplayer");
        ConnectButton(settingsBtn, menuManager, "OpenSettings");
        ConnectButton(exitBtn, menuManager, "QuitGame");
        
        return panel;
    }
    
    static GameObject CreateSettingsPanel(Transform parent, MenuManager menuManager)
    {
        Transform old = parent.Find("SettingsPanel");
        if (old != null) DestroyImmediate(old.gameObject);
        
        // Ana panel - SEFFAF
        GameObject panel = CreateTransparentPanel(parent, "SettingsPanel");
        
        // ===== YARI SEFFAF SIYAH ARKA PLAN ===== (1920x1080 icin buyuk)
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(panel.transform, false);
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(1100, 800); // 1920x1080 icin buyuk panel
        bgRect.anchoredPosition = Vector2.zero;
        Image bgImg = bgPanel.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f); // Yari seffaf siyah
        
        // Renkler
        Color yellowGold = new Color(0.9f, 0.75f, 0.2f, 1f);
        Color greenBtn = new Color(0.35f, 0.65f, 0.35f, 1f); // Yesil buton
        Color greenBtnDark = new Color(0.25f, 0.5f, 0.25f, 1f); // Secilince koyu yesil
        Color redButton = new Color(0.55f, 0.25f, 0.25f, 1f);
        Color greenSaveBtn = new Color(0.25f, 0.45f, 0.25f, 1f);
        
        // ===== SETTINGS Title ===== (1920x1080)
        CreateSettingsText(panel.transform, "Title", "SETTINGS", 84, new Vector2(0, 340), textLight);
        
        // ===== GRAPHICS Section =====
        CreateSettingsText(panel.transform, "GraphicsLabel", "GRAPHICS", 38, new Vector2(-470, 240), textLight);
        
        // Graphics quality buttons - HEPSI YESIL (secilince koyulasir)
        CreateGreenQualityButton(panel.transform, "FastBtn", "FAST", new Vector2(-100, 240), greenBtn, greenBtnDark, 180, 60, 28);
        CreateGreenQualityButton(panel.transform, "NormalBtn", "NORMAL", new Vector2(120, 240), greenBtn, greenBtnDark, 180, 60, 28);
        CreateGreenQualityButton(panel.transform, "HighBtn", "HIGH", new Vector2(340, 240), greenBtn, greenBtnDark, 180, 60, 28);
        
        // ===== AUDIO Section =====
        CreateSettingsText(panel.transform, "AudioLabel", "AUDIO", 38, new Vector2(-470, 140), textLight);
        
        // Master slider - 1920x1080
        CreateSettingsText(panel.transform, "MasterLabel", "Master", 32, new Vector2(-470, 80), textLight);
        CreateYellowSlider(panel.transform, "MasterSlider", new Vector2(60, 80), 1f, 580, 38);
        CreateSettingsText(panel.transform, "MasterValue", "100%", 32, new Vector2(460, 80), textLight);
        
        // Music slider
        CreateSettingsText(panel.transform, "MusicLabel", "Music", 32, new Vector2(-470, 10), textLight);
        CreateYellowSlider(panel.transform, "MusicSlider", new Vector2(60, 10), 0.74f, 580, 38);
        CreateSettingsText(panel.transform, "MusicValue", "74%", 32, new Vector2(460, 10), textLight);
        
        // Effects slider
        CreateSettingsText(panel.transform, "EffectsLabel", "Effects", 32, new Vector2(-470, -60), textLight);
        CreateYellowSlider(panel.transform, "EffectsSlider", new Vector2(60, -60), 1f, 580, 38);
        CreateSettingsText(panel.transform, "EffectsValue", "100%", 32, new Vector2(460, -60), textLight);
        
        // ===== MOUSE Section =====
        CreateSettingsText(panel.transform, "MouseLabel", "MOUSE", 38, new Vector2(-470, -150), textLight);
        
        // Sensitivity slider
        CreateSettingsText(panel.transform, "SensLabel", "Sensitivity", 32, new Vector2(-470, -220), textLight);
        CreateYellowSlider(panel.transform, "SensitivitySlider", new Vector2(60, -220), 0.5f, 580, 38);
        CreateSettingsText(panel.transform, "SensValue", "1.0x", 32, new Vector2(460, -220), textLight);
        
        // ===== BACK and SAVE Buttons ===== 1920x1080
        CreateColoredButton(panel.transform, "BackButton", "BACK", new Vector2(-150, -330), redButton, 260, 70, 34);
        CreateColoredButton(panel.transform, "SaveButton", "SAVE", new Vector2(190, -330), greenSaveBtn, 260, 70, 34);
        
        // Connect Back button to MenuManager
        GameObject backBtn = panel.transform.Find("BackButton").gameObject;
        ConnectButton(backBtn, menuManager, "CloseSettings");
        
        // Add SettingsPanel script and CONNECT REFERENCES
        SettingsPanel sp = panel.AddComponent<SettingsPanel>();
        SerializedObject so = new SerializedObject(sp);
        
        // Buttons
        so.FindProperty("graphicsFastButton").objectReferenceValue = panel.transform.Find("FastBtn").GetComponent<Button>();
        so.FindProperty("graphicsNormalButton").objectReferenceValue = panel.transform.Find("NormalBtn").GetComponent<Button>();
        so.FindProperty("graphicsHighButton").objectReferenceValue = panel.transform.Find("HighBtn").GetComponent<Button>();
        so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        so.FindProperty("saveButton").objectReferenceValue = panel.transform.Find("SaveButton").GetComponent<Button>();
        
        // Sliders
        so.FindProperty("masterVolumeSlider").objectReferenceValue = panel.transform.Find("MasterSlider").GetComponent<Slider>();
        so.FindProperty("musicVolumeSlider").objectReferenceValue = panel.transform.Find("MusicSlider").GetComponent<Slider>();
        so.FindProperty("sfxVolumeSlider").objectReferenceValue = panel.transform.Find("EffectsSlider").GetComponent<Slider>();
        so.FindProperty("mouseSensitivitySlider").objectReferenceValue = panel.transform.Find("SensitivitySlider").GetComponent<Slider>();
        
        // Menu Manager
        so.FindProperty("menuManager").objectReferenceValue = menuManager;
        
        so.ApplyModifiedProperties();
        
        return panel;
    }
    
    static GameObject CreateGreenQualityButton(Transform parent, string name, string text, Vector2 position, Color normalColor, Color pressedColor, float width = 160, float height = 50, int fontSize = 24)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = normalColor;
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Basinca koyulasma
        colors.selectedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return btnObj;
    }
    
    static GameObject CreateMultiplayerPanel(Transform parent, MenuManager menuManager)
    {
        Transform old = parent.Find("MultiplayerPanel");
        if (old != null) DestroyImmediate(old.gameObject);
        
        GameObject panel = CreateTransparentPanel(parent, "MultiplayerPanel");
        
        // Title - Sag tarafa hizali
        CreateStyledText(panel.transform, "Title", "MULTIPLAYER", 56, new Vector2(350, 200), accentPink, TextAlignmentOptions.Right);
        
        // Host button
        GameObject hostBtn = CreateStyledButton(panel.transform, "HostButton", "HOST GAME", new Vector2(350, 80));
        
        // Join button
        GameObject joinBtn = CreateStyledButton(panel.transform, "JoinButton", "JOIN GAME", new Vector2(350, 10));
        
        // IP Input container (hidden by default)
        GameObject ipContainer = new GameObject("IPInputContainer");
        ipContainer.transform.SetParent(panel.transform, false);
        RectTransform ipRect = ipContainer.AddComponent<RectTransform>();
        ipRect.anchoredPosition = new Vector2(350, -60);
        ipRect.sizeDelta = new Vector2(400, 50);
        
        GameObject ipField = CreateStyledInputField(ipContainer.transform, "IPInputField", "127.0.0.1", new Vector2(-60, 0));
        GameObject connectBtn = CreateSmallStyledButton(ipContainer.transform, "ConnectButton", "CONNECT", new Vector2(130, 0));
        
        // Status text
        GameObject statusText = CreateStyledText(panel.transform, "StatusText", "Select an option", 18, new Vector2(350, -130), textLight, TextAlignmentOptions.Right);
        
        // Back button
        GameObject backBtn = CreateStyledButton(panel.transform, "BackButton", "BACK", new Vector2(350, -200));
        ConnectButton(backBtn, menuManager, "CloseMultiplayer");
        
        // Add MultiplayerPanel script and connect
        MultiplayerPanel mp = panel.AddComponent<MultiplayerPanel>();
        SerializedObject mpSo = new SerializedObject(mp);
        mpSo.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
        mpSo.FindProperty("joinButton").objectReferenceValue = joinBtn.GetComponent<Button>();
        mpSo.FindProperty("connectButton").objectReferenceValue = connectBtn.GetComponent<Button>();
        mpSo.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        mpSo.FindProperty("ipInputField").objectReferenceValue = ipField.GetComponent<TMP_InputField>();
        mpSo.FindProperty("ipInputContainer").objectReferenceValue = ipContainer;
        mpSo.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        mpSo.FindProperty("menuManager").objectReferenceValue = menuManager;
        mpSo.ApplyModifiedProperties();
        
        ipContainer.SetActive(false);
        
        return panel;
    }
    
    static void CreateRequiredManagers()
    {
        if (FindObjectOfType<GameModeManager>() == null)
        {
            GameObject gmm = new GameObject("GameModeManager");
            gmm.AddComponent<GameModeManager>();
        }
        
        if (FindObjectOfType<SettingsManager>() == null)
        {
            GameObject sm = new GameObject("SettingsManager");
            sm.AddComponent<SettingsManager>();
        }
    }
    
    // SEFFAF panel - video gorunsun!
    static GameObject CreateTransparentPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // NO Image component = fully transparent, video shows through!
        // Sadece CanvasGroup ekle (fade icin)
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        
        return panel;
    }
    
    static GameObject CreateStyledButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 45);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        // Yari seffaf arka plan
        Image img = btnObj.AddComponent<Image>();
        img.color = buttonBg;
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textLight;
        
        return btnObj;
    }
    
    static GameObject CreateSmallStyledButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btn = CreateStyledButton(parent, name, text, position);
        btn.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 35);
        btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;
        return btn;
    }
    
    static GameObject CreateStyledText(Transform parent, string name, string text, int fontSize, Vector2 position, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 60);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alignment;
        tmp.color = color;
        
        // Glowing effect with outline
        tmp.outlineWidth = 0.15f;
        tmp.outlineColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.5f);
        
        return textObj;
    }
    
    static GameObject CreateStyledSlider(Transform parent, string name, Vector2 position)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 20);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.8f;
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        
        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = accentCyan;
        
        slider.fillRect = fillRect;
        
        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 30);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = textLight;
        
        slider.handleRect = handleRect;
        
        return sliderObj;
    }
    
    static GameObject CreateStyledInputField(Transform parent, string name, string placeholder, Vector2 position)
    {
        GameObject fieldObj = new GameObject(name);
        fieldObj.transform.SetParent(parent, false);
        
        RectTransform rect = fieldObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);
        rect.anchoredPosition = position;
        
        Image img = fieldObj.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        
        TMP_InputField inputField = fieldObj.AddComponent<TMP_InputField>();
        
        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(fieldObj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 0);
        taRect.offsetMax = new Vector2(-10, 0);
        textArea.AddComponent<RectMask2D>();
        
        // Placeholder
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(textArea.transform, false);
        RectTransform phRect = phObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI phTmp = phObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 18;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        phTmp.alignment = TextAlignmentOptions.Left;
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.fontSize = 18;
        textTmp.color = accentCyan;
        textTmp.alignment = TextAlignmentOptions.Left;
        
        inputField.textViewport = taRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = phTmp;
        
        return fieldObj;
    }
    
    static void ConnectButton(GameObject buttonObj, MenuManager target, string methodName)
    {
        Button btn = buttonObj.GetComponent<Button>();
        if (btn == null || target == null) return;
        
        btn.onClick.RemoveAllListeners();
        
        UnityAction action = null;
        
        switch (methodName)
        {
            case "StartSingleplayer":
                action = target.StartSingleplayer;
                break;
            case "OpenMultiplayer":
                action = target.OpenMultiplayer;
                break;
            case "OpenSettings":
                action = target.OpenSettings;
                break;
            case "CloseSettings":
                action = target.CloseSettings;
                break;
            case "CloseMultiplayer":
                action = target.CloseMultiplayer;
                break;
            case "QuitGame":
                action = target.QuitGame;
                break;
        }
        
        if (action != null)
        {
            UnityEventTools.AddPersistentListener(btn.onClick, action);
            Debug.Log("[Setup] Connected: " + buttonObj.name + " -> " + methodName);
        }
    }
    
    // ===== SETTINGS PANEL HELPER METHODS =====
    
    static GameObject CreateSettingsText(Transform parent, string name, string text, int fontSize, Vector2 position, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = color;
        
        return textObj;
    }
    
    static GameObject CreateQualityButton(Transform parent, string name, string text, Vector2 position, Color bgColor, float width = 120, float height = 35, int fontSize = 16)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        Button btn = btnObj.AddComponent<Button>();
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return btnObj;
    }
    
    static GameObject CreateYellowSlider(Transform parent, string name, Vector2 position, float defaultValue, float width = 350, float height = 20)
    {
        Color yellowGold = new Color(0.9f, 0.75f, 0.2f, 1f);
        Color grayBg = new Color(0.25f, 0.25f, 0.25f, 1f);
        
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = defaultValue;
        
        // Background (gray)
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = grayBg;
        
        // Fill area (yellow)
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = yellowGold;
        
        slider.fillRect = fillRect;
        
        // Handle (small white circle)
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(5, 0);
        handleAreaRect.offsetMax = new Vector2(-5, 0);
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16, 28);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        
        slider.handleRect = handleRect;
        
        return sliderObj;
    }
    
    static GameObject CreateColoredButton(Transform parent, string name, string text, Vector2 position, Color bgColor, float width = 160, float height = 45, int fontSize = 20)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return btnObj;
    }
}
