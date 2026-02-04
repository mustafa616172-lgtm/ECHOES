using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ECHOES - Multiplayer Panel Setup Tool
/// Editor araci - Multiplayer Panel UI olusturur.
/// </summary>
public class MultiplayerPanelSetup : Editor
{
    private static Color bgDark = new Color(0.08f, 0.08f, 0.08f, 0.95f);
    private static Color textLight = new Color(0.9f, 0.9f, 0.9f, 1f);
    private static Color accentGreen = new Color(0.2f, 0.8f, 0.4f, 1f);
    private static Color buttonBg = new Color(0.15f, 0.15f, 0.15f, 1f);

    [MenuItem("Tools/ECHOES/Create Multiplayer Panel")]
    public static void CreateMultiplayerPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MultiplayerSetup] No Canvas found! Create a Canvas first.");
            return;
        }
        
        // Create main panel
        GameObject panel = new GameObject("MultiplayerPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = bgDark;
        
        // Add MultiplayerPanel script
        MultiplayerPanel mpScript = panel.AddComponent<MultiplayerPanel>();
        
        // Title
        GameObject title = CreateText(panel.transform, "Title", "MULTIPLAYER", 48, new Vector2(0, 250));
        
        // Host Button
        GameObject hostBtn = CreateButton(panel.transform, "HostButton", "[ HOST OL ]", new Vector2(0, 100));
        
        // Join Button
        GameObject joinBtn = CreateButton(panel.transform, "JoinButton", "[ KATIL ]", new Vector2(0, 20));
        
        // IP Input Container
        GameObject ipContainer = new GameObject("IPInputContainer");
        ipContainer.transform.SetParent(panel.transform, false);
        RectTransform ipContainerRect = ipContainer.AddComponent<RectTransform>();
        ipContainerRect.anchoredPosition = new Vector2(0, -60);
        ipContainerRect.sizeDelta = new Vector2(400, 50);
        
        // IP Input Field
        GameObject ipField = CreateInputField(ipContainer.transform, "IPInputField", "127.0.0.1", new Vector2(-50, 0));
        
        // Connect Button
        GameObject connectBtn = CreateButton(ipContainer.transform, "ConnectButton", "BAGLAN", new Vector2(150, 0));
        RectTransform connectRect = connectBtn.GetComponent<RectTransform>();
        connectRect.sizeDelta = new Vector2(120, 40);
        
        // Back Button
        GameObject backBtn = CreateButton(panel.transform, "BackButton", "[ GERI ]", new Vector2(0, -150));
        
        // Status Text
        GameObject statusText = CreateText(panel.transform, "StatusText", "Multiplayer modu secin", 18, new Vector2(0, -220));
        
        // Connect references
        SerializedObject so = new SerializedObject(mpScript);
        so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
        so.FindProperty("joinButton").objectReferenceValue = joinBtn.GetComponent<Button>();
        so.FindProperty("connectButton").objectReferenceValue = connectBtn.GetComponent<Button>();
        so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        so.FindProperty("ipInputField").objectReferenceValue = ipField.GetComponent<TMP_InputField>();
        so.FindProperty("ipInputContainer").objectReferenceValue = ipContainer;
        so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();
        
        // Connect to MenuManager
        MenuManager mm = FindObjectOfType<MenuManager>();
        if (mm != null)
        {
            SerializedObject mmSo = new SerializedObject(mm);
            mmSo.FindProperty("multiplayerPanel").objectReferenceValue = panel;
            mmSo.ApplyModifiedProperties();
            
            // Also set reference in MultiplayerPanel
            so.FindProperty("menuManager").objectReferenceValue = mm;
            so.ApplyModifiedProperties();
        }
        
        panel.SetActive(false);
        Selection.activeGameObject = panel;
        
        Debug.Log("[MultiplayerSetup] Multiplayer Panel created!");
        EditorUtility.DisplayDialog("Success", "Multiplayer Panel created!\n\nIt's connected to MenuManager.", "OK");
    }
    
    [MenuItem("Tools/ECHOES/Create InGame Menu")]
    public static void CreateInGameMenu()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Create canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create menu panel
        GameObject menuPanel = new GameObject("InGameMenuPanel");
        menuPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 350);
        
        Image panelBg = menuPanel.AddComponent<Image>();
        panelBg.color = bgDark;
        
        // Title
        CreateText(menuPanel.transform, "Title", "OYUN MENUSU", 32, new Vector2(0, 130));
        
        // Server IP
        CreateText(menuPanel.transform, "ServerIPText", "Sunucu IP: 127.0.0.1:7777", 18, new Vector2(0, 70));
        
        // Status
        CreateText(menuPanel.transform, "StatusText", "Durum: Host", 18, new Vector2(0, 40));
        
        // Player Count
        CreateText(menuPanel.transform, "PlayerCountText", "Oyuncular: 1/4", 18, new Vector2(0, 10));
        
        // Resume Button
        GameObject resumeBtn = CreateButton(menuPanel.transform, "ResumeButton", "[ DEVAM ET ]", new Vector2(0, -50));
        
        // Exit Button
        GameObject exitBtn = CreateButton(menuPanel.transform, "ExitButton", "[ CIKIS ]", new Vector2(0, -120));
        
        // Create parent object with InGameMenu script
        GameObject menuManager = new GameObject("InGameMenu");
        menuManager.transform.SetParent(canvas.transform, false);
        InGameMenu igm = menuManager.AddComponent<InGameMenu>();
        
        SerializedObject so = new SerializedObject(igm);
        so.FindProperty("menuPanel").objectReferenceValue = menuPanel;
        so.FindProperty("serverIPText").objectReferenceValue = menuPanel.transform.Find("ServerIPText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("connectionStatusText").objectReferenceValue = menuPanel.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("playerCountText").objectReferenceValue = menuPanel.transform.Find("PlayerCountText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("resumeButton").objectReferenceValue = resumeBtn.GetComponent<Button>();
        so.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();
        
        menuPanel.SetActive(false);
        Selection.activeGameObject = menuManager;
        
        Debug.Log("[MultiplayerSetup] InGame Menu created!");
        EditorUtility.DisplayDialog("Success", "InGame Menu created!\n\nPress ESC in game to open it.", "OK");
    }
    
    static GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 50);
        rect.anchoredPosition = position;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = buttonBg;
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        btn.colors = colors;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = accentGreen;
        
        return btnObj;
    }
    
    static GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 50);
        rect.anchoredPosition = position;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textLight;
        
        return textObj;
    }
    
    static GameObject CreateInputField(Transform parent, string name, string placeholder, Vector2 position)
    {
        GameObject fieldObj = new GameObject(name);
        fieldObj.transform.SetParent(parent, false);
        
        RectTransform rect = fieldObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 40);
        rect.anchoredPosition = position;
        
        Image img = fieldObj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        
        TMP_InputField inputField = fieldObj.AddComponent<TMP_InputField>();
        
        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(fieldObj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 0);
        taRect.offsetMax = new Vector2(-10, 0);
        
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
        phTmp.fontSize = 16;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        phTmp.alignment = TextAlignmentOptions.Left;
        
        // Input text
        GameObject itObj = new GameObject("Text");
        itObj.transform.SetParent(textArea.transform, false);
        RectTransform itRect = itObj.AddComponent<RectTransform>();
        itRect.anchorMin = Vector2.zero;
        itRect.anchorMax = Vector2.one;
        itRect.offsetMin = Vector2.zero;
        itRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI itTmp = itObj.AddComponent<TextMeshProUGUI>();
        itTmp.fontSize = 16;
        itTmp.color = accentGreen;
        itTmp.alignment = TextAlignmentOptions.Left;
        
        inputField.textViewport = taRect;
        inputField.textComponent = itTmp;
        inputField.placeholder = phTmp;
        
        return fieldObj;
    }
}
