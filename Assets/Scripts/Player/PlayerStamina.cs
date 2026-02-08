using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float drainRate = 15f; // Stamina lost per second while running
    public float regenRate = 10f; // Stamina gained per second while resting
    public float regenDelay = 1.5f; // Time before regen starts after running

    [Header("UI Settings")]
    public bool createTemporaryUI = true;
    private Slider staminaSlider;
    private Image fillImage;
    private CanvasGroup canvasGroup;

    private float lastDrainTime;
    private bool isDrainPending = false; // To track if we drained this frame

    void Start()
    {
        currentStamina = maxStamina;
        if (createTemporaryUI)
        {
            CreateStaminaUI();
        }
    }

    void Update()
    {
        // Regen Logic
        if (Time.time - lastDrainTime > regenDelay && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            UpdateUI();
        }

        // Fade UI if full (Optional polish)
        if (canvasGroup != null)
        {
            float targetAlpha = (currentStamina < maxStamina - 1f) ? 1f : 0.3f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Consumes stamina. Returns true if stamina was available.
    /// </summary>
    public bool ConsumeStamina(float amountMultiplier = 1f)
    {
        if (currentStamina > 0)
        {
            currentStamina -= drainRate * amountMultiplier * Time.deltaTime;
            lastDrainTime = Time.time;
            UpdateUI();
            
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                return false; // Exhausted
            }
            return true;
        }
        return false;
    }

    public bool HasStamina => currentStamina > 5f; // Buffer to prevent jittering at 0

    void UpdateUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina / maxStamina;
            
            // Change color based on stamina
            if (fillImage != null)
            {
                if (currentStamina < maxStamina * 0.25f)
                    fillImage.color = Color.Lerp(Color.red, Color.yellow, currentStamina / (maxStamina * 0.25f));
                else
                    fillImage.color = Color.white;
            }
        }
    }

    void CreateStaminaUI()
    {
        // Check if we already have one
        if (staminaSlider != null) return;

        // 1. Create Canvas
        GameObject canvasObj = new GameObject("StaminaCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        // 2. Create Slider Background
        GameObject sliderObj = new GameObject("StaminaSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        staminaSlider = sliderObj.AddComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0); // Bottom Left
        sliderRect.anchorMax = new Vector2(0, 0);
        sliderRect.pivot = new Vector2(0, 0);
        sliderRect.anchoredPosition = new Vector2(20, 20); // Padding
        sliderRect.sizeDelta = new Vector2(250, 20);

        // 3. Create Background Image
        Image bgImage = sliderObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // 4. Create Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // 5. Create Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.white;
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        staminaSlider.targetGraphic = bgImage;
        staminaSlider.fillRect = fillRect;
        staminaSlider.direction = Slider.Direction.LeftToRight;
        staminaSlider.minValue = 0;
        staminaSlider.maxValue = 1;
        staminaSlider.value = 1;

        Debug.Log("[PlayerStamina] UI Created Automatically");
    }
}
