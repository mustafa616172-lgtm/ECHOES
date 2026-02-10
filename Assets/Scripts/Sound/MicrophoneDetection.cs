using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detects real microphone input from the player.
/// When the player talks loudly, it emits a sound event that enemies can hear.
/// Toggle on/off from settings or in-game.
/// </summary>
public class MicrophoneDetection : MonoBehaviour
{
    [Header("Microphone Settings")]
    [SerializeField] private bool micEnabled = true;
    [SerializeField] private float sensitivity = 0.8f;          // Mic sensitivity multiplier (lowered)
    [SerializeField] private float detectionThreshold = 0.15f;  // Volume threshold to trigger (raised)
    [SerializeField] private float loudThreshold = 0.4f;        // Volume for shout detection (raised)
    [SerializeField] private float soundCooldown = 2f;          // Min time between sound emissions (longer)
    
    [Header("Sound Emission")]
    [SerializeField] private float whisperRadius = 5f;           // Quiet talking radius
    [SerializeField] private float talkRadius = 12f;             // Normal talking radius
    [SerializeField] private float shoutRadius = 25f;            // Shouting/screaming radius
    
    [Header("UI")]
    [SerializeField] private bool showMicIndicator = true;
    
    // Private 
    private AudioClip micClip;
    private string micDevice;
    private float[] sampleData;
    private int sampleSize = 128;
    private float lastSoundEmitTime;
    private float currentVolume;
    private bool micAvailable = false;
    
    // UI Elements
    private GameObject micUI;
    private Image micIcon;
    private Image volumeBar;
    private Text micStatusText;
    
    /// <summary>Is microphone currently enabled and active?</summary>
    public bool IsMicActive => micEnabled && micAvailable;
    
    /// <summary>Current mic volume level (0-1)</summary>
    public float CurrentVolume => currentVolume;
    
    void Start()
    {
        sampleData = new float[sampleSize];
        
        if (micEnabled)
        {
            StartMicrophone();
        }
        
        if (showMicIndicator)
        {
            CreateMicUI();
        }
        
        Debug.Log("[MicDetection] Initialized");
    }
    
    void StartMicrophone()
    {
        // Check if mic is available
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[MicDetection] No microphone found!");
            micAvailable = false;
            return;
        }
        
        micDevice = Microphone.devices[0];
        Debug.Log($"[MicDetection] Using microphone: {micDevice}");
        
