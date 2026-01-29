using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class NetworkUISetup : MonoBehaviour
{
#if UNITY_EDITOR
    // Retro Analog Horror Renk Paleti
    private static Color bgDark = new Color(0.05f, 0.08f, 0.05f, 0.98f);         // Koyu yesil-siyah
    private static Color bgMedium = new Color(0.12f, 0.15f, 0.10f, 0.95f);       // Orta koyu yesil
    private static Color accentGreen = new Color(0.2f, 0.5f, 0.2f, 1f);          // CRT yesil
    private static Color textGreen = new Color(0.3f, 0.7f, 0.3f, 1f);            // Parlak terminal yesil
    private static Color textDim = new Color(0.4f, 0.45f, 0.35f, 1f);            // Soluk yesil
    private static Color btnBg = new Color(0.15f, 0.12f, 0.08f, 0.9f);           // Kahverengi-gri buton
    private static Color btnBorder = new Color(0.3f, 0.25f, 0.15f, 1f);          // Vintage kenar
    private static Color warningRed = new Color(0.6f, 0.15f, 0.1f, 1f);          // Koyu kirmizi
    private static Color inputBg = new Color(0.08f, 0.1f, 0.06f, 0.95f);         // Input alani

    [MenuItem("Tools/ECHOES/Create Retro Horror UI")]
    static void CreateNetworkUI()
    {
        // Ana Canvas olustur
        GameObject canvasObj = new GameObject("NetworkCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // NetworkUI scripti ekle
        NetworkUI networkUI = canvasObj.AddComponent<NetworkUI>();

        // ===== 1. ANA MENU PANELI =====
        GameObject mainMenuPanel = CreateRetroPanel(canvasObj.transform, "MainMenuPanel");

        // CRT Scanline efekti (overlay)
        GameObject scanlines = CreateScanlineOverlay(mainMenuPanel.transform);

        // Ana baslik - ECHOES sistemi
        GameObject titleContainer = new GameObject("TitleContainer");
        titleContainer.transform.SetParent(mainMenuPanel.transform);
        RectTransform titleContainerRect = titleContainer.AddComponent<RectTransform>();
        titleContainerRect.anchoredPosition = new Vector2(0, 280);
        titleContainerRect.sizeDelta = new Vector2(800, 200);
        titleContainerRect.localScale = Vector3.one;

        // [CLASSIFIED] etiketi
        GameObject classified = CreateRetroText(titleContainer.transform, "Classified", "[CLASSIFIED]", 18, textDim);
        RectTransform classRect = classified.GetComponent<RectTransform>();
        classRect.anchoredPosition = new Vector2(0, 60);

        // Ana baslik
        GameObject title = CreateRetroText(titleContainer.transform, "Title", "E.C.H.O.E.S.", 56, textGreen);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 10);

        // Alt baslik
        GameObject subtitle = CreateRetroText(titleContainer.transform, "Subtitle", "EXPERIMENTAL COGNITIVE HALLUCINATION OBSERVATION SYSTEM", 14, textDim);
        RectTransform subRect = subtitle.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, -40);

        // Dosya numarasi
        GameObject fileNum = CreateRetroText(titleContainer.transform, "FileNumber", "FILE NO: ECH-1987-0042", 12, new Color(0.5f, 0.4f, 0.3f));
        RectTransform fileRect = fileNum.GetComponent<RectTransform>();
        fileRect.anchoredPosition = new Vector2(0, -70);

        // Ayirici cizgi
        GameObject divider = CreateDivider(mainMenuPanel.transform, new Vector2(0, 150));

        // HOST butonu
        GameObject hostBtn = CreateRetroButton(mainMenuPanel.transform, "HostButton", "[ OTURUM BASLAT ]", new Vector2(0, 80));
        
        // IP input field
        GameObject ipLabel = CreateRetroText(mainMenuPanel.transform, "IPLabel", "HEDEF SUNUCU IP:", 14, textDim);
        RectTransform ipLabelRect = ipLabel.GetComponent<RectTransform>();
        ipLabelRect.anchoredPosition = new Vector2(-80, 10);
        ipLabelRect.sizeDelta = new Vector2(200, 30);

        GameObject ipFieldObj = CreateRetroInputField(mainMenuPanel.transform, "IPInputField", "127.0.0.1", new Vector2(80, 10));
        
        // CLIENT butonu
        GameObject clientBtn = CreateRetroButton(mainMenuPanel.transform, "ClientButton", "[ OTURUMA BAGLAN ]", new Vector2(0, -60));

        // Alt uyari metni
        GameObject warning = CreateRetroText(mainMenuPanel.transform, "Warning", "! IZINSIZ ERISIM YASAKTIR - SEVIYE 4 GUVENLIK PROTOKOLU !", 12, warningRed);
        RectTransform warnRect = warning.GetComponent<RectTransform>();
        warnRect.anchoredPosition = new Vector2(0, -150);

        // ===== 2. LOBI PANELI =====
        GameObject lobbyPanel = CreateRetroPanel(canvasObj.transform, "LobbyPanel");
        lobbyPanel.SetActive(false);

        // Lobi scanlines
        CreateScanlineOverlay(lobbyPanel.transform);

        // Lobi basligi
        GameObject lobbyTitle = CreateRetroText(lobbyPanel.transform, "LobbyTitle", "[ AKTIF OTURUM ]", 36, textGreen);
        RectTransform lobbyTitleRect = lobbyTitle.GetComponent<RectTransform>();
        lobbyTitleRect.anchoredPosition = new Vector2(0, 350);

        // Dosya durumu
        GameObject statusHeader = CreateRetroText(lobbyPanel.transform, "StatusHeader", "BAGLANTI DURUMU:", 16, textDim);
        RectTransform statusHeaderRect = statusHeader.GetComponent<RectTransform>();
        statusHeaderRect.anchoredPosition = new Vector2(0, 280);

        // Oyuncu sayisi (buyuk)
        GameObject playerCount = CreateRetroText(lobbyPanel.transform, "PlayerCountText", "AJANLAR: 1/4", 28, textGreen);
        RectTransform playerCountRect = playerCount.GetComponent<RectTransform>();
        playerCountRect.anchoredPosition = new Vector2(0, 220);

        // Durum texti
        GameObject statusText = CreateRetroText(lobbyPanel.transform, "StatusText", "> SUNUCU AKTIF...", 20, accentGreen);
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 160);

        // Terminal ciktisi gorunumu
        GameObject terminalBg = new GameObject("TerminalOutput");
        terminalBg.transform.SetParent(lobbyPanel.transform);
        RectTransform termBgRect = terminalBg.AddComponent<RectTransform>();
        termBgRect.anchoredPosition = new Vector2(0, 0);
        termBgRect.sizeDelta = new Vector2(600, 200);
        termBgRect.localScale = Vector3.one;
        Image termBgImg = terminalBg.AddComponent<Image>();
        termBgImg.color = new Color(0.02f, 0.04f, 0.02f, 0.8f);

        GameObject terminalText = CreateRetroText(terminalBg.transform, "TerminalLines", 
            "> Sistem baslatildi...\n> Baglanti sinyali algilandi\n> Sifreleme protokolu aktif\n> Bekleniyor: Ek operatorler...\n> _", 
            14, new Color(0.25f, 0.55f, 0.25f));
        RectTransform termTextRect = terminalText.GetComponent<RectTransform>();
        termTextRect.anchorMin = Vector2.zero;
        termTextRect.anchorMax = Vector2.one;
        termTextRect.offsetMin = new Vector2(20, 10);
        termTextRect.offsetMax = new Vector2(-20, -10);
        TextMeshProUGUI termTMP = terminalText.GetComponent<TextMeshProUGUI>();
        termTMP.alignment = TextAlignmentOptions.TopLeft;

        // Oyunu baslat butonu (sadece host icin)
        GameObject startGameBtn = CreateRetroButton(lobbyPanel.transform, "StartGameButton", "[ OPERASYONU BASLAT ]", new Vector2(0, -180));
        Image startImg = startGameBtn.GetComponent<Image>();
        startImg.color = new Color(0.1f, 0.2f, 0.1f, 0.9f);

        // Baglanti kes butonu
        GameObject disconnectBtn = CreateRetroButton(lobbyPanel.transform, "DisconnectButton", "[ OTURUMU SONLANDIR ]", new Vector2(0, -260));
        Image dcImg = disconnectBtn.GetComponent<Image>();
        dcImg.color = new Color(0.25f, 0.08f, 0.05f, 0.9f);

        // ===== 3. HUD PANELI =====
        GameObject hudPanel = CreateRetroPanel(canvasObj.transform, "HUDPanel");
        hudPanel.SetActive(false);
        hudPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0); // Gorunmez arka plan

        // Sag ust kose - oyuncu sayisi
        GameObject hudPlayerCount = CreateRetroText(hudPanel.transform, "HUDPlayerCount", "AJANLAR: 1/4", 16, textGreen);
        RectTransform hudCountRect = hudPlayerCount.GetComponent<RectTransform>();
        hudCountRect.anchorMin = new Vector2(1, 1);
        hudCountRect.anchorMax = new Vector2(1, 1);
        hudCountRect.pivot = new Vector2(1, 1);
        hudCountRect.anchoredPosition = new Vector2(-30, -30);
        hudCountRect.sizeDelta = new Vector2(200, 40);

        // Sol alt kose - durum
        GameObject hudStatus = CreateRetroText(hudPanel.transform, "HUDStatus", "[REC] OPERASYON AKTIF", 14, warningRed);
        RectTransform hudStatusRect = hudStatus.GetComponent<RectTransform>();
        hudStatusRect.anchorMin = new Vector2(0, 0);
        hudStatusRect.anchorMax = new Vector2(0, 0);
        hudStatusRect.pivot = new Vector2(0, 0);
        hudStatusRect.anchoredPosition = new Vector2(30, 30);
        hudStatusRect.sizeDelta = new Vector2(300, 40);

        // NetworkUI referanslarini bagla
        SerializedObject so = new SerializedObject(networkUI);
        so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
        so.FindProperty("clientButton").objectReferenceValue = clientBtn.GetComponent<Button>();
        so.FindProperty("disconnectButton").objectReferenceValue = disconnectBtn.GetComponent<Button>();
        so.FindProperty("ipInputField").objectReferenceValue = ipFieldObj.GetComponent<TMP_InputField>();
        so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("playerCountText").objectReferenceValue = playerCount.GetComponent<TextMeshProUGUI>();
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        so.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
        so.FindProperty("hudPanel").objectReferenceValue = hudPanel;
        so.ApplyModifiedProperties();

        // EventSystem ekle
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Prefab olarak kaydet
        string prefabPath = "Assets/Prefabs/NetworkCanvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
        
        Debug.Log("<color=green>RETRO HORROR UI OLUSTURULDU!</color>");
        Selection.activeGameObject = canvasObj;
    }

    // Retro panel olustur
    static GameObject CreateRetroPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        
        Image img = panel.AddComponent<Image>();
        img.color = bgDark;
        
        return panel;
    }

    // Retro buton olustur
    static GameObject CreateRetroButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 50);
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = btnBg;
        
        // Kenar cizgisi efekti icin Outline
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = btnBorder;
        outline.effectDistance = new Vector2(2, 2);
        
        Button btn = btnObj.AddComponent<Button>();
        
        // Hover efekti
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = colors;
        
        // Buton metni
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localScale = Vector3.one;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textGreen;
        tmp.fontStyle = FontStyles.Bold;
        
        return btnObj;
    }

    // Retro text olustur
    static GameObject CreateRetroText(Transform parent, string name, string text, int fontSize, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 60);
        rect.localScale = Vector3.one;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        
        return textObj;
    }

    // Retro input field olustur
    static GameObject CreateRetroInputField(Transform parent, string name, string defaultText, Vector2 position)
    {
        GameObject fieldObj = new GameObject(name);
        fieldObj.transform.SetParent(parent);
        
        RectTransform rect = fieldObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 35);
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        
        Image img = fieldObj.AddComponent<Image>();
        img.color = inputBg;
        
        Outline outline = fieldObj.AddComponent<Outline>();
        outline.effectColor = accentGreen;
        outline.effectDistance = new Vector2(1, 1);
        
        TMP_InputField inputField = fieldObj.AddComponent<TMP_InputField>();
        
        // Text area
        GameObject textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(fieldObj.transform);
        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 0);
        textAreaRect.offsetMax = new Vector2(-10, 0);
        textAreaRect.localScale = Vector3.one;
        
        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textAreaObj.transform);
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        placeholderRect.localScale = Vector3.one;
        
        TextMeshProUGUI placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = defaultText;
        placeholderTmp.fontSize = 16;
        placeholderTmp.color = textDim;
        placeholderTmp.alignment = TextAlignmentOptions.Left;
        
        // Input text
        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textAreaObj.transform);
        RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;
        inputTextRect.localScale = Vector3.one;
        
        TextMeshProUGUI inputTmp = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputTmp.text = "";
        inputTmp.fontSize = 16;
        inputTmp.color = textGreen;
        inputTmp.alignment = TextAlignmentOptions.Left;
        
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputTmp;
        inputField.placeholder = placeholderTmp;
        
        return fieldObj;
    }

    // Ayirici cizgi
    static GameObject CreateDivider(Transform parent, Vector2 position)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent);
        
        RectTransform rect = divider.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 2);
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        
        Image img = divider.AddComponent<Image>();
        img.color = new Color(0.3f, 0.4f, 0.3f, 0.5f);
        
        return divider;
    }

    // CRT Scanline efekti
    static GameObject CreateScanlineOverlay(Transform parent)
    {
        GameObject scanlines = new GameObject("Scanlines");
        scanlines.transform.SetParent(parent);
        
        RectTransform rect = scanlines.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        
        // Basit scanline efekti icin yari saydam siyah
        Image img = scanlines.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.1f);
        img.raycastTarget = false; // Tiklamayi engellemez
        
        return scanlines;
    }
#endif
}
