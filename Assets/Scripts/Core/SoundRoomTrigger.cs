using UnityEngine;

/// <summary>
/// ECHOES - Sound Room Trigger
/// Detects when player enters the sound room and notifies the StorySequenceManager.
/// One-shot trigger that disables itself after firing.
/// Place at the sound room entrance with a BoxCollider (Is Trigger = true).
/// 
/// Supports both Rigidbody (OnTriggerEnter) and CharacterController (OnTriggerEnter via CC).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SoundRoomTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag to detect (should be Player)")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Auto-configure collider as trigger on Start")]
    [SerializeField] private bool autoSetup = true;

    private bool hasTriggered = false;
    private StorySequenceManager storyManager;

    void Start()
    {
        if (autoSetup)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            col.isTrigger = true;
            if (col.size == Vector3.one)
            {
                col.size = new Vector3(2f, 3f, 1f);
            }
        }

        // Use singleton first, fallback to search
        storyManager = StorySequenceManager.Instance;
        if (storyManager == null)
            storyManager = FindFirstObjectByType<StorySequenceManager>();

        if (storyManager == null)
        {
            Debug.LogWarning("[SoundRoomTrigger] No StorySequenceManager found in scene!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (other == null) return;

        // Check tag or CharacterController presence
        if (!IsPlayer(other)) return;

        hasTriggered = true;
        Debug.Log("[SoundRoomTrigger] Player entered sound room!");

        // Re-check in case manager was created after Start()
        if (storyManager == null)
            storyManager = StorySequenceManager.Instance;

        if (storyManager != null)
        {
            storyManager.OnPlayerEnteredSoundRoom();
        }
        else
        {
            Debug.LogError("[SoundRoomTrigger] StorySequenceManager is null - cannot trigger step 2!");
        }
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;

        // Fallback: check for CharacterController or PlayerController
        if (other.GetComponent<CharacterController>() != null) return true;
        if (other.GetComponentInParent<PlayerController>() != null) return true;

        return false;
    }

    /// <summary>
    /// Reset the trigger (for testing/debugging)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log("[SoundRoomTrigger] Trigger reset.");
    }

    public bool HasTriggered => hasTriggered;

    void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireCube(col.center, col.size);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            hasTriggered ? "Sound Room (TRIGGERED)" : "Sound Room Trigger");
#endif
    }
}
