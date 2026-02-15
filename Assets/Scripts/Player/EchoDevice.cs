using UnityEngine;
using System.Collections;

/// <summary>
/// ECHOES - Echo Device Controller
/// Main controller for the Echo device that allows player to "see" through sound.
/// Features: Pulse/Echo waves, frequency tuning, battery management.
/// </summary>
public class EchoDevice : MonoBehaviour
{
    [Header("Device Settings")]
    [SerializeField] private bool hasDevice = false;
    [SerializeField] private GameObject deviceModel;
    [SerializeField] private Transform deviceHoldPosition;
    
    [Header("Echo Pulse")]
    [SerializeField] private KeyCode pulseKey = KeyCode.Q;
    [SerializeField] private float pulseCooldown = 2f;
    [SerializeField] private float pulseRadius = 30f;
    [SerializeField] private float pulseSpeed = 10f;
    [SerializeField] private float pulseDuration = 3f;
    [SerializeField] private float batteryConsumptionPerPulse = 5f;
    
    [Header("Frequency Settings")]
    [SerializeField] private float currentFrequency = 440f; // Default A4 note
    [SerializeField] private float minFrequency = 20f;
    [SerializeField] private float maxFrequency = 20000f;
    [SerializeField] private float frequencyScrollSpeed = 50f;
    
    [Header("Battery")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float currentBattery = 100f;
    [SerializeField] private float batteryDrainPerSecond = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pulseSound;
    [SerializeField] private AudioClip frequencyChangeSound;
    [SerializeField] private AudioSource deviceAudioSource;
    
    [Header("Visual Effects")]
    [SerializeField] private MonoBehaviour pulseEffect; // Can be EchoPulseEffect or SimpleEchoPulseEffect
    
    [Header("Device Model Positioning")]
    [Tooltip("Offset from camera for device position (right, down, forward)")]
    [SerializeField] private Vector3 deviceHandOffset = new Vector3(0.35f, -0.25f, 0.4f);
    [Tooltip("Rotation of device when held (X=pitch, Y=yaw, Z=roll)")]
    [SerializeField] private Vector3 deviceHandRotation = new Vector3(-15f, 10f, 0f);
    
    private float lastPulseTime;
    private bool isPulseActive = false;
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Setup audio source
        if (deviceAudioSource == null)
        {
            GameObject audioObj = new GameObject("EchoDeviceAudio");
            audioObj.transform.SetParent(transform);
            deviceAudioSource = audioObj.AddComponent<AudioSource>();
            deviceAudioSource.spatialBlend = 0f; // 2D sound
            deviceAudioSource.volume = 0.7f;
        }
        
        // Find or create pulse effect (using simplified version)
        if (pulseEffect == null)
        {
            // Try to find SimpleEchoPulseEffect first
            pulseEffect = GetComponent<SimpleEchoPulseEffect>();
            if (pulseEffect == null && playerCamera != null)
            {
                // Check camera
                pulseEffect = playerCamera.gameObject.GetComponent<SimpleEchoPulseEffect>();
                if (pulseEffect == null)
                {
                    pulseEffect = playerCamera.gameObject.AddComponent<SimpleEchoPulseEffect>();
                    Debug.Log("[EchoDevice] Created SimpleEchoPulseEffect on camera");
                }
            }
        }
        
        // Hide device model initially if not equipped
        if (deviceModel != null)
        {
            deviceModel.SetActive(hasDevice);
        }
    }
    
    void Update()
    {
        if (!hasDevice)
        {
            // Debug: Check if Q is pressed even without device
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.LogWarning("[EchoDevice] Q pressed but hasDevice = false!");
            }
            return;
        }
        
