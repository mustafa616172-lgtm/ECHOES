using UnityEngine;

/// <summary>
/// Makes an object pickable and throwable by the player.
/// E = Pick up, Left Click = Throw, Right Click = Drop
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ThrowableObject : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float holdDistance = 1.5f;
    [SerializeField] private float holdHeight = 0f;
    
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float dropForce = 2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showOutlineWhenLooking = true;
    [SerializeField] private Color outlineColor = Color.yellow;
    
    private Rigidbody rb;
    private Collider col;
    private bool isHeld = false;
    private Transform holdPoint;
    private Camera playerCamera;
    
    private static ThrowableObject currentlyHeldObject;
    
    // Outline effect
    private Renderer objectRenderer;
    private Material[] originalMaterials;
    private bool isHighlighted = false;
    
    public bool IsHeld => isHeld;
    public static ThrowableObject CurrentlyHeld => currentlyHeldObject;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            originalMaterials = objectRenderer.materials;
        }
    }
    
    private void Start()
    {
        // Find player camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerCamera = player.GetComponentInChildren<Camera>();
            }
        }
    }
    
    private void Update()
    {
        if (playerCamera == null) return;
        
        if (isHeld)
        {
            UpdateHeldPosition();
            HandleHeldInput();
        }
        else
        {
            CheckPlayerLooking();
            HandlePickupInput();
        }
    }
    
    private void CheckPlayerLooking()
    {
        if (currentlyHeldObject != null) return; // Already holding something
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.gameObject == gameObject)
            {
                SetHighlight(true);
                return;
            }
        }
        
        SetHighlight(false);
    }
    
    private void HandlePickupInput()
    {
        if (!isHighlighted) return;
        if (currentlyHeldObject != null) return;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            Pickup();
        }
    }
    
    private void HandleHeldInput()
    {
        // Left click = Throw
        if (Input.GetMouseButtonDown(0))
        {
            Throw();
        }
        // Right click = Drop
        else if (Input.GetMouseButtonDown(1))
        {
            Drop();
        }
    }
    
    private void Pickup()
    {
        isHeld = true;
        currentlyHeldObject = this;
        
        // Disable physics while held
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Disable collision with player
        col.isTrigger = true;
        
        SetHighlight(false);
        
        Debug.Log($"[ThrowableObject] Picked up: {gameObject.name}");
    }
    
    private void UpdateHeldPosition()
    {
        if (playerCamera == null) return;
        
        // Position in front of camera
        Vector3 targetPos = playerCamera.transform.position 
            + playerCamera.transform.forward * holdDistance
            + playerCamera.transform.up * holdHeight;
        
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
        
        // Rotate with camera (optional: keep object upright)
        transform.rotation = Quaternion.Lerp(transform.rotation, 
            Quaternion.LookRotation(playerCamera.transform.forward), 
            Time.deltaTime * 10f);
    }
    
    private void Throw()
    {
        Release();
        
        // Add throw force
        Vector3 throwDirection = playerCamera.transform.forward;
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
        
        // Add slight upward arc
        rb.AddForce(Vector3.up * throwForce * 0.2f, ForceMode.Impulse);
        
        Debug.Log($"[ThrowableObject] Threw: {gameObject.name}");
    }
    
    private void Drop()
    {
        Release();
        
        // Gentle drop
        rb.AddForce(Vector3.down * dropForce, ForceMode.Impulse);
        
        Debug.Log($"[ThrowableObject] Dropped: {gameObject.name}");
    }
    
    private void Release()
    {
        isHeld = false;
        currentlyHeldObject = null;
        
        // Re-enable physics
        rb.isKinematic = false;
        rb.useGravity = true;
        col.isTrigger = false;
        
        // Inherit player velocity for more realistic physics
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                rb.linearVelocity = cc.velocity;
            }
        }
    }
    
    private void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return;
        isHighlighted = highlight;
        
        if (!showOutlineWhenLooking || objectRenderer == null) return;
        
        if (highlight)
        {
            // Simple highlight - tint the material
            foreach (var mat in objectRenderer.materials)
            {
                mat.SetColor("_EmissionColor", outlineColor * 0.3f);
                mat.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            // Remove highlight
            foreach (var mat in objectRenderer.materials)
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (currentlyHeldObject == this)
        {
            currentlyHeldObject = null;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Show pickup range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
