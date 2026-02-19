using UnityEngine;

/// <summary>
/// ECHOES - Ghost Audio Source
/// Placed on scene objects that emit ghost/echo voices (e.g., dead doctor's voice).
/// These are the sound sources that the player can "steal" using the Sound Recorder device.
/// 
/// Setup: Place on any GameObject with an AudioClip assigned.
/// The tag "GhostSound" is auto-assigned on Awake for detection by SoundRecorderDevice.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class GhostAudioSource : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique clip ID that must match VoiceLockDoor.requiredClipID to unlock")]
    public string clipID = "ghost_voice_01";
    [Tooltip("Display name shown in the recording UI")]
    public string clipDisplayName = "Bilinmeyen Ses";

    [Header("Audio")]
    [Tooltip("The ghost audio clip to play and record")]
    public AudioClip ghostClip;
    [Tooltip("Volume of the ambient ghost voice")]
    [Range(0f, 1f)] public float ambientVolume = 0.4f;
    [Tooltip("Should the ghost voice loop in the scene?")]
    public bool loopAmbient = true;
    [Tooltip("Play ambient sound on start?")]
    public bool playOnStart = true;

    [Header("3D Audio Settings")]
    public float minDistance = 1f;
    public float maxDistance = 15f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Recording")]
    [Tooltip("Can this source be recorded by the player?")]
    public bool isRecordable = true;
    [Tooltip("Disable recording after first capture? (one-time steal)")]
    public bool oneTimeRecord = true;
    [Tooltip("How close the player must be to record (OverlapSphere radius)")]
    public float recordingRange = 6f;

    [Header("Visual Feedback")]
    [Tooltip("Pulsing light to indicate ghost presence")]
    public Light ghostLight;
    [Tooltip("Light color when not being recorded")]
    public Color idleColor = new Color(0.4f, 0.1f, 0.6f, 1f);
    [Tooltip("Light color when being recorded")]
    public Color recordingColor = new Color(1f, 0.1f, 0.1f, 1f);
    [Tooltip("Light pulse speed")]
    public float pulseSpeed = 2f;
    [Tooltip("Light intensity range")]
    public float minIntensity = 0.3f;
    public float maxIntensity = 1.5f;
    [Tooltip("Light range")]
    public float lightRange = 4f;

    [Header("Ghost Aura")]
    [Tooltip("Enable eerie audio distortion on ambient playback")]
    public bool showAuraEffect = true;
    [Tooltip("Intensity of audio distortion applied to ambient playback")]
    [Range(0f, 0.5f)] public float ambientDistortion = 0.1f;

    // Runtime state
    private AudioSource audioSource;
    private bool hasBeenRecorded = false;
    private bool isBeingRecorded = false;
    private float pulseTimer = 0f;
    private System.Random noiseRandom;
    private Coroutine fadeCoroutine;

    // Public state
    public bool HasBeenRecorded => hasBeenRecorded;
    public bool IsBeingRecorded => isBeingRecorded;
    public bool CanRecord => isRecordable && !hasBeenRecorded;

    void Awake()
    {
        // Auto-assign the GhostSound tag for OverlapSphere detection
        if (gameObject.tag != "GhostSound")
        {
            try
            {
                gameObject.tag = "GhostSound";
            }
            catch
            {
#if UNITY_EDITOR
                Debug.LogWarning("[GhostAudioSource] Could not set 'GhostSound' tag on '" + gameObject.name + "'. Add the 'GhostSound' tag via Edit > Project Settings > Tags and Layers");
#endif
            }
        }
    }

    void Start()
    {
        // RequireComponent guarantees AudioSource exists - just get it
        audioSource = GetComponent<AudioSource>();

        if (ghostClip != null)
        {
            audioSource.clip = ghostClip;
            audioSource.loop = loopAmbient;
            audioSource.volume = ambientVolume;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = rolloffMode;
            audioSource.dopplerLevel = 0f;

            if (playOnStart)
                audioSource.Play();
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("[GhostAudioSource] No ghostClip assigned on '" + gameObject.name + "'!");
#endif
        }

        // Setup light
        if (ghostLight == null)
            CreateGhostLight();

        noiseRandom = new System.Random(gameObject.GetInstanceID());

#if UNITY_EDITOR
        Debug.Log("[GhostAudioSource] Initialized '" + clipID + "' on '" + gameObject.name + "'. Recordable=" + isRecordable);
#endif
    }

    void Update()
    {
        UpdateLightPulse();
    }

    // ==========================================
    // RECORDING INTERFACE
    // ==========================================

    /// <summary>
    /// Called by SoundRecorderDevice when recording starts on this source.
    /// </summary>
    public void OnRecordingStart()
    {
        if (!CanRecord) return;

        isBeingRecorded = true;
#if UNITY_EDITOR
        Debug.Log("[GhostAudioSource] Recording STARTED on '" + clipID + "'");
#endif

        if (ghostLight != null)
            ghostLight.color = recordingColor;
    }

    /// <summary>
    /// Called by SoundRecorderDevice when recording ends.
    /// Returns the clip data for storage.
    /// </summary>
    public RecordedClipData OnRecordingComplete()
    {
        isBeingRecorded = false;

        RecordedClipData data = new RecordedClipData
        {
            clipID = this.clipID,
            clipName = this.clipDisplayName,
            audioClip = this.ghostClip,
            hasClip = true,
            sourcePosition = transform.position
        };

        if (oneTimeRecord)
        {
            hasBeenRecorded = true;
            isRecordable = false;
#if UNITY_EDITOR
            Debug.Log("[GhostAudioSource] '" + clipID + "' recorded and LOCKED (one-time).");
#endif
            // Fade out ambient audio after recording
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutAmbient());
        }
