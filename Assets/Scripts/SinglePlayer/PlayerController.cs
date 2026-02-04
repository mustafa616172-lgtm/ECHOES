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
        // Don't process input if cursor is unlocked (menu is open)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }
        
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
        
        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runSpeed : walkSpeed;
        
        controller.Move(move * speed * Time.deltaTime);
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
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
}
