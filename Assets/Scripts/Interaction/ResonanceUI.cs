using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ResonanceUI : MonoBehaviour
{
    public static ResonanceUI Instance;

    [Header("UI Elements")]
    public GameObject uiPanel;
    public RawImage staticWaveImage;
    public RawImage playerWaveImage;
    public TextMeshProUGUI statusText;
    
    [Header("Frequency Slider")]
    public RectTransform sliderTrack;
    public RectTransform sliderHandle;
    public Image sliderFill;
    public TextMeshProUGUI freqValueText;
    public RectTransform targetMarker;

    [Header("Battery Display")]
    public Image batteryFill;
    public Image batteryBackground;
    public TextMeshProUGUI batteryText;

    [Header("Waveform Settings")]
    public int textureWidth = 512;
    public int textureHeight = 200;
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    public Color targetColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color playerColor = new Color(0.2f, 1f, 0.2f, 1f);

    [Header("Analog TV Effect")]
    [Range(0f, 1f)] public float scanlineIntensity = 0.35f;
    [Range(0f, 1f)] public float staticNoiseAmount = 0.15f;
    [Range(0f, 1f)] public float glitchChance = 0.03f;
    [Range(0f, 1f)] public float flickerIntensity = 0.08f;
    public int scanlineSpacing = 3;

    private Texture2D targetTexture;
    private Texture2D playerTexture;
    private Color[] clearColors;

    private ResonanceDoor currentDoor;
    private bool isOpen = false;
    private bool texturesReady = false;
    
    private bool isDragging = false;
    private Canvas parentCanvas;
    
    private int openFrame = -10;
    
    // Analog TV state
    private float glitchTimer = 0f;
    private int glitchY = 0;
    private int glitchHeight = 0;
    private float glitchOffset = 0f;
    private float currentFlicker = 1f;
    private float scanlineScroll = 0f;
    private float batteryFlashTimer = 0f;
    private float markerOffset = 0f; // Random offset so marker is misleading

    public bool IsOpen => isOpen;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (uiPanel != null) 
            uiPanel.SetActive(false);
        else
            Debug.LogWarning("[ResonanceUI] uiPanel is NOT assigned!");
        
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void InitializeTextures()
    {
        if (staticWaveImage == null || playerWaveImage == null)
        {
            Debug.LogWarning("[ResonanceUI] RawImage references missing.");
            return;
        }

        targetTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        targetTexture.filterMode = FilterMode.Point;
        playerTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        playerTexture.filterMode = FilterMode.Point;

        staticWaveImage.texture = targetTexture;
        playerWaveImage.texture = playerTexture;

        clearColors = new Color[textureWidth * textureHeight];
        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = backgroundColor;

        texturesReady = true;
    }

    public void OpenInteraction(ResonanceDoor door)
    {
        if (isOpen && currentDoor == door) return;

        Debug.Log("[ResonanceUI] Opening for door: " + (door != null ? door.gameObject.name : "null"));

        currentDoor = door;
        isOpen = true;
        openFrame = Time.frameCount;

        if (uiPanel != null) 
            uiPanel.SetActive(true);
        else
            Debug.LogError("[ResonanceUI] uiPanel is NULL!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!texturesReady) InitializeTextures();

        if (texturesReady && currentDoor != null)
            DrawWave(targetTexture, currentDoor.requiredFrequency, targetColor);

        UpdateTargetMarker();
    }

    public void CloseInteraction()
    {
        if (!isOpen) return;
        
        Debug.Log("[ResonanceUI] Closing.");
        
        isOpen = false;
        isDragging = false;
        markerOffset = 0f; // Reset so next open gets new random offset
        
        if (currentDoor != null)
            currentDoor.OnUIClosed();
        
        currentDoor = null;
        if (uiPanel != null) uiPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UpdateMatchProgress(float progress, float quality)
    {
        if (!isOpen || currentDoor == null) return;

        float playerFreq = 0f;
        if (SoundRecorderDevice.Instance != null)
            playerFreq = SoundRecorderDevice.Instance.CurrentFrequency;

        if (statusText != null)
        {
            float diff = Mathf.Abs(playerFreq - currentDoor.requiredFrequency);
            bool isMatching = diff < currentDoor.tolerance;

            if (isMatching)
            {
                int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f);
                statusText.text = "RESONANCE " + percent + "%";
                statusText.color = Color.Lerp(Color.yellow, Color.green, progress);
            }
            else
            {
                if (playerFreq < currentDoor.requiredFrequency - currentDoor.tolerance)
                    statusText.text = ">>> INCREASE FREQUENCY";
                else if (playerFreq > currentDoor.requiredFrequency + currentDoor.tolerance)
                    statusText.text = "DECREASE FREQUENCY <<<";
                else
                    statusText.text = "ADJUST FREQUENCY";

                statusText.color = Color.Lerp(Color.red, Color.yellow, quality);
            }
        }
    }

    void Update()
    {
        if (!isOpen || currentDoor == null) return;

        SoundRecorderDevice device = SoundRecorderDevice.Instance;
        if (device == null) return;

        if (Time.frameCount > openFrame)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInteraction();
                return;
            }
        }

        HandleSliderDrag(device);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            device.currentFrequency += scroll * device.scrollSensitivity * 100f;
            device.currentFrequency = Mathf.Clamp(device.currentFrequency, device.minFrequency, device.maxFrequency);
        }

        float playerFreq = device.CurrentFrequency;

        UpdateAnalogTVState(device.ResonanceQuality);

        if (texturesReady)
        {
            DrawWave(targetTexture, currentDoor.requiredFrequency, targetColor);
            DrawWave(playerTexture, playerFreq, playerColor);
        }
        
        ApplyFlicker();
        UpdateSliderVisual(playerFreq, device);

        if (freqValueText != null)
            freqValueText.text = Mathf.RoundToInt(playerFreq) + " Hz";

        UpdateBatteryDisplay(device);
    }

    // ==========================================
    // BATTERY DISPLAY
    // ==========================================

    private void UpdateBatteryDisplay(SoundRecorderDevice device)
    {
        if (device == null) return;
        
        float batteryPct = device.BatteryNormalized;
        
        if (batteryFill != null)
        {
            batteryFill.fillAmount = batteryPct;
            
            if (batteryPct > 0.5f)
                batteryFill.color = Color.Lerp(Color.yellow, Color.green, (batteryPct - 0.5f) * 2f);
            else if (batteryPct > 0.15f)
                batteryFill.color = Color.Lerp(Color.red, Color.yellow, (batteryPct - 0.15f) / 0.35f);
            else
            {
                batteryFlashTimer += Time.deltaTime * 6f;
                float flash = (Mathf.Sin(batteryFlashTimer) > 0f) ? 1f : 0.3f;
                batteryFill.color = new Color(1f, 0f, 0f, flash);
            }
        }
        
        if (batteryText != null)
        {
            int pct = Mathf.RoundToInt(batteryPct * 100f);
            batteryText.text = pct + "%";
            batteryText.color = batteryPct < 0.15f ? Color.red : Color.white;
        }
    }

    // ==========================================
    // ANALOG TV EFFECTS
    // ==========================================

    private void UpdateAnalogTVState(float quality)
    {
        scanlineScroll += Time.deltaTime * 30f;
        if (scanlineScroll > scanlineSpacing) scanlineScroll -= scanlineSpacing;
        
        glitchTimer -= Time.deltaTime;
        if (glitchTimer <= 0f)
        {
            float adjustedChance = glitchChance * (1f - quality * 0.7f);
            if (Random.value < adjustedChance)
            {
                glitchY = Random.Range(0, textureHeight);
                glitchHeight = Random.Range(3, textureHeight / 4);
                glitchOffset = Random.Range(-20f, 20f);
                glitchTimer = Random.Range(0.02f, 0.08f);
            }
            else
            {
                glitchY = -1;
                glitchTimer = Random.Range(0.05f, 0.2f);
            }
        }

        float flickerTarget = 1f - Random.Range(0f, flickerIntensity * (1f - quality * 0.5f));
        currentFlicker = Mathf.Lerp(currentFlicker, flickerTarget, Time.deltaTime * 20f);
    }

    private void ApplyFlicker()
    {
        Color flickerColor = new Color(currentFlicker, currentFlicker, currentFlicker, 1f);
        if (staticWaveImage != null)
            staticWaveImage.color = flickerColor;
        if (playerWaveImage != null)
            playerWaveImage.color = flickerColor;
    }

    private void ApplyAnalogEffects(Texture2D tex)
    {
        if (tex == null) return;
        
        float quality = 0f;
        if (SoundRecorderDevice.Instance != null)
            quality = SoundRecorderDevice.Instance.ResonanceQuality;
        
        float noiseScale = staticNoiseAmount * (1f - quality * 0.6f);
        int scrollOffset = Mathf.FloorToInt(scanlineScroll);

        for (int y = 0; y < textureHeight; y++)
        {
            bool isScanline = ((y + scrollOffset) % scanlineSpacing) == 0;
            bool inGlitchBand = (glitchY >= 0 && y >= glitchY && y < glitchY + glitchHeight);
            int xShift = inGlitchBand ? Mathf.RoundToInt(glitchOffset) : 0;
            
            for (int x = 0; x < textureWidth; x++)
            {
                int srcX = Mathf.Clamp(x + xShift, 0, textureWidth - 1);
                Color pixel = tex.GetPixel(srcX, y);

                if (isScanline)
                    pixel *= (1f - scanlineIntensity);

                if (Random.value < noiseScale * 0.3f)
                {
                    float noiseVal = Random.Range(0.05f, 0.25f);
                    pixel = Color.Lerp(pixel, new Color(noiseVal, noiseVal, noiseVal, 1f), 0.6f);
                }
                
                if (inGlitchBand)
                {
                    pixel.r *= Random.Range(0.7f, 1.0f);
                    pixel.g *= Random.Range(0.9f, 1.1f);
                    pixel.b *= Random.Range(0.8f, 1.2f);
                }

                tex.SetPixel(x, y, pixel);
            }
        }
    }

    // ==========================================
    // SLIDER DRAG
    // ==========================================

    private void HandleSliderDrag(SoundRecorderDevice device)
    {
        if (sliderTrack == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOverTrack())
            {
                isDragging = true;
                ApplyMousePositionToFrequency(device);
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
            ApplyMousePositionToFrequency(device);

        if (Input.GetMouseButtonUp(0))
            isDragging = false;
    }

    private bool IsMouseOverTrack()
    {
        if (sliderTrack == null) return false;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderTrack, Input.mousePosition, GetCanvasCamera(), out localPoint);
        
        Rect expandedRect = sliderTrack.rect;
        expandedRect.xMin -= 40f;
        expandedRect.xMax += 40f;
        
        return expandedRect.Contains(localPoint);
    }

    private void ApplyMousePositionToFrequency(SoundRecorderDevice device)
    {
        if (sliderTrack == null || device == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderTrack, Input.mousePosition, GetCanvasCamera(), out localPoint);

        float trackHeight = sliderTrack.rect.height;
        float normalizedY = (localPoint.y + trackHeight * 0.5f) / trackHeight;
        normalizedY = Mathf.Clamp01(normalizedY);

        device.currentFrequency = Mathf.Lerp(device.minFrequency, device.maxFrequency, normalizedY);
    }

    private Camera GetCanvasCamera()
    {
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return parentCanvas != null ? parentCanvas.worldCamera : null;
    }

    // ==========================================
    // VISUALS
    // ==========================================

    private void UpdateSliderVisual(float freq, SoundRecorderDevice device)
    {
        if (sliderHandle == null || sliderTrack == null) return;

        float normalized = Mathf.InverseLerp(device.minFrequency, device.maxFrequency, freq);

        float trackHeight = sliderTrack.rect.height;
        float handleY = Mathf.Lerp(-trackHeight * 0.5f, trackHeight * 0.5f, normalized);
        sliderHandle.anchoredPosition = new Vector2(sliderHandle.anchoredPosition.x, handleY);

        if (sliderFill != null)
            sliderFill.fillAmount = normalized;

        Image handleImage = sliderHandle.GetComponent<Image>();
        if (handleImage != null)
        {
            float quality = device.ResonanceQuality;
            if (quality < 0.5f)
                handleImage.color = Color.Lerp(Color.red, Color.yellow, quality * 2f);
            else
                handleImage.color = Color.Lerp(Color.yellow, Color.green, (quality - 0.5f) * 2f);
        }
    }

    private void UpdateTargetMarker()
    {
        if (targetMarker == null || sliderTrack == null || currentDoor == null) return;

        SoundRecorderDevice device = SoundRecorderDevice.Instance;
        if (device == null) return;

        // Generate random offset on each open (marker is NOT at exact resonance)
        if (markerOffset == 0f)
        {
            // Offset 30-80 Hz, randomly above or below
            float offsetAmount = Random.Range(100f, 150f);
            markerOffset = (Random.value > 0.5f) ? offsetAmount : -offsetAmount;
            
            // Clamp so it stays within frequency range
            float fakeFreq = currentDoor.requiredFrequency + markerOffset;
            if (fakeFreq < device.minFrequency || fakeFreq > device.maxFrequency)
                markerOffset = -markerOffset; // Flip if out of range
        }

        float fakeTarget = currentDoor.requiredFrequency + markerOffset;
        float normalized = Mathf.InverseLerp(device.minFrequency, device.maxFrequency, fakeTarget);
        float trackHeight = sliderTrack.rect.height;
        float markerY = Mathf.Lerp(-trackHeight * 0.5f, trackHeight * 0.5f, normalized);
        targetMarker.anchoredPosition = new Vector2(targetMarker.anchoredPosition.x, markerY);
    }

    // ==========================================
    // WAVEFORM + ANALOG POST-PROCESS
    // ==========================================

    private void DrawWave(Texture2D tex, float frequency, Color waveColor)
    {
        if (tex == null || clearColors == null) return;

        tex.SetPixels(clearColors);

        float amplitude = textureHeight * 0.35f;
        float centerY = textureHeight * 0.5f;
        float waveCycles = frequency / 100f;

        for (int x = 0; x < textureWidth; x++)
        {
            float normalizedX = (float)x / textureWidth;
            float angle = normalizedX * waveCycles * Mathf.PI * 2f + Time.time * 3f;
            float y = centerY + Mathf.Sin(angle) * amplitude;

            int yInt = Mathf.Clamp(Mathf.RoundToInt(y), 1, textureHeight - 2);

            for (int thickness = -2; thickness <= 2; thickness++)
            {
                int py = Mathf.Clamp(yInt + thickness, 0, textureHeight - 1);
                float fade = 1f - (Mathf.Abs(thickness) * 0.3f);
                Color glowColor = waveColor * fade;
                glowColor.a = 1f;
                tex.SetPixel(x, py, glowColor);
            }
        }

        Color dimLine = new Color(0.15f, 0.15f, 0.15f, 0.4f);
        int centerInt = Mathf.RoundToInt(centerY);
        for (int x = 0; x < textureWidth; x++)
            tex.SetPixel(x, centerInt, dimLine);

        ApplyAnalogEffects(tex);

        tex.Apply();
    }
}
