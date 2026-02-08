using UnityEngine;

namespace ECHOES.Horror
{
    public class FogController : MonoBehaviour
    {
        [Header("Fog Settings")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = new Color(0.02f, 0.02f, 0.05f, 1f);
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        
        [Header("Linear Fog")]
        [SerializeField] private float fogStartDistance = 5f;
        [SerializeField] private float fogEndDistance = 15f;
        
        [Header("Exponential Fog")]
        [SerializeField] private float fogDensity = 0.08f;
        
        [Header("Dynamic Fog")]
        [SerializeField] private bool enableDynamicFog = true;
        [SerializeField] private float fogPulseSpeed = 0.5f;
        [SerializeField] private float fogPulseAmount = 0.02f;
        [SerializeField] private bool randomFogSurges = true;
        [SerializeField] private float surgeProbability = 0.01f;
        [SerializeField] private float surgeIntensity = 0.15f;
        [SerializeField] private float surgeDuration = 3f;
        
        [Header("Color Variation")]
        [SerializeField] private bool enableColorVariation = true;
        [SerializeField] private Color secondaryFogColor = new Color(0.03f, 0.02f, 0.04f, 1f);
        [SerializeField] private float colorTransitionSpeed = 0.2f;
        
        private float baseFogDensity;
        private float currentFogDensity;
        private float targetFogDensity;
        private bool inSurge = false;
        private float surgeTimer = 0f;
        private Color currentFogColor;
        private Color targetFogColor;
        
        private void Start()
        {
            baseFogDensity = fogDensity;
            currentFogDensity = fogDensity;
            targetFogDensity = fogDensity;
            currentFogColor = fogColor;
            targetFogColor = fogColor;
            
            ApplyFogSettings();
        }
        
        private void Update()
        {
            if (!enableFog)
            {
                RenderSettings.fog = false;
                return;
            }
            
            RenderSettings.fog = true;
            
            if (enableDynamicFog)
            {
                UpdateDynamicFog();
            }
            
            if (enableColorVariation)
            {
                UpdateFogColor();
            }
            
            ApplyFogSettings();
        }
        
        private void UpdateDynamicFog()
        {
            // Handle surge
            if (inSurge)
            {
                surgeTimer -= Time.deltaTime;
                if (surgeTimer <= 0f)
                {
                    inSurge = false;
                    targetFogDensity = baseFogDensity;
                }
            }
            else
            {
                // Check for random surge
                if (randomFogSurges && Random.value < surgeProbability * Time.deltaTime)
                {
                    TriggerFogSurge();
                }
                else
                {
                    // Gentle pulsing
                    float pulse = Mathf.Sin(Time.time * fogPulseSpeed) * fogPulseAmount;
                    targetFogDensity = baseFogDensity + pulse;
                }
            }
            
            // Smooth fog density transition
            currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, Time.deltaTime * 2f);
            fogDensity = currentFogDensity;
        }
        
        private void UpdateFogColor()
        {
            // Slowly transition between fog colors
            float colorLerp = (Mathf.Sin(Time.time * colorTransitionSpeed) + 1f) * 0.5f;
            targetFogColor = Color.Lerp(fogColor, secondaryFogColor, colorLerp);
            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, Time.deltaTime);
        }
        
        private void ApplyFogSettings()
        {
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = currentFogColor;
            
            switch (fogMode)
            {
                case FogMode.Linear:
                    RenderSettings.fogStartDistance = fogStartDistance;
                    RenderSettings.fogEndDistance = fogEndDistance;
                    break;
                case FogMode.Exponential:
                case FogMode.ExponentialSquared:
                    RenderSettings.fogDensity = fogDensity;
                    break;
            }
        }
        
        private void TriggerFogSurge()
        {
            inSurge = true;
            surgeTimer = surgeDuration;
            targetFogDensity = baseFogDensity + surgeIntensity;
        }
        
        public void SetFogDensity(float density)
        {
            baseFogDensity = density;
            fogDensity = density;
            ApplyFogSettings();
        }
        
        public void SetFogColor(Color color)
        {
            fogColor = color;
            currentFogColor = color;
            targetFogColor = color;
            ApplyFogSettings();
        }
        
        public void SetFogDistance(float start, float end)
        {
            fogStartDistance = start;
            fogEndDistance = end;
            ApplyFogSettings();
        }
        
        public void TriggerManualSurge(float intensity, float duration)
        {
            inSurge = true;
            surgeTimer = duration;
            targetFogDensity = baseFogDensity + intensity;
        }
        
        public void SetFogEnabled(bool enabled)
        {
            enableFog = enabled;
            RenderSettings.fog = enabled;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyFogSettings();
            }
        }
    }
}
