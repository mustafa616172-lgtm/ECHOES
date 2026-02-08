using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ECHOES - Indoor Area Volume
/// Place this in enclosed/indoor areas for enhanced horror atmosphere
/// Increases vignette, chromatic aberration, and darkness for tight spaces
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class IndoorVolumeZone : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Volume localVolume;
    [SerializeField] private float blendDistance = 2f;
    
    [Header("Indoor Adjustments")]
    [Tooltip("Extra darkness for enclosed spaces")]
    [SerializeField] private float indoorDarknessBonus = -0.3f; // Extra -0.3 exposure
    
    [Tooltip("Extra vignette for claustrophobia")]
    [SerializeField] private float indoorVignetteBonus = 0.15f; // +0.15 vignette
    
    [Tooltip("Extra chromatic aberration for unease")]
    [SerializeField] private float indoorChromaticBonus = 0.1f; // +0.1 chromatic
    
    private BoxCollider triggerCollider;
    private VolumeProfile indoorProfile;
    
    void Awake()
    {
        SetupCollider();
        SetupVolume();
    }
    
    void SetupCollider()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        
        // Make sure it's set up properly
        if (triggerCollider.size == Vector3.one)
        {
            Debug.LogWarning("[IndoorVolumeZone] BoxCollider size is default (1,1,1). Adjust in Inspector!");
        }
    }
    
    void SetupVolume()
    {
        // Create local volume if not assigned
        if (localVolume == null)
        {
            localVolume = gameObject.AddComponent<Volume>();
        }
        
        localVolume.isGlobal = false;
        localVolume.priority = 10; // Higher than global volume
        localVolume.blendDistance = blendDistance;
        
        CreateIndoorProfile();
    }
    
    void CreateIndoorProfile()
    {
        // Create a new profile for this indoor area
        indoorProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        localVolume.profile = indoorProfile;
        
        // Add Color Adjustments for extra darkness
        var colorAdjustments = indoorProfile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = indoorDarknessBonus;
        
        // Add extra vignette for claustrophobia
        var vignette = indoorProfile.Add<UnityEngine.Rendering.Universal.Vignette>();
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.6f; // Base vignette from global + bonus
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.3f; // Tighter vignette for indoor
        
        // Add extra chromatic aberration for unease
        var chromatic = indoorProfile.Add<UnityEngine.Rendering.Universal.ChromaticAberration>();
        chromatic.intensity.overrideState = true;
        chromatic.intensity.value = 0.4f; // Base + bonus
        
        Debug.Log("[IndoorVolumeZone] Indoor profile created with enhanced horror effects");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            Debug.Log("[IndoorVolumeZone] Player entered indoor area - increasing horror effects");
            localVolume.weight = 1f;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            Debug.Log("[IndoorVolumeZone] Player left indoor area - returning to normal");
            localVolume.weight = 0f;
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualize the indoor zone in editor
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(0.2f, 0.3f, 1f, 0.2f); // Blue transparent
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            
            Gizmos.color = new Color(0.2f, 0.3f, 1f, 0.6f); // Blue wire
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
