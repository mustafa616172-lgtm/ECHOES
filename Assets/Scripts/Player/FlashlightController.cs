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
    [SerializeField] private Vector3 equippedPosition = new Vector3(0.5f, -0.3f, 0.6f); // Closer to camera
    [SerializeField] private Vector3 equippedRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 unequippedPosition = new Vector3(0.5f, -2f, 0.6f); // Off screen
    
    [Header("Camera Tracking")]
    [Tooltip("Follow camera rotation for realism")]
    [SerializeField] private bool followCameraRotation = true;
    [SerializeField] private Transform cameraLookTarget; // Will be found automatically
    
    [Header("Custom Model Settings")]
    [Tooltip("Offset for your custom flashlight 3D model")]
    [SerializeField] private Vector3 modelPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 modelRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 modelScale = Vector3.one;
    
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
        // 1. NUCLEAR CLEANUP: Destroy ALL children named "Flashlight" or related
        // uniqueID check to ensure we don't kill ourselves if this script is on the duplicate (unlikely)
        var allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t != transform && (t.name == "Flashlight" || t.name == "FlashlightModel_TEMP" || t.name.Contains("Sphere")))
            {
                Destroy(t.gameObject);
                Debug.LogWarning($"[FlashlightController] Nuclear cleanup: Destroyed {t.name}");
            }
        }
        
        // Also remove SimpleFlashlightTest component
        var simpleTest = GetComponent("SimpleFlashlightTest");
        if (simpleTest != null) Destroy(simpleTest);

        flashlightObject = null; // Reset reference to force recreation
        flashlightLight = null;

        SetupFlashlight();
        SetupVolumeProfile();
        
        // Start equipped logic
        if (startEquipped)
        {
            isEquipped = true;
            targetPosition = equippedPosition;
            if (flashlightObject != null)
            {
                // Force position immediately (no animation on start)
                // Need to convert local offset to world for new logic?
                // Actually start logic sets localPosition initially, but Update overrides it with world position.
                // To be safe, we let Update handle position, but set target for it.
            }
            if (flashlightLight != null) flashlightLight.enabled = true;
        }
        else
        {
            isEquipped = false;
            targetPosition = unequippedPosition;
        }
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

    void SetupFlashlight()
    {
        cameraTransform = transform;
        
        // Create flashlight object (Always create fresh after cleanup)
        if (flashlightObject == null)
        {
            flashlightObject = new GameObject("Flashlight");
            flashlightObject.transform.SetParent(cameraTransform);
            
            // Create visual
            GameObject tempModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempModel.name = "FlashlightModel_TEMP";
            tempModel.transform.SetParent(flashlightObject.transform);
            tempModel.transform.localPosition = Vector3.zero;
            tempModel.transform.localScale = Vector3.one * 0.15f; // Slightly smaller
            
            // Remove collider
            if (tempModel.GetComponent<Collider>()) Destroy(tempModel.GetComponent<Collider>());
            
            // Glow Material
            var renderer = tempModel.GetComponent<MeshRenderer>();
            if (renderer)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.yellow;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.yellow * 5f);
                renderer.material = mat;
            }
        }
        
        // Light
        if (flashlightLight == null)
        {
            GameObject lightObj = new GameObject("Light");
            lightObj.transform.SetParent(flashlightObject.transform);
            lightObj.transform.localPosition = Vector3.zero;
            lightObj.transform.localRotation = Quaternion.identity;
            flashlightLight = lightObj.AddComponent<Light>();
        }
        
        flashlightLight.type = LightType.Spot;
        flashlightLight.intensity = lightIntensity;
        flashlightLight.range = lightRange;
        flashlightLight.spotAngle = spotAngle;
        flashlightLight.enabled = false;
    }

    void Update()
    {
        // CONSTANTLY check for camera
        if (cameraLookTarget == null)
        {
            if (Camera.main != null) cameraLookTarget = Camera.main.transform;
            return; // Don't update if no camera
        }
    
        // Toggle input
        if (Input.GetKeyDown(toggleKey)) ToggleFlashlight();
        
        if (flashlightObject != null && cameraLookTarget != null)
        {
            // --- WORLD SPACE LOCKING ONLY ---
            // We calculate where the flashlight SHOULD be relative to Camera
            Vector3 desiredWorldPos = cameraLookTarget.TransformPoint(targetPosition);
            
            // We smoothly move the ACTUAL flashlight (parented to Player) to that world point
            flashlightObject.transform.position = Vector3.Lerp(
                flashlightObject.transform.position,
                desiredWorldPos,
                Time.deltaTime * 20f
            );
            
            // Rotation Tracking
            if (followCameraRotation)
            {
                Quaternion targetRot = cameraLookTarget.rotation * Quaternion.Euler(modelRotationOffset);
                flashlightObject.transform.rotation = Quaternion.Slerp(
                    flashlightObject.transform.rotation,
                    targetRot,
                    Time.deltaTime * 25f
                );
            }
            
            if (flashlightLight != null) flashlightLight.transform.rotation = flashlightObject.transform.rotation;
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

    void OnDrawGizmos()
    {
        if (showDebugGizmos && flashlightObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(flashlightObject.transform.position, 0.2f);
            Gizmos.DrawLine(transform.position, flashlightObject.transform.position);
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
        
        // Apply offsets for perfect positioning
        newModel.transform.localPosition = modelPositionOffset;
        newModel.transform.localRotation = Quaternion.Euler(modelRotationOffset);
        newModel.transform.localScale = modelScale;
        
        Debug.Log($"[Flashlight] Model replaced: {modelPrefab.name}");
        Debug.Log($"[Flashlight] Applied offsets - Pos: {modelPositionOffset}, Rot: {modelRotationOffset}, Scale: {modelScale}");
    }
}
