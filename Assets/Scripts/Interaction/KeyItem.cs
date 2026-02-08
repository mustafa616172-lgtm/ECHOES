using UnityEngine;

public class KeyItem : MonoBehaviour, IInteractable
{
    public string keyID = "MainDoorKey";
    public string keyName = "Rusty Key";

    public string GetInteractionPrompt()
    {
        return $"[E] Pick up {keyName}";
    }

    public void Interact()
    {
        if (KeyInventory.Instance == null)
        {
            // Auto-create inventory if missing
            GameObject invInfo = new GameObject("KeyInventory");
            invInfo.AddComponent<KeyInventory>();
        }

        KeyInventory.Instance.AddKey(keyID);
        
        // Visual Feedback (could play sound here)
        Debug.Log($"Picked up {keyName}");
        
        // Destroy object
        Destroy(gameObject);
    }
}
