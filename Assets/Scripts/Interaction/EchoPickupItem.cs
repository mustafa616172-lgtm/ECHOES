using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ECHOES - Echo Device Pickup Item
/// Special pickup script for the Echo device that equips it to the player.
/// Directly handles door unlock/open and ambient lighting as safety fallback.
/// </summary>
public class EchoPickupItem : MonoBehaviour, IInteractable
{
    [Header("Echo Device Settings")]
    [SerializeField] private GameObject echoDevicePrefab;
    [SerializeField] private string displayName = "Echo Cihazi";
    
    [Header("Visual")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private float bobSpeed = 1.5f;
    [SerializeField] private float bobHeight = 0.05f;
    
    [Header("Sound")]
    [SerializeField] private AudioClip pickupSound;

    [Header("Door Control")]
    [Tooltip("Directly unlock and open all locked doors when Echo is picked up")]
    [SerializeField] private bool autoUnlockDoors = true;
    [Tooltip("Specific door to unlock/open (auto-finds all if empty)")]
    [SerializeField] private DoorInteractable targetDoor;

    [Header("Ambient Restore")]
    [Tooltip("Restore ambient lighting when Echo is picked up")]
    [SerializeField] private bool restoreAmbientOnPickup = false;
    
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

        Debug.Log("[EchoPickupItem] Initialized on: " + gameObject.name + " at position " + transform.position);
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
        
        // Pulsing glow effect
        if (enableGlow && objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            Color emission = glowColor * (0.5f + pulse * 0.8f);
            objectRenderer.material.SetColor("_EmissionColor", emission);
        }
    }
    
    /// <summary>
    /// Called when player presses E to pick up the Echo device.
    /// </summary>
    public void Interact()
    {
        if (collected)
        {
            Debug.LogWarning("[EchoPickupItem] Already collected, ignoring.");
            return;
        }
        collected = true;
        
        Debug.Log("========================================");
        Debug.Log("[EchoPickupItem] ECHO DEVICE PICKED UP!");
        Debug.Log("========================================");
        
        // === STEP 1: ALWAYS unlock/open doors - this runs NO MATTER WHAT ===
        if (autoUnlockDoors)
        {
            ForceUnlockAndOpenDoors();
        }

        // === STEP 2: ALWAYS restore ambient lighting - REMOVED FOR HORROR ===
        /*
        if (restoreAmbientOnPickup)
        {
            RestoreAmbientLighting();
        }
        */

        // === STEP 3: Notify story system (if exists) ===
        NotifyStorySystem();

        // === STEP 4: Equip echo device to player ===
        EquipToPlayer();
        
        // === STEP 5: Play pickup sound ===
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // === STEP 6: Hide the pickup item ===
        gameObject.SetActive(false);
        Debug.Log("[EchoPickupItem] Pickup complete. Object deactivated.");
    }

    /// <summary>
    /// Find ALL locked doors and force them unlocked. 
    /// This runs ALWAYS, regardless of story state or player finding.
    /// </summary>
    void ForceUnlockAndOpenDoors()
    {
        Debug.Log("[EchoPickupItem] --- DOOR UNLOCK START ---");

        // Method 1: Specific target door
        if (targetDoor != null)
        {
            Debug.Log("[EchoPickupItem] Target door found: " + targetDoor.gameObject.name + 
                       " locked=" + targetDoor.IsDoorLocked());
            targetDoor.Unlock();
            Debug.Log("[EchoPickupItem] Target door UNLOCKED: " + targetDoor.gameObject.name);
        }

        // Method 2: Find ALL doors in scene
        DoorInteractable[] allDoors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
        Debug.Log("[EchoPickupItem] Found " + allDoors.Length + " doors in scene.");

        int unlockedCount = 0;
        foreach (DoorInteractable door in allDoors)
        {
            if (door == null) continue;

            bool wasLocked = door.IsDoorLocked();
            if (wasLocked)
            {
                door.Unlock();
                unlockedCount++;
                Debug.Log("[EchoPickupItem] UNLOCKED door: " + door.gameObject.name);
            }
            else
            {
                Debug.Log("[EchoPickupItem] Door already unlocked: " + door.gameObject.name);
            }
        }
        
        Debug.Log("[EchoPickupItem] --- DOOR UNLOCK COMPLETE: " + unlockedCount + " doors unlocked ---");
    }

