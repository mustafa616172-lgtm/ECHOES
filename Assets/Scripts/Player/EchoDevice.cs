using UnityEngine;
using System.Collections;
using System.Collections.Generic;  

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
    
    [Header("Drop Settings")]
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private float dropForwardDistance = 1.5f;
    [SerializeField] private float dropTossForce = 2f;
    [SerializeField] private GameObject echoPickupPrefab;
    
    [Header("Echo Pulse")]
    [SerializeField] private KeyCode pulseKey = KeyCode.Q;
    [SerializeField] private float pulseCooldown = 2f;
    [SerializeField] private float pulseRadius = 30f;
    [SerializeField] private float pulseSpeed = 10f;
    [SerializeField] private float pulseDuration = 3f;
    [SerializeField] private float batteryConsumptionPerPulse = 5f;
    [SerializeField] private float pulseFrequency = 440f; // Fixed frequency for the visual effect
    
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
    [SerializeField] private Vector3 deviceHandOffset = new Vector3(-0.3f, -0.25f, 0.4f);
    [Tooltip("Rotation of device when held (X=pitch, Y=yaw, Z=roll)")]
    [SerializeField] private Vector3 deviceHandRotation = new Vector3(-15f, 10f, 0f);
    [Tooltip("Scale of device when held")]
    [SerializeField] private Vector3 deviceHandScale = new Vector3(10f, 10f, 10f);
    
    private float lastPulseTime;
    private bool isPulseActive = false;
    private Camera playerCamera;
    private GameObject originalSceneObject; // Reference to original Echo object in scene
    
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
        
        // Device model stays visible in scene
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
        
        HandlePulseActivation();
        HandleBatteryDrain();
        HandleDropInput();
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
                simpleEffect.TriggerPulse(pulseRadius, pulseSpeed, pulseDuration, pulseFrequency);
                Debug.Log("[EchoDevice] SimpleEchoPulseEffect triggered!");
            }
            else
            {
                EchoPulseEffect advancedEffect = pulseEffect as EchoPulseEffect;
                if (advancedEffect != null)
                {
                    advancedEffect.TriggerPulse(pulseRadius, pulseSpeed, pulseDuration, pulseFrequency);
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
        
        Debug.Log($"[EchoDevice] Pulse activated! Frequency: {pulseFrequency:F1}Hz, Battery: {currentBattery:F1}%");
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
    /// Called when player picks up the Echo device.
    /// Always creates fresh container and model, cleaning up any old ones first.
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
        
        if (playerCamera == null)
        {
            Debug.LogError("[EchoDevice] Cannot create device model - playerCamera is NULL!");
            return;
        }
        
        // --- CLEANUP: Destroy any old model/container from a previous equip ---
        if (deviceModel != null)
        {
            DestroyImmediate(deviceModel);
            deviceModel = null;
            Debug.Log("[EchoDevice] Cleaned up old device model");
        }
        
        if (deviceHoldPosition != null)
        {
            DestroyImmediate(deviceHoldPosition.gameObject);
            deviceHoldPosition = null;
            Debug.Log("[EchoDevice] Cleaned up old device container");
        }
        
        // Also destroy any leftover EchoDeviceContainer (from deferred Destroy)
        Transform existingContainer = playerCamera.transform.Find("EchoDeviceContainer");
        if (existingContainer != null)
        {
            DestroyImmediate(existingContainer.gameObject);
            Debug.Log("[EchoDevice] Cleaned up leftover EchoDeviceContainer");
        }
        
        // --- CREATE: Fresh container as child of camera ---
        GameObject container = new GameObject("EchoDeviceContainer");
        container.transform.SetParent(playerCamera.transform, false);
        container.transform.localPosition = deviceHandOffset;
        container.transform.localRotation = Quaternion.Euler(deviceHandRotation);
        deviceHoldPosition = container.transform;
        Debug.Log($"[EchoDevice] Created EchoDeviceContainer as CHILD of camera: {playerCamera.name}");
        
        // --- CREATE: Device model inside container ---
        if (devicePrefab != null)
        {
            deviceModel = Instantiate(devicePrefab, deviceHoldPosition);
            deviceModel.transform.localPosition = Vector3.zero;
            deviceModel.transform.localScale = deviceHandScale;
            Debug.Log($"[EchoDevice] Device model instantiated from prefab with scale: {deviceHandScale}");
        }
        else
        {
            deviceModel = CreatePlaceholderDevice(deviceHoldPosition);
            Debug.Log("[EchoDevice] Created placeholder device model (no prefab assigned)");
        }
        
        if (deviceModel != null)
        {
            deviceModel.SetActive(true);
            
            // Disable physics on the hand model (it's cosmetic only)
            Rigidbody modelRb = deviceModel.GetComponent<Rigidbody>();
            if (modelRb != null) DestroyImmediate(modelRb);
            Collider modelCol = deviceModel.GetComponent<Collider>();
            if (modelCol != null) DestroyImmediate(modelCol);
            
            Debug.Log($"[EchoDevice] Device model active, WORLD pos: {deviceModel.transform.position}, parent: {deviceModel.transform.parent.name}");
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
    public float BatteryPercentage => (currentBattery / maxBattery) * 100f;
    
    // ============================================
    // DROP SYSTEM
    // ============================================
    
    void HandleDropInput()
    {
        if (Input.GetKeyDown(dropKey))
        {
            DropDevice();
        }
    }
    
    /// <summary>
    /// Drops the Echo device from player's hand onto the ground.
    /// Uses raycast to find the ground so it doesn't fall through the map.
    /// </summary>
    public void DropDevice()
    {
        if (!hasDevice) return;
        
        Debug.Log("[EchoDevice] DropDevice called!");
        
        hasDevice = false;
        
        // Calculate drop position using ground raycast
        Vector3 dropPos = CalculateDropPosition();
        
        // Spawn the pickup object
        GameObject droppedDevice = null;
        
        if (originalSceneObject != null)
        {
            // Re-enable and reposition the original scene object
            originalSceneObject.SetActive(true);
            originalSceneObject.transform.position = dropPos;
            originalSceneObject.transform.rotation = Quaternion.identity;
            droppedDevice = originalSceneObject;
            
            // Make sure it has EchoPickupItem component
            if (droppedDevice.GetComponent<EchoPickupItem>() == null)
            {
                droppedDevice.AddComponent<EchoPickupItem>();
            }
            
            Debug.Log("[EchoDevice] Re-enabled original scene object");
        }
        else if (echoPickupPrefab != null)
        {
            droppedDevice = Instantiate(echoPickupPrefab, dropPos, Quaternion.identity);
            Debug.Log("[EchoDevice] Spawned Echo pickup from prefab");
        }
        else
        {
            droppedDevice = CreateDroppedPlaceholder(dropPos);
            Debug.Log("[EchoDevice] Created placeholder dropped device");
        }
        
        // Setup physics and collider properly
        if (droppedDevice != null)
        {
            SetupDroppedPhysics(droppedDevice);
        }
        
        // Clean up hand model and container
        if (deviceModel != null)
        {
            Destroy(deviceModel);
            deviceModel = null;
        }
        
        if (deviceHoldPosition != null)
        {
            Destroy(deviceHoldPosition.gameObject);
            deviceHoldPosition = null;
        }
        
        // Remove from inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.RemoveItem("echo_device");
        }
        
        // Show feedback
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.ShowPrompt("Echo Cihazi birakildi");
        }
        
        Debug.Log("[EchoDevice] Device dropped successfully");
    }
    
    /// <summary>
    /// Calculates a safe drop position by raycasting to find the ground.
    /// Prevents the device from falling through the map.
    /// </summary>
    Vector3 CalculateDropPosition()
    {
        Vector3 origin;
        Vector3 forward;
        
        if (playerCamera != null)
        {
            origin = playerCamera.transform.position;
            forward = playerCamera.transform.forward;
        }
        else
        {
            origin = transform.position;
            forward = transform.forward;
        }
        
        // Project forward but keep it mostly horizontal (don't throw into ground or sky)
        Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
        if (flatForward.magnitude < 0.01f)
            flatForward = transform.forward;
        Vector3 targetPos = origin + flatForward * dropForwardDistance;
        
        // Raycast down from the target position to find the ground
        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(targetPos.x, origin.y + 2f, targetPos.z); // Start above player
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 50f))
        {
            // Place slightly above the ground so it doesn't clip
            Vector3 groundPos = hit.point + Vector3.up * 0.15f;
            Debug.Log($"[EchoDevice] Ground found at Y={hit.point.y:F2}, dropping at Y={groundPos.y:F2}");
            return groundPos;
        }
        
        // Fallback: Raycast from player position straight down
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 50f))
        {
            Vector3 groundPos = hit.point + Vector3.up * 0.15f + flatForward * 0.8f;
            Debug.Log($"[EchoDevice] Fallback ground at Y={hit.point.y:F2}");
            return groundPos;
        }
        
        // Last resort: use player's feet position
        Debug.LogWarning("[EchoDevice] No ground found via raycast! Using player position");
        return transform.position + flatForward * dropForwardDistance;
    }
    
    /// <summary>
    /// Sets up proper physics and collider on the dropped device so it doesn't fall through.
    /// </summary>
    void SetupDroppedPhysics(GameObject droppedDevice)
    {
        // Ensure proper collider exists (check both object and children)
        Collider existingCol = droppedDevice.GetComponent<Collider>();
        if (existingCol == null)
        {
            existingCol = droppedDevice.GetComponentInChildren<Collider>();
        }
        
        if (existingCol == null)
        {
            // No collider found anywhere - add one
            BoxCollider col = droppedDevice.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.4f, 0.15f); // Realistic device size
            col.center = Vector3.zero;
            Debug.Log("[EchoDevice] Added BoxCollider to dropped device");
        }
        else
        {
            // Use existing collider but make sure it's not a trigger
            existingCol.enabled = true;
            existingCol.isTrigger = false;
            Debug.Log($"[EchoDevice] Using existing collider: {existingCol.GetType().Name}");
        }
        
        // Setup Rigidbody for physics
        Rigidbody rb = droppedDevice.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedDevice.AddComponent<Rigidbody>();
        }
        
        rb.mass = 1f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Freeze X/Z rotation so it doesn't tip over and roll away
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Gentle horizontal nudge forward (not a throw that goes through floors)
        Vector3 nudge = (playerCamera != null ? playerCamera.transform.forward : transform.forward);
        nudge.y = 0; // Keep horizontal only
        rb.AddForce(nudge.normalized * dropTossForce * 0.5f, ForceMode.Impulse);
        
        Debug.Log("[EchoDevice] Physics setup complete on dropped device");
    }
    
    /// <summary>
    /// Stores reference to the original scene object (called by EchoPickupItem before destroying itself)
    /// </summary>
    public void SetOriginalSceneObject(GameObject obj)
    {
        originalSceneObject = obj;
    }

    /// <summary>
    /// Creates a simple visual placeholder when dropping the device (no prefab assigned)
    /// </summary>
    GameObject CreateDroppedPlaceholder(Vector3 position)
    {
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = "Echo_Dropped";
        placeholder.transform.position = position;
        placeholder.transform.localScale = new Vector3(0.15f, 0.2f, 0.08f);
        
        // Cyan color to match Echo theme
        Renderer rend = placeholder.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = new Color(0.2f, 0.8f, 1f);
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 0.5f));
        }
        
        // Add EchoPickupItem so player can pick it back up
        placeholder.AddComponent<EchoPickupItem>();
        
        return placeholder;
    }
}
