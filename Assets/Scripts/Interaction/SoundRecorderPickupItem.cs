using UnityEngine;

/// <summary>
/// ECHOES - Sound Recorder Pickup Item
/// Interaction script to pick up the Sound Recorder Device.
/// </summary>
public class SoundRecorderPickupItem : MonoBehaviour, IInteractable
{
    [Header("Device Settings")]
    [SerializeField] private GameObject devicePrefab;
    [SerializeField] private string displayName = "Ses Kayit Cihazi";
    
    [Header("Visual")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.2f, 0.2f, 1f); // Reddish glow
    [SerializeField] private float bobSpeed = 1.5f;
    [SerializeField] private float bobHeight = 0.05f;
    
    [Header("Sound")]
    [SerializeField] private AudioClip pickupSound;

    private Vector3 startPosition;
    private Renderer objectRenderer;
    private bool collected = false;
    
    void Start()
    {
        startPosition = transform.position;
        objectRenderer = GetComponentInChildren<Renderer>();
        
        if (objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            objectRenderer.material.EnableKeyword("_EMISSION");
        }
    }
    
    void Update()
    {
        if (collected) return;
        
        // Bobbing animation
        if (bobHeight > 0)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
        
        // Pulsing glow
        if (enableGlow && objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            Color emission = glowColor * (0.5f + pulse * 0.8f);
            objectRenderer.material.SetColor("_EmissionColor", emission);
        }
    }
    
    public void Interact()
    {
        if (collected) return;
        collected = true;
        
        Debug.Log($"[SoundRecorderPickup] Picked up {displayName}");
        
        // Equip to player
        if (EquipToPlayer())
        {
            // Play sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            // Notify story/UI
            if (StorySequenceManager.Instance != null)
            {
               // StorySequenceManager.Instance.OnSoundRecorderPickedUp(); // If this method exists
            }
            
            // Disable this object (it's now "in hand")
            gameObject.SetActive(false);
        }
        else
        {
            collected = false; // Failed to equip
        }
    }
    
    bool EquipToPlayer()
    {
        // Find player (same logic as EchoPickup)
        GameObject player = FindPlayer();
        if (player == null) return false;
        
        SoundRecorderDevice recorder = player.GetComponent<SoundRecorderDevice>();
        if (recorder == null)
        {
            recorder = player.AddComponent<SoundRecorderDevice>();
        }
        
        recorder.EquipDevice(devicePrefab);
        recorder.SetOriginalSceneObject(gameObject);
        
        // Add to inventory (if system exists)
        if (InventorySystem.Instance != null)
        {
             // Check if thumbnail generator exists, otherwise null
             Sprite icon = null; // Placeholder
             InventorySystem.Instance.AddItem("sound_recorder", displayName, 
                 InventorySystem.ItemType.SoundRecorder, 
                 "Frekanslari kaydedip tekrar yayabilen cihaz.\n[Q] Ac/Kapa\n[Mouse Scroll] Frekans Ayari\n[G] Birak",
                 icon);
        }
        
        return true;
    }
    
    GameObject FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;
        
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) return pc.gameObject;
        
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.parent != null)
            return mainCam.transform.parent.gameObject;

        return null;
    }
    
    public string GetInteractionPrompt()
    {
        return $"[E] {displayName} Al";
    }
    
    void OnEnable()
    {
        if (collected)
        {
            collected = false;
            startPosition = transform.position;
            Debug.Log("[SoundRecorderPickup] Re-enabled (Dropped)");
        }
    }
}
