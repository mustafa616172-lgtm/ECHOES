using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Rotation Settings")]
    public Vector3 rotationAxis = Vector3.up;
    public float openAngle = 90f;
    public float closeAngle = 0f;
    public float smoothSpeed = 2f;
    public bool isOpen = false;
    
    [Header("Lock Settings")]
    public bool isLocked = false;
    public string requiredKeyID = "";

    private Quaternion targetRotation;
    private Quaternion initialRotation;
    private bool isAnimating = false;

    void Start()
    {
        initialRotation = transform.localRotation;
        
        // If start open, set initial state
        if (isOpen)
        {
            targetRotation = initialRotation * Quaternion.AngleAxis(openAngle, rotationAxis);
        }
        else
        {
            targetRotation = initialRotation * Quaternion.AngleAxis(closeAngle, rotationAxis);
        }
    }

    void Update()
    {
        if (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }

    public string GetInteractionPrompt()
    {
        if (isLocked) return "[E] Locked";
        return isOpen ? "[E] Close Door" : "[E] Open Door";
    }

    public void Interact()
    {
        if (isLocked)
        {
            TryUnlock();
        }
        else
        {
            ToggleDoor();
        }
    }

    void TryUnlock()
    {
        if (string.IsNullOrEmpty(requiredKeyID))
        {
            // Locked but no key defined? Just unlock (or maybe it's jammed)
            isLocked = false;
            InteractionUI.Instance.ShowPrompt("Door Unlocked");
            return;
        }

        if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyID))
        {
            isLocked = false;
            InteractionUI.Instance.ShowPrompt("Unlocked with Key");
            // Optional: Play unlock sound
        }
        else
        {
            InteractionUI.Instance.ShowPrompt("Need Key!");
            // Optional: Play locked rattle sound
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        float targetAngle = isOpen ? openAngle : closeAngle;
        targetRotation = initialRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
    }
}
