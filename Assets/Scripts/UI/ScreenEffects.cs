using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horror screen effects for ECHOES.
/// - Damage flash (red vignette when hit)
/// - Low health pulse (persistent red edges)
/// - Screen shake (on attack)
/// Auto-instantiates on player.
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    [Header("Damage Flash")]
    [SerializeField] private float damageFlashDuration = 0.4f;
    [SerializeField] private Color damageFlashColor = new Color(0.8f, 0f, 0f, 0.5f);
    
    [Header("Low Health")]
    [SerializeField] private float lowHealthThreshold = 0.3f;  // 30% health
    [SerializeField] private float criticalHealthThreshold = 0.15f; // 15% health
    [SerializeField] private float lowHealthPulseSpeed = 2f;
    [SerializeField] private float criticalPulseSpeed = 5f;
    
    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private float shakeDuration = 0.3f;
    
    // UI Elements
    private GameObject effectCanvas;
    private Image damageOverlay;      // Full screen red flash
    private Image vignetteOverlay;    // Persistent vignette edges
    
    // State
    private float damageFlashTimer = 0f;
    private float shakeTimer = 0f;
    private float currentShakeIntensity = 0f;
    private Vector3 originalCameraPos;
    private Transform cameraTransform;
    
    // References
    private PlayerHealth playerHealth;
    
    // Textures
    private Texture2D vignetteTexture;
    
    public static ScreenEffects Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        CreateEffectTextures();
        CreateEffectUI();
        FindReferences();
        Debug.Log("[ScreenEffects] Initialized with damage flash, low health pulse, and shake");
    }
    
    void FindReferences()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
                originalCameraPos = cameraTransform.localPosition;
            }
        }
    }
    
    void Update()
    {
        FindReferences();
        UpdateDamageFlash();
        UpdateLowHealthVignette();
        UpdateScreenShake();
    }
    
    // ============================================
    // DAMAGE FLASH - Red overlay when hit
    // ============================================
    
    /// <summary>Call this to trigger a damage flash</summary>
    public void TriggerDamageFlash()
    {
        damageFlashTimer = damageFlashDuration;
    }
    
    /// <summary>Call this to trigger damage flash with screen shake</summary>
    public void TriggerDamageFlash(float intensity)
    {
        damageFlashTimer = damageFlashDuration;
        TriggerShake(intensity);
    }
    
    void UpdateDamageFlash()
    {
        if (damageOverlay == null) return;
        
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            float alpha = (damageFlashTimer / damageFlashDuration) * damageFlashColor.a;
            Color c = damageFlashColor;
            c.a = alpha;
            damageOverlay.color = c;
        }
        else
        {
            damageOverlay.color = Color.clear;
        }
    }
    
    // ============================================
    // LOW HEALTH VIGNETTE - Pulsing red edges
    // ============================================
    
    void UpdateLowHealthVignette()
    {
        if (vignetteOverlay == null || playerHealth == null) return;
        
        float healthPercent = playerHealth.HealthPercentage;
        
        if (healthPercent <= criticalHealthThreshold)
        {
            // Critical - strong fast pulse
            float pulse = (Mathf.Sin(Time.time * criticalPulseSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(0.3f, 0.7f, pulse);
            vignetteOverlay.color = new Color(0.6f, 0f, 0f, alpha);
        }
        else if (healthPercent <= lowHealthThreshold)
        {
            // Low health - gentle pulse
            float t = 1f - ((healthPercent - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold));
            float pulse = (Mathf.Sin(Time.time * lowHealthPulseSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(0.05f, 0.3f, t) * Mathf.Lerp(0.5f, 1f, pulse);
            vignetteOverlay.color = new Color(0.5f, 0f, 0f, alpha);
        }
        else
        {
            vignetteOverlay.color = Color.clear;
        }
    }
    
    // ============================================
    // SCREEN SHAKE
    // ============================================
    
    /// <summary>Trigger screen shake with custom intensity</summary>
    public void TriggerShake(float intensity = -1f)
    {
        if (intensity < 0) intensity = shakeIntensity;
        currentShakeIntensity = intensity;
        shakeTimer = shakeDuration;
    }
    
    void UpdateScreenShake()
    {
        if (cameraTransform == null) return;
        
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float t = shakeTimer / shakeDuration;
            float currentIntensity = currentShakeIntensity * t;
            
            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * currentIntensity,
                Random.Range(-1f, 1f) * currentIntensity,
                0f
            );
            
            cameraTransform.localPosition = originalCameraPos + offset;
        }
        else
        {
            cameraTransform.localPosition = originalCameraPos;
        }
    }
    
    // ============================================
    // TEXTURE & UI CREATION
    // ============================================
    
    void CreateEffectTextures()
    {
        // === Vignette texture (dark edges, clear center) ===
        vignetteTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[256 * 256];
        Vector2 center = new Vector2(128f, 128f);
        
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / 128f;
                float vignette = Mathf.Clamp01(dist - 0.4f) / 0.6f; // Transparent center, dark edges
                vignette = vignette * vignette; // Smooth falloff
                pixels[y * 256 + x] = new Color(0f, 0f, 0f, vignette);
            }
        }
        vignetteTexture.SetPixels(pixels);
        vignetteTexture.Apply();
    }
    
    void CreateEffectUI()
    {
        // Destroy old
        GameObject old = GameObject.Find("ScreenEffectsCanvas");
        if (old != null) Destroy(old);
        
        // Create canvas on top of everything
        effectCanvas = new GameObject("ScreenEffectsCanvas");
        Canvas canvas = effectCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Always on top
        CanvasScaler scaler = effectCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // No GraphicRaycaster - don't block input
        
        // === Damage flash overlay (full screen red) ===
        damageOverlay = CreateFullScreenImage("DamageFlash", Color.clear);
        
        // === Vignette overlay (edges) ===
        vignetteOverlay = CreateFullScreenImage("Vignette", Color.clear);
        vignetteOverlay.sprite = Sprite.Create(vignetteTexture,
            new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        vignetteOverlay.type = Image.Type.Simple;
        vignetteOverlay.preserveAspect = false;
        
        // Disable raycast targets so effects don't block clicks
        damageOverlay.raycastTarget = false;
        vignetteOverlay.raycastTarget = false;
        
        Debug.Log("[ScreenEffects] Effect overlays created");
    }
    
    Image CreateFullScreenImage(string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(effectCanvas.transform, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }
    
    void OnDestroy()
    {
        if (effectCanvas != null) Destroy(effectCanvas);
        if (vignetteTexture != null) Destroy(vignetteTexture);
    }
}
