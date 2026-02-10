using UnityEngine;

/// <summary>
/// ECHOES - Player Controller
/// Oyuncu hareketi ve kamera kontrolu.
/// ESC menüsü ile uyumlu calisir.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 7f;
    public float gravity = -20f;

    [Header("Camera")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    private CharacterController controller;
    private Camera playerCamera;
    private Transform cameraTransform;
    
    private Vector3 velocity;
    private float pitch = 0f;
    private bool isGrounded;
    
    [Header("Sound")]
    [SerializeField] private float runNoiseRadius = 10f;
    [SerializeField] private float noiseInterval = 0.5f;
    [SerializeField] private AudioClip footstepSound;
    private float lastNoiseTime;
    private AudioSource audioSource;
    
    [Header("Stealth")]
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private float maxBreathHoldTime = 5f;
    [SerializeField] private float breathRecoveryRate = 2f;
    [SerializeField] private AudioClip breathSound;
    
    // Stealth state
    private bool isCrouching = false;
    private bool isHoldingBreath = false;
    private float currentBreath;
    private float targetHeight;
    
    // Public accessors for AI
    public bool IsCrouching => isCrouching;
    public bool IsHoldingBreath => isHoldingBreath;
    public bool IsSilent => isCrouching || isHoldingBreath;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Find camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
        }
        else
        {
            CreateCamera();
        }
        
        if (GetComponent<PlayerStamina>() == null)
        {
            gameObject.AddComponent<PlayerStamina>();
            Debug.Log("[PlayerController] Auto-added PlayerStamina component");
        }
        
        // Auto-add PlayerHealth if not present
        if (GetComponent<PlayerHealth>() == null)
        {
            gameObject.AddComponent<PlayerHealth>();
            Debug.Log("[PlayerController] Auto-added PlayerHealth component");
        }
        
        // Initialize stealth
        currentBreath = maxBreathHoldTime;
        targetHeight = standHeight;
        if (controller != null)
        {
            standHeight = controller.height;
        }
        
        // Initialize AudioSource for sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
            Debug.Log("[PlayerController] Auto-added AudioSource for sounds");
        }
        
        // Auto-add AwarenessIndicator if not present
        if (FindObjectOfType<AwarenessIndicator>() == null)
        {
            GameObject indicatorObj = new GameObject("AwarenessSystem");
            indicatorObj.AddComponent<AwarenessIndicator>();
            Debug.Log("[PlayerController] Auto-added AwarenessIndicator");
        }
        
        // Auto-add HeartbeatEffect
        if (GetComponent<HeartbeatEffect>() == null)
        {
            gameObject.AddComponent<HeartbeatEffect>();
            Debug.Log("[PlayerController] Auto-added HeartbeatEffect");
        }
        
        // Auto-add MicrophoneDetection
        if (GetComponent<MicrophoneDetection>() == null)
        {
            gameObject.AddComponent<MicrophoneDetection>();
            Debug.Log("[PlayerController] Auto-added MicrophoneDetection");
        }
        
        // Auto-add ScreenEffects
        if (GetComponent<ScreenEffects>() == null)
        {
            gameObject.AddComponent<ScreenEffects>();
            Debug.Log("[PlayerController] Auto-added ScreenEffects");
        }
        
        // Auto-add InventorySystem
        if (GetComponent<InventorySystem>() == null)
        {
            gameObject.AddComponent<InventorySystem>();
            Debug.Log("[PlayerController] Auto-added InventorySystem");
        }
        
        // Auto-add AmbientSoundSystem
        if (GetComponent<AmbientSoundSystem>() == null)
        {
            gameObject.AddComponent<AmbientSoundSystem>();
            Debug.Log("[PlayerController] Auto-added AmbientSoundSystem");
        }
        
        Debug.Log("[PlayerController] Initialized");
    }
    
    void CreateCamera()
    {
        GameObject camHolder = new GameObject("CameraHolder");
        camHolder.transform.SetParent(transform);
        camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);
        camHolder.transform.localRotation = Quaternion.identity;
        
        playerCamera = camHolder.AddComponent<Camera>();
        playerCamera.nearClipPlane = 0.1f;
        playerCamera.fieldOfView = 70f;
        playerCamera.tag = "MainCamera";
        
        camHolder.AddComponent<AudioListener>();
        cameraTransform = camHolder.transform;
        
        Debug.Log("[PlayerController] Camera created");
    }

    void Update()
    {
        if (isInputLocked) return;

        // Don't process input if cursor is unlocked (menu is open)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }
        
        HandleCrouch();
        HandleBreathHold();
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMovement()
    {
        if (controller == null) return;
        
        isGrounded = controller.isGrounded;
        
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        Vector3 move = transform.right * h + transform.forward * v;
        move = move.normalized;
        
        // Stamina and Sprint Logic
        bool isMoving = move.sqrMagnitude > 0.1f;
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool canRun = true;
        
        // Can't run while crouching
        if (isCrouching) canRun = false;
        
        // Check Stamina if exists
        PlayerStamina stamina = GetComponent<PlayerStamina>();
        if (stamina != null)
        {
            // If we want to run and are moving, try to consume stamina
            if (wantsToRun && isMoving && !isCrouching)
            {
                canRun = stamina.ConsumeStamina();
            }
            else
            {
                // Ensure we respect the hasStamina buffer for starting to run again
                if (!stamina.HasStamina) canRun = false;
            }
        }
        
        bool running = wantsToRun && canRun && !isCrouching;
        
        // Determine speed based on state
        float speed;
        if (isCrouching)
            speed = crouchSpeed;
        else if (running)
            speed = runSpeed;
        else
            speed = walkSpeed;
        
        // Emit footstep noise when running (not when crouching or holding breath)
        if (running && isMoving && !isHoldingBreath)
        {
            EmitFootstepNoise();
        }
        
        controller.Move(move * speed * Time.deltaTime);
        
        // Can't jump while crouching
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    void HandleCrouch()
    {
        // Left Ctrl to toggle crouch
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
            targetHeight = isCrouching ? crouchHeight : standHeight;
        }
        
        // Smoothly adjust controller height
        if (controller != null && Mathf.Abs(controller.height - targetHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            float heightDiff = newHeight - controller.height;
            
            controller.height = newHeight;
            
            // Adjust camera position
            if (cameraTransform != null)
            {
                Vector3 camPos = cameraTransform.localPosition;
                camPos.y = newHeight * 0.8f; // Camera at 80% of height
                cameraTransform.localPosition = camPos;
            }
        }
    }
    
    void HandleBreathHold()
    {
        // Space to hold breath (only when not jumping)
        if (Input.GetKey(KeyCode.Space) && isGrounded && currentBreath > 0)
        {
            isHoldingBreath = true;
            currentBreath -= Time.deltaTime;
            
            if (currentBreath <= 0)
            {
                currentBreath = 0;
                isHoldingBreath = false;
                
                // Play gasp sound when out of breath
                if (breathSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(breathSound);
                }
            }
        }
        else
        {
            isHoldingBreath = false;
            
            // Recover breath
            if (currentBreath < maxBreathHoldTime)
            {
                currentBreath += breathRecoveryRate * Time.deltaTime;
                currentBreath = Mathf.Min(currentBreath, maxBreathHoldTime);
            }
        }
    }

    void HandleMouseLook()
    {
        if (cameraTransform == null) return;
        
        float sensitivity = mouseSensitivity;
        if (SettingsManager.Instance != null)
            sensitivity = SettingsManager.Instance.MouseSensitivity;
        
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
        
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
        
        transform.Rotate(Vector3.up * mouseX);
    }
    
    public void SetPosition(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }

    private bool isInputLocked = false;
    public void SetInputLock(bool locked)
    {
        isInputLocked = locked;
        controller.enabled = !locked; // Optional: disable controller to prevent physics sliding
        if (locked)
        {
            velocity = Vector3.zero; // Stop moving
        }
    }
    
    private void EmitFootstepNoise()
    {
        if (Time.time - lastNoiseTime < noiseInterval) return;
        lastNoiseTime = Time.time;
        
        // Emit noise for AI
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.EmitSound(transform.position, runNoiseRadius, SoundManager.SoundType.Footstep);
        }
        
        // Play footstep sound if available
        if (footstepSound != null)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D sound for player
                audioSource.volume = 0.3f;
            }
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(footstepSound);
        }
    }
}