        // Start recording continuously (loop, 1 second buffer)
        micClip = Microphone.Start(micDevice, true, 1, 44100);
        micAvailable = true;
    }
    
    void StopMicrophone()
    {
        if (micAvailable && Microphone.IsRecording(micDevice))
        {
            Microphone.End(micDevice);
        }
        micAvailable = false;
    }
    
    void Update()
    {
        if (!micEnabled || !micAvailable) return;
        
        // Read microphone volume
        currentVolume = GetMicrophoneVolume() * sensitivity;
        
        // Check if player is making enough noise
        if (currentVolume > detectionThreshold && Time.time - lastSoundEmitTime > soundCooldown)
        {
            EmitMicSound(currentVolume);
            lastSoundEmitTime = Time.time;
        }
        
        // Update UI
        UpdateMicUI();
    }
    
    float GetMicrophoneVolume()
    {
        if (micClip == null) return 0f;
        
        int micPosition = Microphone.GetPosition(micDevice);
        if (micPosition < sampleSize) return 0f;
        
        // Read samples from mic
        micClip.GetData(sampleData, micPosition - sampleSize);
        
        // Calculate RMS (Root Mean Square) volume
        float sum = 0f;
        for (int i = 0; i < sampleSize; i++)
        {
            sum += sampleData[i] * sampleData[i];
        }
        
        return Mathf.Sqrt(sum / sampleSize);
    }
    
    void EmitMicSound(float volume)
    {
        if (SoundManager.Instance == null) return;
        
        // Determine radius based on volume
        float radius;
        string volumeType;
        
        if (volume >= loudThreshold)
        {
            radius = shoutRadius;
            volumeType = "SHOUT";
        }
        else if (volume >= detectionThreshold * 2f)
        {
            radius = talkRadius;
            volumeType = "TALK";
        }
        else
        {
            radius = whisperRadius;
            volumeType = "WHISPER";
        }
        
        // Emit sound at player position
        SoundManager.Instance.EmitSound(transform.position, radius, SoundManager.SoundType.Footstep);
        Debug.Log($"[MicDetection] Player {volumeType} detected! Volume: {volume:F3}, Radius: {radius}m");
    }
    
    void CreateMicUI()
    {
        // Find dedicated HUD canvas
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) return;
        
        // Mic indicator - bottom left corner
        micUI = new GameObject("MicIndicator");
        micUI.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = micUI.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 0);
        rootRect.anchorMax = new Vector2(0, 0);
        rootRect.pivot = new Vector2(0, 0);
        rootRect.anchoredPosition = new Vector2(15, 85);
        rootRect.sizeDelta = new Vector2(130, 25);
        
        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(micUI.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);
        RectTransform bgRect = bgImg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Mic icon text
        GameObject iconObj = new GameObject("MicIcon");
        iconObj.transform.SetParent(micUI.transform, false);
        micStatusText = iconObj.AddComponent<Text>();
        micStatusText.text = "MIC";
        micStatusText.fontSize = 12;
        micStatusText.fontStyle = FontStyle.Bold;
        micStatusText.alignment = TextAnchor.MiddleLeft;
        micStatusText.color = Color.green;
        micStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform iconRect = micStatusText.rectTransform;
        iconRect.anchorMin = new Vector2(0.05f, 0);
        iconRect.anchorMax = new Vector2(0.35f, 1);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        
        // Volume bar background
        GameObject barBg = new GameObject("BarBG");
        barBg.transform.SetParent(micUI.transform, false);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform barBgRect = barBgImg.rectTransform;
        barBgRect.anchorMin = new Vector2(0.38f, 0.2f);
        barBgRect.anchorMax = new Vector2(0.95f, 0.8f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;
        
        // Volume fill bar
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barBg.transform, false);
        volumeBar = fill.AddComponent<Image>();
        volumeBar.color = Color.green;
        RectTransform fillRect = volumeBar.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }
    
    void UpdateMicUI()
    {
        if (volumeBar == null) return;
        
        // Update volume bar
        float displayVolume = Mathf.Clamp01(currentVolume / loudThreshold);
        volumeBar.rectTransform.anchorMax = new Vector2(displayVolume, 1f);
        
        // Color based on volume
        if (currentVolume >= loudThreshold)
        {
            volumeBar.color = Color.red;
            if (micStatusText != null) micStatusText.color = Color.red;
        }
        else if (currentVolume >= detectionThreshold)
        {
            volumeBar.color = Color.yellow;
            if (micStatusText != null) micStatusText.color = Color.yellow;
        }
        else
        {
            volumeBar.color = Color.green;
            if (micStatusText != null) micStatusText.color = Color.green;
        }
    }
    
    /// <summary>Toggle microphone on/off</summary>
    public void ToggleMicrophone()
    {
        micEnabled = !micEnabled;
        
        if (micEnabled)
            StartMicrophone();
        else
            StopMicrophone();
            
        Debug.Log($"[MicDetection] Microphone {(micEnabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>Set microphone sensitivity (0.5 - 5.0)</summary>
    public void SetSensitivity(float value)
    {
        sensitivity = Mathf.Clamp(value, 0.5f, 5f);
    }
    
    void OnDestroy()
    {
        StopMicrophone();
        if (micUI != null)
            Destroy(micUI);
    }
    
    void OnDisable()
    {
        StopMicrophone();
    }
}
