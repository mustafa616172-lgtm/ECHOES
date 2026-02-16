using UnityEngine;

/// <summary>
/// ECHOES - Speaker Door Link
/// Attach to the door (Kapimentese). When the door opens,
/// all linked speakers get a slight volume boost.
/// When the door closes, speakers return to base volume.
/// 
/// Auto-finds speakers if none are manually assigned.
/// Works with DoorInteractable's isOpen state via Update polling.
/// </summary>
public class SpeakerDoorLink : MonoBehaviour
{
    [Header("Linked Speakers")]
    [Tooltip("Speakers affected by this door. Auto-found if empty.")]
    [SerializeField] private SpeakerStaticNoise[] speakers;

    [Header("Volume Settings")]
    [Tooltip("Volume multiplier when door is open (1.0 = no change)")]
    [Range(1f, 3f)]
    [SerializeField] private float openVolumeMultiplier = 1.4f;

    [Tooltip("Fade duration for volume changes")]
    [Range(0.1f, 3f)]
    [SerializeField] private float fadeDuration = 0.8f;

    private DoorInteractable doorInteractable;
    private bool lastKnownDoorState = false;
    private bool initialized = false;

    void Start()
    {
        doorInteractable = GetComponent<DoorInteractable>();
        if (doorInteractable == null)
        {
            doorInteractable = GetComponentInParent<DoorInteractable>();
        }
        if (doorInteractable == null)
        {
            doorInteractable = GetComponentInChildren<DoorInteractable>();
        }

        if (doorInteractable == null)
        {
            Debug.LogWarning("[SpeakerDoorLink] No DoorInteractable found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Auto-find speakers if not assigned
        if (speakers == null || speakers.Length == 0)
        {
            speakers = FindObjectsOfType<SpeakerStaticNoise>();
            Debug.Log("[SpeakerDoorLink] Auto-found " + speakers.Length + " speakers");
        }

        // Get initial door state via reflection or field
        lastKnownDoorState = IsDoorOpen();
        initialized = true;

        Debug.Log("[SpeakerDoorLink] Initialized on " + gameObject.name + " with " + speakers.Length + " speakers, door open: " + lastKnownDoorState);
    }

    void Update()
    {
        if (!initialized || doorInteractable == null) return;

        bool currentState = IsDoorOpen();
        if (currentState != lastKnownDoorState)
        {
            lastKnownDoorState = currentState;
            OnDoorStateChanged(currentState);
        }
    }

    void OnDoorStateChanged(bool isOpen)
    {
        if (speakers == null) return;

        if (isOpen)
        {
            Debug.Log("[SpeakerDoorLink] Door opened - boosting speaker volume x" + openVolumeMultiplier);
            foreach (var speaker in speakers)
            {
                if (speaker != null && speaker.IsPlaying)
                {
                    speaker.BoostVolume(openVolumeMultiplier, fadeDuration);
                }
            }
        }
        else
        {
            Debug.Log("[SpeakerDoorLink] Door closed - resetting speaker volume");
            foreach (var speaker in speakers)
            {
                if (speaker != null && speaker.IsPlaying)
                {
                    speaker.ResetVolume(fadeDuration);
                }
            }
        }
    }

    /// <summary>
    /// Check if the door is currently open by reading its rotation state.
    /// Uses the interaction prompt as a proxy (if it says "Close" the door is open).
    /// </summary>
    bool IsDoorOpen()
    {
        if (doorInteractable == null) return false;
        string prompt = doorInteractable.GetInteractionPrompt();
        // If prompt contains "Close" or "Kapat", the door is currently open
        return prompt.Contains("Close") || prompt.Contains("Kapat");
    }
}
