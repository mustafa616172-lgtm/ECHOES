using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ambient horror sound system for ECHOES.
/// Plays random creepy sounds (whispers, creaks, distant footsteps,
/// breathing, scratching) at randomized intervals.
/// Includes procedural sound generation for all sounds - no external assets needed.
/// </summary>
public class AmbientSoundSystem : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private float minInterval = 8f;
    [SerializeField] private float maxInterval = 25f;
    [SerializeField] private float baseVolume = 0.15f;
    
    [Header("Sound Volumes")]
    [SerializeField] [Range(0f, 1f)] private float whisperVolume = 0.12f;
    [SerializeField] [Range(0f, 1f)] private float creakVolume = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float breathVolume = 0.08f;
    [SerializeField] [Range(0f, 1f)] private float scratchVolume = 0.15f;
    [SerializeField] [Range(0f, 1f)] private float metalVolume = 0.18f;
    [SerializeField] [Range(0f, 1f)] private float lowDroneVolume = 0.1f;
    
    [Header("Horror Intensity")]
    [SerializeField] private bool scaleWithDanger = true;
    [SerializeField] private float dangerVolumeMultiplier = 2f;
    [SerializeField] private float dangerIntervalDivider = 2f;
    
    [Header("Spatial Settings")]
    [SerializeField] private float minSoundDistance = 5f;
    [SerializeField] private float maxSoundDistance = 20f;
    [SerializeField] private bool useSpatialAudio = true;
    
    [Header("Custom Sounds (Optional)")]
    [SerializeField] private AudioClip[] customWhispers;
    [SerializeField] private AudioClip[] customCreaks;
    [SerializeField] private AudioClip[] customFootsteps;
    
    // Internal
    private float nextSoundTime;
    private AudioSource mainSource;
    private AudioSource spatialSource;
    private AwarenessIndicator dangerIndicator;
    
    // Procedural sound clips
    private AudioClip[] whisperClips;
    private AudioClip[] creakClips;
    private AudioClip[] footstepClips;
    private AudioClip[] breathClips;
    private AudioClip[] scratchClips;
    private AudioClip[] metalClips;
    private AudioClip lowDroneClip;
    
    private enum AmbientSoundType
    {
        Whisper,
        Creak,
        DistantFootstep,
        Breathing,
        Scratching,
        MetalClang,
        LowDrone
    }
    
    void Start()
    {
        // Main audio source (2D, for whispers/breathing)
        mainSource = gameObject.AddComponent<AudioSource>();
        mainSource.spatialBlend = 0f;
        mainSource.playOnAwake = false;
        mainSource.volume = baseVolume;
        
        // Spatial audio source (3D, for distant sounds)
        if (useSpatialAudio)
        {
            GameObject spatialObj = new GameObject("AmbientSpatialSource");
            spatialObj.transform.SetParent(transform);
            spatialSource = spatialObj.AddComponent<AudioSource>();
            spatialSource.spatialBlend = 1f;
            spatialSource.rolloffMode = AudioRolloffMode.Linear;
            spatialSource.minDistance = 1f;
            spatialSource.maxDistance = maxSoundDistance;
            spatialSource.playOnAwake = false;
        }
        
        GenerateProceduralSounds();
        ScheduleNextSound();
        
        Debug.Log("[AmbientSound] Horror ambient system initialized with procedural audio");
    }
    
    void Update()
    {
        // Find danger indicator for intensity scaling
        if (dangerIndicator == null)
            dangerIndicator = FindObjectOfType<AwarenessIndicator>();
        
        if (Time.time >= nextSoundTime)
        {
            PlayRandomAmbientSound();
            ScheduleNextSound();
        }
    }
    
    void ScheduleNextSound()
    {
        float interval = Random.Range(minInterval, maxInterval);
        
        // More frequent sounds when danger is high
        if (scaleWithDanger && dangerIndicator != null)
        {
            float danger = dangerIndicator.DangerLevel / 100f;
            interval /= Mathf.Lerp(1f, dangerIntervalDivider, danger);
        }
        
        nextSoundTime = Time.time + interval;
    }
    
    void PlayRandomAmbientSound()
    {
        // Weighted random selection
        AmbientSoundType type = GetWeightedRandomType();
        
        float volumeMultiplier = 1f;
        if (scaleWithDanger && dangerIndicator != null)
        {
            float danger = dangerIndicator.DangerLevel / 100f;
            volumeMultiplier = Mathf.Lerp(1f, dangerVolumeMultiplier, danger);
        }
        
        AudioClip clip = null;
        float volume = baseVolume;
        bool spatial = false;
        
        switch (type)
        {
            case AmbientSoundType.Whisper:
                clip = GetRandomClip(customWhispers, whisperClips);
                volume = whisperVolume;
                spatial = false; // Whispers feel closer in 2D
                break;
                
            case AmbientSoundType.Creak:
                clip = GetRandomClip(customCreaks, creakClips);
                volume = creakVolume;
                spatial = true;
                break;
                
            case AmbientSoundType.DistantFootstep:
                clip = GetRandomClip(customFootsteps, footstepClips);
                volume = footstepVolume;
                spatial = true;
                break;
                
            case AmbientSoundType.Breathing:
                clip = GetRandomClip(null, breathClips);
                volume = breathVolume;
                spatial = false;
                break;
                
            case AmbientSoundType.Scratching:
                clip = GetRandomClip(null, scratchClips);
                volume = scratchVolume;
                spatial = true;
                break;
                
            case AmbientSoundType.MetalClang:
                clip = GetRandomClip(null, metalClips);
                volume = metalVolume;
                spatial = true;
                break;
                
            case AmbientSoundType.LowDrone:
                clip = lowDroneClip;
                volume = lowDroneVolume;
                spatial = false;
                break;
        }
        
        if (clip == null) return;
        
        volume *= volumeMultiplier;
        
        if (spatial && spatialSource != null)
        {
            // Position sound randomly around the player
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Clamp(randomDir.y, -0.3f, 0.3f); // Keep mostly horizontal
            float dist = Random.Range(minSoundDistance, maxSoundDistance);
            spatialSource.transform.position = transform.position + randomDir * dist;
            spatialSource.volume = volume;
            spatialSource.pitch = Random.Range(0.85f, 1.15f);
            spatialSource.clip = clip;
            spatialSource.Play();
        }
        else
        {
            mainSource.volume = volume;
            mainSource.pitch = Random.Range(0.9f, 1.1f);
            // Pan randomly for 2D sounds to feel more directional
            mainSource.panStereo = Random.Range(-0.6f, 0.6f);
            mainSource.PlayOneShot(clip);
        }
    }
    
    AmbientSoundType GetWeightedRandomType()
    {
        // Weights: higher = more common
        float[] weights = {
            25f, // Whisper
            20f, // Creak
            15f, // Distant Footstep
            12f, // Breathing
            12f, // Scratching
            8f,  // Metal Clang
            8f   // Low Drone
        };
        
        float total = 0f;
        foreach (float w in weights) total += w;
        
        float random = Random.Range(0f, total);
        float cumulative = 0f;
        
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (random <= cumulative)
                return (AmbientSoundType)i;
        }
        
        return AmbientSoundType.Whisper;
    }
    
    AudioClip GetRandomClip(AudioClip[] custom, AudioClip[] procedural)
    {
        if (custom != null && custom.Length > 0)
            return custom[Random.Range(0, custom.Length)];
        if (procedural != null && procedural.Length > 0)
            return procedural[Random.Range(0, procedural.Length)];
        return null;
    }
    
    // ============================================
    // PROCEDURAL SOUND GENERATION
    // ============================================
    
    void GenerateProceduralSounds()
    {
        whisperClips = new AudioClip[3];
        for (int i = 0; i < 3; i++)
            whisperClips[i] = GenerateWhisper(i);
        
        creakClips = new AudioClip[3];
        for (int i = 0; i < 3; i++)
            creakClips[i] = GenerateCreak(i);
        
        footstepClips = new AudioClip[2];
        for (int i = 0; i < 2; i++)
            footstepClips[i] = GenerateFootstep(i);
        
        breathClips = new AudioClip[2];
        for (int i = 0; i < 2; i++)
            breathClips[i] = GenerateBreathing(i);
        
        scratchClips = new AudioClip[2];
        for (int i = 0; i < 2; i++)
            scratchClips[i] = GenerateScratching(i);
        
        metalClips = new AudioClip[2];
        for (int i = 0; i < 2; i++)
            metalClips[i] = GenerateMetalClang(i);
        
        lowDroneClip = GenerateLowDrone();
        
        Debug.Log("[AmbientSound] Generated 14 procedural horror sounds");
    }
    
    AudioClip GenerateWhisper(int variant)
    {
        int sampleRate = 44100;
        float duration = Random.Range(0.8f, 2.0f);
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        float freqBase = 200f + variant * 80f;
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(t / duration * Mathf.PI); // Fade in/out
            
            // Breathy noise with subtle tonal component
            float noise = (Random.Range(-1f, 1f)) * 0.3f;
            float tone = Mathf.Sin(2f * Mathf.PI * freqBase * t) * 0.05f;
            float formant = Mathf.Sin(2f * Mathf.PI * (freqBase * 2.5f) * t) * 0.03f;
            
            // Modulate like speech rhythm
            float rhythm = Mathf.Sin(t * 6f) * 0.5f + 0.5f;
            
            data[i] = (noise + tone + formant) * envelope * rhythm * 0.4f;
        }
        
        AudioClip clip = AudioClip.Create($"Whisper_{variant}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateCreak(int variant)
    {
        int sampleRate = 44100;
        float duration = Random.Range(0.5f, 1.5f);
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        float baseFreq = 80f + variant * 40f;
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 2f); // Sharp decay
            
            // Creaking: frequency sweep with harmonics
            float freq = baseFreq + Mathf.Sin(t * 4f) * 30f;
            float v = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f;
            v += Mathf.Sin(2f * Mathf.PI * freq * 2.3f * t) * 0.2f;
            v += Mathf.Sin(2f * Mathf.PI * freq * 3.7f * t) * 0.1f;
            
            // Add some crackle
            if (Random.Range(0f, 1f) < 0.02f)
                v += Random.Range(-0.5f, 0.5f);
            
            data[i] = v * envelope * 0.5f;
        }
        
        AudioClip clip = AudioClip.Create($"Creak_{variant}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateFootstep(int variant)
    {
        int sampleRate = 44100;
        float duration = 0.3f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 15f); // Very quick decay
            
            // Thump + surface noise
            float thump = Mathf.Sin(2f * Mathf.PI * (60f + variant * 20f) * t) * 0.5f;
            float surface = Random.Range(-1f, 1f) * 0.3f * Mathf.Exp(-t * 8f);
            
            data[i] = (thump + surface) * envelope * 0.5f;
        }
        
        AudioClip clip = AudioClip.Create($"DistantStep_{variant}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateBreathing(int variant)
    {
        int sampleRate = 44100;
        float duration = Random.Range(2f, 3.5f);
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        float breathCycle = 1.5f + variant * 0.5f;
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            
            // Breathing envelope: inhale/exhale cycle
            float breathPhase = Mathf.Sin(t / breathCycle * Mathf.PI * 2f);
            float envelope = Mathf.Abs(breathPhase) * Mathf.Sin(t / duration * Mathf.PI);
            
            // Breathy noise
            float noise = Random.Range(-1f, 1f) * 0.3f;
            // Subtle tonal (throat resonance)
            float tone = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.02f;
            
            data[i] = (noise + tone) * envelope * 0.3f;
        }
        
        AudioClip clip = AudioClip.Create($"Breathing_{variant}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateScratching(int variant)
    {
        int sampleRate = 44100;
        float duration = Random.Range(0.5f, 1.2f);
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        float scratchFreq = 3f + variant * 2f; // Scratch speed
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            
            // Rhythmic scratching pattern
            float scratchEnvelope = Mathf.Abs(Mathf.Sin(t * scratchFreq * Mathf.PI));
            float noise = Random.Range(-1f, 1f);
            
            // High-frequency filtered noise (more scratchy)
            float highPass = noise - (i > 0 ? data[i - 1] : 0f);
            
            float fadeEnv = Mathf.Sin(t / duration * Mathf.PI);
            
            data[i] = highPass * scratchEnvelope * fadeEnv * 0.3f;
        }
        
        AudioClip clip = AudioClip.Create($"Scratch_{variant}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateMetalClang(int variant)
    {
        int sampleRate = 44100;
        float duration = 1.5f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        float baseFreq = 400f + variant * 200f;
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 3f);
            
            // Metallic: inharmonic overtones
            float v = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.3f;
            v += Mathf.Sin(2f * Mathf.PI * baseFreq * 2.76f * t) * 0.2f;
            v += Mathf.Sin(2f * Mathf.PI * baseFreq * 5.4f * t) * 0.1f;
            v += Mathf.Sin(2f * Mathf.PI * baseFreq * 8.93f * t) * 0.05f;
            
            data[i] = v * envelope * 0.4f;
        }
        
        AudioClip clip = AudioClip.Create($"MetalClang_{variant}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateLowDrone()
    {
        int sampleRate = 44100;
        float duration = 4f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(t / duration * Mathf.PI);
            
            // Deep rumbling drone
            float drone = Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.3f;
            drone += Mathf.Sin(2f * Mathf.PI * 37f * t) * 0.2f; // Slight detune for beat
            drone += Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.1f;
            
            // LFO modulation for unease
            float lfo = Mathf.Sin(t * 0.5f) * 0.3f + 0.7f;
            
            data[i] = drone * envelope * lfo * 0.3f;
        }
        
        AudioClip clip = AudioClip.Create("LowDrone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    void OnDestroy()
    {
        // Clean up procedural clips
        if (whisperClips != null) foreach (var c in whisperClips) if (c != null) Destroy(c);
        if (creakClips != null) foreach (var c in creakClips) if (c != null) Destroy(c);
        if (footstepClips != null) foreach (var c in footstepClips) if (c != null) Destroy(c);
        if (breathClips != null) foreach (var c in breathClips) if (c != null) Destroy(c);
        if (scratchClips != null) foreach (var c in scratchClips) if (c != null) Destroy(c);
        if (metalClips != null) foreach (var c in metalClips) if (c != null) Destroy(c);
        if (lowDroneClip != null) Destroy(lowDroneClip);
    }
}
