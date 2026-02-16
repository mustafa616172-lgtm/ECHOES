using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ECHOES - Darkness Enforcer
/// Forces the scene to be completely dark at startup.
/// Disables all scene lights, sets ambient to black, kills reflections.
/// Only the player's flashlight should provide illumination.
/// Attach this to a GameObject in the scene (e.g. GameManager or an empty "DarknessEnforcer" object).
/// 
/// AUTOMATIC RUNNER ADDED:
/// This script now also attempts to run automatically on scene load.
/// </summary>
public class DarknessEnforcer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoRun()
    {
        // Check if already exists
        if (FindFirstObjectByType<DarknessEnforcer>() != null) return;

        GameObject obj = new GameObject("DarknessEnforcer_Auto");
        obj.AddComponent<DarknessEnforcer>();
        // DontDestroyOnLoad(obj); // Optional: let it handle per-scene darkness if different scenes need different logic
    }
    [Header("Settings")]
    [Tooltip("If true, disables ALL lights in the scene on Awake (except flashlight)")]
    [SerializeField] private bool disableAllLights = true;
    
    [Tooltip("Names to exclude from disabling (e.g. flashlight)")]
    [SerializeField] private string[] excludeLightNames = new string[] { "Flashlight", "FlashLight", "PlayerFlashlight", "Spot Light" };

    void Awake()
    {
        EnforceCompleteDarkness();
    }

    void Start()
    {
        // Double-enforce in Start in case other scripts set lighting in Awake
        EnforceCompleteDarkness();
    }

    public void EnforceCompleteDarkness()
    {
        // 1. Kill all ambient lighting
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        
        // 2. Kill reflections
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        
        // 3. Kill skybox contribution
        RenderSettings.ambientSkyColor = Color.black;
        RenderSettings.ambientEquatorColor = Color.black;
        RenderSettings.ambientGroundColor = Color.black;
        
        // 4. Disable all scene lights (except flashlight)
        if (disableAllLights)
        {
            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int disabledCount = 0;
            foreach (Light light in allLights)
            {
                if (light == null) continue;
                
                // Check if this light should be excluded (flashlight etc.)
                bool shouldExclude = false;
                string lightName = light.gameObject.name;
                
                foreach (string excludeName in excludeLightNames)
                {
                    if (lightName.Contains(excludeName))
                    {
                        shouldExclude = true;
                        break;
                    }
                }

                // Also exclude lights that are children of the player
                Transform parent = light.transform.parent;
                while (parent != null)
                {
                    if (parent.CompareTag("Player"))
                    {
                        shouldExclude = true;
                        break;
                    }
                    parent = parent.parent;
                }
                
                if (!shouldExclude)
                {
                    light.enabled = false;
                    disabledCount++;
                }
            }
            Debug.Log("[DarknessEnforcer] Disabled " + disabledCount + " scene lights.");
        }
        
        Debug.Log("[DarknessEnforcer] Complete darkness enforced - all ambient light removed.");
    }
}
