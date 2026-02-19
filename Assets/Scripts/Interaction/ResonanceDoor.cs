using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ResonanceDoor : MonoBehaviour, IInteractable
{
    [Header("Puzzle Settings")]
    public float requiredFrequency = 440f;
    public float tolerance = 15f;
    public float detectionRange = 8f;
    public float unlockDuration = 2.0f;

    [Header("Visual Feedback")]
    public float vibrationIntensity = 0.03f;
    public Transform doorModel;
    
    [Header("Audio")]
    public AudioClip resonanceHum;

    // State
    private bool isUnlocked = false;
    private bool isInteracting = false;
    private float matchTimer = 0f;
    private float currentQuality = 0f;
    private Vector3 originalPosition;
    private AudioSource doorAudio;
    private ResonanceDoorEffects doorEffects;

    void Start()
    {
        if (doorModel == null) doorModel = transform;
        originalPosition = doorModel.localPosition;
        
        doorAudio = GetComponent<AudioSource>();
        if (doorAudio != null)
        {
            doorAudio.loop = true;
            doorAudio.playOnAwake = false;
            doorAudio.spatialBlend = 1f;
            doorAudio.volume = 0f;
            
            if (resonanceHum != null)
                doorAudio.clip = resonanceHum;
        }
        
        // Remove competing IInteractable components so InteractionController finds US
        RemoveCompetingInteractables();
        
        // Ensure collider exists for raycast detection
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;
            Debug.Log("[ResonanceDoor] Auto-added BoxCollider.");
        }

        // Auto-find ResonanceUI if not available
        if (ResonanceUI.Instance == null)
        {
            ResonanceUI foundUI = FindObjectOfType<ResonanceUI>(true);
            if (foundUI != null)
            {
                ResonanceUI.Instance = foundUI;
                Debug.Log("[ResonanceDoor] Found ResonanceUI and assigned Instance.");
            }
            else
            {
                Debug.LogWarning("[ResonanceDoor] NO ResonanceUI in scene! Run Tools > ECHOES > Setup Resonance UI");
            }
        }
        
        Debug.Log("[ResonanceDoor] Init on '" + gameObject.name + "'. Freq=" + requiredFrequency + "Hz, UI=" + (ResonanceUI.Instance != null));

        // Auto-attach effects companion
        doorEffects = GetComponent<ResonanceDoorEffects>();
        if (doorEffects == null)
            doorEffects = gameObject.AddComponent<ResonanceDoorEffects>();

        // Auto-create screen effects singleton if not in scene
        if (ResonanceScreenEffects.Instance == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                var screenFx = cam.gameObject.AddComponent<ResonanceScreenEffects>();
                Debug.Log("[ResonanceDoor] Auto-added ResonanceScreenEffects to main camera.");
            }
        }
    }

    private void RemoveCompetingInteractables()
    {
        // Use DestroyImmediate so they're gone THIS frame
        // Check SpeakerDoorLink FIRST - it depends on DoorInteractable
        var speakerLinks = GetComponents<SpeakerDoorLink>();
        foreach (var s in speakerLinks)
        {
            Debug.Log($"[ResonanceDoor] Removing SpeakerDoorLink from '{gameObject.name}'");
            DestroyImmediate(s);
        }
        
        var controllers = GetComponents<DoorController>();
        foreach (var c in controllers)
        {
            Debug.Log($"[ResonanceDoor] Removing DoorController from '{gameObject.name}'");
            DestroyImmediate(c);
        }
        
        var doorInteractables = GetComponents<DoorInteractable>();
        foreach (var d in doorInteractables)
        {
            Debug.Log($"[ResonanceDoor] Removing DoorInteractable from '{gameObject.name}'");
            DestroyImmediate(d);
        }

        // Also check children
        var childControllers = GetComponentsInChildren<DoorController>();
        foreach (var c in childControllers)
        {
            Debug.Log($"[ResonanceDoor] Removing child DoorController from '{c.gameObject.name}'");
            DestroyImmediate(c);
        }
        
        var childDoorInteractables = GetComponentsInChildren<DoorInteractable>();
        foreach (var d in childDoorInteractables)
        {
            Debug.Log($"[ResonanceDoor] Removing child DoorInteractable from '{d.gameObject.name}'");
            DestroyImmediate(d);
        }
    }

    // ==========================================
    // IInteractable
    // ==========================================

    public string GetInteractionPrompt()
    {
        if (isUnlocked) return "";
        
        if (SoundRecorderDevice.Instance == null || !SoundRecorderDevice.Instance.IsActive)
            return "[E] Locked (Device Required)";
        
        if (isInteracting)
            return "[E] Stop Tuning";
        
        return "[E] Tune Frequency";
    }

    public void Interact()
    {
        Debug.Log($"[ResonanceDoor] Interact()! unlocked={isUnlocked}, interacting={isInteracting}");
        
        if (isUnlocked) return;

        if (SoundRecorderDevice.Instance == null || !SoundRecorderDevice.Instance.IsActive)
        {
            if (InteractionUI.Instance != null)
                InteractionUI.Instance.ShowMessage("You need the Sound Recorder Device!", 2f);
            return;
        }

        if (isInteracting)
        {
            StopInteraction();
        }
        else
        {
            StartInteraction();
        }
    }

    private void StartInteraction()
    {
        isInteracting = true;
        Debug.Log("[ResonanceDoor] Starting interaction. Opening UI...");

        if (ResonanceUI.Instance != null)
            ResonanceUI.Instance.OpenInteraction(this);
        else
            Debug.LogError("[ResonanceDoor] ResonanceUI.Instance is NULL! Cannot open UI.");
    }

    private void StopInteraction()
    {
        Debug.Log("[ResonanceDoor] Stopping interaction.");
        isInteracting = false;
        matchTimer = 0f;
        currentQuality = 0f;

        if (SoundRecorderDevice.Instance != null)
            SoundRecorderDevice.Instance.SetResonance(0f);

        if (ResonanceUI.Instance != null && ResonanceUI.Instance.IsOpen)
            ResonanceUI.Instance.CloseInteraction();

        if (doorModel != null)
            doorModel.localPosition = originalPosition;

        if (doorAudio != null && doorAudio.isPlaying)
            doorAudio.Stop();
    }

    /// <summary>
    /// Called by ResonanceUI.CloseInteraction() to sync state.
    /// Prevents ResonanceDoor Update from running after UI is closed.
    /// </summary>
    public void OnUIClosed()
    {
        if (isInteracting)
        {
            Debug.Log("[ResonanceDoor] OnUIClosed - syncing state.");
            isInteracting = false;
            matchTimer = 0f;
            currentQuality = 0f;
            
            if (SoundRecorderDevice.Instance != null)
                SoundRecorderDevice.Instance.SetResonance(0f);
            
            if (doorModel != null)
                doorModel.localPosition = originalPosition;
            
            if (doorAudio != null && doorAudio.isPlaying)
                doorAudio.Stop();
        }
    }

    // ==========================================
    // Update - only while interacting
    // ==========================================

    void Update()
    {
        if (isUnlocked || !isInteracting) return;

        SoundRecorderDevice device = SoundRecorderDevice.Instance;
        if (device == null || !device.IsActive)
        {
            StopInteraction();
            return;
        }

        // Auto-close if player walks away
        float distance = Vector3.Distance(transform.position, device.transform.position);
        if (distance > detectionRange)
        {
            StopInteraction();
            return;
        }

        // Calculate quality (purely frequency-based)
        float freqDiff = Mathf.Abs(device.CurrentFrequency - requiredFrequency);
        
        if (freqDiff <= tolerance)
        {
            float t = 1f - (freqDiff / tolerance);
            currentQuality = 0.9f + (t * 0.1f);
        }
        else if (freqDiff <= tolerance * 10f)
        {
            float t = 1f - ((freqDiff - tolerance) / (tolerance * 9f));
            currentQuality = t * 0.9f;
        }
        else
        {
            currentQuality = 0f;
        }

        device.SetResonance(currentQuality);
        ApplyVibration();
        UpdateDoorAudio();

        // Match timer
        if (freqDiff <= tolerance)
        {
            matchTimer += Time.deltaTime;
            if (matchTimer >= unlockDuration)
            {
                Unlock();
                return;
            }
        }
        else
        {
            matchTimer = Mathf.Max(0f, matchTimer - Time.deltaTime * 0.5f);
        }
        
        if (ResonanceUI.Instance != null && ResonanceUI.Instance.IsOpen)
            ResonanceUI.Instance.UpdateMatchProgress(matchTimer / unlockDuration, currentQuality);
    }

    // ==========================================
    // Feedback
    // ==========================================

    private void ApplyVibration()
    {
        if (currentQuality > 0.1f)
        {
            float shake = currentQuality * vibrationIntensity;
            float shakeSpeed = 20f + (currentQuality * 40f);
            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * shakeSpeed) * shake,
                Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shake * 0.5f,
                Mathf.Sin(Time.time * shakeSpeed * 0.7f) * shake * 0.3f
            );
            doorModel.localPosition = originalPosition + offset;
        }
        else
        {
            doorModel.localPosition = Vector3.Lerp(doorModel.localPosition, originalPosition, Time.deltaTime * 10f);
        }
    }

    private void UpdateDoorAudio()
    {
        if (doorAudio == null) return;
        
        doorAudio.volume = Mathf.Lerp(doorAudio.volume, currentQuality * 0.6f, Time.deltaTime * 5f);
        doorAudio.pitch = 0.8f + (currentQuality * 0.4f);
        
        if (currentQuality > 0.05f && !doorAudio.isPlaying && resonanceHum != null)
            doorAudio.Play();
        else if (currentQuality <= 0.05f && doorAudio.isPlaying)
            doorAudio.Stop();
    }

    // ==========================================
    // Unlock
    // ==========================================

    public void Unlock()
    {
        if (isUnlocked) return;
        
        isUnlocked = true;
        isInteracting = false;
        Debug.Log("[ResonanceDoor] UNLOCKED! Resonance achieved!");

        if (SoundRecorderDevice.Instance != null)
            SoundRecorderDevice.Instance.SetResonance(0f);

        if (ResonanceUI.Instance != null)
            ResonanceUI.Instance.CloseInteraction();
        
        StartCoroutine(DestructionSequence());
    }

    /// <summary>Stop all effects when door unlocks</summary>
    private void CleanupEffects()
    {
        if (doorEffects != null)
            doorEffects.StopAllEffects();
    }

    private IEnumerator DestructionSequence()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startScale = doorModel.localScale;
        Quaternion startRotation = doorModel.localRotation;
        
        if (doorAudio != null)
        {
            doorAudio.volume = 1f;
            doorAudio.pitch = 1.5f;
        }

        CleanupEffects();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float shakeAmount = Mathf.Lerp(0.02f, 0.15f, t);
            float shakeSpeed = 30f + (t * 60f);
            
            doorModel.localPosition = originalPosition + new Vector3(
                Mathf.Sin(Time.time * shakeSpeed) * shakeAmount,
                Mathf.Cos(Time.time * shakeSpeed * 1.4f) * shakeAmount * 0.5f,
                Mathf.Sin(Time.time * shakeSpeed * 0.8f) * shakeAmount * 0.3f
            );
            
            if (t > 0.6f)
            {
                float scaleT = (t - 0.6f) / 0.4f;
                doorModel.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);
                float twist = scaleT * 15f;
                doorModel.localRotation = startRotation * Quaternion.Euler(
                    Random.Range(-twist, twist),
                    Random.Range(-twist, twist),
                    Random.Range(-twist, twist)
                );
            }
            
            if (doorAudio != null && t > 0.7f)
                doorAudio.volume = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            
            yield return null;
        }
        
        if (doorAudio != null) doorAudio.Stop();
        Destroy(doorModel.gameObject);
        if (doorModel == transform) Destroy(gameObject);
    }

    public float MatchProgress => Mathf.Clamp01(matchTimer / unlockDuration);
    public float CurrentQuality => currentQuality;
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
