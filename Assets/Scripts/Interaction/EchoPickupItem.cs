using UnityEngine;

/// <summary>
/// ECHOES - Echo Device Pickup Item
/// Special pickup script for the Echo device that equips it to the player.
/// </summary>
public class EchoPickupItem : MonoBehaviour, IInteractable
{
    [Header("Echo Device Settings")]
    [SerializeField] private GameObject echoDevicePrefab;
    [SerializeField] private string displayName = "Echo Cihazı";
    
    [Header("Visual")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan glow
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
        
        // Enable emission for glow effect
        if (objectRenderer != null && objectRenderer.material.HasProperty("_EmissionColor"))
        {
            objectRenderer.material.EnableKeyword("_EMISSION");
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
        
        // Pulsing glow effect
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
        
        Debug.Log("[EchoPickupItem] Interact called - attempting to find player...");
        
        // Find player using multiple methods
        GameObject player = FindPlayer();
        
        if (player != null)
        {
            Debug.Log($"[EchoPickupItem] Player found: {player.name}");
            
            EchoDevice echoDevice = player.GetComponent<EchoDevice>();
            if (echoDevice == null)
            {
                echoDevice = player.AddComponent<EchoDevice>();
                Debug.Log("[EchoPickupItem] Added EchoDevice component to player");
            }
            else
            {
                Debug.Log("[EchoPickupItem] EchoDevice already exists on player");
            }
            
            echoDevice.EquipEchoDevice(echoDevicePrefab);
            
            // Show tutorial UI
            if (InteractionUI.Instance != null)
            {
                InteractionUI.Instance.ShowPrompt("Echo Cihazı Alındı!\n[Q] Yankı Dalgası\n[Mouse Scroll] Frekans Ayarı");
            }
            
            Debug.Log($"[EchoPickupItem] {displayName} collected and equipped successfully!");
        }
        else
        {
            Debug.LogError("[EchoPickupItem] PLAYER NOT FOUND! Cannot equip Echo device!");
            Debug.LogError("[EchoPickupItem] Make sure Player GameObject has Tag='Player' or PlayerController component");
        }
        
        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Destroy the pickup item
        Destroy(gameObject);
    }
    
    GameObject FindPlayer()
    {
        // Method 1: Try by tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[EchoPickupItem] Found player by Tag='Player'");
            return player;
        }
        
        // Method 2: Try to find PlayerController
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("[EchoPickupItem] Found player by PlayerController component");
            return playerController.gameObject;
        }
        
        // Method 3: Try main camera's parent (player is often camera parent)
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.parent != null)
        {
            Debug.Log("[EchoPickupItem] Found player as Camera's parent");
            return mainCam.transform.parent.gameObject;
        }
        
        Debug.LogWarning("[EchoPickupItem] All player finding methods failed!");
        return null;
    }
    
    public string GetInteractionPrompt()
    {
        return $"[E] {displayName} Al";
    }
}
