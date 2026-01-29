using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour 
{
    [Header("Hareket Ayarlarý")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    [Header("Kamera Ayarlarý")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    // Komponentler
    private CharacterController characterController;
    private Camera playerCamera;
    private AudioListener audioListener;

    // Durum deðiþkenleri
    private Vector3 velocity;
    private float pitch = 0f;
    private bool isGrounded;

    private void Awake()
    {
        // Komponentleri bul
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        audioListener = GetComponentInChildren<AudioListener>();
    }

    public override void OnNetworkSpawn()
    {
        // Sadece kendi karakterimizin kamerasý ve ses dinleyicisi aktif olmalý
        if (IsOwner)
        {
            if (playerCamera != null)
                playerCamera.enabled = true;
            
            if (audioListener != null)
                audioListener.enabled = true;

            // Fareyi kilitle
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (playerCamera != null)
                playerCamera.enabled = false;
            
            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    void Update()
    {
        // Sadece kendi karakterimizi kontrol edebiliriz
        if (!IsOwner) return;

        HandleMovement();
        HandleMouseLook();
        HandleCursorToggle();
    }

    void HandleMovement()
    {
        // Yerde miyiz?
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Yere yapýþ
        }

        // Input al
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Koþma
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Hareket yönü
        Vector3 move = transform.right * x + transform.forward * z;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        // Zýplama
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Yerçekimi
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        if (playerCamera == null) return;

        // Fare hareketi al
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Yukarý/aþaðý bakýþ (kamera)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        // Saða/sola dönüþ (karakter gövdesi)
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCursorToggle()
    {
        // ESC tuþu ile fareyi serbest býrak/kilitle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}