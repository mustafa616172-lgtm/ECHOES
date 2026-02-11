using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Player health system for single player mode.
/// Handles damage, death, health regeneration, and health bar UI.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Regeneration")]
    [SerializeField] private bool enableRegeneration = true;
    [SerializeField] private float regenRate = 5f;
    [SerializeField] private float regenDelay = 3f;
    
    [Header("Effects")]
    [SerializeField] private bool showDamageScreen = true;
    
    [Header("UI Settings")]
    [SerializeField] private bool createHealthUI = true;
    
    private float lastDamageTime;
    private bool isDead = false;
    
    // UI References
    private Slider healthSlider;
    private Image healthFillImage;
    private Text healthValueText;
    private CanvasGroup healthCanvasGroup;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    
    public delegate void HealthChanged(float current, float max);
    public event HealthChanged OnHealthChanged;
    
    public delegate void PlayerDied();
    public event PlayerDied OnPlayerDied;
    
    private void Start()
    {
        currentHealth = maxHealth;
        
        if (createHealthUI)
        {
            CreateHealthUI();
        }
    }
    
    private void Update()
    {
        if (isDead) return;
        
        // Health regeneration
        if (enableRegeneration && currentHealth < maxHealth)
        {
            if (Time.unscaledTime - lastDamageTime >= regenDelay)
            {
                currentHealth += regenRate * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                UpdateHealthUI();
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        lastDamageTime = Time.unscaledTime;
        
        Debug.Log("[PlayerHealth] Took damage: " + damage + ", Current health: " + currentHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
        
        if (showDamageScreen)
        {
            ShowDamageEffect();
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }
    
    public void SetMaxHealth(float newMax, bool healToFull = false)
    {
        maxHealth = newMax;
        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }
    
    private void ShowDamageEffect()
    {
        if (ScreenEffects.Instance != null)
        {
            ScreenEffects.Instance.TriggerDamageFlash(0.2f);
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        Debug.Log("[PlayerHealth] Player died!");
        UpdateHealthUI();
        
        OnPlayerDied?.Invoke();
        
        // Disable player controls
        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        // Show death screen or restart game
        // This can be handled by subscribing to OnPlayerDied event
    }
    
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }
    
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
        
        if (healthValueText != null)
        {
            healthValueText.text = Mathf.CeilToInt(currentHealth).ToString();
        }
        
        // Pulse effect when low health
        if (healthFillImage != null)
        {
            if (currentHealth < maxHealth * 0.25f)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * 4f));
                healthFillImage.color = Color.Lerp(new Color(0.5f, 0f, 0f), Color.red, pulse);
            }
            else
            {
                healthFillImage.color = new Color(0.8f, 0.1f, 0.1f); // Dark red
            }
        }
    }
    
    private void CreateHealthUI()
    {
        // Prevent duplicate
        if (healthSlider != null) return;
        
        // Clean up leftover old canvases from previous versions
        GameObject oldHealth = GameObject.Find("HealthCanvas");
        if (oldHealth != null) Destroy(oldHealth);
        GameObject oldStamina = GameObject.Find("StaminaCanvas");
        if (oldStamina != null) Destroy(oldStamina);
        
        // Destroy any existing HealthBar to prevent duplicates
        GameObject existingBar = GameObject.Find("HealthBar");
        if (existingBar != null) Destroy(existingBar);
        
        // Find or create dedicated HUD canvas
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        
        if (canvasObj == null)
        {
            canvasObj = new GameObject("PlayerHUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create Health Bar Container - positioned above stamina bar
        GameObject healthBarContainer = new GameObject("HealthBar");
        healthBarContainer.transform.SetParent(canvasObj.transform, false);
        
        RectTransform containerRect = healthBarContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0); // Bottom Left
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(20, 50); // Above stamina bar (stamina is at y=20)
        containerRect.sizeDelta = new Vector2(250, 25);
        
        // Create Slider Background
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(healthBarContainer.transform, false);
        healthSlider = sliderObj.AddComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.anchoredPosition = Vector2.zero;
        
        // Background Image (dark)
        Image bgImage = sliderObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.05f, 0.05f, 0.9f); // Dark red-ish background
        
        // Create Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-4, -4); // Slight padding
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create Fill Image
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        healthFillImage = fill.AddComponent<Image>();
        healthFillImage.color = new Color(0.8f, 0.1f, 0.1f); // Dark red
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        
        // Configure Slider
        healthSlider.targetGraphic = bgImage;
        healthSlider.fillRect = fillRect;
        healthSlider.direction = Slider.Direction.LeftToRight;
        healthSlider.minValue = 0;
        healthSlider.maxValue = 1;
        healthSlider.value = 1;
        healthSlider.interactable = false; // Player can't interact with it
        
        // Create "+" Icon on left side of bar
        GameObject plusIcon = new GameObject("PlusIcon");
        plusIcon.transform.SetParent(healthBarContainer.transform, false);
        Text plusText = plusIcon.AddComponent<Text>();
        plusText.text = "+";
        plusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        plusText.fontSize = 20;
        plusText.fontStyle = FontStyle.Bold;
        plusText.color = Color.white;
        plusText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform plusRect = plusIcon.GetComponent<RectTransform>();
        plusRect.anchorMin = new Vector2(0, 0.5f);
        plusRect.anchorMax = new Vector2(0, 0.5f);
        plusRect.pivot = new Vector2(1, 0.5f);
        plusRect.anchoredPosition = new Vector2(-5, 0); // Left of bar
        plusRect.sizeDelta = new Vector2(20, 25);
        
        // Create outline for plus icon
        Outline plusOutline = plusIcon.AddComponent<Outline>();
        plusOutline.effectColor = Color.black;
        plusOutline.effectDistance = new Vector2(1, -1);
        
        // Create Health Value Text on right side of bar
        GameObject valueObj = new GameObject("HealthValue");
        valueObj.transform.SetParent(healthBarContainer.transform, false);
        healthValueText = valueObj.AddComponent<Text>();
        healthValueText.text = Mathf.CeilToInt(maxHealth).ToString();
        healthValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthValueText.fontSize = 16;
        healthValueText.fontStyle = FontStyle.Bold;
        healthValueText.color = Color.white;
        healthValueText.alignment = TextAnchor.MiddleRight;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1, 0.5f);
        valueRect.anchorMax = new Vector2(1, 0.5f);
        valueRect.pivot = new Vector2(0, 0.5f);
        valueRect.anchoredPosition = new Vector2(5, 0); // Right of bar
        valueRect.sizeDelta = new Vector2(40, 25);
        
        // Create outline for value text
        Outline valueOutline = valueObj.AddComponent<Outline>();
        valueOutline.effectColor = Color.black;
        valueOutline.effectDistance = new Vector2(1, -1);
        
        Debug.Log("[PlayerHealth] Health UI Created Successfully!");
    }
}
