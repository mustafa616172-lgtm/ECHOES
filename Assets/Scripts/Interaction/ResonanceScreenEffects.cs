using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen effects during resonance interaction.
/// Creates a vignette overlay, chromatic aberration simulation, and camera shake.
/// Attach to the main Camera or a persistent GameObject in the scene.
/// Effects scale with resonance quality and lerp in/out smoothly.
/// </summary>
public class ResonanceScreenEffects : MonoBehaviour
{
    public static ResonanceScreenEffects Instance;

    [Header("Vignette")]
    [Range(0f, 1f)] public float vignetteMaxAlpha = 0.4f;
    public Color vignetteColor = new Color(0f, 0f, 0f, 1f);

    [Header("Camera Shake")]
    public float shakeMaxIntensity = 0.02f;
    public float shakeSpeed = 25f;

    [Header("Chromatic Aberration")]
    [Tooltip("Max pixel offset for RGB channel split")]
    public float chromaticMaxOffset = 3f;
    public Color channelTintR = new Color(1f, 0.85f, 0.85f, 0.15f);
    public Color channelTintB = new Color(0.85f, 0.85f, 1f, 0.15f);

    // Auto-created UI elements
    private GameObject vignetteObj;
    private Image vignetteImage;
    private GameObject chromaticRedObj;
    private Image chromaticRedImage;
    private GameObject chromaticBlueObj;
    private Image chromaticBlueImage;
    private Texture2D vignetteTexture;

    // Camera reference
    private Camera mainCam;
    private Quaternion originalRotation;

    // State
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private bool effectsCreated = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mainCam = Camera.main;
        CreateEffects();
    }

    private void CreateEffects()
    {
        // Find or create Canvas for screen effects
        Canvas effectCanvas = null;
        
        // Try to find existing screen-space overlay canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder >= 90)
            {
                effectCanvas = c;
                break;
            }
        }

        if (effectCanvas == null)
        {
            GameObject canvasObj = new GameObject("ResonanceEffectsCanvas");
            effectCanvas = canvasObj.AddComponent<Canvas>();
            effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            effectCanvas.sortingOrder = 100; // On top of everything
            canvasObj.AddComponent<CanvasScaler>();
            DontDestroyOnLoad(canvasObj);
        }

        // Create vignette
        CreateVignette(effectCanvas.transform);

        // Create chromatic aberration overlays
        CreateChromaticOverlays(effectCanvas.transform);

        effectsCreated = true;
        SetEffectsVisible(false);
    }

    private void CreateVignette(Transform parent)
    {
        vignetteObj = new GameObject("VignetteOverlay");
        vignetteObj.transform.SetParent(parent, false);

        vignetteImage = vignetteObj.AddComponent<Image>();
        vignetteImage.raycastTarget = false;

        // Create radial gradient texture
        int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Smooth vignette falloff
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.4f) / 0.6f));
                vignetteTexture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        vignetteTexture.Apply();

        vignetteImage.sprite = Sprite.Create(
            vignetteTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.preserveAspect = false;

        // Stretch to fill screen
        RectTransform rt = vignetteObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
    }

    private void CreateChromaticOverlays(Transform parent)
    {
        // Red channel offset
        chromaticRedObj = CreateColorOverlay(parent, "ChromaticRed", channelTintR);
        chromaticRedImage = chromaticRedObj.GetComponent<Image>();

        // Blue channel offset
        chromaticBlueObj = CreateColorOverlay(parent, "ChromaticBlue", channelTintB);
        chromaticBlueImage = chromaticBlueObj.GetComponent<Image>();
    }

    private GameObject CreateColorOverlay(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(color.r, color.g, color.b, 0f);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return obj;
    }

    void LateUpdate()
    {
        if (!effectsCreated) return;

        // Determine target intensity
        bool uiOpen = ResonanceUI.Instance != null && ResonanceUI.Instance.IsOpen;
        float quality = 0f;
        
        if (uiOpen && SoundRecorderDevice.Instance != null)
        {
            quality = SoundRecorderDevice.Instance.ResonanceQuality;
            targetIntensity = quality;
        }
        else
        {
            targetIntensity = 0f;
        }

        // Smooth lerp
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 5f);

        // Hide when negligible
        if (currentIntensity < 0.01f && targetIntensity < 0.01f)
        {
            SetEffectsVisible(false);
            return;
        }

        SetEffectsVisible(true);

        // Update vignette
        UpdateVignette();

        // Update chromatic aberration
        UpdateChromatic();

        // Update camera shake
        UpdateCameraShake();
    }

    private void UpdateVignette()
    {
        if (vignetteImage == null) return;

        float alpha = currentIntensity * vignetteMaxAlpha;
        vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, alpha);
    }

    private void UpdateChromatic()
    {
        if (chromaticRedImage == null || chromaticBlueImage == null) return;

        float offset = currentIntensity * chromaticMaxOffset;
        float alpha = currentIntensity * 0.15f;

        // Red shifts left, Blue shifts right
        RectTransform redRt = chromaticRedObj.GetComponent<RectTransform>();
        redRt.offsetMin = new Vector2(-offset, 0);
        redRt.offsetMax = new Vector2(-offset, 0);
        chromaticRedImage.color = new Color(channelTintR.r, channelTintR.g, channelTintR.b, alpha);

        RectTransform blueRt = chromaticBlueObj.GetComponent<RectTransform>();
        blueRt.offsetMin = new Vector2(offset, 0);
        blueRt.offsetMax = new Vector2(offset, 0);
        chromaticBlueImage.color = new Color(channelTintB.r, channelTintB.g, channelTintB.b, alpha);
    }

    private void UpdateCameraShake()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        if (currentIntensity > 0.1f)
        {
            float shakeAmount = currentIntensity * shakeMaxIntensity;
            float t = Time.time * shakeSpeed;
            
            // Perlin noise for smooth shake
            float shakeX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shakeAmount;
            float shakeY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shakeAmount;
            
            mainCam.transform.localRotation *= Quaternion.Euler(shakeX, shakeY, 0f);
        }
    }

    private void SetEffectsVisible(bool visible)
    {
        if (vignetteObj != null) vignetteObj.SetActive(visible);
        if (chromaticRedObj != null) chromaticRedObj.SetActive(visible);
        if (chromaticBlueObj != null) chromaticBlueObj.SetActive(visible);
    }

    void OnDestroy()
    {
        if (vignetteTexture != null)
            Destroy(vignetteTexture);
    }
}
