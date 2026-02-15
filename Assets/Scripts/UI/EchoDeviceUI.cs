using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ECHOES - Echo Device UI
/// Diegetic UI for displaying Echo device status (battery, frequency)
/// Appears as an overlay on the device itself (no traditional HUD)
/// </summary>
public class EchoDeviceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EchoDevice echoDevice;
    
    [Header("UI Elements")]
    [SerializeField] private Canvas deviceCanvas;
    [SerializeField] private Image batteryFillImage;
    [SerializeField] private TextMeshProUGUI frequencyText;
    [SerializeField] private TextMeshProUGUI batteryPercentText;
    [SerializeField] private GameObject[] batteryLEDs;
    
    [Header("Colors")]
    [SerializeField] private Color batteryFullColor = Color.green;
    [SerializeField] private Color batteryMediumColor = Color.yellow;
    [SerializeField] private Color batteryLowColor = Color.red;
    [SerializeField] private Gradient batteryGradient;
    
    [Header("Animation")]
    [SerializeField] private bool animateFrequency = true;
    [SerializeField] private float frequencyUpdateSpeed = 2f;
    
    private float displayedFrequency = 440f;
    
    void Start()
    {
        // Find Echo device if not assigned
        if (echoDevice == null)
        {
            echoDevice = FindObjectOfType<EchoDevice>();
        }
        
        // Setup battery gradient if not set
        if (batteryGradient == null || batteryGradient.colorKeys.Length == 0)
        {
            batteryGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0].color = batteryLowColor;
            colorKeys[0].time = 0.0f;
            colorKeys[1].color = batteryMediumColor;
            colorKeys[1].time = 0.5f;
            colorKeys[2].color = batteryFullColor;
            colorKeys[2].time = 1.0f;
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 1.0f;
            alphaKeys[0].time = 0.0f;
            alphaKeys[1].alpha = 1.0f;
            alphaKeys[1].time = 1.0f;
            
            batteryGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        // Hide UI initially if device not equipped
        if (deviceCanvas != null && echoDevice != null)
        {
            deviceCanvas.gameObject.SetActive(echoDevice.HasDevice);
        }
    }
    
    void Update()
    {
        if (echoDevice == null) return;
        
        // Show/hide UI based on device ownership
        if (deviceCanvas != null)
        {
            bool shouldShow = echoDevice.HasDevice;
            if (deviceCanvas.gameObject.activeSelf != shouldShow)
            {
                deviceCanvas.gameObject.SetActive(shouldShow);
            }
        }
        
        if (!echoDevice.HasDevice) return;
        
        UpdateBatteryUI();
        UpdateFrequencyUI();
        UpdateLEDs();
    }
    
    void UpdateBatteryUI()
    {
        float batteryPercent = echoDevice.BatteryPercentage / 100f;
        
        // Update battery fill amount
        if (batteryFillImage != null)
        {
            batteryFillImage.fillAmount = batteryPercent;
            batteryFillImage.color = batteryGradient.Evaluate(batteryPercent);
        }
        
        // Update battery percentage text
        if (batteryPercentText != null)
        {
            batteryPercentText.text = $"{echoDevice.BatteryPercentage:F0}%";
            batteryPercentText.color = batteryGradient.Evaluate(batteryPercent);
        }
    }
    
    void UpdateFrequencyUI()
    {
        if (frequencyText == null) return;
        
        float targetFrequency = echoDevice.CurrentFrequency;
        
        // Smooth frequency display for analog feel
        if (animateFrequency)
        {
            displayedFrequency = Mathf.Lerp(displayedFrequency, targetFrequency, Time.deltaTime * frequencyUpdateSpeed);
        }
        else
        {
            displayedFrequency = targetFrequency;
        }
        
        // Format frequency display (convert to kHz if > 1000 Hz)
        if (displayedFrequency >= 1000f)
        {
            frequencyText.text = $"{displayedFrequency / 1000f:F2} kHz";
        }
        else
        {
            frequencyText.text = $"{displayedFrequency:F0} Hz";
        }
    }
    
    void UpdateLEDs()
    {
        if (batteryLEDs == null || batteryLEDs.Length == 0) return;
        
        float batteryPercent = echoDevice.BatteryPercentage / 100f;
        int activeLEDs = Mathf.CeilToInt(batteryLEDs.Length * batteryPercent);
        
        for (int i = 0; i < batteryLEDs.Length; i++)
        {
            if (batteryLEDs[i] != null)
            {
                batteryLEDs[i].SetActive(i < activeLEDs);
            }
        }
    }
    
    /// <summary>
    /// Show a temporary message on the device (e.g., "Low Battery")
    /// </summary>
    public void ShowDeviceMessage(string message, float duration = 2f)
    {
        // This can be extended to show temporary messages
        Debug.Log($"[EchoDeviceUI] Message: {message}");
    }
}
