using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance;

    [Header("Settings")]
    public float miniMapZoom = 25f;
    public float bigMapZoom = 50f;
    public float minZoom = 10f;
    public float maxZoom = 150f;
    public LayerMask mapLayers; // Layers to render
    public KeyCode toggleKey = KeyCode.M;
    public Color mapBgColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    public bool IsBigMapOpen => isBigMapOpen;

    // Private Member Variables
    private Transform playerTransform;
    private bool isBigMapOpen = false;
    private Camera mapCamera;
    private RenderTexture mapRenderTexture;
    private GameObject miniMapPanel;
    private GameObject bigMapPanel;
    private RawImage miniMapImage;
    private RawImage bigMapImage;
    private Text bigMapCoordsText;
    private float savedTimeScale = 1f;

    private List<MapMarker> activeMarkers = new List<MapMarker>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("[MapSystem] Player not found!");
            return;
        }

        SetupMapCamera();
        CreateMiniMapUI();
        CreateBigMapUI();
        
        // Auto-add dynamic marker to player if missing
        var existingMarker = playerTransform.GetComponent<MapMarker>();
        if (existingMarker == null)
        {
            var pm = playerTransform.gameObject.AddComponent<MapMarker>();
            pm.type = MapMarker.MarkerType.Player;
            
            // Try load icon, else generate one
            Sprite arrowSprite = Resources.Load<Sprite>("UI/ArrowIcon");
            if (arrowSprite == null) arrowSprite = GenerateArrowSprite();
            
            pm.icon = arrowSprite;
            pm.color = Color.green;
            pm.iconSize = 4f;
            pm.rotateWithObject = true;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Toggle Big Map
        if (Input.GetKeyDown(toggleKey))
        {
            // Check inventory conflict
            if (InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen) 
            {
                return; 
            }
            ToggleBigMap();
        }

        // Camera Update Logic
        if (mapCamera != null)
        {
            if (isBigMapOpen)
            {
                HandleBigMapInput();
            }
            else
            {
                // Mini map follows player strictly
                Vector3 targetPos = playerTransform.position;
                targetPos.y = 100f; // Height
                mapCamera.transform.position = targetPos;
                mapCamera.orthographicSize = miniMapZoom;
                
                // Reset rotation to top-down north-up
                mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }

    void ToggleBigMap()
    {
        isBigMapOpen = !isBigMapOpen;

        if (bigMapPanel != null) bigMapPanel.SetActive(isBigMapOpen);
        if (miniMapPanel != null) miniMapPanel.SetActive(!isBigMapOpen);

        if (isBigMapOpen)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = savedTimeScale > 0 ? savedTimeScale : 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleBigMapInput()
    {
        // 1. Zoom with Scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            bigMapZoom -= scroll * 30f;
            bigMapZoom = Mathf.Clamp(bigMapZoom, minZoom, maxZoom);
        }
        
        // 2. Pan with WASD / Arrow Keys
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Speed up with Shift
        float speed = (Input.GetKey(KeyCode.LeftShift) ? 100f : 40f) * Time.unscaledDeltaTime;
        
        // Pan logic
        Vector3 currentCamPos = mapCamera.transform.position;
        currentCamPos.x += h * speed * (bigMapZoom/20f); // Scale pan speed by zoom
        currentCamPos.z += v * speed * (bigMapZoom/20f);
        
        mapCamera.transform.position = currentCamPos;
        mapCamera.orthographicSize = bigMapZoom; // Apply zoom
        
        // Update coordinates text
        if (bigMapCoordsText != null)
        {
            bigMapCoordsText.text = $"POS: {Mathf.Round(playerTransform.position.x)}, {Mathf.Round(playerTransform.position.z)}";
        }
    }

    // ===================================================================================
    // HELPER: Generate Arrow Sprite (Triangle)
    // ===================================================================================
    Sprite GenerateArrowSprite()
    {
        int width = 64;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] fill = new Color[width * height];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.clear;
        tex.SetPixels(fill);

        Vector2 p1 = new Vector2(width/2, height); // Top
        Vector2 p2 = new Vector2(0, 0); // Bottom Left
        Vector2 p3 = new Vector2(width, 0); // Bottom Right

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsPointInTriangle(new Vector2(x, y), p1, p2, p3))
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    bool IsPointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
        var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;
        if ((s < 0) != (t < 0)) return false;
        var A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
        return A < 0 ? (s <= 0 && s + t >= A) : (s >= 0 && s + t <= A);
    }

    // ===================================================================================
    // SETUP
    // ===================================================================================

    void SetupMapCamera()
    {
        GameObject camObj = new GameObject("MapCamera");
        camObj.transform.SetParent(transform);
        mapCamera = camObj.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.cullingMask = mapLayers;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = mapBgColor;
        mapCamera.orthographicSize = miniMapZoom;
        
        // Point down
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // High Quality Render Texture
        mapRenderTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
        mapRenderTexture.Create();
        mapCamera.targetTexture = mapRenderTexture;
    }

    void CreateMiniMapUI()
    {
        GameObject canvasObj = GetOrCreateCanvas();
        
        // Container (Top Right)
        miniMapPanel = new GameObject("MiniMapPanel");
        miniMapPanel.transform.SetParent(canvasObj.transform, false);
        
        RectTransform rt = miniMapPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(240, 240);

        // Circular Mask
        Image mask = miniMapPanel.AddComponent<Image>();
        mask.sprite = Resources.Load<Sprite>("Knob"); 
        
        Mask maskComp = miniMapPanel.AddComponent<Mask>();
        maskComp.showMaskGraphic = true;

        // Raw Image
        GameObject rawImgObj = new GameObject("MapRender");
        rawImgObj.transform.SetParent(miniMapPanel.transform, false);
        miniMapImage = rawImgObj.AddComponent<RawImage>();
        miniMapImage.texture = mapRenderTexture;
        RectTransform rawRect = miniMapImage.rectTransform;
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.sizeDelta = Vector2.zero;
        
        // Border Ring
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(miniMapPanel.transform, false);
        Image border = borderObj.AddComponent<Image>();
        border.color = Color.clear;
        Outline outline = borderObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.6f, 0.7f, 0.8f);
        outline.effectDistance = new Vector2(3, -3);
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;

        // Center Icon (Static)
        GameObject centerIcon = new GameObject("CenterMarker");
        centerIcon.transform.SetParent(miniMapPanel.transform, false);
        Image centerImg = centerIcon.AddComponent<Image>();
        centerImg.color = Color.green;
        RectTransform centerRect = centerImg.rectTransform;
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(10, 10);
        
        // Label N (North)
        GameObject northObj = new GameObject("NorthLabel");
        northObj.transform.SetParent(miniMapPanel.transform, false);
        Text northText = northObj.AddComponent<Text>();
        northText.text = "N";
        northText.fontStyle = FontStyle.Bold;
        northText.alignment = TextAnchor.MiddleCenter;
        northText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        northText.color = new Color(0.8f, 0.8f, 1f, 0.8f);
        RectTransform northRect = northText.rectTransform;
        northRect.anchorMin = new Vector2(0.5f, 1);
        northRect.anchorMax = new Vector2(0.5f, 1);
        northRect.anchoredPosition = new Vector2(0, -15);
    }

    void CreateBigMapUI()
    {
        GameObject canvasObj = GetOrCreateCanvas();

        bigMapPanel = new GameObject("BigMapPanel");
        bigMapPanel.transform.SetParent(canvasObj.transform, false);
        bigMapPanel.SetActive(false);

        // Full screen background
        Image bg = bigMapPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        
        // Map Container
        GameObject contentObj = new GameObject("MapContent");
        contentObj.transform.SetParent(bigMapPanel.transform, false);
        bigMapImage = contentObj.AddComponent<RawImage>();
        bigMapImage.texture = mapRenderTexture;
        RectTransform mapRect = bigMapImage.rectTransform;
        mapRect.anchorMin = new Vector2(0.05f, 0.05f);
        mapRect.anchorMax = new Vector2(0.95f, 0.95f);
        
        // UI Overlay for coords/info
        GameObject infoObj = new GameObject("MapInfo");
        infoObj.transform.SetParent(bigMapPanel.transform, false);
        bigMapCoordsText = infoObj.AddComponent<Text>();
        bigMapCoordsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bigMapCoordsText.fontSize = 18;
        bigMapCoordsText.color = new Color(0.7f, 0.7f, 0.7f);
        bigMapCoordsText.alignment = TextAnchor.MiddleRight;
        RectTransform infoRect = bigMapCoordsText.rectTransform;
        infoRect.anchorMin = new Vector2(1, 0);
        infoRect.anchorMax = new Vector2(1, 0);
        infoRect.pivot = new Vector2(1, 0);
        infoRect.anchoredPosition = new Vector2(-50, 50);
        
        // Instructions
        GameObject instrObj = new GameObject("MapInstructions");
        instrObj.transform.SetParent(bigMapPanel.transform, false);
        Text instrText = instrObj.AddComponent<Text>();
        instrText.text = "[M] Exit   [Scroll] Zoom   [Drag/WASD] Pan";
        instrText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        instrText.fontSize = 20;
        instrText.alignment = TextAnchor.MiddleCenter;
        instrText.color = Color.white;
        RectTransform instrRect = instrText.rectTransform;
        instrRect.anchorMin = new Vector2(0.5f, 0);
        instrRect.anchorMax = new Vector2(0.5f, 0);
        instrRect.anchoredPosition = new Vector2(0, 30);
        instrRect.sizeDelta = new Vector2(500, 40);
    }
    
    GameObject GetOrCreateCanvas()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("PlayerHUDCanvas");
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10; // Ensure it's on top
            
            CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[MapSystem] Created missing PlayerHUDCanvas.");
        }
        return canvasObj;
    }

    public void RegisterMarker(MapMarker marker)
    {
        if (!activeMarkers.Contains(marker)) activeMarkers.Add(marker);
    }

    public void UnregisterMarker(MapMarker marker)
    {
        if (activeMarkers.Contains(marker)) activeMarkers.Remove(marker);
    }
}
