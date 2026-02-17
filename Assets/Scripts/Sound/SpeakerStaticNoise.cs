using UnityEngine;
using System.Collections;
using System;

public enum SpeakerMode { Static, Breathing, VoiceLine, Silent }

[RequireComponent(typeof(AudioSource))]
public class SpeakerStaticNoise : MonoBehaviour
{
    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 0.12f;

    [Header("Noise Character")]
    [Range(0f, 1f)]
    [SerializeField] private float noiseLevel = 0f; // Was 0.3f
    [Range(0f, 1f)]
    [SerializeField] private float crackleLevel = 0f; // Was 0.5f
    [Range(0.001f, 0.05f)]
    [SerializeField] private float crackleRate = 0.008f;
    [Range(0f, 1f)]
    [SerializeField] private float humLevel = 0f; // Was 0.2f
    [SerializeField] private float humFrequency = 50f;

    [Header("3D Spatial Audio")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 12f;
    [Range(0f, 360f)]
    [SerializeField] private float spread = 45f;

    [Header("Clip")]
    [SerializeField] private float clipLength = 4f;

    [Header("Voice/Breathing")]
    [Tooltip("Optional custom voice line clip (can be assigned via Inspector)")]
    [SerializeField] private AudioClip customVoiceClip;
    [Tooltip("Optional custom breathing clip")]
    [SerializeField] private AudioClip customBreathingClip;

    public bool IsPlaying { get; private set; }
    public SpeakerMode CurrentMode { get; private set; }
    private AudioSource audioSource;
    private AudioSource voiceSource;
    private float baseVolume;
    private Coroutine fadeCoroutine;
    private AudioClip staticClip;
    private AudioClip breathingClip;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = masterVolume;
        IsPlaying = true;
        CurrentMode = SpeakerMode.Static;

        // Create secondary AudioSource for voice lines
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 1.0f;
        voiceSource.minDistance = minDistance;
        voiceSource.maxDistance = maxDistance * 1.5f;
        voiceSource.priority = 100;
        voiceSource.volume = 0f;

        staticClip = GenerateStaticNoiseClip();
        breathingClip = customBreathingClip != null ? customBreathingClip : GenerateBreathingClip();
        ConfigureSpatialAudio(staticClip);
    }

    AudioClip GenerateStaticNoiseClip()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int totalSamples = Mathf.CeilToInt(clipLength * sampleRate);
        float[] samples = new float[totalSamples];
        
        // STATIC REMOVED BY REQUEST
        // Return pure silence
        
