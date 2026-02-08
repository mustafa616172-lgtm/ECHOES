using UnityEngine;

public class DrawerController : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public Vector3 slideDirection = Vector3.forward; // Local direction
    public float slideDistance = 0.5f;
    public float smoothSpeed = 3f;
    public bool isOpen = false;
    
    [Header("Lock Settings")]
    public bool isLocked = false;
    public string requiredKeyID = "";

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 targetPosition;

    void Start()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition + (slideDirection.normalized * slideDistance);
        targetPosition = isOpen ? openPosition : closedPosition;
    }

    void Update()
    {
        if (Vector3.Distance(transform.localPosition, targetPosition) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothSpeed);
        }
    }

    public string GetInteractionPrompt()
    {
        if (isLocked) return "[E] Locked";
        return isOpen ? "[E] Close Drawer" : "[E] Open Drawer";
    }

    public void Interact()
    {
        if (isLocked)
        {
            TryUnlock();
        }
        else
        {
            ToggleDrawer();
        }
    }

    void TryUnlock()
    {
        if (string.IsNullOrEmpty(requiredKeyID))
        {
            isLocked = false;
            InteractionUI.Instance.ShowPrompt("Drawer Unlocked");
            return;
        }

        if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey(requiredKeyID))
        {
            isLocked = false;
            InteractionUI.Instance.ShowPrompt("Unlocked with Key");
        }
        else
        {
            InteractionUI.Instance.ShowPrompt("Need Key!");
        }
    }

    void ToggleDrawer()
    {
        isOpen = !isOpen;
        targetPosition = isOpen ? openPosition : closedPosition;
    }
}
