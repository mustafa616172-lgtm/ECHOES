using UnityEngine;

/// <summary>
/// ECHOES - First Room Return Trigger
/// Detects when the player returns to the first room after acquiring Echo device.
/// Notifies StorySequenceManager to apply gaslighting effect (Step 5).
/// 
/// Supports both Rigidbody (OnTriggerEnter) and CharacterController.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class FirstRoomTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
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
                col.size = new Vector3(2f, 3f, 1f);
        }

        storyManager = StorySequenceManager.Instance;
        if (storyManager == null)
            storyManager = FindFirstObjectByType<StorySequenceManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (other == null) return;

        if (!IsPlayer(other)) return;

        hasTriggered = true;
        Debug.Log("[FirstRoomTrigger] Player returned to first room!");

        if (storyManager == null)
            storyManager = StorySequenceManager.Instance;

        if (storyManager != null)
        {
            storyManager.OnPlayerReturnedToFirstRoom();
        }
        else
        {
            Debug.LogError("[FirstRoomTrigger] StorySequenceManager is null!");
        }
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        if (other.GetComponent<CharacterController>() != null) return true;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        return false;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log("[FirstRoomTrigger] Trigger reset.");
    }

    public bool HasTriggered => hasTriggered;

    void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(col.center, col.size);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            hasTriggered ? "First Room (TRIGGERED)" : "First Room Trigger");
#endif
    }
}
