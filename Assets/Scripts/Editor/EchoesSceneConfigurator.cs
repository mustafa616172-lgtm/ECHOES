using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ECHOES - Scene Graphics Configurator
/// Automatically sets up horror graphics for Echoes scene
/// </summary>
public class EchoesSceneConfigurator : MonoBehaviour
{
    [MenuItem("ECHOES/Setup Graphics for Current Scene")]
    public static void SetupGraphicsForScene()
    {
        Debug.Log("=== ECHOES Scene Graphics Setup Started ===");
        
        // 1. Setup Global Volume
        SetupGlobalVolume();
        
        // 2. Setup Fog
        SetupFog();
        
        // 3. Adjust Lighting
        SetupLighting();
        
        // 4. Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        Debug.Log("=== ECHOES Scene Graphics Setup Complete! ===");
        EditorUtility.DisplayDialog("Success", 
            "Horror graphics configured!\n\n" +
            "✓ Global Volume with DefaultVolumeProfile\n" +
            "✓ Fog settings (density 0.08, blue-dark)\n" +
            "✓ Lighting adjusted for horror atmosphere\n\n" +
            "Press Play to test!", "OK");
    }
    
    static void SetupGlobalVolume()
    {
        // Check if Global Volume already exists
        Volume existingVolume = Object.FindFirstObjectByType<Volume>();
        if (existingVolume != null)
        {
            Debug.Log("[Graphics Setup] Global Volume already exists, updating...");
            ConfigureVolume(existingVolume);
            return;
        }
        
        // Create new Global Volume
        GameObject volumeObj = new GameObject("Global Volume");
        Volume volume = volumeObj.AddComponent<Volume>();
        ConfigureVolume(volume);
        
        Debug.Log("[Graphics Setup] Global Volume created!");
    }
    
    static void ConfigureVolume(Volume volume)
    {
        volume.isGlobal = true;
        volume.priority = 1;
        
        // Load DefaultVolumeProfile
        string profilePath = "Assets/Settings/DefaultVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        
        if (profile != null)
        {
            volume.profile = profile;
            Debug.Log("[Graphics Setup] DefaultVolumeProfile assigned to Global Volume");
        }
        else
        {
            Debug.LogWarning("[Graphics Setup] DefaultVolumeProfile not found at: " + profilePath);
        }
    }
    
    static void SetupFog()
    {
        // Enable fog
        RenderSettings.fog = true;
        
        // Horror fog settings
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.08f; // 5-15m visibility
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.05f, 1f); // Deep blue-black
        
        Debug.Log("[Graphics Setup] Fog configured: ExponentialSquared, density 0.08, dark blue-black");
    }
    
    static void SetupLighting()
    {
        // Ambient lighting - very dark, blue-tinted
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f, 1f);
        
        // Find and adjust Directional Light
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                // Very dim directional light for horror
                light.intensity = 0.3f;
                light.color = new Color(0.7f, 0.75f, 0.85f, 1f); // Cool blue-grey
                light.shadows = LightShadows.Soft;
                
                Debug.Log("[Graphics Setup] Directional Light adjusted: intensity 0.3, cool blue-grey");
            }
        }
        
        Debug.Log("[Graphics Setup] Ambient lighting set: dark blue (0.05, 0.05, 0.08)");
    }
}

/// <summary>
/// Automatically runs when Echoes scene is opened
/// </summary>
[InitializeOnLoad]
public class EchoesSceneAutoSetup
{
    static EchoesSceneAutoSetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    
    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        // Only auto-setup for Echoes scene
        if (scene.name == "Echoes")
        {
            Debug.Log("[Auto Setup] Echoes scene detected!");
            
            // Check if graphics are already configured
            Volume volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null || volume.profile == null)
            {
                Debug.Log("[Auto Setup] Graphics not configured, running auto-setup...");
                EchoesSceneConfigurator.SetupGraphicsForScene();
            }
            else
            {
                Debug.Log("[Auto Setup] Graphics already configured!");
            }
        }
    }
}
#endif