#if UNITY_EDITOR
        else
        {
            Debug.Log("[GhostAudioSource] '" + clipID + "' recorded (reusable).");
        }
#endif

        return data;
    }

    /// <summary>
    /// Called if recording is cancelled/interrupted.
    /// </summary>
    public void OnRecordingCancelled()
    {
        isBeingRecorded = false;
        if (ghostLight != null)
            ghostLight.color = idleColor;
#if UNITY_EDITOR
        Debug.Log("[GhostAudioSource] Recording CANCELLED on '" + clipID + "'");
#endif
    }

    // ==========================================
    // VISUAL FEEDBACK
    // ==========================================

    private void UpdateLightPulse()
    {
        if (ghostLight == null) return;

        pulseTimer += Time.deltaTime * pulseSpeed;

        float intensity;
        if (isBeingRecorded)
        {
            intensity = Mathf.Lerp(minIntensity, maxIntensity * 2f,
                (Mathf.Sin(pulseTimer * 4f) + 1f) * 0.5f);
            ghostLight.color = Color.Lerp(ghostLight.color, recordingColor, Time.deltaTime * 10f);
        }
        else if (hasBeenRecorded)
        {
            intensity = minIntensity * 0.3f +
                Mathf.Sin(pulseTimer * 0.5f) * minIntensity * 0.1f;
            ghostLight.color = Color.Lerp(ghostLight.color,
                new Color(0.2f, 0.2f, 0.3f), Time.deltaTime * 2f);
        }
        else
        {
            float sineWave = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
            float flicker = 1f + (Mathf.PerlinNoise(pulseTimer * 3f, 0f) - 0.5f) * 0.3f;
            intensity = Mathf.Lerp(minIntensity, maxIntensity, sineWave) * flicker;
            ghostLight.color = Color.Lerp(ghostLight.color, idleColor, Time.deltaTime * 3f);
        }

        ghostLight.intensity = intensity;
    }

    private void CreateGhostLight()
    {
        ghostLight = GetComponentInChildren<Light>();

        if (ghostLight == null)
        {
            GameObject lightObj = new GameObject("GhostLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 0.5f;

            ghostLight = lightObj.AddComponent<Light>();
            ghostLight.type = LightType.Point;
            ghostLight.range = lightRange;
            ghostLight.intensity = minIntensity;
            ghostLight.color = idleColor;
            ghostLight.shadows = LightShadows.None;
            ghostLight.renderMode = LightRenderMode.Auto;
        }

        ghostLight.enabled = true;
    }

    // ==========================================
    // AMBIENT AUDIO EFFECTS
    // ==========================================

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!showAuraEffect || ambientDistortion <= 0f) return;
        if (hasBeenRecorded) return;
        if (noiseRandom == null) return; // Guard: audio thread may fire before Start

        for (int i = 0; i < data.Length; i++)
        {
            if (noiseRandom.NextDouble() < ambientDistortion * 0.1f)
            {
                data[i] += (float)(noiseRandom.NextDouble() * 2.0 - 1.0) * ambientDistortion;
            }

            float wobble = Mathf.Sin(i * 0.01f + pulseTimer * 100f) * ambientDistortion * 0.3f;
            data[i] += wobble;
        }
    }

    private System.Collections.IEnumerator FadeOutAmbient()
    {
        if (audioSource == null) yield break;

        float startVol = audioSource.volume;
        float elapsed = 0f;
        float duration = 3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            audioSource.volume = Mathf.Lerp(startVol, 0.05f, t);
            yield return null;
        }

        audioSource.volume = 0.05f;
        fadeCoroutine = null;
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    // ==========================================
    // GIZMOS
    // ==========================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, recordingRange);

        Gizmos.color = new Color(0.5f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}

/// <summary>
/// Data struct for a recorded ghost clip stored in the player's device.
/// Shared between GhostAudioSource, SoundRecorderDevice, and VoiceLockDoor.
/// </summary>
[System.Serializable]
public struct RecordedClipData
{
    public string clipID;
    public string clipName;
    public AudioClip audioClip;
    public bool hasClip;
    public Vector3 sourcePosition;
}