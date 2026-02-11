using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ECHOES Map System - Minimap + Full Map (M key)
/// Creates an overhead camera that renders the level layout.
/// </summary>
public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance { get; private set; }

    [Header("Map Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    [SerializeField] private float cameraHeight = 50f;
    [SerializeField] private float minimapOrthoSize = 25f;
    [SerializeField] private float fullmapOrthoSize = 60f;

    [Header("Minimap UI")]
    [SerializeField] private int minimapSize = 220;
    [SerializeField] private int minimapMargin = 15;

    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 50f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 20f;
    [SerializeField] private float maxZoom = 120f;

    // Internal state
    private Camera mapCamera;
    private Light mapLight;
    private Transform playerTransform;
    private bool isBigMapOpen = false;
    private float savedTimeScale = 1f;

    // RenderTextures
    private RenderTexture minimapRT;
    private RenderTexture fullmapRT;

    // UI elements
    private Canvas mapCanvas;
    private RawImage minimapImage;
    private RawImage fullmapImage;
    private GameObject minimapPanel;
    private GameObject fullmapPanel;
    private GameObject fullmapPlayerArrow;
    private Text controlsHintText;
    private Text zoomText;

    // Markers
    private List<MapMarker> markers = new List<MapMarker>();

    // Pan offset for full map
    private Vector3 panOffset = Vector3.zero;
    private float currentZoom;

    // Player arrow on minimap
    private GameObject minimapPlayerArrow;

    public bool IsBigMapOpen => isBigMapOpen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        currentZoom = fullmapOrthoSize;
        StartCoroutine(InitAfterSpawn());
    }

    IEnumerator InitAfterSpawn()
    {
        yield return null;

        playerTransform = FindPlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogError("[MapSystem] Player not found!");
            enabled = false;
            yield break;
        }

        CreateRenderTextures();
        CreateMapCamera();
        CreateMapLight();
        CreateUI();
        CreateMinimapPlayerArrow();

        Debug.Log("[MapSystem] Initialized successfully");
    }

    Transform FindPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player.transform;

        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) return pc.transform;

        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null) return cc.transform;

        return null;
    }

    void CreateRenderTextures()
    {
        minimapRT = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        minimapRT.antiAliasing = 2;
        minimapRT.filterMode = FilterMode.Bilinear;
        minimapRT.Create();

        fullmapRT = new RenderTexture(2048, 2048, 24, RenderTextureFormat.ARGB32);
        fullmapRT.antiAliasing = 2;
        fullmapRT.filterMode = FilterMode.Bilinear;
        fullmapRT.Create();
    }

    void CreateMapCamera()
    {
        GameObject camObj = new GameObject("MapCamera");
        camObj.transform.SetParent(transform);
        mapCamera = camObj.AddComponent<Camera>();

        mapCamera.orthographic = true;
        mapCamera.orthographicSize = minimapOrthoSize;

        Vector3 pos = playerTransform.position;
        camObj.transform.position = new Vector3(pos.x, pos.y + cameraHeight, pos.z);
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Solid color background so unlit areas show as dark gray, not void
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        mapCamera.nearClipPlane = 0.3f;
        mapCamera.farClipPlane = cameraHeight + 50f;
        mapCamera.depth = -10;
        mapCamera.targetTexture = minimapRT;

        // Render everything visible (not just Minimap layer)
        mapCamera.cullingMask = ~0;

        AudioListener listener = camObj.GetComponent<AudioListener>();
        if (listener != null) Destroy(listener);

        Debug.Log("[MapSystem] Camera created at height " + cameraHeight);
    }

    void CreateMapLight()
    {
        GameObject lightObj = new GameObject("MapLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.position = new Vector3(0, cameraHeight + 10f, 0);
        lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        mapLight = lightObj.AddComponent<Light>();
        mapLight.type = LightType.Directional;
        mapLight.color = new Color(0.9f, 0.9f, 1f);
        mapLight.intensity = 1.5f;
        mapLight.shadows = LightShadows.None;

        // Only affect Minimap layer so gameplay lighting stays untouched
        int minimapLayer = LayerMask.NameToLayer("Minimap");
        if (minimapLayer >= 0)
        {
            mapLight.cullingMask = (1 << minimapLayer);
        }
        else
        {
            mapLight.cullingMask = 0;
            Debug.LogWarning("[MapSystem] 'Minimap' layer not found. MapLight will not affect any objects.");
        }

        Debug.Log("[MapSystem] MapLight created");
    }

    // ============================================
    // UI CREATION
    // ============================================

    void CreateUI()
    {
        GameObject canvasObj = new GameObject("MapCanvas");
        canvasObj.transform.SetParent(transform);
        mapCanvas = canvasObj.AddComponent<Canvas>();
        mapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mapCanvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        CreateMinimapUI(canvasObj);
        CreateFullmapUI(canvasObj);
    }

    void CreateMinimapUI(GameObject canvasObj)
    {
        minimapPanel = new GameObject("MinimapPanel");
        minimapPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = minimapPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-minimapMargin, minimapMargin);
        panelRect.sizeDelta = new Vector2(minimapSize, minimapSize);

        Image border = minimapPanel.AddComponent<Image>();
        border.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        border.raycastTarget = false;

        GameObject mapImgObj = new GameObject("MinimapImage");
        mapImgObj.transform.SetParent(minimapPanel.transform, false);

        minimapImage = mapImgObj.AddComponent<RawImage>();
        minimapImage.texture = minimapRT;
        minimapImage.raycastTarget = false;

        RectTransform imgRect = mapImgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = new Vector2(3, 3);
        imgRect.offsetMax = new Vector2(-3, -3);

        GameObject hintObj = new GameObject("MapHint");
        hintObj.transform.SetParent(minimapPanel.transform, false);
        Text hint = hintObj.AddComponent<Text>();
        hint.text = "[M] Harita";
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 12;
        hint.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        hint.alignment = TextAnchor.UpperCenter;
        hint.raycastTarget = false;

        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 1);
        hintRect.anchorMax = new Vector2(1, 1);
        hintRect.pivot = new Vector2(0.5f, 0);
        hintRect.anchoredPosition = new Vector2(0, 5);
        hintRect.sizeDelta = new Vector2(0, 20);
    }

    void CreateFullmapUI(GameObject canvasObj)
    {
        fullmapPanel = new GameObject("FullmapPanel");
        fullmapPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = fullmapPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image bg = fullmapPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        bg.raycastTarget = false;

        GameObject mapImgObj = new GameObject("FullmapImage");
        mapImgObj.transform.SetParent(fullmapPanel.transform, false);

        fullmapImage = mapImgObj.AddComponent<RawImage>();
        fullmapImage.texture = fullmapRT;
        fullmapImage.raycastTarget = false;

        RectTransform imgRect = mapImgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.02f, 0.05f);
        imgRect.anchorMax = new Vector2(0.98f, 0.95f);
        imgRect.sizeDelta = Vector2.zero;

        CreateFullmapPlayerArrow(mapImgObj);

        GameObject ctrlObj = new GameObject("ControlsHint");
        ctrlObj.transform.SetParent(fullmapPanel.transform, false);
        controlsHintText = ctrlObj.AddComponent<Text>();
        controlsHintText.text = "WASD: Kaydir  |  Scroll: Zoom  |  R: Oyuncuya Don  |  M/ESC: Kapat";
        controlsHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        controlsHintText.fontSize = 16;
        controlsHintText.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        controlsHintText.alignment = TextAnchor.MiddleCenter;
        controlsHintText.raycastTarget = false;

        RectTransform ctrlRect = ctrlObj.GetComponent<RectTransform>();
        ctrlRect.anchorMin = new Vector2(0, 0);
        ctrlRect.anchorMax = new Vector2(1, 0);
        ctrlRect.pivot = new Vector2(0.5f, 0);
        ctrlRect.anchoredPosition = new Vector2(0, 10);
        ctrlRect.sizeDelta = new Vector2(0, 30);

        GameObject zoomObj = new GameObject("ZoomText");
        zoomObj.transform.SetParent(fullmapPanel.transform, false);
        zoomText = zoomObj.AddComponent<Text>();
        zoomText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        zoomText.fontSize = 14;
        zoomText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        zoomText.alignment = TextAnchor.UpperRight;
        zoomText.raycastTarget = false;

        RectTransform zoomRect = zoomObj.GetComponent<RectTransform>();
        zoomRect.anchorMin = new Vector2(1, 1);
        zoomRect.anchorMax = new Vector2(1, 1);
        zoomRect.pivot = new Vector2(1, 1);
        zoomRect.anchoredPosition = new Vector2(-20, -10);
        zoomRect.sizeDelta = new Vector2(200, 30);

        fullmapPanel.SetActive(false);
    }

    void CreateFullmapPlayerArrow(GameObject parent)
    {
        fullmapPlayerArrow = new GameObject("PlayerArrow");
        fullmapPlayerArrow.transform.SetParent(parent.transform, false);

        Text arrow = fullmapPlayerArrow.AddComponent<Text>();
        arrow.text = "\u25B2";
        arrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        arrow.fontSize = 24;
        arrow.color = Color.green;
        arrow.alignment = TextAnchor.MiddleCenter;
        arrow.raycastTarget = false;

        RectTransform rect = fullmapPlayerArrow.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(30, 30);
    }

    void CreateMinimapPlayerArrow()
    {
        if (minimapPanel == null) return;

        Transform imgTransform = minimapPanel.transform.Find("MinimapImage");
        if (imgTransform == null) return;

        minimapPlayerArrow = new GameObject("MinimapArrow");
        minimapPlayerArrow.transform.SetParent(imgTransform, false);

        Text arrow = minimapPlayerArrow.AddComponent<Text>();
        arrow.text = "\u25B2";
        arrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        arrow.fontSize = 18;
        arrow.color = Color.green;
        arrow.alignment = TextAnchor.MiddleCenter;
        arrow.raycastTarget = false;

        RectTransform rect = minimapPlayerArrow.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(24, 24);
        rect.anchoredPosition = Vector2.zero;
    }

    // ============================================
    // UPDATE LOGIC
    // ============================================

    void Update()
    {
        if (playerTransform == null || mapCamera == null) return;
        HandleInput();
    }

    void LateUpdate()
    {
        if (playerTransform == null || mapCamera == null) return;

        if (isBigMapOpen)
            UpdateFullmapCamera();
        else
            UpdateMinimapCamera();

        UpdateMinimapPlayerArrow();
    }

    void UpdateMinimapCamera()
    {
        Vector3 pos = playerTransform.position;
        mapCamera.transform.position = new Vector3(pos.x, pos.y + cameraHeight, pos.z);
        mapCamera.orthographicSize = minimapOrthoSize;
        mapCamera.targetTexture = minimapRT;
    }

    void UpdateFullmapCamera()
    {
        Vector3 pos = playerTransform.position + panOffset;
        mapCamera.transform.position = new Vector3(pos.x, pos.y + cameraHeight, pos.z);
        mapCamera.orthographicSize = currentZoom;
        mapCamera.targetTexture = fullmapRT;

        // Force render after moving camera to prevent black frame artifacts
        mapCamera.Render();

        UpdateFullmapPlayerArrow();
        UpdateZoomText();
    }

    void UpdateMinimapPlayerArrow()
    {
        if (minimapPlayerArrow == null) return;
        float playerYaw = playerTransform.eulerAngles.y;
        minimapPlayerArrow.transform.localRotation = Quaternion.Euler(0, 0, -playerYaw);
    }

    void UpdateFullmapPlayerArrow()
    {
        if (fullmapPlayerArrow == null || fullmapImage == null) return;

        Vector3 camPos = mapCamera.transform.position;
        Vector3 playerPos = playerTransform.position;

        float halfSize = currentZoom;
        float aspect = mapCamera.aspect;

        float dx = playerPos.x - camPos.x;
        float dz = playerPos.z - camPos.z;

        float nx = dx / (halfSize * aspect);
        float nz = dz / halfSize;

        RectTransform parentRect = fullmapImage.GetComponent<RectTransform>();
        float uiX = nx * parentRect.rect.width * 0.5f;
        float uiY = nz * parentRect.rect.height * 0.5f;

        RectTransform arrowRect = fullmapPlayerArrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(uiX, uiY);

        float playerYaw = playerTransform.eulerAngles.y;
        arrowRect.localRotation = Quaternion.Euler(0, 0, -playerYaw);
    }

    void UpdateZoomText()
    {
        if (zoomText == null) return;
        float zoomPercent = Mathf.InverseLerp(maxZoom, minZoom, currentZoom) * 100f;
        zoomText.text = "Zoom: " + Mathf.RoundToInt(zoomPercent) + "%";
    }

    // ============================================
    // INPUT
    // ============================================

    void HandleInput()
    {
        if (IsAnyUIBlocking()) return;

        if (Input.GetKeyDown(toggleKey))
        {
            if (isBigMapOpen)
                CloseFullMap();
            else
                OpenFullMap();
        }

        if (isBigMapOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseFullMap();
        }

        if (isBigMapOpen)
        {
            float dt = Time.unscaledDeltaTime;

            float moveX = 0f;
            float moveZ = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveZ += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveZ -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX += 1f;

            if (moveX != 0f || moveZ != 0f)
            {
                panOffset += new Vector3(moveX, 0f, moveZ) * panSpeed * dt * (currentZoom / fullmapOrthoSize);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom -= scroll * zoomSpeed * 5f;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                panOffset = Vector3.zero;
                currentZoom = fullmapOrthoSize;
            }
        }
    }

    bool IsAnyUIBlocking()
    {
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen)
            return true;

        if (InventorySystem.Instance != null && InventorySystem.Instance.IsReadingNote)
            return true;

        if (!isBigMapOpen && Cursor.lockState == CursorLockMode.None)
            return true;

        return false;
    }

    // ============================================
    // FULL MAP OPEN / CLOSE
    // ============================================

    void OpenFullMap()
    {
        isBigMapOpen = true;

        mapCamera.targetTexture = fullmapRT;
        mapCamera.orthographicSize = currentZoom;

        panOffset = Vector3.zero;
        currentZoom = fullmapOrthoSize;

        fullmapPanel.SetActive(true);
        minimapPanel.SetActive(false);

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Force immediate render so map is not blank on first frame
        UpdateFullmapCamera();

        Debug.Log("[MapSystem] Full map opened");
    }

    void CloseFullMap()
    {
        isBigMapOpen = false;

        mapCamera.targetTexture = minimapRT;
        mapCamera.orthographicSize = minimapOrthoSize;

        fullmapPanel.SetActive(false);
        minimapPanel.SetActive(true);

        Time.timeScale = savedTimeScale > 0 ? savedTimeScale : 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        panOffset = Vector3.zero;

        Debug.Log("[MapSystem] Full map closed");
    }

    // ============================================
    // MARKER SYSTEM
    // ============================================

    public void RegisterMarker(MapMarker marker)
    {
        if (!markers.Contains(marker))
            markers.Add(marker);
    }

    public void UnregisterMarker(MapMarker marker)
    {
        markers.Remove(marker);
    }

    public float GetMarkerIconHeight()
    {
        return cameraHeight - 5f;
    }

    // ============================================
    // CLEANUP
    // ============================================

    void OnDestroy()
    {
        if (minimapRT != null) { minimapRT.Release(); Destroy(minimapRT); }
        if (fullmapRT != null) { fullmapRT.Release(); Destroy(fullmapRT); }
        if (Instance == this) Instance = null;
    }
}