        // Block input when menu is open
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.LogWarning("[EchoDevice] Q pressed but cursor is unlocked (menu open)");
            }
            return;
        }
        
        HandleFrequencyAdjustment();
        HandlePulseActivation();
        HandleBatteryDrain();
    }
    
    void HandleFrequencyAdjustment()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float oldFrequency = currentFrequency;
            currentFrequency += scroll * frequencyScrollSpeed;
            currentFrequency = Mathf.Clamp(currentFrequency, minFrequency, maxFrequency);
            
            // Play frequency change sound (pitch based on frequency)
            if (frequencyChangeSound != null && deviceAudioSource != null && Mathf.Abs(oldFrequency - currentFrequency) > 1f)
            {
                deviceAudioSource.pitch = Mathf.Lerp(0.8f, 1.2f, currentFrequency / maxFrequency);
                deviceAudioSource.PlayOneShot(frequencyChangeSound, 0.3f);
            }
            
            Debug.Log($"[EchoDevice] Frequency adjusted to: {currentFrequency:F1} Hz");
        }
    }
    
    void HandlePulseActivation()
    {
        if (Input.GetKeyDown(pulseKey))
        {
            Debug.Log($"[EchoDevice] Q Key pressed! hasDevice={hasDevice}, battery={currentBattery}");
            
            // Check cooldown
            if (Time.time - lastPulseTime < pulseCooldown)
            {
                float remainingCooldown = pulseCooldown - (Time.time - lastPulseTime);
                Debug.Log($"[EchoDevice] Pulse on cooldown: {remainingCooldown:F1}s remaining");
                return;
            }
            
            // Check battery
            if (currentBattery < batteryConsumptionPerPulse)
            {
                Debug.Log("[EchoDevice] Insufficient battery!");
                if (InteractionUI.Instance != null)
                {
                    InteractionUI.Instance.ShowPrompt("Pil Yetersiz!");
                }
                return;
            }
            
            ActivatePulse();
        }
    }
    
    void ActivatePulse()
    {
        Debug.Log("[EchoDevice] ActivatePulse() called!");
        
        lastPulseTime = Time.time;
        currentBattery -= batteryConsumptionPerPulse;
        currentBattery = Mathf.Max(0, currentBattery);
        
        // Trigger pulse effect
        if (pulseEffect != null)
        {
            // Try both types
            SimpleEchoPulseEffect simpleEffect = pulseEffect as SimpleEchoPulseEffect;
            if (simpleEffect != null)
            {
                simpleEffect.TriggerPulse(pulseRadius, pulseSpeed, pulseDuration, currentFrequency);
                Debug.Log("[EchoDevice] SimpleEchoPulseEffect triggered!");
            }
            else
            {
                EchoPulseEffect advancedEffect = pulseEffect as EchoPulseEffect;
                if (advancedEffect != null)
                {
                    advancedEffect.TriggerPulse(pulseRadius, pulseSpeed, pulseDuration, currentFrequency);
                    Debug.Log("[EchoDevice] EchoPulseEffect triggered!");
                }
            }
        }
        else
        {
            Debug.LogError("[EchoDevice] pulseEffect is NULL! Visual effect won't work!");
        }
        
        // Play pulse sound
        if (pulseSound != null && deviceAudioSource != null)
        {
            deviceAudioSource.pitch = 1f;
            deviceAudioSource.PlayOneShot(pulseSound);
            Debug.Log("[EchoDevice] Pulse sound played");
        }
        else if (pulseSound == null)
        {
            Debug.LogWarning("[EchoDevice] No pulse sound assigned (optional)");
        }
        
        Debug.Log($"[EchoDevice] Pulse activated! Frequency: {currentFrequency:F1}Hz, Battery: {currentBattery:F1}%");
    }
    
    void HandleBatteryDrain()
    {
        if (isPulseActive && currentBattery > 0)
        {
            currentBattery -= batteryDrainPerSecond * Time.deltaTime;
            currentBattery = Mathf.Max(0, currentBattery);
            
            if (currentBattery <= 0)
            {
                isPulseActive = false;
                Debug.Log("[EchoDevice] Battery depleted!");
            }
        }
    }
    
    /// <summary>
    /// Called when player picks up the Echo device
    /// </summary>
    public void EquipEchoDevice(GameObject devicePrefab)
    {
        hasDevice = true;
        Debug.Log("[EchoDevice] EquipEchoDevice called! hasDevice = true");
        
        // Find camera if not already found
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            Debug.Log($"[EchoDevice] Camera found: {(playerCamera != null ? playerCamera.name : "NULL")}");
        }
        
        // Setup pulse effect if not already set (use simplified version)
        if (pulseEffect == null && playerCamera != null)
        {
            pulseEffect = playerCamera.gameObject.GetComponent<SimpleEchoPulseEffect>();
            if (pulseEffect == null)
            {
                pulseEffect = playerCamera.gameObject.AddComponent<SimpleEchoPulseEffect>();
                Debug.Log("[EchoDevice] Created SimpleEchoPulseEffect on camera");
            }
            else
            {
                Debug.Log("[EchoDevice] Found existing SimpleEchoPulseEffect");
            }
        }
        
        // Instantiate device model (MUST be child of camera to follow view)
        if (deviceHoldPosition == null && playerCamera != null)
        {
            // Create EchoDeviceContainer as child of camera (like FlashlightContainer)
            Transform existingContainer = playerCamera.transform.Find("EchoDeviceContainer");
            if (existingContainer != null)
            {
                deviceHoldPosition = existingContainer;
                Debug.Log("[EchoDevice] Found existing EchoDeviceContainer");
            }
            else
            {
                GameObject container = new GameObject("EchoDeviceContainer");
                // CRITICAL: SetParent to CAMERA, not player!
                container.transform.SetParent(playerCamera.transform, false); // false = use local space
                // Position using configurable offset (can be adjusted in Inspector)
                container.transform.localPosition = deviceHandOffset;
                container.transform.localRotation = Quaternion.Euler(deviceHandRotation);
                deviceHoldPosition = container.transform;
                Debug.Log($"[EchoDevice] Created EchoDeviceContainer as CHILD of camera: {playerCamera.name}");
                Debug.Log($"[EchoDevice] Container position: {deviceHandOffset}, rotation: {deviceHandRotation}");
            }
            
            // Create device model - either from prefab or placeholder
            if (devicePrefab != null)
            {
                deviceModel = Instantiate(devicePrefab, deviceHoldPosition);
                // Keep local position at zero but DON'T reset rotation
                // This allows the prefab to have its own rotation for hand-held appearance
                deviceModel.transform.localPosition = Vector3.zero;
                // Keep original prefab rotation (don't override it)
                Debug.Log($"[EchoDevice] Device model instantiated from prefab with rotation: {deviceModel.transform.localEulerAngles}");
            }
            else
            {
                // Create simple placeholder if no prefab assigned
                deviceModel = CreatePlaceholderDevice(deviceHoldPosition);
                Debug.Log("[EchoDevice] Created placeholder device model (no prefab assigned)");
            }
            
            if (deviceModel != null)
            {
                deviceModel.SetActive(true);
                // Log both world and local positions for debugging
                Debug.Log($"[EchoDevice] Device model WORLD position: {deviceModel.transform.position}");
                Debug.Log($"[EchoDevice] Device model LOCAL position: {deviceModel.transform.localPosition}");
                Debug.Log($"[EchoDevice] Device model parent: {deviceModel.transform.parent.name}");
            }
        }
        else if (deviceHoldPosition != null)
        {
            Debug.Log($"[EchoDevice] DeviceHoldPosition already exists, parent: {deviceHoldPosition.parent.name}");
        }
        else if (playerCamera == null)
        {
            Debug.LogError("[EchoDevice] Cannot create device model - playerCamera is NULL!");
        }
        
        Debug.Log($"[EchoDevice] Device equipped and ready! hasDevice={hasDevice}, pulseEffect={pulseEffect != null}");
    }
    
    
    /// <summary>
    /// Recharge battery (e.g., from battery pickup)
    /// </summary>
    public void RechargeBattery(float amount)
    {
        currentBattery += amount;
        currentBattery = Mathf.Min(currentBattery, maxBattery);
        Debug.Log($"[EchoDevice] Battery recharged! Current: {currentBattery:F1}%");
    }
    
    /// <summary>
    /// Creates a simple placeholder device model for visual feedback
    /// </summary>
    GameObject CreatePlaceholderDevice(Transform parent)
    {
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = "EchoDevice_Placeholder";
        placeholder.transform.SetParent(parent);
        placeholder.transform.localPosition = Vector3.zero;
        placeholder.transform.localRotation = Quaternion.identity;
        placeholder.transform.localScale = new Vector3(0.08f, 0.12f, 0.04f);
        
        // Make it cyan colored to match Echo theme
        Renderer rend = placeholder.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = new Color(0.2f, 0.8f, 1f); // Cyan
            rend.material.SetFloat("_Metallic", 0.7f);
            rend.material.SetFloat("_Glossiness", 0.8f);
            
            // Add emission glow
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 0.5f));
        }
        
        // Remove collider (we don't need physics for held device)
        Collider col = placeholder.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        return placeholder;
    }
    
    // Public getters for UI
    public bool HasDevice => hasDevice;
    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;
    public float CurrentFrequency => currentFrequency;
    public float BatteryPercentage => (currentBattery / maxBattery) * 100f;
}
