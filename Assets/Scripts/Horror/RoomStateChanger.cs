using UnityEngine;

/// <summary>
/// ECHOES - Room State Changer
/// Subtly changes room objects to create a gaslighting effect.
/// When triggered (player enters sound room), objects in the first room
/// are rotated/moved so when the player returns, things feel "wrong".
/// 
/// Changes are applied silently and reversibly.
/// Stores original transforms on Start for safe revert.
/// </summary>
public class RoomStateChanger : MonoBehaviour
{
    [System.Serializable]
    public class ObjectChange
    {
        [Tooltip("The object to modify")]
        public Transform target;

        [Tooltip("Rotation to add (Euler angles)")]
        public Vector3 rotationDelta = new Vector3(0f, 180f, 0f);

        [Tooltip("Position offset to add")]
        public Vector3 positionDelta = Vector3.zero;

        [Tooltip("Optional: change scale")]
        public bool changeScale = false;
        public Vector3 newScale = Vector3.one;

        // Runtime state (not serialized)
        [HideInInspector] public Vector3 originalPosition;
        [HideInInspector] public Quaternion originalRotation;
        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public bool stored = false;
    }

    [Header("Objects to Change")]
    [SerializeField] private ObjectChange[] changes;

    [Header("Status")]
    [SerializeField] private bool changesApplied = false;

    /// <summary>
    /// Whether changes have been applied.
    /// </summary>
    public bool IsChangesApplied => changesApplied;

    void Start()
    {
        StoreOriginalTransforms();
    }

    void StoreOriginalTransforms()
    {
        if (changes == null) return;

        int stored = 0;
        foreach (var change in changes)
        {
            if (change == null || change.target == null)
            {
                Debug.LogWarning("[RoomStateChanger] Null entry in changes array!");
                continue;
            }

            change.originalPosition = change.target.localPosition;
            change.originalRotation = change.target.localRotation;
            change.originalScale = change.target.localScale;
            change.stored = true;
            stored++;
        }

        Debug.Log("[RoomStateChanger] Stored " + stored + " original transforms.");
    }

    /// <summary>
    /// Apply all changes (called when player can't see the room).
    /// Safe to call multiple times - only applies once.
    /// </summary>
    public void ApplyChanges()
    {
        if (changesApplied) return;
        if (changes == null || changes.Length == 0)
        {
            Debug.LogWarning("[RoomStateChanger] No changes configured!");
            return;
        }

        int applied = 0;
        foreach (var change in changes)
        {
            if (change == null || change.target == null)
            {
                Debug.LogWarning("[RoomStateChanger] Skipping null change entry.");
                continue;
            }

            // Store originals if not already stored (safety)
            if (!change.stored)
            {
                change.originalPosition = change.target.localPosition;
                change.originalRotation = change.target.localRotation;
                change.originalScale = change.target.localScale;
                change.stored = true;
            }

            // Apply rotation
            if (change.rotationDelta != Vector3.zero)
                change.target.localRotation *= Quaternion.Euler(change.rotationDelta);

            // Apply position offset
            if (change.positionDelta != Vector3.zero)
                change.target.localPosition += change.positionDelta;

            // Apply scale if requested
            if (change.changeScale)
                change.target.localScale = change.newScale;

            applied++;
        }

        changesApplied = true;
        Debug.Log("[RoomStateChanger] Applied " + applied + " changes.");
    }

    /// <summary>
    /// Revert all changes back to original (for testing/debugging).
    /// </summary>
    public void RevertChanges()
    {
        if (!changesApplied) return;
        if (changes == null) return;

        int reverted = 0;
        foreach (var change in changes)
        {
            if (change == null || change.target == null || !change.stored) continue;

            change.target.localPosition = change.originalPosition;
            change.target.localRotation = change.originalRotation;
            change.target.localScale = change.originalScale;
            reverted++;
        }

        changesApplied = false;
        Debug.Log("[RoomStateChanger] Reverted " + reverted + " changes.");
    }

    /// <summary>
    /// Get the number of configured changes.
    /// </summary>
    public int ChangeCount => changes != null ? changes.Length : 0;

    void OnDrawGizmosSelected()
    {
        if (changes == null) return;

        foreach (var change in changes)
        {
            if (change == null || change.target == null) continue;

            // Show original position -> changed position
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(change.target.position, 0.2f);

            if (change.positionDelta != Vector3.zero)
            {
                Vector3 changedPos = change.target.position + change.positionDelta;
                Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
                Gizmos.DrawWireSphere(changedPos, 0.2f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(change.target.position, changedPos);
            }
        }
    }
}
