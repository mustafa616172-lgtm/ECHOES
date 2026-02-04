using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// ECHOES - Selection Panel Setup
/// MainMenu'de SelectionPanel'i olusturur ve button'lari baglar.
/// </summary>
public class SelectionPanelSetup : Editor
{
    [MenuItem("ECHOES/Setup Selection Panel")]
    public static void SetupSelectionPanel()
    {
        // Find MenuManager
        MenuManager menuManager = FindObjectOfType<MenuManager>();
        if (menuManager == null)
        {
            EditorUtility.DisplayDialog("Error", "MenuManager not found in scene!", "OK");
            return;
        }

        // Find Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found in scene!", "OK");
            return;
        }

        // Check if selection panel already exists
        if (menuManager.selectionPanel != null)
        {
            EditorUtility.DisplayDialog("Info", "SelectionPanel already exists!", "OK");
            return;
        }

        // Create SelectionPanel
        GameObject selectionPanel = CreateSelectionPanel(canvas.transform, menuManager);
        
        // Assign to MenuManager
        menuManager.selectionPanel = selectionPanel;
        
        // Hide panel initially
        selectionPanel.SetActive(false);
        
        // Mark everything dirty
        EditorUtility.SetDirty(menuManager);
        EditorUtility.SetDirty(selectionPanel);
        
        // Update START GAME button in MainMenuPanel to call OpenSelectionMenu
        UpdateStartGameButton(menuManager);
        
        EditorUtility.DisplayDialog("Success", 
            "SelectionPanel created!\n\n" +
            "Buttons:\n" +
            "- SINGLE PLAYER ? StartSingleplayer()\n" +
            "- MULTIPLAYER ? StartMultiplayer()\n" +
            "- BACK ? CloseSelectionMenu()\n\n" +
            "Don't forget to save the scene!", "OK");
    }
    
    static GameObject CreateSelectionPanel(Transform parent, MenuManager menuManager)
    {
        // Create panel
        GameObject panel = new GameObject("SelectionPanel");
        panel.transform.SetParent(parent, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // Dark background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);
        
        // Container for content
        GameObject container = new GameObject("Container");
        container.transform.SetParent(panel.transform, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(400, 350);
        
        // Add vertical layout
        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(20, 20, 20, 20);
        
        // Title
        CreateTitle(container.transform, "OYUN MODU SEÇÝN");
        
        // Spacer
        CreateSpacer(container.transform, 30);
        
        // SINGLE PLAYER button
        Button singleBtn = CreateButton(container.transform, "[ SINGLE PLAYER ]", new Color(0.1f, 0.5f, 0.1f));
        ConnectButton(singleBtn.gameObject, menuManager, "StartSingleplayer");
        
        // MULTIPLAYER button  
        Button multiBtn = CreateButton(container.transform, "[ MULTIPLAYER ]", new Color(0.1f, 0.3f, 0.5f));
        ConnectButton(multiBtn.gameObject, menuManager, "StartMultiplayer");
        
        // Spacer
        CreateSpacer(container.transform, 20);
        
        // BACK button
        Button backBtn = CreateButton(container.transform, "[ GERÝ ]", new Color(0.4f, 0.1f, 0.1f));
        ConnectButton(backBtn.gameObject, menuManager, "CloseSelectionMenu");
        
        return panel;
    }
    
    static void CreateTitle(Transform parent, string text)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        
        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 50);
        
        TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
    }
    
    static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        
        RectTransform rect = spacer.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, height);
        
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }
    
    static Button CreateButton(Transform parent, string text, Color bgColor)
    {
        GameObject buttonObj = new GameObject(text.Replace("[", "").Replace("]", "").Trim() + "Button");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 55);
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = bgColor;
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(bgColor.r + 0.2f, bgColor.g + 0.2f, bgColor.b + 0.2f);
        colors.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f);
        button.colors = colors;
        
        // Add layout element
        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.preferredHeight = 55;
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        return button;
    }
    
    static void ConnectButton(GameObject buttonObj, MenuManager target, string methodName)
    {
        Button btn = buttonObj.GetComponent<Button>();
        if (btn == null || target == null) return;
        
        UnityAction action = null;
        
        switch (methodName)
        {
            case "StartSingleplayer":
                action = target.StartSingleplayer;
                break;
            case "StartMultiplayer":
                action = target.StartMultiplayer;
                break;
            case "CloseSelectionMenu":
                action = target.CloseSelectionMenu;
                break;
        }
        
        if (action != null)
        {
            btn.onClick.AddListener(action);
            Debug.Log($"[SelectionPanelSetup] Connected {buttonObj.name} to {methodName}");
        }
    }
    
    static void UpdateStartGameButton(MenuManager menuManager)
    {
        // Find MainMenuPanel
        if (menuManager.mainMenuPanel == null)
        {
            Debug.LogWarning("[SelectionPanelSetup] MainMenuPanel not found");
            return;
        }
        
        // Find START GAME button in MainMenuPanel
        Button[] buttons = menuManager.mainMenuPanel.GetComponentsInChildren<Button>(true);
        
        foreach (Button btn in buttons)
        {
            string btnName = btn.gameObject.name.ToLower();
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            string textContent = btnText != null ? btnText.text.ToLower() : "";
            
            if (btnName.Contains("start") || btnName.Contains("game") || 
                textContent.Contains("start") || textContent.Contains("baþla"))
            {
                // Clear existing onClick and add OpenSelectionMenu
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(menuManager.OpenSelectionMenu);
                
                Debug.Log($"[SelectionPanelSetup] Updated '{btn.gameObject.name}' to call OpenSelectionMenu");
                EditorUtility.SetDirty(btn);
                return;
            }
        }
        
        Debug.LogWarning("[SelectionPanelSetup] START GAME button not found in MainMenuPanel");
    }
}
