using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

/// <summary>
/// ECHOES - Story Sequence Manager
/// Central state machine that orchestrates the 5-step horror story flow.
/// 
/// Step 1 - Wake Up: Breathing on speakers, door closed
/// Step 2 - Sound Room: Auto-close + lock door, play voice line
/// Step 3 - Darkness: Lights off + ambient black, echo silhouette glows
/// Step 4 - Ghost: Figure visible only during Echo pulse
/// Step 5 - Return: Room objects changed (gaslighting)
/// </summary>
public class StorySequenceManager : MonoBehaviour
{
    public enum StoryState
    {
        WaitingForWakeUp = 0,
        Exploring = 1,
        EnteredSoundRoom = 2,
        Darkness = 3,
        EchoAcquired = 4,
        ReturnedToRoom = 5,
        Complete = 6
    }

    // Singleton - only one story manager per scene
    public static StorySequenceManager Instance { get; private set; }

    [Header("Current State")]
    [SerializeField] private StoryState currentState = StoryState.WaitingForWakeUp;
    public StoryState CurrentState => currentState;

    [Header("References - Door")]
    [Tooltip("The main door between rooms (Kapimentese)")]
    public DoorInteractable mainDoor;

    [Header("References - Speakers")]
    [Tooltip("All speakers in the sound room")]
    public SpeakerStaticNoise[] speakers;

    [Header("References - Lights")]
    [Tooltip("Lights in the sound room to turn off")]
    public Light[] soundRoomLights;
    [Tooltip("Light switches in the sound room")]
    public LightSwitchInteractable[] soundRoomSwitches;

    [Header("References - Echo Device")]
    [Tooltip("The Echo device pickup object")]
    public GameObject echoPickupObject;

    [Header("References - Ghost")]
    [Tooltip("Ghost figure behind glass (EchoVisibleObject)")]
    public EchoVisibleObject ghostFigure;

    [Header("References - Room State")]
    [Tooltip("Room state changer for gaslighting effect")]
    public RoomStateChanger roomStateChanger;

    [Header("References - Triggers")]
    [Tooltip("Trigger at sound room entrance")]
    public SoundRoomTrigger soundRoomTrigger;
    [Tooltip("Trigger at first room entrance (for return detection)")]
    public FirstRoomTrigger firstRoomReturnTrigger;

    [Header("Voice Line")]
    [Tooltip("Voice line clip (Denek 47 frekans baslatiliyor) - leave empty for procedural")]
    public AudioClip voiceLineClip;

    [Header("Horror Audio")]
    [Tooltip("Woman Breathing Loop")]
    public AudioClip womanBreathingClip;
    [Tooltip("Woman Horror Scream One-Shot")]
    public AudioClip horrorScreamClip;

    [Header("Timing")]
    [SerializeField] private float delayBeforeDoorClose = 1.5f;
    [SerializeField] private float delayBeforeVoiceLine = 2f;
    [SerializeField] private float delayBeforeDarkness = 2f;
    [SerializeField] private float echoGlowDelay = 1.5f;
    [SerializeField] private float maxVoiceLineWait = 30f;

    [Header("Darkness Settings")]
    [Tooltip("How dark ambient lighting should be during darkness (0 = pitch black)")]
    [SerializeField] private float darknessAmbientIntensity = 0f;
    [Tooltip("Echo device silhouette glow color")]
    [SerializeField] private Color echoSilhouetteColor = new Color(0.85f, 0.9f, 1f, 1f);
    [Tooltip("Echo device silhouette glow intensity")]
    [SerializeField] private float echoSilhouetteIntensity = 0.4f;
    [Tooltip("Echo device point light intensity")]
    [SerializeField] private float echoPointLightIntensity = 0.15f;
    [Tooltip("Echo device point light range")]
    [SerializeField] private float echoPointLightRange = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool isProcessing = false;
    private Coroutine activeSequence;

    // Saved ambient settings to restore later
    private Color savedAmbientLight;
    private float savedAmbientIntensity;
    private float savedReflectionIntensity;
    private AmbientMode savedAmbientMode;
    private bool ambientSaved = false;

