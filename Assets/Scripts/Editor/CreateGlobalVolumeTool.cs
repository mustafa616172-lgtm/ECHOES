using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace Echoes.Editor
{
    public class CreateGlobalVolumeTool : EditorWindow
    {
        [MenuItem("Tools/ECHOES/Setup Night Atmosphere")]
        public static void SetupNightAtmosphere()
        {
            Debug.Log("🌑 Starting Night Atmosphere Setup...");

            // 1. Create/Ensure Profile Exists
            VolumeProfile profile = GetOrCreateNightProfile();
            if (profile == null) return;

            // 2. Setup Scene Objects
            SetupSceneVolume(profile);
            SetupNightLighting();

            Debug.Log("🌑 Night Atmosphere Setup Complete! Please check your Game view.");
        }

        private static VolumeProfile GetOrCreateNightProfile()
        {
            // 1. Try to find the ORIGINAL asset from "Realistic Volume Profiles"
            string originalAssetPath = "Assets/Realistic Volume Profiles/URP/Volumes/Night URP Volume.asset";
            VolumeProfile originalProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(originalAssetPath);

            if (originalProfile != null)
            {
                Debug.Log("[ECHOES] Found original 'Night URP Volume' asset! Using it directly.");
                // We create a copy in our settings to avoid modifying the original asset directly (safer)
                // OR we can just return it if we want to use it as is.
                // Let's create a copy in our Settings folder to be safe and customizable.
                
                string myPath = "Assets/Settings/EchoesNightVolume.asset";
                
                // Check if we alreadyhave our copy
                VolumeProfile myProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(myPath);
                if (myProfile == null)
                {
                    // Copy the alignment
                    AssetDatabase.CopyAsset(originalAssetPath, myPath);
                    myProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(myPath);
                    Debug.Log($"[ECHOES] Created local copy of Night Profile at {myPath}");
                }
                
                return myProfile; // Return our copy
            }
            
            // Fallback: If asset is missing, create our procedural one (as backup)
            Debug.LogWarning("[ECHOES] Could not find original 'Night URP Volume'. Creating procedural profile...");
            
            string path = "Assets/Settings/EchoesNightVolume.asset";
            // ... (rest of procedural creation logic removed for brevity, or kept as fallback)
            // For now, let's keep the procedural logic as a fallback inside else block or just return new profile
            
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            
            // Add components manually as backup
            if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.value = TonemappingMode.ACES;
            tonemapping.active = true;
            
             // 4. Color Adjustments (The key to "Night" look)
            if (!profile.TryGet(out ColorAdjustments colorAdj)) colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.value = -1.5f; // Very Dark
            colorAdj.contrast.value = 20f; // High contrast
            colorAdj.colorFilter.value = new Color(0.7f, 0.75f, 0.9f); // Blue-ish filter
            colorAdj.saturation.value = -30f; // Desaturated
            colorAdj.active = true;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void SetupSceneVolume(VolumeProfile profile)
        {
            // Find existing Global Volume
            Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            Volume globalVolume = null;

            foreach (var vol in volumes)
            {
                if (vol.isGlobal)
                {
                    globalVolume = vol;
                    break;
                }
            }

            if (globalVolume == null)
            {
                GameObject volObj = new GameObject("Global Volume (Night)");
                globalVolume = volObj.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                Debug.Log("[ECHOES] Created new Global Volume in scene");
            }
            else
            {
                globalVolume.gameObject.name = "Global Volume (Night)";
                Debug.Log($"[ECHOES] Found existing Global Volume: {globalVolume.name}");
            }

            globalVolume.profile = profile;
            globalVolume.weight = 1f;
            globalVolume.priority = 10; // High priority to override defaults
            EditorUtility.SetDirty(globalVolume);
        }

        private static void SetupNightLighting()
        {
            // 1. Directional Light (Moon)
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light sun = null;

            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    if (sun.gameObject.name.Contains("Sun")) sun.gameObject.name = "Moon Light";
                    break; // Take the first directional light
                }
            }

            if (sun == null)
            {
                GameObject sunObj = new GameObject("Moon Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            // Configure Moon
            sun.color = new Color(0.6f, 0.7f, 0.9f); // Blue moonlight
            sun.intensity = 0.2f; // Dim
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f); 

            // 2. Lighting Settings (Environment)
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.05f); // Very dark blue ambient
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.03f); // Pitch dark fog
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.05f; // Thick fog
            
            // 3. Disable any "DayNight" scripts if found on light source
            var dayNightScript = sun.GetComponent("DayNightCycle"); // Placeholder name
            if (dayNightScript != null) 
            {
                Object.DestroyImmediate(dayNightScript);
                Debug.LogWarning("[ECHOES] Removed DayNightCycle script from Light source");
            }

            Debug.Log("[ECHOES] Lighting and RenderSettings configured for Night");
        }
    }
}
