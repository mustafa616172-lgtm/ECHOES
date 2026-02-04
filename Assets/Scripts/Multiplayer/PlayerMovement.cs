using UnityEngine;
using Unity.Netcode;

/// <summary>
/// ECHOES - Network Player Movement
/// Multiplayer modunda oyuncu hareketi ve kamera kontrolu.
/// Sadece local player (IsOwner) icin input ve kamera aktif edilir.
/// </summary>
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    
    // Components
    private CharacterController characterController;
    private Camera playerCamera;
    private AudioListener audioListener;
    private Transform cameraTransform;
    
    // State
    private Vector3 velocity;
    private float verticalLookRotation = 0f;
    private bool isGrounded;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        // Find camera in children
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            audioListener = playerCamera.GetComponent<AudioListener>();
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"[PlayerMovement] OnNetworkSpawn - IsOwner: {IsOwner}, ClientId: {OwnerClientId}");
        
        if (IsOwner)
        {
            // This is LOCAL player - enable camera and input
            EnableLocalPlayer();
        }
        else
        {
            // This is REMOTE player - disable camera and input
            DisableRemotePlayer();
        }
    }
    
    void EnableLocalPlayer()
    {
        Debug.Log("[PlayerMovement] Enabling LOCAL player (camera, audio, input)");
        
        // Enable camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.tag = "MainCamera";
            Debug.Log("[PlayerMovement] Player camera ENABLED");
        }
        
        // Enable audio listener
        if (audioListener != null)
        {
            audioListener.enabled = true;
        }
        
        // Disable scene's main camera if exists
        DisableSceneCamera();
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void DisableRemotePlayer()
    {
        Debug.Log("[PlayerMovement] This is REMOTE player - disabling camera/input");
        
        // Disable camera (should already be disabled in prefab)
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
        }
        
        // Disable audio listener
        if (audioListener != null)
        {
            audioListener.enabled = false;
        }
        
        // Disable this script for remote players (no input processing)
        // Movement will be synced via NetworkTransform
        enabled = false;
    }
    
    void DisableSceneCamera()
    {
        // Find and disable any other cameras tagged as MainCamera
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam != playerCamera && cam.enabled)
            {
                Debug.Log($"[PlayerMovement] Disabling scene camera: {cam.gameObject.name}");
                cam.enabled = false;
                
                // Also disable audio listener on that camera
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }
        }
    }
    
    void Update()
    {
        // Only process input for local player
        if (!IsOwner) return;
        
        HandleMovement();
        HandleLook();
    }
    
    void HandleMovement()
    {
        // Ground check
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Calculate movement direction
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.Normalize();
        
        // Apply speed
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        characterController.Move(moveDirection * speed * Time.deltaTime);
        
        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    void HandleLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera vertically
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -maxLookAngle, maxLookAngle);
        
        if (cameraTransform != null)
        {
            cameraTransform.localEulerAngles = new Vector3(verticalLookRotation, 0, 0);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsOwner)
        {
            // Unlock cursor when player despawns
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
