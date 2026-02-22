using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ECHOES - Sound Recorder Device
/// A separate device from the Echo Device.
/// Functions:
/// 1. Frequency Tuning (Mouse Scroll)
/// 2. Resonance Interaction (Opening doors)
/// 3. Pickup/Drop (E/G)
/// 4. Activation (Q)
/// </summary>
public class SoundRecorderDevice : MonoBehaviour
{
    [Header("Device Settings")]
    [SerializeField] private bool hasDevice = false;
    [SerializeField] private GameObject deviceModel; // The visual model in hand
    [SerializeField] private Transform deviceHoldPosition;
    
    [Header("Drop Settings")]
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private float dropForwardDistance = 1.5f;
    [SerializeField] private float dropTossForce = 2f;
    [SerializeField] private GameObject pickupPrefab;
    
    [Header("Activation")]
    [SerializeField] private KeyCode activationKey = KeyCode.Q;
    [SerializeField] private bool isActive = false;
    
    [Header("Frequency Settings")]
    [SerializeField] private float currentFrequency = 440f; // Default A4 note
    [SerializeField] private float minFrequency = 20f;
    [SerializeField] private float maxFrequency = 20000f;
    [SerializeField] private float frequencyScrollSpeed = 50f;
    
    [Header("Visual Feedback")]
    [Tooltip("Renderers to change color based on resonance")]
    [SerializeField] private List<Renderer> indicatorRenderers = new List<Renderer>();
    [SerializeField] private bool generateDynamicLights = true;
    
    [Header("Audio")]
    [SerializeField] private AudioSource deviceAudioSource;
    [SerializeField] private AudioClip frequencyChangeSound;
    [SerializeField] private AudioClip activationSound;
    
    [Header("Device Model Positioning")]
    [Tooltip("Offset from camera for device position")]
    [SerializeField] private Vector3 deviceHandOffset = new Vector3(0.3f, -0.25f, 0.4f);
    [Tooltip("Rotation of device when held")]
    [SerializeField] private Vector3 deviceHandRotation = new Vector3(-15f, -10f, 0f);
    [Tooltip("Scale of device when held")]
    [SerializeField] private Vector3 deviceHandScale = new Vector3(1f, 1f, 1f);

    // Internal state
    private float resonanceResetTimer = 0f;
    private bool isResonating = false;
    private float currentTargetFrequency = 440f;
    private Camera playerCamera;
    private GameObject originalSceneObject;

    // Public properties
    public bool HasDevice => hasDevice;
    public float CurrentFrequency => currentFrequency;
    public bool IsActive => isActive;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        // Auto-detect renderers if not assigned
        if (indicatorRenderers.Count == 0)
        {
            GameObject targetRoot = deviceModel != null ? deviceModel : gameObject;
            indicatorRenderers.AddRange(targetRoot.GetComponentsInChildren<Renderer>());
        }
        
        if (deviceAudioSource == null)
        {
            deviceAudioSource = GetComponent<AudioSource>();
            if (deviceAudioSource == null)
            {
                deviceAudioSource = gameObject.AddComponent<AudioSource>();
                deviceAudioSource.spatialBlend = 0f; // 2D sound for player
            }
        }
        
        // Ensure UI exists
        if (SoundWaveUI.Instance == null)
        {
            GameObject uiObj = new GameObject("SoundWaveUI_Manager");
            uiObj.AddComponent<SoundWaveUI>();
        }
        
        if (generateDynamicLights)
        {
            GenerateDynamicLights();
        }
        
