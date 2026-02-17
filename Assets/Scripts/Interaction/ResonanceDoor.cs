using UnityEngine;
using System.Collections;

/// <summary>
/// ECHOES - Resonance Door/Lock Mechanic
/// A lock that requires a specific sound frequency to open.
/// Generates its own procedural audio (Sine Wave + Static).
/// </summary>
public class ResonanceDoor : MonoBehaviour
{
    [Header("Resonance Settings")]
    [Tooltip("The frequency required to open this lock (Hz)")]
    public float requiredFrequency = 440.0f;
    
    [Tooltip("Acceptable error range (Hz)")]
    public float tolerance = 15.0f;
    
    [Tooltip("Distance at which the player can interact/hear the mechanics")]
    public float interactionRange = 6.0f;
    
    [Tooltip("Time required to hold the correct frequency to unlock")]
    public float unlockTime = 1.0f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    
    private SoundRecorderDevice playerDevice;
    private AudioSource sineSource;
    private AudioSource staticSource;
    
    private float holdTimer = 0f;
    private bool isUnlocked = false;
    private Material doorMaterial;
    private Vector3 originalLocalPosition;
    
    void Start()
    {
        playerDevice = FindObjectOfType<SoundRecorderDevice>();
        originalLocalPosition = transform.localPosition;
        
        SetupAudioSources();
        
        // Optional: Cache material for visual feedback on the door itself (e.g. vibration)
        Renderer r = GetComponent<Renderer>();
        if (r != null) doorMaterial = r.material;
    }

    void SetupAudioSources()
    {
        // 1. Sine Wave Source
        GameObject sineObj = new GameObject("SineSource");
        sineObj.transform.SetParent(transform);
        sineObj.transform.localPosition = Vector3.zero;
        sineSource = sineObj.AddComponent<AudioSource>();
        sineSource.clip = GenerateSineWave(requiredFrequency);
        sineSource.loop = true;
        sineSource.spatialBlend = 1.0f; // 3D Sound
        sineSource.minDistance = 1f;
        sineSource.maxDistance = interactionRange;
        sineSource.rolloffMode = AudioRolloffMode.Linear;
        sineSource.volume = 0f;
        sineSource.Play();

        // 2. Static Noise Source
        GameObject staticObj = new GameObject("StaticSource");
        staticObj.transform.SetParent(transform);
        staticObj.transform.localPosition = Vector3.zero;
        staticSource = staticObj.AddComponent<AudioSource>();
        staticSource.clip = GenerateStaticNoise();
        staticSource.loop = true;
        staticSource.spatialBlend = 1.0f;
        staticSource.minDistance = 1f;
        staticSource.maxDistance = interactionRange;
        staticSource.rolloffMode = AudioRolloffMode.Linear;
        staticSource.volume = 0f;
        staticSource.Play();
    }

    void Update()
    {
        if (isUnlocked || playerDevice == null || !playerDevice.HasDevice)
        {
            // Silence if done or no device
            if (sineSource.volume > 0) sineSource.volume = Mathf.MoveTowards(sineSource.volume, 0f, Time.deltaTime);
            if (staticSource.volume > 0) staticSource.volume = Mathf.MoveTowards(staticSource.volume, 0f, Time.deltaTime);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerDevice.transform.position);
        
        if (distance <= interactionRange)
        {
            HandleResonanceLogic(distance);
        }
        else
        {
            // Fade out if out of range
            sineSource.volume = Mathf.MoveTowards(sineSource.volume, 0f, Time.deltaTime);
            staticSource.volume = Mathf.MoveTowards(staticSource.volume, 0f, Time.deltaTime);
            
            // Notify device we are out of range
            if (playerDevice != null)
            {
                playerDevice.ExitResonanceArea();
            }
        }
    }