        AudioClip clip = AudioClip.Create("SpeakerStatic_" + gameObject.name, totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void ConfigureSpatialAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = masterVolume;
        audioSource.priority = 200;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;

        AnimationCurve rolloff = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(minDistance / maxDistance, 0.85f, -0.5f, -0.5f),
            new Keyframe(0.35f, 0.4f, -1.2f, -1.2f),
            new Keyframe(0.65f, 0.1f, -0.5f, -0.3f),
            new Keyframe(0.85f, 0.02f, -0.1f, -0.1f),
            new Keyframe(1f, 0f, -0.05f, 0f)
        );
        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloff);

        AnimationCurve spatialCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 1f)
        );
        audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialCurve);

        audioSource.spread = spread;
        audioSource.dopplerLevel = 0f;
        audioSource.reverbZoneMix = 1.1f;
        audioSource.Play();
        audioSource.time = UnityEngine.Random.Range(0f, clipLength * 0.9f);
    }

    public void SetVolume(float target, float fadeDuration = 0.5f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolume(target, fadeDuration));
    }

    public void BoostVolume(float multiplier, float fadeDuration = 0.8f)
    {
        SetVolume(Mathf.Clamp01(baseVolume * multiplier), fadeDuration);
    }

    public void ResetVolume(float fadeDuration = 0.8f)
    {
        SetVolume(baseVolume, fadeDuration);
    }

    public void TurnOff(float fadeDuration = 1f)
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAndStop(fadeDuration));
    }

    public void TurnOn(float fadeDuration = 1f)
    {
        if (IsPlaying) return;
        IsPlaying = true;
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.volume = 0f;
            audioSource.Play();
        }
        SetVolume(baseVolume, fadeDuration);
    }

    public void TriggerHeavyStatic(float duration = 1.5f)
    {
        // DISABLED BY REQUEST: No static noise anywhere
        // StartCoroutine(HeavyStaticRoutine(duration));
    }

    /// <summary>
    /// Switch speaker mode (Static, Breathing, VoiceLine, Silent)
    /// </summary>
    public void SetMode(SpeakerMode mode, float fadeDuration = 1f)
    {
        if (CurrentMode == mode) return;
        SpeakerMode previousMode = CurrentMode;
        CurrentMode = mode;

        switch (mode)
        {
            case SpeakerMode.Static:
                // Stop before changing clip to avoid Unity clip-switch warnings
                audioSource.Stop();
                audioSource.clip = staticClip;
                audioSource.loop = true;
                audioSource.Play();
                SetVolume(baseVolume, fadeDuration);
                IsPlaying = true;
                break;

            case SpeakerMode.Breathing:
                audioSource.Stop();
                audioSource.clip = breathingClip;
                audioSource.loop = true;
                audioSource.Play();
                SetVolume(baseVolume * 0.6f, fadeDuration);
                IsPlaying = true;
                break;

            case SpeakerMode.VoiceLine:
                // Static continues at low volume, voice plays on voiceSource
                SetVolume(baseVolume * 0.3f, 0.3f);
                break;

            case SpeakerMode.Silent:
                TurnOff(fadeDuration);
                break;
        }

        Debug.Log("[SpeakerStaticNoise] Mode: " + previousMode + " -> " + mode + " on " + gameObject.name);
    }

    /// <summary>
    /// Play a voice line through the speaker's secondary audio source
    /// </summary>
    public void PlayVoiceLine(AudioClip clip, float voiceVolume = 0.35f, System.Action onComplete = null)
    {
        if (clip == null) return;
        SetMode(SpeakerMode.VoiceLine, 0.3f);
        StartCoroutine(PlayVoiceLineRoutine(clip, voiceVolume, onComplete));
    }

    /// <summary>
    /// Start breathing mode on speakers
    /// </summary>
    public void PlayBreathing()
    {
        SetMode(SpeakerMode.Breathing, 1.5f);
    }

    /// <summary>
    /// Set a custom breathing clip at runtime
    /// </summary>
    public void SetBreathingClip(AudioClip clip)
    {
        breathingClip = clip;
    }

    /// <summary>
    /// Stop breathing and return to static
    /// </summary>
    public void StopBreathing(float fadeDuration = 1f)
    {
        if (CurrentMode == SpeakerMode.Breathing)
            SetMode(SpeakerMode.Static, fadeDuration);
    }

    private IEnumerator PlayVoiceLineRoutine(AudioClip clip, float vol, System.Action onComplete)
    {
        if (voiceSource == null) yield break;

        // Add static burst before voice
        TriggerHeavyStatic(0.3f);
        yield return new WaitForSeconds(0.3f);

        voiceSource.clip = clip;
        voiceSource.volume = 0f;
        voiceSource.Play();

        // Fade in voice
        float e = 0f;
        while (e < 0.5f)
        {
            e += Time.deltaTime;
            voiceSource.volume = Mathf.Lerp(0f, vol, e / 0.5f);
            yield return null;
        }
        voiceSource.volume = vol;

        // Wait for clip to finish
        yield return new WaitForSeconds(clip.length - 0.5f);

        // Fade out voice
        e = 0f;
        while (e < 0.5f)
        {
            e += Time.deltaTime;
            voiceSource.volume = Mathf.Lerp(vol, 0f, e / 0.5f);
            yield return null;
        }
        voiceSource.volume = 0f;
        voiceSource.Stop();

        // Return to static
        SetMode(SpeakerMode.Static, 0.5f);

        onComplete?.Invoke();
    }

    /// <summary>
    /// Generates a creepy breathing clip: slow inhale/exhale with faint static
    /// </summary>
    AudioClip GenerateBreathingClip()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        float breathLen = 6f;
        int totalSamples = Mathf.CeilToInt(breathLen * sampleRate);
        float[] samples = new float[totalSamples];
        System.Random rng = new System.Random(gameObject.GetInstanceID() + 999);

        float breathCycle = 3.5f; // seconds per full breath cycle

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float breathPhase = (t % breathCycle) / breathCycle;

            // Inhale 0-0.4, pause 0.4-0.5, exhale 0.5-0.9, pause 0.9-1.0
            float breathEnvelope = 0f;
            if (breathPhase < 0.4f)
            {
                // Inhale - rising
                float p = breathPhase / 0.4f;
                breathEnvelope = Mathf.Sin(p * Mathf.PI * 0.5f) * 0.7f;
            }
            else if (breathPhase < 0.5f)
            {
                // Short pause at top
                float p = (breathPhase - 0.4f) / 0.1f;
                breathEnvelope = Mathf.Lerp(0.7f, 0.6f, p);
            }
            else if (breathPhase < 0.9f)
            {
                // Exhale - falling
                float p = (breathPhase - 0.5f) / 0.4f;
                breathEnvelope = Mathf.Cos(p * Mathf.PI * 0.5f) * 0.6f;
            }
            // else silence between breaths

            // Breath sound: filtered noise
            float noise = (float)rng.NextDouble() * 2f - 1f;
            float noise2 = (float)rng.NextDouble() * 2f - 1f;
            float filtered = (noise + noise2) * 0.5f;

            // Add very subtle low-frequency modulation (like vocal cords)
            filtered *= 1f + 0.3f * Mathf.Sin(t * 180f);
            filtered *= 1f + 0.1f * Mathf.Sin(t * 340f);

            float sample = filtered * breathEnvelope * 0.5f;

            // Very faint background static
            sample += ((float)rng.NextDouble() * 2f - 1f) * 0.03f;

            samples[i] = Mathf.Clamp(sample, -0.9f, 0.9f);
        }

        // Crossfade loop point
        int fadeLen = Mathf.Min(sampleRate / 2, totalSamples / 4);
        for (int i = 0; i < fadeLen; i++)
        {
            float fade = (float)i / fadeLen;
            int endIdx = totalSamples - fadeLen + i;
            samples[endIdx] = samples[endIdx] * (1f - fade) + samples[i] * fade;
        }

        AudioClip clip = AudioClip.Create("Breathing_" + gameObject.name, totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private IEnumerator FadeVolume(float target, float duration)
    {
        if (audioSource == null) yield break;
        float start = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        audioSource.volume = target;
        fadeCoroutine = null;
    }

    private IEnumerator FadeAndStop(float duration)
    {
        if (audioSource == null) yield break;
        float start = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();
        fadeCoroutine = null;
    }

    private IEnumerator HeavyStaticRoutine(float duration)
    {
        if (audioSource == null) yield break;
        float orig = audioSource.volume;
        float target = Mathf.Min(orig * 3f, 0.5f);
        float e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(orig, target, e / 0.1f);
            yield return null;
        }
        yield return new WaitForSeconds(Mathf.Max(0f, duration - 0.2f));
        e = 0f;
        while (e < 0.1f)
        {
            e += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(target, orig, e / 0.1f);
            yield return null;
        }
        audioSource.volume = orig;
    }

    void OnDestroy()
    {
        // Clean up dynamically created AudioSource
        if (voiceSource != null)
            Destroy(voiceSource);
    }

    void OnValidate()
    {
        if (audioSource != null && Application.isPlaying)
        {
            audioSource.volume = masterVolume;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
        }
    }
}
