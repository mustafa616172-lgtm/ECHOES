using UnityEngine;

namespace ECHOES.Horror
{
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Flicker Settings")]
        [SerializeField] private bool enableFlicker = true;
        [SerializeField] private float minIntensity = 0.0f;
        [SerializeField] private float maxIntensity = 1.0f;
        [SerializeField] private float flickerSpeed = 0.1f;
        
        [Header("Random Flicker")]
        [SerializeField] private bool useRandomFlicker = true;
        [SerializeField] private float randomFlickerChance = 0.05f;
        [SerializeField] private float randomFlickerDuration = 0.2f;
        
        [Header("Complete Shutoff")]
        [SerializeField] private bool allowCompleteShutoff = true;
        [SerializeField] private float shutoffChance = 0.01f;
        [SerializeField] private float minShutoffDuration = 0.5f;
        [SerializeField] private float maxShutoffDuration = 2.0f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip flickerSound;
        [SerializeField] private AudioClip shutoffSound;
        [SerializeField] private float audioVolume = 0.5f;
        
        private Light lightComponent;
        private float originalIntensity;
        private float targetIntensity;
        private float currentIntensity;
        private bool isShutoff = false;
        private float shutoffTimer = 0f;
        private float randomFlickerTimer = 0f;
        
        private void Awake()
        {
            lightComponent = GetComponent<Light>();
            originalIntensity = lightComponent.intensity;
            maxIntensity = originalIntensity;
            currentIntensity = originalIntensity;
            targetIntensity = originalIntensity;
        }
        
        private void Update()
        {
            if (!enableFlicker)
            {
                lightComponent.intensity = originalIntensity;
                return;
            }
            
            // Handle complete shutoff
            if (isShutoff)
            {
                shutoffTimer -= Time.deltaTime;
                if (shutoffTimer <= 0f)
                {
                    isShutoff = false;
                    targetIntensity = maxIntensity;
                }
                else
                {
                    lightComponent.intensity = 0f;
                    return;
                }
            }
            
            // Check for random shutoff
            if (allowCompleteShutoff && !isShutoff && Random.value < shutoffChance * Time.deltaTime)
            {
                TriggerShutoff();
                return;
            }
            
            // Random flicker
            if (useRandomFlicker)
            {
                randomFlickerTimer -= Time.deltaTime;
                
                if (randomFlickerTimer <= 0f)
                {
                    if (Random.value < randomFlickerChance)
                    {
                        targetIntensity = Random.Range(minIntensity, maxIntensity);
                        randomFlickerTimer = randomFlickerDuration;
                        PlayFlickerSound();
                    }
                    else
                    {
                        targetIntensity = maxIntensity;
                    }
                }
            }
            else
            {
                // Smooth continuous flicker
                targetIntensity = maxIntensity + Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * (maxIntensity - minIntensity) - (maxIntensity - minIntensity) * 0.5f;
            }
            
            // Smoothly lerp to target intensity
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 10f);
            lightComponent.intensity = currentIntensity;
        }
        
        private void TriggerShutoff()
        {
            isShutoff = true;
            shutoffTimer = Random.Range(minShutoffDuration, maxShutoffDuration);
            
            if (audioSource != null && shutoffSound != null)
            {
                audioSource.PlayOneShot(shutoffSound, audioVolume);
            }
        }
        
        private void PlayFlickerSound()
        {
            if (audioSource != null && flickerSound != null && Random.value < 0.3f)
            {
                audioSource.PlayOneShot(flickerSound, audioVolume * 0.5f);
            }
        }
        
        public void SetFlickerEnabled(bool enabled)
        {
            enableFlicker = enabled;
        }
        
        public void SetIntensityRange(float min, float max)
        {
            minIntensity = min;
            maxIntensity = max;
        }
    }
}
