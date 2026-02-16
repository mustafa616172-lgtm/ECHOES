using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SpeakerStaticNoise : MonoBehaviour
{
    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 0.12f;

    [Header("Noise Character")]
    [Range(0f, 1f)]
    [SerializeField] private float noiseLevel = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float crackleLevel = 0.5f;
    [Range(0.001f, 0.05f)]
    [SerializeField] private float crackleRate = 0.008f;
    [Range(0f, 1f)]
    [SerializeField] private float humLevel = 0.2f;
    [SerializeField] private float humFrequency = 50f;

    [Header("3D Spatial Audio")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 12f;
    [Range(0f, 360f)]
    [SerializeField] private float spread = 45f;

    [Header("Clip")]
    [SerializeField] private float clipLength = 4f;

    public bool IsPlaying { get; private set; }
    private AudioSource audioSource;
    private float baseVolume;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = masterVolume;
        IsPlaying = true;
        AudioClip noiseClip = GenerateStaticNoiseClip();
        ConfigureSpatialAudio(noiseClip);
    }

    AudioClip GenerateStaticNoiseClip()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int totalSamples = Mathf.CeilToInt(clipLength * sampleRate);
        float[] samples = new float[totalSamples];
        System.Random rng = new System.Random(gameObject.GetInstanceID() + transform.position.GetHashCode());
        float humPhase = 0f;
        float humPhase2 = 0f;
        float humPhase3 = 0f;
        float dt = 1f / sampleRate;
        float varPhase = (float)rng.NextDouble() * Mathf.PI * 2f;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            float n1 = (float)rng.NextDouble() * 2f - 1f;
            float n2 = (float)rng.NextDouble() * 2f - 1f;
            sample += (n1 + n2) * 0.5f * noiseLevel;

            if (rng.NextDouble() < crackleRate)
            {
                int popLen = 5 + rng.Next(25);
                float popAmp = 0.5f + (float)rng.NextDouble() * 0.5f;
                for (int p = 0; p < popLen && (i + p) < totalSamples; p++)
                {
                    float decay = 1f - (float)p / popLen;
                    decay *= decay;
                    samples[i + p] += ((float)rng.NextDouble() * 2f - 1f) * popAmp * decay * crackleLevel;
                }
            }

            float hum = Mathf.Sin(humPhase) * humLevel;
            humPhase += humFrequency * dt * Mathf.PI * 2f;
            if (humPhase > Mathf.PI * 100f) humPhase -= Mathf.PI * 100f;
            hum += Mathf.Sin(humPhase2) * humLevel * 0.35f;
            humPhase2 += humFrequency * 2f * dt * Mathf.PI * 2f;
            if (humPhase2 > Mathf.PI * 100f) humPhase2 -= Mathf.PI * 100f;
            hum += Mathf.Sin(humPhase3) * humLevel * 0.15f;
            humPhase3 += humFrequency * 3f * dt * Mathf.PI * 2f;
            if (humPhase3 > Mathf.PI * 100f) humPhase3 -= Mathf.PI * 100f;
            sample += hum;

            float volumeMod = 0.7f + 0.3f * Mathf.Sin(t * 0.6f + varPhase);
            volumeMod *= 0.8f + 0.2f * Mathf.Sin(t * 1.7f + varPhase * 0.3f);
            if (rng.NextDouble() < 0.0003f)
            {
                int dropLen = sampleRate / 20 + rng.Next(sampleRate / 8);
                for (int d = 0; d < dropLen && (i + d) < totalSamples; d++)
                {
                    float df = (float)d / dropLen;
                    samples[i + d] *= df < 0.5f ? df * 2f : (1f - df) * 0.1f;
                }
            }
            sample *= volumeMod;
            samples[i] += Mathf.Clamp(sample, -0.9f, 0.9f);
        }

        int fadeLen = Mathf.Min(sampleRate / 2, totalSamples / 4);
        for (int i = 0; i < fadeLen; i++)
        {
            float fade = (float)i / fadeLen;
            int endIdx = totalSamples - fadeLen + i;
            samples[endIdx] = samples[endIdx] * (1f - fade) + samples[i] * fade;
        }

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
        audioSource.time = Random.Range(0f, clipLength * 0.9f);
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
        StartCoroutine(HeavyStaticRoutine(duration));
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
        float orig = audioSource.volume;
        float target = Mathf.Min(orig * 3f, 0.5f);
        float e = 0f;
        while (e < 0.1f) { e += Time.deltaTime; audioSource.volume = Mathf.Lerp(orig, target, e / 0.1f); yield return null; }
        yield return new WaitForSeconds(duration - 0.2f);
        e = 0f;
        while (e < 0.1f) { e += Time.deltaTime; audioSource.volume = Mathf.Lerp(target, orig, e / 0.1f); yield return null; }
        audioSource.volume = orig;
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