        // Initial state
        if (deviceModel != null)
        {
            deviceModel.SetActive(hasDevice);
        }
    }
    
    void GenerateDynamicLights()
    {
        if (deviceModel == null) return;
        
        // Create small point lights logic (simplified for brevity, ensuring it attaches to model)
        GameObject lightsRoot = new GameObject("DeviceLights");
        lightsRoot.transform.SetParent(deviceModel.transform);
        lightsRoot.transform.localPosition = Vector3.zero;
        lightsRoot.transform.localRotation = Quaternion.identity;
        
        // Main Indicator Light
        CreateLight(lightsRoot, new Vector3(0, 0.05f, 0.05f), 0.2f, 1.5f);
    }
    
    void CreateLight(GameObject parent, Vector3 localPos, float range, float intensity)
    {
        GameObject lightObj = new GameObject("LedLight");
        lightObj.transform.SetParent(parent.transform, false);
        lightObj.transform.localPosition = localPos;
        
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = Color.red; 
        l.renderMode = LightRenderMode.ForcePixel;
        
        // Visual orb
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.transform.SetParent(lightObj.transform, false);
        orb.transform.localScale = Vector3.one * 0.02f;
        Destroy(orb.GetComponent<Collider>());
        
        Renderer r = orb.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Standard"));
        r.material.EnableKeyword("_EMISSION");
        r.material.SetColor("_EmissionColor", Color.red);
        
        indicatorRenderers.Add(r);
    }

    void Update()
    {
        if (!hasDevice) return;
        
        // Only allow inputs if cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked) return;

        HandleActivationInput();
        HandleDropInput();

        if (isActive)
        {
            HandleFrequencyInput();
            HandleResonanceReset();
        }
    }
    
    void HandleActivationInput()
    {
        if (Input.GetKeyDown(activationKey))
        {
            isActive = !isActive;
            Debug.Log($"[SoundRecorder] Activation toggled: {isActive}");
            
            if (activationSound != null && deviceAudioSource != null)
            {
                deviceAudioSource.PlayOneShot(activationSound);
            }
            
            if (SoundWaveUI.Instance != null)
            {
                if (isActive) 
                {
                     // Show UI if we are in a resonance area, or just show "Ready"
                     if (isResonating) SoundWaveUI.Instance.Show();
                }
                else 
                {
                    SoundWaveUI.Instance.Hide();
                }
            }
        }
    }

    void HandleFrequencyInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float oldFrequency = currentFrequency;
            currentFrequency += scroll * frequencyScrollSpeed;
            currentFrequency = Mathf.Clamp(currentFrequency, minFrequency, maxFrequency);
            
            // Audio feedback
            if (frequencyChangeSound != null && deviceAudioSource != null && Mathf.Abs(oldFrequency - currentFrequency) > 1f)
            {
                if (!deviceAudioSource.isPlaying)
                {
                    deviceAudioSource.pitch = Mathf.Lerp(0.8f, 1.2f, currentFrequency / 2000f);
                    deviceAudioSource.PlayOneShot(frequencyChangeSound, 0.2f);
                }
            }
            
            // Update UI
            if (SoundWaveUI.Instance != null && isResonating)
            {
                SoundWaveUI.Instance.UpdateFrequencies(currentTargetFrequency, currentFrequency);
            }
        }
    }

    public void EnterResonanceArea(float targetFreq)
    {
        currentTargetFrequency = targetFreq;
        if (isActive && SoundWaveUI.Instance != null)
        {
            SoundWaveUI.Instance.Show();
            SoundWaveUI.Instance.UpdateFrequencies(targetFreq, currentFrequency);
        }
    }
    
    public void ExitResonanceArea()
    {
        if (SoundWaveUI.Instance != null)
        {
            SoundWaveUI.Instance.Hide();
        }
        isResonating = false;
        ResetVisuals();
    }

    void HandleResonanceReset()
    {
        if (isResonating)
        {
            resonanceResetTimer -= Time.deltaTime;
            if (resonanceResetTimer <= 0)
            {
                isResonating = false;
                ResetVisuals();
            }
        }
    }

    public void SetResonanceState(float matchFactor, bool inRange)
    {
        if (!hasDevice || !isActive) return;
        
        isResonating = inRange;
        resonanceResetTimer = 0.2f; // Auto-reset
        
        Color emissionColor;
        if (inRange)
        {
            emissionColor = Color.Lerp(Color.red, Color.green, matchFactor);
            emissionColor *= 2f; 
        }
        else
        {
            emissionColor = Color.black;
        }

        foreach (Renderer r in indicatorRenderers)
        {
            if (r != null && r.material != null)
            {
                if (inRange)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", emissionColor);
                }
                else
                {
                    r.material.DisableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", Color.black);
                }
                
                Light l = r.GetComponentInParent<Light>();
                if (l != null) l.color = inRange ? emissionColor : Color.black;
            }
        }
    }
    
    private void ResetVisuals()
    {
        foreach (Renderer r in indicatorRenderers)
        {
            if (r != null && r.material != null)
            {
                r.material.DisableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    // --- Dropping Logic ---

    void HandleDropInput()
    {
        if (Input.GetKeyDown(dropKey))
        {
            DropDevice();
        }
    }

    public void DropDevice()
    {
        if (!hasDevice) return;
        
        hasDevice = false;
        isActive = false; // Turn off when dropped
        if (SoundWaveUI.Instance != null) SoundWaveUI.Instance.Hide();

        Vector3 dropPos = CalculateDropPosition();
        
        GameObject droppedItem = null;
        if (originalSceneObject != null)
        {
            originalSceneObject.SetActive(true);
            originalSceneObject.transform.position = dropPos;
            originalSceneObject.transform.rotation = Quaternion.identity;
            droppedItem = originalSceneObject;
        }
        else if (pickupPrefab != null)
        {
            droppedItem = Instantiate(pickupPrefab, dropPos, Quaternion.identity);
        }
        else
        {
            droppedItem = CreateDroppedPlaceholder(dropPos);
        }

        if (droppedItem != null)
        {
            SetupDroppedPhysics(droppedItem);
        }

        // Cleanup model
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
        
        Debug.Log("[SoundRecorder] Device dropped.");
        if (InteractionUI.Instance != null) InteractionUI.Instance.ShowPrompt("Ses Kayit Cihazi birakildi");
    }

    Vector3 CalculateDropPosition()
    {
        Vector3 origin = playerCamera != null ? playerCamera.transform.position : transform.position;
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        
        Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
        if (flatForward.magnitude < 0.01f) flatForward = transform.forward;
        
        Vector3 targetPos = origin + flatForward * dropForwardDistance;
        
        RaycastHit hit;
        if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out hit, 10f))
        {
             return hit.point + Vector3.up * 0.15f;
        }
        return transform.position + flatForward * 0.5f;
    }
    
    void SetupDroppedPhysics(GameObject item)
    {
        Collider col = item.GetComponent<Collider>();
        if (col == null) item.AddComponent<BoxCollider>();
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null) rb = item.AddComponent<Rigidbody>();
        
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(transform.forward * dropTossForce, ForceMode.Impulse);
    }
    
    GameObject CreateDroppedPlaceholder(Vector3 pos)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = "SoundRecorder_Dropped";
        p.transform.position = pos;
        p.transform.localScale = new Vector3(0.1f, 0.15f, 0.05f);
        p.GetComponent<Renderer>().material.color = Color.gray;
        
        // Add component to pick it up again
        SoundRecorderPickupItem pickup = p.AddComponent<SoundRecorderPickupItem>();
        // Note: We can't easily assign the original prefab back here if we don't have a reference to it.
        // The PickupItem script usually needs a reference to the device prefab to equip it.
        // For now, this is a fallback that might not work perfectly without a proper prefab assigned in inspector.
        
        return p;
    }

    // --- Equip Logic ---

    public void EquipDevice(GameObject prefab)
    {
        hasDevice = true;
        isActive = false; // Start off until Q is pressed
        
        // Cleanup old
        if (deviceModel != null) Destroy(deviceModel);
        if (deviceHoldPosition != null) Destroy(deviceHoldPosition.gameObject);
        
        // Create container
        GameObject container = new GameObject("SoundRecorderContainer");
        if (playerCamera != null) container.transform.SetParent(playerCamera.transform, false);
        else container.transform.SetParent(transform, false);
        
        container.transform.localPosition = deviceHandOffset;
        container.transform.localRotation = Quaternion.Euler(deviceHandRotation);
        deviceHoldPosition = container.transform;
        
        // Instinctiate
        if (prefab != null)
        {
            deviceModel = Instantiate(prefab, deviceHoldPosition);
            deviceModel.transform.localPosition = Vector3.zero;
            deviceModel.transform.localRotation = Quaternion.identity;
            deviceModel.transform.localScale = deviceHandScale;
        }
        else
        {
            deviceModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deviceModel.transform.SetParent(deviceHoldPosition, false);
            deviceModel.transform.localScale = new Vector3(0.05f, 0.1f, 0.02f);
        }
        
        // Setup renderers again
        indicatorRenderers.Clear();
        indicatorRenderers.AddRange(deviceModel.GetComponentsInChildren<Renderer>());
        if (generateDynamicLights) GenerateDynamicLights();

        Debug.Log("[SoundRecorder] Device equipped.");
        if (InteractionUI.Instance != null) InteractionUI.Instance.ShowPrompt("Sound Recorder Alindi! [Q] Ac/Kapa [G] Birak");
    }
    
    public void SetOriginalSceneObject(GameObject obj)
    {
        originalSceneObject = obj;
    }
}