    void HandleResonanceLogic(float distance)
    {
        float currentFreq = playerDevice.CurrentFrequency;
        float freqDiff = Mathf.Abs(currentFreq - requiredFrequency);
        
        // Calculate Match Factor (0.0 to 1.0)
        // 0.0 = Far from frequency
        // 1.0 = Exact frequency
        // usage range is roughly +/- 200 Hz for gradual feedback
        float perceptionRange = 200f;
        float matchFactor = 1.0f - Mathf.Clamp01(freqDiff / perceptionRange);
        
        // --- Audio Feedback ---
        // Sine wave volume increases with match (Harmonic resonance)
        float targetSineVol = matchFactor * masterVolume;
        
        // Static noise increases with MISMATCH (Disharmony)
        // But only if we are somewhat close to the frequency
        // If we are very far, maybe silence? Or low static?
        // Prompt says: "Cızırtı azalmalı, net bir sinüs..." -> Static decreases as match increases.
        float targetStaticVol = (1.0f - matchFactor) * masterVolume * 0.8f; 
        
        sineSource.volume = Mathf.Lerp(sineSource.volume, targetSineVol, Time.deltaTime * 5f);
        staticSource.volume = Mathf.Lerp(staticSource.volume, targetStaticVol, Time.deltaTime * 5f);
        
        // Update Sine Pitch to match strict harmony? 
        // Or keep it fixed at required? 
        // If we change pitch, it sounds like tuning. Let's try to match sine pitch to current freq slightly?
        // No, let's keep it simple: The door hums at the REQUIRED frequency.
        // As you get closer, you hear it clearer.
        
        // --- Visual Feedback (UI + Device) ---
        // Pass the match factor and Target Frequency to the device
        playerDevice.EnterResonanceArea(requiredFrequency);
        playerDevice.SetResonanceState(matchFactor, true);
        
        // --- Visual Feedback (Door/Self) ---
        if (doorMaterial != null)
        {
            // Shake or vibrate based on match
            if (matchFactor > 0.8f)
            {
                float shake = (matchFactor - 0.8f) * 0.05f;
                transform.localPosition = originalLocalPosition + Random.insideUnitSphere * shake;
            }
            else
            {
                transform.localPosition = originalLocalPosition;
            }
        }

        // --- Unlock Logic ---
        if (freqDiff <= tolerance)
        {
            holdTimer += Time.deltaTime;
            
            // Optional: Door vibrates more intensely?
            
            if (holdTimer >= unlockTime)
            {
                Unlock();
            }
        }
        else
        {
            holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime);
        }
    }

    void Unlock()
    {
        isUnlocked = true;
        Debug.Log("[ResonanceDoor] Unlocked via Frequency Resonance!");
        
        // 1. Silence Audio
        sineSource.Stop();
        staticSource.Stop();
        
        // Close UI
        if (playerDevice != null) playerDevice.ExitResonanceArea();
        
        // 2. Play Unlock Sound/Effect (if any)
        
        // 3. Destroy or Open
        StartCoroutine(DestructionSequence());
    }

    IEnumerator DestructionSequence()
    {
        // Visual feedback: Shrink and disappear for now
        float timer = 0f;
        Vector3 startScale = transform.localScale;
        
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / 0.5f);
            yield return null;
        }
        
        Destroy(gameObject);
    }

    // --- Procedural Audio Generation ---

    AudioClip GenerateSineWave(float frequency)
    {
        int sampleRate = 44100;
        int lengthSamples = sampleRate * 2; // 2 seconds loop
        float[] samples = new float[lengthSamples];
        
        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t);
        }
        
        AudioClip clip = AudioClip.Create("ResonanceSine", lengthSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateStaticNoise()
    {
        int sampleRate = 44100;
        int lengthSamples = sampleRate * 2;
        float[] samples = new float[lengthSamples];
        System.Random rng = new System.Random();
        
        for (int i = 0; i < lengthSamples; i++)
        {
            samples[i] = (float)rng.NextDouble() * 2f - 1f; // -1 to 1
        }
        
        AudioClip clip = AudioClip.Create("ResonanceStatic", lengthSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
