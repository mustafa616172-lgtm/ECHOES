using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player danger level based on enemy proximity and awareness.
/// Sleek design at top-right corner. Green=Safe, Yellow=Caution, Red=Danger
/// </summary>
public class AwarenessIndicator : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color safeColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color cautionColor = new Color(1f, 0.8f, 0f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.15f, 0.15f, 1f);
    
    [Header("Distance Settings")]
    [SerializeField] private float safeDistance = 25f;
    [SerializeField] private float dangerDistance = 8f;
    
    private SimpleEnemyWalker trackedEnemy;
    private Transform playerTransform;
    
    // UI references
    private GameObject uiRoot;
    private Image fillBar;
    private Text eyeText;
    private Text statusLabel;
    private Image outerFrame;
    
    private float currentDanger = 0f;
    private float targetDanger = 0f;
    
    /// <summary>Current danger level 0-100 for other systems</summary>
    public float DangerLevel => currentDanger;
    
    void Start()
    {
        CreateUI();
        FindReferences();
        Debug.Log("[AwarenessIndicator] Started and UI created");
    }
    
    void Update()
    {
        FindReferences();
        CalculateDanger();
        UpdateUI();
    }
    
    void FindReferences()
    {
        if (trackedEnemy == null)
            trackedEnemy = FindObjectOfType<SimpleEnemyWalker>();
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }
    
    void CalculateDanger()
    {
        targetDanger = 0f;
        
        if (trackedEnemy == null || playerTransform == null)
        {
            currentDanger = Mathf.Lerp(currentDanger, 0f, Time.deltaTime * 3f);
            return;
        }
        
        float distance = Vector3.Distance(playerTransform.position, trackedEnemy.transform.position);
        
        if (distance <= dangerDistance)
            targetDanger = 100f;
        else if (distance >= safeDistance)
            targetDanger = 0f;
        else
        {
            float range = safeDistance - dangerDistance;
            float distFromDanger = distance - dangerDistance;
            targetDanger = (1f - (distFromDanger / range)) * 100f;
        }
        
        // Boost from enemy awareness state
        if (trackedEnemy.CurrentAwarenessLevel == SimpleEnemyWalker.AwarenessLevel.Suspicious)
            targetDanger = Mathf.Max(targetDanger, 40f);
        else if (trackedEnemy.CurrentAwarenessLevel == SimpleEnemyWalker.AwarenessLevel.Alert)
            targetDanger = Mathf.Max(targetDanger, 70f);
        else if (trackedEnemy.CurrentAwarenessLevel == SimpleEnemyWalker.AwarenessLevel.Hostile)
            targetDanger = 100f;
        
        currentDanger = Mathf.Lerp(currentDanger, targetDanger, Time.deltaTime * 4f);
    }
    
    void CreateUI()
    {
        // Destroy any old indicator first
        GameObject oldIndicator = GameObject.Find("DangerIndicator");
        if (oldIndicator != null) Destroy(oldIndicator);
        GameObject oldCanvas = GameObject.Find("DangerCanvas");
        if (oldCanvas != null) Destroy(oldCanvas);
        
        // Create dedicated canvas
        GameObject canvasObj = new GameObject("DangerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // === Root container: top-right ===
        uiRoot = new GameObject("DangerIndicator");
        uiRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = uiRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = new Vector2(-20, -15);
        rootRect.sizeDelta = new Vector2(200, 40);
        
        // === Outer frame ===
        GameObject frameObj = new GameObject("Frame");
        frameObj.transform.SetParent(uiRoot.transform, false);
        outerFrame = frameObj.AddComponent<Image>();
        outerFrame.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        SetAnchors(outerFrame.rectTransform, 0, 0, 1, 1);
        
        // === Dark background ===
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(uiRoot.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        RectTransform bgRect = bgImg.rectTransform;
        SetAnchors(bgRect, 0, 0, 1, 1);
        bgRect.offsetMin = new Vector2(2, 2);
        bgRect.offsetMax = new Vector2(-2, -2);
        
        // === Eye icon (separate GO, only Text, no Image) ===
        GameObject eyeObj = new GameObject("EyeIcon");
        eyeObj.transform.SetParent(uiRoot.transform, false);
        eyeText = eyeObj.AddComponent<Text>();
        eyeText.text = "\u25C9";
        eyeText.fontSize = 20;
        eyeText.fontStyle = FontStyle.Bold;
        eyeText.alignment = TextAnchor.MiddleCenter;
        eyeText.color = safeColor;
        eyeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        SetAnchors(eyeText.rectTransform, 0.02f, 0.1f, 0.17f, 0.9f);
        
        Outline eyeOutline = eyeObj.AddComponent<Outline>();
        eyeOutline.effectColor = Color.black;
        eyeOutline.effectDistance = new Vector2(1, -1);
        
        // === Bar background ===
        GameObject barBgObj = new GameObject("BarBG");
        barBgObj.transform.SetParent(uiRoot.transform, false);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        SetAnchors(barBgImg.rectTransform, 0.19f, 0.25f, 0.72f, 0.75f);
        
        // === Fill bar ===
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barBgObj.transform, false);
        fillBar = fillObj.AddComponent<Image>();
        fillBar.color = safeColor;
        RectTransform fillRect = fillBar.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.01f, 1f);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        // === Status label on right ===
        GameObject labelObj = new GameObject("StatusLabel");
        labelObj.transform.SetParent(uiRoot.transform, false);
        statusLabel = labelObj.AddComponent<Text>();
        statusLabel.text = "SAFE";
        statusLabel.fontSize = 12;
        statusLabel.fontStyle = FontStyle.Bold;
        statusLabel.alignment = TextAnchor.MiddleCenter;
        statusLabel.color = safeColor;
        statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        SetAnchors(statusLabel.rectTransform, 0.74f, 0.1f, 0.98f, 0.9f);
        
        Outline labelOutline = labelObj.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0, 0, 0, 0.8f);
        labelOutline.effectDistance = new Vector2(1, -1);
        
        Debug.Log("[AwarenessIndicator] UI created at top-right corner");
    }
    
    /// <summary>Helper to set anchors cleanly</summary>
    void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    
    void UpdateUI()
    {
        if (fillBar == null) return;
        
        float fillAmount = Mathf.Clamp01(currentDanger / 100f);
        fillBar.rectTransform.anchorMax = new Vector2(fillAmount, 1f);
        
        Color barColor;
        string label;
        
        if (currentDanger < 25f)
        {
            barColor = safeColor;
            label = "SAFE";
        }
        else if (currentDanger < 60f)
        {
            float t = (currentDanger - 25f) / 35f;
            barColor = Color.Lerp(safeColor, cautionColor, t);
            label = "CAUTION";
        }
        else
        {
            float t = Mathf.Clamp01((currentDanger - 60f) / 40f);
            barColor = Color.Lerp(cautionColor, dangerColor, t);
            label = "DANGER";
            
            if (currentDanger > 90f)
            {
                float pulse = (Mathf.Sin(Time.time * 6f) + 1f) / 2f;
                barColor = Color.Lerp(barColor, Color.white, pulse * 0.2f);
                label = "DANGER!";
                
                if (outerFrame != null)
                {
                    Color frameColor = Color.Lerp(dangerColor, Color.white, pulse * 0.3f);
                    frameColor.a = 0.9f;
                    outerFrame.color = frameColor;
                }
            }
            else if (outerFrame != null)
            {
                outerFrame.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            }
        }
        
        fillBar.color = barColor;
        
        if (statusLabel != null)
        {
            statusLabel.text = label;
            statusLabel.color = barColor;
        }
        
        if (eyeText != null)
            eyeText.color = barColor;
    }
    
    void OnDestroy()
    {
        // Clean up canvas when this is destroyed
        GameObject dangerCanvas = GameObject.Find("DangerCanvas");
        if (dangerCanvas != null) Destroy(dangerCanvas);
    }
}
