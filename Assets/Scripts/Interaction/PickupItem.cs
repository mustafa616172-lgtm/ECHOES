using UnityEngine;

/// <summary>
/// Attach to any GameObject to make it a collectible item.
/// Player presses E to pick it up (uses IInteractable).
/// Supports: Key, Battery, Note, HealthKit, Misc.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    public string itemID = "item_01";
    public string displayName = "Gizemli Obje";
    public InventorySystem.ItemType itemType = InventorySystem.ItemType.Misc;
    
    [Header("Note Content (only for Notes)")]
    [TextArea(3, 10)]
    public string noteContent = "";
    
    [Header("Quantity")]
    public int quantity = 1;
    
    [Header("Visual")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.1f;
    
    [Header("Sound")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupSoundRadius = 5f;
    
    private Vector3 startPosition;
    private Renderer objectRenderer;
    private Color originalEmission;
    private bool collected = false;
    
    void Start()
    {
        startPosition = transform.position;
        objectRenderer = GetComponentInChildren<Renderer>();
        
        // Save original emission
        if (objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            originalEmission = objectRenderer.material.GetColor("_EmissionColor");
        }
    }
    
    void Update()
    {
        if (collected) return;
        
        // Gentle bobbing animation
        if (bobHeight > 0)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
        
        // Subtle glow pulse
        if (enableGlow && objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) / 2f;
            Color emission = glowColor * (0.3f + pulse * 0.5f);
            objectRenderer.material.SetColor("_EmissionColor", emission);
            objectRenderer.material.EnableKeyword("_EMISSION");
        }
    }
    
    public void Interact()
    {
        if (collected) return;
        collected = true;
        
        // Add to inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(itemID, displayName, itemType, noteContent, quantity);
        }
        else
        {
            Debug.LogWarning("[PickupItem] InventorySystem not found!");
            // Fallback to KeyInventory for keys
            if (itemType == InventorySystem.ItemType.Key && KeyInventory.Instance != null)
            {
                KeyInventory.Instance.AddKey(itemID);
            }
        }
        
        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Emit sound event (enemies can hear item pickup)
        if (SoundManager.Instance != null && pickupSoundRadius > 0)
        {
            SoundManager.Instance.EmitSound(transform.position, pickupSoundRadius,
                SoundManager.SoundType.ObjectDrop);
        }
        
        // Restore emission and destroy
        if (objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            objectRenderer.material.SetColor("_EmissionColor", originalEmission);
        }
        
        Debug.Log($"[PickupItem] Collected: {displayName}");
        Destroy(gameObject);
    }
    
    public string GetInteractionPrompt()
    {
        switch (itemType)
        {
            case InventorySystem.ItemType.Key:
                return $"[E] {displayName} al (Anahtar)";
            case InventorySystem.ItemType.Battery:
                return $"[E] {displayName} al (Pil)";
            case InventorySystem.ItemType.Note:
                return $"[E] {displayName} oku";
            case InventorySystem.ItemType.HealthKit:
                return $"[E] {displayName} al (Saglik)";
            default:
                return $"[E] {displayName} al";
        }
    }
    
    // Auto-set glow color based on item type
    void OnValidate()
    {
        switch (itemType)
        {
            case InventorySystem.ItemType.Key:
                glowColor = new Color(1f, 0.85f, 0.2f);
                break;
            case InventorySystem.ItemType.Battery:
                glowColor = new Color(0.3f, 0.9f, 1f);
                break;
            case InventorySystem.ItemType.Note:
                glowColor = new Color(0.9f, 0.85f, 0.7f);
                break;
            case InventorySystem.ItemType.HealthKit:
                glowColor = new Color(0.2f, 1f, 0.2f);
                break;
        }
    }
}
