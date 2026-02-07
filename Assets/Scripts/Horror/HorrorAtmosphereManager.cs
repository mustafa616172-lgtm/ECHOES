using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ECHOES.Horror
{
    public class HorrorAtmosphereManager : MonoBehaviour
    {
        [Header("Volume Profile")]
        [SerializeField] private Volume globalVolume;
        
        [Header("Atmosphere Zones")]
        [SerializeField] private AtmosphereZone[] atmosphereZones;
        
        [Header("Dynamic Effects")]
        [SerializeField] private bool enableDynamicEffects = true;
        [SerializeField] private float effectTransitionSpeed = 1.0f;
        
        [Header("Fear Level System")]
        [SerializeField] private bool useFearLevel = true;
        [SerializeField] [Range(0, 1)] private float currentFearLevel = 0.5f;
        [SerializeField] private float maxVignetteIntensity = 0.6f;
        [SerializeField] private float maxChromaticAberration = 0.5f;
        
        private VolumeProfile profile;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private ColorAdjustments colorAdjustments;
        private FilmGrain filmGrain;
        
        private AtmosphereZone currentZone;
        private float targetExposure;
        private float targetSaturation;
        private float currentExposure;
        private float currentSaturation;
        
        [System.Serializable]
        public class AtmosphereZone
        {
            public string zoneName;
            public Transform zoneTransform;
            public float zoneRadius = 10f;
            public float exposureOffset = 0f;
            public float saturationOffset = 0f;
            public Color colorTint = Color.white;
        }
        
        private void Start()
        {
            if (globalVolume == null)
            {
                globalVolume = FindObjectOfType<Volume>();
            }
            
            if (globalVolume != null && globalVolume.profile != null)
            {
                profile = globalVolume.profile;
                
                profile.TryGet(out vignette);
                profile.TryGet(out chromaticAberration);
                profile.TryGet(out colorAdjustments);
                profile.TryGet(out filmGrain);
                
                if (colorAdjustments != null)
                {
                    currentExposure = colorAdjustments.postExposure.value;
                    currentSaturation = colorAdjustments.saturation.value;
                    targetExposure = currentExposure;
                    targetSaturation = currentSaturation;
                }
            }
        }
        
        private void Update()
        {
            if (!enableDynamicEffects || profile == null)
                return;
            
            // Check for atmosphere zones
            UpdateCurrentZone();
            
            // Apply fear level effects
            if (useFearLevel)
            {
                ApplyFearLevelEffects();
            }
            
            // Smooth transition to target values
            if (colorAdjustments != null)
            {
                currentExposure = Mathf.Lerp(currentExposure, targetExposure, Time.deltaTime * effectTransitionSpeed);
                currentSaturation = Mathf.Lerp(currentSaturation, targetSaturation, Time.deltaTime * effectTransitionSpeed);
                
                colorAdjustments.postExposure.value = currentExposure;
                colorAdjustments.saturation.value = currentSaturation;
            }
        }
        
        private void UpdateCurrentZone()
        {
            if (atmosphereZones == null || atmosphereZones.Length == 0)
                return;
            
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            AtmosphereZone closestZone = null;
            float closestDistance = float.MaxValue;
            
            foreach (var zone in atmosphereZones)
            {
                if (zone.zoneTransform == null)
                    continue;
                
                float distance = Vector3.Distance(cameraPos, zone.zoneTransform.position);
                if (distance < zone.zoneRadius && distance < closestDistance)
                {
                    closestZone = zone;
                    closestDistance = distance;
                }
            }
            
            if (closestZone != currentZone)
            {
                currentZone = closestZone;
                ApplyZoneEffects(closestZone);
            }
        }
        
        private void ApplyZoneEffects(AtmosphereZone zone)
        {
            if (zone == null)
            {
                // Reset to default
                targetExposure = -1.5f;
                targetSaturation = -30f;
                return;
            }
            
            targetExposure = -1.5f + zone.exposureOffset;
            targetSaturation = -30f + zone.saturationOffset;
            
            if (colorAdjustments != null)
            {
                colorAdjustments.colorFilter.value = zone.colorTint;
            }
        }
        
        private void ApplyFearLevelEffects()
        {
            if (vignette != null)
            {
                float vignetteIntensity = Mathf.Lerp(0.45f, maxVignetteIntensity, currentFearLevel);
                vignette.intensity.value = vignetteIntensity;
            }
            
            if (chromaticAberration != null)
            {
                float chromaticIntensity = Mathf.Lerp(0.3f, maxChromaticAberration, currentFearLevel);
                chromaticAberration.intensity.value = chromaticIntensity;
            }
            
            if (filmGrain != null)
            {
                float grainIntensity = Mathf.Lerp(0.35f, 0.55f, currentFearLevel);
                filmGrain.intensity.value = grainIntensity;
            }
        }
        
        public void SetFearLevel(float level)
        {
            currentFearLevel = Mathf.Clamp01(level);
        }
        
        public void IncreaseFearLevel(float amount)
        {
            currentFearLevel = Mathf.Clamp01(currentFearLevel + amount);
        }
        
        public void DecreaseFearLevel(float amount)
        {
            currentFearLevel = Mathf.Clamp01(currentFearLevel - amount);
        }
        
        public void AddAtmosphereZone(string name, Transform transform, float radius, float exposureOffset = 0f, float saturationOffset = 0f)
        {
            AtmosphereZone newZone = new AtmosphereZone
            {
                zoneName = name,
                zoneTransform = transform,
                zoneRadius = radius,
                exposureOffset = exposureOffset,
                saturationOffset = saturationOffset
            };
            
            System.Array.Resize(ref atmosphereZones, atmosphereZones.Length + 1);
            atmosphereZones[atmosphereZones.Length - 1] = newZone;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (atmosphereZones == null)
                return;
            
            Gizmos.color = Color.cyan;
            foreach (var zone in atmosphereZones)
            {
                if (zone.zoneTransform != null)
                {
                    Gizmos.DrawWireSphere(zone.zoneTransform.position, zone.zoneRadius);
                }
            }
        }
    }
}