    // Echo silhouette objects
    private GameObject echoSilhouetteLight;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            LogWarning("Duplicate StorySequenceManager detected, destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        AutoFindReferences();

        // Start story - wait for eye blink or start immediately
        EyeBlinkIntro eyeBlinkIntro = FindFirstObjectByType<EyeBlinkIntro>();
        if (eyeBlinkIntro != null)
        {
            activeSequence = StartCoroutine(WaitForEyeBlinkThenStart(eyeBlinkIntro));
        }
        else
        {
            activeSequence = StartCoroutine(StartStory());
        }

        Log("Initialized. State: " + currentState);

        // Start breathing IMMEDIATELY (while screen is black)
        StartBreathingOnSpeakers();
    }

    void StartBreathingOnSpeakers()
    {
        int speakerCount = 0;
        if (speakers != null)
        {
            foreach (var speaker in speakers)
            {
                if (speaker != null)
                {
                    // Assign custom breathing clip if available
                    if (womanBreathingClip != null)
                    {
                        speaker.SetBreathingClip(womanBreathingClip);
                    }

                    speaker.PlayBreathing();
                    speakerCount++;
                }
            }
        }
        Log("Breathing started immediately on " + speakerCount + " speakers");
    }

    void StopBreathingOnSpeakers()
    {
        if (speakers != null)
        {
            foreach (var speaker in speakers)
            {
                if (speaker != null)
                {
                    speaker.StopBreathing(2.0f); // Fade out over 2 seconds
                }
            }
        }
        Log("Breathing stopped on speakers");
    }

    void OnDestroy()
    {
        // Restore ambient if we changed it
        if (ambientSaved)
        {
            // RestoreAmbientLighting(); // DISABLED: Keep darkness
            // Debug.Log("[StorySequence] Ambient restore skipped on destroy to maintain darkness.");
        }

        // Clean up silhouette
        if (echoSilhouetteLight != null)
            Destroy(echoSilhouetteLight);

        if (Instance == this)
            Instance = null;
    }

    void AutoFindReferences()
    {
        if (speakers == null || speakers.Length == 0)
            speakers = FindObjectsByType<SpeakerStaticNoise>(FindObjectsSortMode.None);

        if (mainDoor == null)
        {
            DoorInteractable[] doors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
            foreach (var d in doors)
            {
                if (d.gameObject.name.Contains("Kapi") || d.gameObject.name.Contains("mentese"))
                {
                    mainDoor = d;
                    break;
                }
            }
            if (mainDoor == null && doors.Length > 0)
                mainDoor = doors[0];
        }

        if (roomStateChanger == null)
            roomStateChanger = FindFirstObjectByType<RoomStateChanger>();

        if (ghostFigure == null)
            ghostFigure = FindFirstObjectByType<EchoVisibleObject>();

        if (soundRoomTrigger == null)
            soundRoomTrigger = FindFirstObjectByType<SoundRoomTrigger>();

        if (firstRoomReturnTrigger == null)
            firstRoomReturnTrigger = FindFirstObjectByType<FirstRoomTrigger>();

#if UNITY_EDITOR
        // Auto-assign audio clips from Resources or specific paths if missing
        if (womanBreathingClip == null)
            womanBreathingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ses/Ses Efektleri/Nefes/WomanBreathing.mp3");
        
        if (horrorScreamClip == null)
            horrorScreamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ses/Ses Efektleri/Nefes/WomanScream.mp3");
#endif
    }

    IEnumerator WaitForEyeBlinkThenStart(EyeBlinkIntro eyeBlinkIntro)
    {
        yield return new WaitForSeconds(0.5f);

        // EyeBlinkIntro calls Destroy(this) when done, so we check for null
        float timeout = 15f;
        float waited = 0f;
        while (eyeBlinkIntro != null && waited < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            waited += 0.5f;
        }

        if (waited >= timeout)
            LogWarning("EyeBlinkIntro timeout reached, starting story anyway.");

        yield return StartStory();
    }

    // =============================================
    // STEP 1 - WAKE UP
    // =============================================
    IEnumerator StartStory()
    {
        Log("=== STEP 1: WAKE UP ===");
        AdvanceState(StoryState.Exploring);

        // Ensure door is closed
        if (mainDoor != null && mainDoor.IsDoorOpen())
        {
            mainDoor.ForceClose();
        }

        // Breathing already started in Start()
        
        // Wait a bit to ensure potential blink effect is mostly done
        yield return new WaitForSeconds(0.5f);
        
        // STOP BREATHING NOW - User wants it only before wake up
        StopBreathingOnSpeakers();

        activeSequence = null;


    }