    /// <summary>
    /// Restore ambient lighting if it was darkened by story system.
    /// </summary>
    void RestoreAmbientLighting()
    {
        // DISABLED FOR COMPLETE DARKNESS
        Debug.Log("[EchoPickupItem] Ambient restoration intentionally disabled.");
    }

    /// <summary>
    /// Notify StorySequenceManager if it exists.
    /// </summary>
    void NotifyStorySystem()
    {
        StorySequenceManager storyManager = StorySequenceManager.Instance;
        if (storyManager == null)
            storyManager = FindFirstObjectByType<StorySequenceManager>();
        
        if (storyManager != null)
        {
            Debug.Log("[EchoPickupItem] StorySequenceManager found. Current state: " + storyManager.CurrentState);
            storyManager.OnEchoDevicePickedUp();
            Debug.Log("[EchoPickupItem] Story system notified. New state: " + storyManager.CurrentState);
        }
        else
        {
            Debug.LogWarning("[EchoPickupItem] No StorySequenceManager in scene - door unlock handled directly.");
        }
    }

    /// <summary>
    /// Find and equip the echo device to the player.
    /// </summary>
    void EquipToPlayer()
    {
        GameObject player = FindPlayer();
        
        if (player == null)
        {
            Debug.LogError("[EchoPickupItem] PLAYER NOT FOUND! Echo device not equipped.");
            Debug.LogError("[EchoPickupItem] Make sure Player has Tag='Player' or PlayerController component");
            return;
        }
        
        Debug.Log("[EchoPickupItem] Player found: " + player.name);
        
        EchoDevice echoDevice = player.GetComponent<EchoDevice>();
        if (echoDevice == null)
        {
            echoDevice = player.AddComponent<EchoDevice>();
            Debug.Log("[EchoPickupItem] Added EchoDevice component to player");
        }
        
        echoDevice.EquipEchoDevice(echoDevicePrefab);
        echoDevice.SetOriginalSceneObject(gameObject);

        // Generate thumbnail
        Sprite thumbnail = null;
        if (echoDevicePrefab != null)
            thumbnail = ItemThumbnailGenerator.GenerateThumbnail(echoDevicePrefab, 128);
        else
            thumbnail = ItemThumbnailGenerator.GenerateThumbnail(gameObject, 128);
        
        if (thumbnail == null)
            thumbnail = ItemThumbnailGenerator.GenerateColorIcon(new Color(0.2f, 0.8f, 1f), 64);
        
        // Add to inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem("echo_device", displayName, 
                InventorySystem.ItemType.EchoDevice, 
                "Ses dalgalariyla cevreni tarayabilen cihaz.\n[Q] Yanki Dalgasi\n[Mouse Scroll] Frekans Ayari\n[G] Birak",
                thumbnail);
            Debug.Log("[EchoPickupItem] Echo device added to inventory.");
        }
        
        // Show tutorial
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.ShowPrompt("Echo Cihazi Alindi!\n[Q] Yanki Dalgasi\n[Mouse Scroll] Frekans Ayari\n[G] Birak");
        }
        
        Debug.Log("[EchoPickupItem] " + displayName + " equipped successfully!");
    }
    
    /// <summary>
    /// Called when the device is dropped and this object is re-enabled.
    /// </summary>
    void OnEnable()
    {
        if (collected)
        {
            collected = false;
            startPosition = transform.position;
            Debug.Log("[EchoPickupItem] Re-enabled after drop - ready for pickup again");
        }
    }
    
    GameObject FindPlayer()
    {
        // Method 1: Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;
        
        // Method 2: PlayerController
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) return pc.gameObject;
        
        // Method 3: Camera parent
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.parent != null)
            return mainCam.transform.parent.gameObject;

        // Method 4: CharacterController
        CharacterController cc = FindFirstObjectByType<CharacterController>();
        if (cc != null) return cc.gameObject;
        
        return null;
    }
    
    public string GetInteractionPrompt()
    {
        return "[E] " + displayName + " Al";
    }
}
