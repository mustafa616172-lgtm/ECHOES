using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    [Header("UI Elements")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText; // Support TMPro
    public Text legacyPromptText; // Support Legacy Text just in case
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (promptPanel == null)
        {
            CreateTemporaryUI();
        }
        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        
        if (promptText != null) promptText.text = message;
        else if (legacyPromptText != null) legacyPromptText.text = message;
    }

    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    private void CreateTemporaryUI()
    {
        // Auto-create UI if missing
        GameObject canvasObj = new GameObject("InteractionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Panel (Center Screen)
        GameObject panelObj = new GameObject("PromptPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        promptPanel = panelObj;
        
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 50);
        rect.anchoredPosition = new Vector2(0, -50); // Slightly below center

        // Background
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panelObj.transform, false);
        legacyPromptText = textObj.AddComponent<Text>();
        legacyPromptText.alignment = TextAnchor.MiddleCenter;
        legacyPromptText.color = Color.white;
        legacyPromptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        legacyPromptText.fontSize = 24;
        legacyPromptText.rectTransform.sizeDelta = new Vector2(300, 50);
        
        Debug.Log("[InteractionUI] Temporary UI Created");
    }
}