    // =============================================
    // STEP 2 - ENTERED SOUND ROOM
    // Called by SoundRoomTrigger
    // =============================================
    public void OnPlayerEnteredSoundRoom()
    {
        if (currentState != StoryState.Exploring)
        {
            Log("OnPlayerEnteredSoundRoom ignored - wrong state: " + currentState);
            return;
        }
        if (isProcessing) return;

        isProcessing = true;
        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(HandleSoundRoomEntry());
    }

    IEnumerator HandleSoundRoomEntry()
    {
        Log("=== STEP 2: ENTERED SOUND ROOM ===");
        AdvanceState(StoryState.EnteredSoundRoom);

        // Wait a beat
        yield return new WaitForSeconds(delayBeforeDoorClose);

        // Auto-close and lock the door
        if (mainDoor != null)
        {
            if (mainDoor.IsDoorOpen())
                mainDoor.ForceClose();
            yield return new WaitForSeconds(0.8f); // Wait for door animation
            mainDoor.Lock();
            Log("Door closed and locked.");
        }

        // Apply room changes while player can't see first room
        if (roomStateChanger != null)
        {
            roomStateChanger.ApplyChanges();
        }

        // Switch speakers to static + play voice line
        yield return new WaitForSeconds(delayBeforeVoiceLine);

        // Switch speakers back to static briefly before voice
        SetAllSpeakersMode(SpeakerMode.Static, 0.5f);
        yield return new WaitForSeconds(0.5f);

        // Play horror scream (was voice line)
        if (horrorScreamClip != null && speakers != null && speakers.Length > 0)
        {
            SpeakerStaticNoise voiceSpeaker = GetFirstValidSpeaker();
            if (voiceSpeaker != null)
            {
                bool voiceFinished = false;
                // Play horror scream louder (0.7f volume)
                voiceSpeaker.PlayVoiceLine(horrorScreamClip, 0.8f, () => { voiceFinished = true; });

                // Wait for voice line to finish (with safety timeout)
                float voiceWait = 0f;
                while (!voiceFinished && voiceWait < maxVoiceLineWait)
                {
                    voiceWait += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else if (voiceLineClip != null && speakers != null && speakers.Length > 0)
        {
             // Fallback to original voice line if horror clip is missing
            SpeakerStaticNoise voiceSpeaker = GetFirstValidSpeaker();
            if (voiceSpeaker != null)
            {
                bool voiceFinished = false;
                voiceSpeaker.PlayVoiceLine(voiceLineClip, 0.4f, () => { voiceFinished = true; });

                // Wait for voice line to finish (with safety timeout)
                float voiceWait = 0f;
                while (!voiceFinished && voiceWait < maxVoiceLineWait)
                {
                    voiceWait += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else
        {
            // No voice clip - generate a creepy radio effect
            TriggerAllSpeakersHeavyStatic(3f);
            yield return new WaitForSeconds(3f);
        }

        // Proceed to darkness
        yield return new WaitForSeconds(delayBeforeDarkness);
        isProcessing = false;
        activeSequence = StartCoroutine(HandleDarkness());
    }

    // =============================================
    // STEP 3 - DARKNESS + ECHO DEVICE
    // =============================================
    IEnumerator HandleDarkness()
    {
        Log("=== STEP 3: DARKNESS ===");
        AdvanceState(StoryState.Darkness);

        // Turn off all scene lights
        SetSoundRoomLights(false);

        // === PITCH BLACK: Kill ambient lighting ===
        SaveAmbientLighting();
        yield return StartCoroutine(FadeAmbientToBlack(1.5f));

        // Speakers go to very quiet static
        if (speakers != null)
        {
            foreach (var speaker in speakers)
            {
                if (speaker != null)
                    speaker.SetVolume(speaker.IsPlaying ? 0.03f : 0f, 1f);
            }
        }

        Log("Room is now pitch black. Waiting for Echo device pickup...");

        // Make Echo device visible with glowing silhouette
        yield return new WaitForSeconds(echoGlowDelay);
        CreateEchoSilhouette();

        activeSequence = null;
    }

    // =============================================
    // STEP 4 - ECHO DEVICE ACQUIRED
    // Called externally (e.g., by EchoPickupItem)
    // NOW ACCEPTS: Darkness, EnteredSoundRoom, or Exploring states
    // =============================================
    public void OnEchoDevicePickedUp()
    {
        // Accept from multiple states to avoid "first pickup ignored" bug
        // The echo can be picked up at any point after story starts
        if (currentState == StoryState.EchoAcquired || 
            currentState == StoryState.ReturnedToRoom || 
            currentState == StoryState.Complete ||
            currentState == StoryState.WaitingForWakeUp)
        {
            Log("OnEchoDevicePickedUp ignored - already past this step or not started. State: " + currentState);
            return;
        }

        Log("=== STEP 4: ECHO ACQUIRED === (from state: " + currentState + ")");
        AdvanceState(StoryState.EchoAcquired);

        // Stop any running sequence (darkness coroutine etc)
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }

        // Unlock the door
        if (mainDoor != null)
        {
            mainDoor.Unlock();
            Log("Door unlocked!");
        }

        // Remove echo silhouette
        RemoveEchoSilhouette();

        // Restore ambient lighting partially (still dark but not pitch black)
        // DISABLED: We want total darkness
        /*
        if (ambientSaved)
        {
            StartCoroutine(FadeAmbientRestore(2f, 0.3f)); // Restore to 30% initially
        }
        */

        // Enable ghost figure (will only show during Echo pulse)
        if (ghostFigure != null)
        {
            ghostFigure.gameObject.SetActive(true);
            Log("Ghost figure enabled.");
        }

        // Restore speakers to low static
        SetAllSpeakersMode(SpeakerMode.Static, 1f);
        if (speakers != null)
        {
            foreach (var speaker in speakers)
            {
                if (speaker != null)
                    speaker.SetVolume(0.05f, 1.5f);
            }
        }
    }

    // =============================================
    // STEP 5 - RETURNED TO FIRST ROOM
    // Called by FirstRoomTrigger
    // =============================================
    public void OnPlayerReturnedToFirstRoom()
    {
        if (currentState != StoryState.EchoAcquired)
        {
            Log("OnPlayerReturnedToFirstRoom ignored - wrong state: " + currentState);
            return;
        }

        Log("=== STEP 5: RETURNED TO ROOM ===");
        AdvanceState(StoryState.ReturnedToRoom);

        // Speakers do a brief heavy static burst (unsettling)
        TriggerAllSpeakersHeavyStatic(1f);

        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(CompleteStoryIntro());
    }

    IEnumerator CompleteStoryIntro()
    {
        yield return new WaitForSeconds(3f);

        Log("=== STORY INTRO COMPLETE ===");
        AdvanceState(StoryState.Complete);

        // Fully restore lighting - DISABLED for horror
        // SetSoundRoomLights(true); 
        
        /*
        if (ambientSaved)
        {
            StartCoroutine(FadeAmbientRestore(2f, 1f)); // Full restore
        }
        */

        // Speakers return to normal static
        SetAllSpeakersMode(SpeakerMode.Static, 2f);
        if (speakers != null)
        {
            foreach (var speaker in speakers)
            {
                if (speaker != null)
                    speaker.ResetVolume(2f);
            }
        }

        activeSequence = null;
    }

    // =============================================
    // AMBIENT LIGHTING CONTROL
    // =============================================

    void SaveAmbientLighting()
    {
        // DISABLED: No need to save, we want permanent darkness
        // ambientSaved = true;
    }

    IEnumerator FadeAmbientToBlack(float duration)
    {
        float startIntensity = RenderSettings.ambientIntensity;
        Color startColor = RenderSettings.ambientLight;
        float startReflection = RenderSettings.reflectionIntensity;

        // Force to flat color mode for full darkness control
        RenderSettings.ambientMode = AmbientMode.Flat;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t; // Ease-in

            RenderSettings.ambientIntensity = Mathf.Lerp(startIntensity, darknessAmbientIntensity, smooth);
            RenderSettings.ambientLight = Color.Lerp(startColor, Color.black, smooth);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, 0f, smooth);

            yield return null;
        }

        RenderSettings.ambientIntensity = darknessAmbientIntensity;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.reflectionIntensity = 0f;

        Log("Ambient faded to pitch black.");
    }

    IEnumerator FadeAmbientRestore(float duration, float restorePercent)
    {
        yield break; // DISABLED: Never restore ambient light
    }

    void RestoreAmbientLighting()
    {
        // DISABLED: Never restore ambient light
    }

    // =============================================
    // ECHO DEVICE SILHOUETTE
    // =============================================

    void CreateEchoSilhouette()
    {
        if (echoPickupObject == null || !echoPickupObject.activeInHierarchy)
        {
            Log("Echo pickup object not found or inactive, skipping silhouette.");
            return;
        }

        // 1. Add emission glow to the echo device itself
        Renderer[] echoRenderers = echoPickupObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in echoRenderers)
        {
            if (rend != null && rend.material != null)
            {
                Material mat = rend.material;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", echoSilhouetteColor * echoSilhouetteIntensity);
                }
                // Also make the base color slightly luminous
                if (mat.HasProperty("_Color"))
                {
                    Color baseColor = mat.color;
                    mat.color = new Color(
                        Mathf.Lerp(baseColor.r, echoSilhouetteColor.r, 0.3f),
                        Mathf.Lerp(baseColor.g, echoSilhouetteColor.g, 0.3f),
                        Mathf.Lerp(baseColor.b, echoSilhouetteColor.b, 0.3f),
                        baseColor.a
                    );
                }
            }
        }

        // 2. Add a dim point light near the echo device for subtle illumination
        echoSilhouetteLight = new GameObject("EchoSilhouetteLight");
        echoSilhouetteLight.transform.SetParent(echoPickupObject.transform);
        echoSilhouetteLight.transform.localPosition = Vector3.up * 0.15f;

        Light pointLight = echoSilhouetteLight.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = echoSilhouetteColor;
        pointLight.intensity = echoPointLightIntensity;
        pointLight.range = echoPointLightRange;
        pointLight.shadows = LightShadows.None;
        pointLight.renderMode = LightRenderMode.Auto;

        // 3. Start pulsing animation
        StartCoroutine(PulseEchoSilhouette(pointLight, echoRenderers));

        Log("Echo device silhouette created with point light and emission glow.");
    }

    IEnumerator PulseEchoSilhouette(Light pointLight, Renderer[] renderers)
    {
        float baseIntensity = echoPointLightIntensity;
        float baseEmission = echoSilhouetteIntensity;

        while (pointLight != null && currentState == StoryState.Darkness)
        {
            // Slow gentle breathing pulse
            float pulse = (Mathf.Sin(Time.time * 1.2f) + 1f) * 0.5f; // 0-1 range
            float multiplier = 0.7f + pulse * 0.6f; // 0.7 - 1.3 range

            if (pointLight != null)
                pointLight.intensity = baseIntensity * multiplier;

            // Pulse emission too
            foreach (Renderer rend in renderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.SetColor("_EmissionColor",
                        echoSilhouetteColor * (baseEmission * multiplier));
                }
            }

            yield return null;
        }
    }

