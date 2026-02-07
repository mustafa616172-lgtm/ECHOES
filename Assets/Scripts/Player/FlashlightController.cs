using UnityEngine;

/// <summary>
/// ECHOES - Flashlight Controller
/// The Forest style flashlight system - Press L to equip/unequip
/// Supports model replacement and dynamic lighting
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.L;
    
    [Header("Debug Options")]
    [SerializeField] private bool startEquipped = true; // Start with flashlight ON
    [SerializeField] private bool showDebugGizmos = true; // Show position in Scene view
    
    [Header("Flashlight Settings")]
    [SerializeField] private bool isEquipped = false;
    [SerializeField] private float equipSpeed = 5f;
    
    [Header("Light Settings")]
    [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.85f); // Warm white
    [SerializeField] private float lightIntensity = 8f; // INCREASED from 3 to 8
    [SerializeField] private float lightRange = 20f; // INCREASED from 15 to 20
    [SerializeField] private float spotAngle = 50f; // INCREASED from 45 to 50
    
    [Header("Position Settings")]
    [SerializeField] private Vector3 equippedPosition = new Vector3(0.4f, -0.3f, 0.6f); // More visible position
    [SerializeField] private Vector3 equippedRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 unequippedPosition = new Vector3(0.4f, -2f, 0.6f); // Farther down
    
    [Header("Darkness Compensation")]
    [Tooltip("Increases exposure when flashlight is on")]
    [SerializeField] private bool adjustExposure = true;
    [SerializeField] private float exposureBoost = 1.0f; // INCREASED from 0.5 to 1.0
    
    [Header("References (Auto-Setup)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GameObject flashlightObject;
    [SerializeField] private Light flashlightLight;
    
    private Vector3 targetPosition;
    private float originalExposure = -1.5f;
    private UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments;
    
    void Start()
    {
        SetupFlashlight();
        SetupVolumeProfile();
        
        // Start equipped or unequipped based on setting
        if (startEquipped)
        {
            isEquipped = true;
            targetPosition = equippedPosition;
            if (flashlightObject != null)
            {
                flashlightObject.transform.localPosition = equippedPosition;
            }
            if (flashlightLight != null)
            {
                flashlightLight.enabled = true;
            }
            Debug.Log("[Flashlight] Started EQUIPPED (for testing visibility)");
        }
        else
        {
            isEquipped = false;
            targetPosition = unequippedPosition;
            if (flashlightObject != null)
            {
                flashlightObject.transform.localPosition = unequippedPosition;
            }
            Debug.Log("[Flashlight] Started unequipped");
        }
        
        Debug.Log("[Flashlight] Initialized - Press L to toggle");
    }
    
    void SetupFlashlight()
    {
        // Find camera or camera root if not assigned
        if (cameraTransform == null)
        {
            // Try to find main camera first
            Camera cam = Camera.main;
            if (cam != null)
            {
                cameraTransform = cam.transform;
                Debug.Log("[Flashlight] Found Main Camera: " + cam.name);
            }
            else
            {
                // Try to find PlayerCameraRoot (for FirstPerson controller)
                Transform cameraRoot = transform.Find("PlayerCameraRoot");
                if (cameraRoot != null)
                {
                    cameraTransform = cameraRoot;
                    Debug.Log("[Flashlight] Found PlayerCameraRoot");
                }
                else
                {
                    // Try to find any Camera in children
                    cam = GetComponentInChildren<Camera>();
                    if (cam != null)
                    {
                        cameraTransform = cam.transform;
                        Debug.Log("[Flashlight] Found camera in children: " + cam.name);
                    }
                    else
                    {
                        Debug.LogError("[Flashlight] No camera or camera root found! Please assign cameraTransform manually.");
                        enabled = false;
                        return;
                    }
                }
            }
        }
        
        Debug.Log("[Flashlight] Camera transform set to: " + cameraTransform.name);
        
        // Create flashlight object if it doesn't exist
        if (flashlightObject == null)
        {
            flashlightObject = new GameObject("Flashlight");
            flashlightObject.transform.SetParent(cameraTransform);
            flashlightObject.transform.localPosition = unequippedPosition;
            flashlightObject.transform.localRotation = Quaternion.Euler(equippedRotation);
            
            // Create temporary visual (cube) - BIGGER AND MORE VISIBLE
            GameObject tempModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempModel.name = "FlashlightModel_TEMP";
            tempModel.transform.SetParent(flashlightObject.transform);
            tempModel.transform.localPosition = Vector3.zero;
            tempModel.transform.localRotation = Quaternion.identity;
            tempModel.transform.localScale = new Vector3(0.08f, 0.08f, 0.3f); // BIGGER - was 0.05, 0.05, 0.2
            
            // Set layer to avoid blocking camera
            tempModel.layer = LayerMask.NameToLayer("Ignore Raycast");
            if (tempModel.layer == -1) tempModel.layer = 2; // Default to Ignore Raycast layer
            
            // Remove collider
            Collider col = tempModel.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Debug.Log("[Flashlight] Created temporary flashlight model (yellow GLOWING cube)");
            
            // Make it GLOW - emissive yellow
            MeshRenderer renderer = tempModel.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.yellow;
                
                // Enable emission for glow
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.yellow * 2f); // Bright yellow glow
                
                renderer.material = mat;
                Debug.Log("[Flashlight] Applied GLOWING yellow material");
            }
        }
        
        // Create or get spotlight
        if (flashlightLight == null)
        {
            flashlightLight = flashlightObject.GetComponentInChildren<Light>();
            
            if (flashlightLight == null)
            {
                GameObject lightObj = new GameObject("Light");
                lightObj.transform.SetParent(flashlightObject.transform);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.transform.localRotation = Quaternion.identity;
                
                flashlightLight = lightObj.AddComponent<Light>();
            }
        }
        
        // Configure spotlight
        flashlightLight.type = LightType.Spot;
        flashlightLight.color = lightColor;
        flashlightLight.intensity = lightIntensity;
        flashlightLight.range = lightRange;
        flashlightLight.spotAngle = spotAngle;
        flashlightLight.shadows = LightShadows.Soft;
        flashlightLight.enabled = false; // Start disabled
        
        Debug.Log("[Flashlight] Flashlight setup complete!");
    }
    
    void SetupVolumeProfile()
    {
        if (!adjustExposure) return;
        
        // Find volume profile for exposure adjustment
        var volume = FindFirstObjectByType<UnityEngine.Rendering.Volume>();
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out colorAdjustments))
            {
                originalExposure = colorAdjustments.postExposure.value;
                Debug.Log("[Flashlight] Found ColorAdjustments - will adjust exposure");
            }
        }
    }
    
    void Update()
    {
        // Toggle flashlight with L key
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }
        
        // Smooth position animation
        if (flashlightObject != null)
        {
            flashlightObject.transform.localPosition = Vector3.Lerp(
                flashlightObject.transform.localPosition,
                targetPosition,
                Time.deltaTime * equipSpeed
            );
        }
    }
    
    void ToggleFlashlight()
    {
        isEquipped = !isEquipped;
        
        if (isEquipped)
        {
            EquipFlashlight();
        }
        else
        {
            UnequipFlashlight();
        }
    }
    
    void EquipFlashlight()
    {
        Debug.Log("[Flashlight] Equipped - Light ON, Moving to hand");
        
        targetPosition = equippedPosition;
        
        // Turn on light
        if (flashlightLight != null)
        {
            flashlightLight.enabled = true;
            Debug.Log($"[Flashlight] Light enabled - Intensity: {flashlightLight.intensity}, Range: {flashlightLight.range}");
        }
        
        // Increase exposure for better visibility
        if (adjustExposure && colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = originalExposure + exposureBoost;
            Debug.Log($"[Flashlight] Exposure boosted to: {colorAdjustments.postExposure.value}");
        }
    }
    
    void UnequipFlashlight()
    {
        Debug.Log("[Flashlight] Unequipped - Light OFF, Moving away");
        
        targetPosition = unequippedPosition;
        
        // Turn off light
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
            Debug.Log("[Flashlight] Light disabled");
        }
        
        // Restore original exposure
        if (adjustExposure && colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = originalExposure;
            Debug.Log($"[Flashlight] Exposure restored to: {originalExposure}");
        }
    }
    
    // Public methods for external control
    public void ForceEquip()
    {
        if (!isEquipped)
        {
            ToggleFlashlight();
        }
    }
    
    public void ForceUnequip()
    {
        if (isEquipped)
        {
            ToggleFlashlight();
        }
    }
    
    public bool IsEquipped()
    {
        return isEquipped;
    }
    
    // Replace temporary model with your 3D model
    public void SetFlashlightModel(GameObject modelPrefab)
    {
        if (flashlightObject == null) return;
        
        // Remove old temp model
        Transform oldModel = flashlightObject.transform.Find("FlashlightModel_TEMP");
        if (oldModel != null)
        {
            Destroy(oldModel.gameObject);
        }
        
        // Instantiate new model
        GameObject newModel = Instantiate(modelPrefab, flashlightObject.transform);
        newModel.name = "FlashlightModel";
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;
        
        Debug.Log("[Flashlight] Model replaced with: " + modelPrefab.name);
    }
}
