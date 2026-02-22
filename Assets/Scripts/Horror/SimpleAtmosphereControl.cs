using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echoes.Horror
{
    [ExecuteInEditMode]
    public class SimpleAtmosphereControl : MonoBehaviour
    {
        [Header("Manual Darkness Control")]
        [Range(-5f, 5f)]
        [Tooltip("Lower value = Darker. Higher value = Brighter.")]
        public float exposureOffset = -1.5f;

        [Header("Fog Settings")]
        [Range(0f, 0.2f)]
        public float fogDensity = 0.05f;
        public Color fogColor = new Color(0.01f, 0.01f, 0.03f);

        [Header("Moon Light")]
        [Range(0f, 2f)]
        public float moonIntensity = 0.2f;
        public Color moonColor = new Color(0.6f, 0.7f, 0.9f);

        [Header("References (Auto-Found)")]
        public Volume globalVolume;
        public Light moonLight;

        private ColorAdjustments colorAdjustments;

        void OnEnable()
        {
            FindReferences();
        }

        void Update()
        {
            if (!Application.isPlaying && globalVolume == null) FindReferences();
            
            ApplySettings();
        }

        void FindReferences()
        {
            // Find Global Volume
            if (globalVolume == null)
            {
                var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
                foreach (var vol in volumes)
                {
                    if (vol.isGlobal)
                    {
                        globalVolume = vol;
                        break;
                    }
                }
            }

            // Find Moon Light
            if (moonLight == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        moonLight = l;
                        break;
                    }
                }
            }
        }

        void ApplySettings()
        {
            // 1. Exposure (Darkness)
            if (globalVolume != null && globalVolume.profile != null)
            {
                if (globalVolume.profile.TryGet(out colorAdjustments))
                {
                    colorAdjustments.postExposure.value = exposureOffset;
                }
            }

            // 2. Fog
            RenderSettings.fog = true;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fogColor;

            // 3. Moon Light
            if (moonLight != null)
            {
                moonLight.intensity = moonIntensity;
                moonLight.color = moonColor;
            }
        }
    }
}