    void RemoveEchoSilhouette()
    {
        if (echoSilhouetteLight != null)
        {
            Destroy(echoSilhouetteLight);
            echoSilhouetteLight = null;
        }
    }

    // =============================================
    // HELPERS
    // =============================================

    void AdvanceState(StoryState newState)
    {
        Log("State: " + currentState + " -> " + newState);
        currentState = newState;
    }

    void SetAllSpeakersMode(SpeakerMode mode, float fadeDuration)
    {
        if (speakers == null) return;
        foreach (var speaker in speakers)
        {
            if (speaker != null)
                speaker.SetMode(mode, fadeDuration);
        }
    }

    void TriggerAllSpeakersHeavyStatic(float duration)
    {
        if (speakers == null) return;
        foreach (var speaker in speakers)
        {
            if (speaker != null)
                speaker.TriggerHeavyStatic(duration);
        }
    }

    void SetSoundRoomLights(bool enabled)
    {
        if (soundRoomLights != null)
        {
            foreach (var light in soundRoomLights)
            {
                if (light != null)
                    light.enabled = enabled;
            }
        }

        if (soundRoomSwitches != null)
        {
            foreach (var sw in soundRoomSwitches)
            {
                if (sw != null)
                    sw.enabled = enabled;
            }
        }
    }

    SpeakerStaticNoise GetFirstValidSpeaker()
    {
        if (speakers == null) return null;
        foreach (var speaker in speakers)
        {
            if (speaker != null) return speaker;
        }
        return null;
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log("[StorySequence] " + message);
    }

    void LogWarning(string message)
    {
        Debug.LogWarning("[StorySequence] " + message);
    }
}
