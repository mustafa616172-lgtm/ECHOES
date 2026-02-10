using UnityEngine;

/// <summary>
/// Plays heartbeat sound that speeds up as danger increases.
/// Automatically links to AwarenessIndicator danger level.
/// </summary>
public class HeartbeatEffect : MonoBehaviour
{
    [Header("Heartbeat Settings")]
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private float minDangerToStart = 30f;   // Start heartbeat at this danger %
    [SerializeField] private float minInterval = 0.3f;       // Fastest beat interval (high danger)
    [SerializeField] private float maxInterval = 1.5f;       // Slowest beat interval (low danger)
    [SerializeField] private float minVolume = 0.1f;
    [SerializeField] private float maxVolume = 0.7f;
    
    private AudioSource audioSource;
    private AwarenessIndicator awarenessIndicator;
    private float nextBeatTime;
    private bool isBeating = false;
    
    void Start()
    {
        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;  // 2D sound (always same volume)
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        
        // Generate heartbeat sound if no clip assigned
        if (heartbeatClip == null)
        {
            heartbeatClip = GenerateHeartbeatClip();
        }
        
        // Find awareness indicator
        awarenessIndicator = FindObjectOfType<AwarenessIndicator>();
        
        Debug.Log("[HeartbeatEffect] Initialized");
    }
    
    void Update()
    {
        // Find indicator if missing
        if (awarenessIndicator == null)
        {
            awarenessIndicator = FindObjectOfType<AwarenessIndicator>();
            return;
        }
        
        float danger = awarenessIndicator.DangerLevel;
        
        // Should we be beating?
        if (danger >= minDangerToStart)
        {
            if (!isBeating)
            {
                isBeating = true;
                nextBeatTime = Time.time;
            }
            
            // Calculate interval based on danger (higher danger = shorter interval)
            float dangerNormalized = Mathf.InverseLerp(minDangerToStart, 100f, danger);
            float currentInterval = Mathf.Lerp(maxInterval, minInterval, dangerNormalized);
            float currentVolume = Mathf.Lerp(minVolume, maxVolume, dangerNormalized);
            
            // Time to beat?
            if (Time.time >= nextBeatTime)
            {
                PlayBeat(currentVolume);
                nextBeatTime = Time.time + currentInterval;
            }
        }
        else
        {
            isBeating = false;
        }
    }
    
    void PlayBeat(float volume)
    {
        if (heartbeatClip != null && audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(heartbeatClip);
        }
    }
    
    /// <summary>
    /// Generates a simple heartbeat sound procedurally (thump-thump).
    /// Used when no audio clip is assigned.
    /// </summary>
    AudioClip GenerateHeartbeatClip()
    {
        int sampleRate = 44100;
        float duration = 0.4f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        
        // First thump (louder) - "LUB"
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // First beat: 0.0 - 0.1s
            if (t < 0.1f)
            {
                float env = Mathf.Exp(-t * 30f); // Quick decay
                sample += Mathf.Sin(2f * Mathf.PI * 60f * t) * env * 0.8f;   // Low thump
                sample += Mathf.Sin(2f * Mathf.PI * 40f * t) * env * 0.5f;   // Sub bass
            }
            
            // Second beat: 0.15 - 0.25s  ("DUB")
            if (t > 0.15f && t < 0.25f)
            {
                float t2 = t - 0.15f;
                float env = Mathf.Exp(-t2 * 35f); // Slightly faster decay
                sample += Mathf.Sin(2f * Mathf.PI * 50f * t2) * env * 0.6f;  // Slightly softer
                sample += Mathf.Sin(2f * Mathf.PI * 35f * t2) * env * 0.4f;  // Sub bass
            }
            
            samples[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        AudioClip clip = AudioClip.Create("Heartbeat", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        
        Debug.Log("[HeartbeatEffect] Generated procedural heartbeat sound");
        return clip;
    }
}
