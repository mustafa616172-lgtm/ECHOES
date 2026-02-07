using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Settings")]
    public float interactionDistance = 3.0f;
    public LayerMask interactionLayer = ~0; // All layers by default

    private Camera playerCamera;
    private IInteractable currentInteractable;

    void Start()
    {
        FindPlayerCamera();

        // Ensure UI exists
        if (InteractionUI.Instance == null)
        {
            GameObject uiObj = new GameObject("InteractionManager");
            uiObj.AddComponent<InteractionUI>();
        }
    }

    void FindPlayerCamera()
    {
        // 1. Try get from children (standard)
        playerCamera = GetComponentInChildren<Camera>();
        
        // 2. Try find by tag
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main;
        }

        // 3. Try to find the specific "CameraHolder" created by PlayerController
        if (playerCamera == null)
        {
            Transform camHolder = transform.Find("CameraHolder");
            if (camHolder != null) playerCamera = camHolder.GetComponent<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError("❌ [InteractionController] CAMERA NOT FOUND! I cannot shoot rays without eyes! Please tag your camera as 'MainCamera' or make it a child of this object.");
        }
        else
        {
            Debug.Log($"✅ [InteractionController] Camera Found: {playerCamera.name}");
        }
    }

    void Update()
    {
        // Retry finding camera if missing (e.g. created late)
        if (playerCamera == null)
        {
            FindPlayerCamera();
            if (playerCamera == null) return; // Still null, give up this frame
        }

        HandleRaycast();
        HandleInput();
    }

    void HandleRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // DEBUG: Visualize Ray
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance, Color.red);

        bool hitSomething = Physics.Raycast(ray, out hit, interactionDistance, interactionLayer);
        
        if (hitSomething)
        {
            // DEBUG: What are we hitting?
            // Debug.Log($"Hit: {hit.collider.name}");

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            // Try searching in parent if not found on collider (common for compound objects)
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                // DEBUG: Found interactable
                Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * hit.distance, Color.green);
                
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    InteractionUI.Instance.ShowPrompt(currentInteractable.GetInteractionPrompt());
                }
                return;
            }
        }

        // If we hit nothing or non-interactable
        if (currentInteractable != null)
        {
            currentInteractable = null;
            InteractionUI.Instance.HidePrompt();
        }
    }

    void HandleInput()
    {
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
            // Refresh prompt immediately in case state changed (e.g. Door Unlocked)
            InteractionUI.Instance.ShowPrompt(currentInteractable.GetInteractionPrompt());
        }
    }
}
