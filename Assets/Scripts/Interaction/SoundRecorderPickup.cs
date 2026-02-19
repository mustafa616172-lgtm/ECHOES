using UnityEngine;

public class SoundRecorderPickup : MonoBehaviour, IInteractable
{
    public string prompt = "Ses Kayit Cihazi [E]";
    private SoundRecorderDevice device;

    void Start()
    {
        device = GetComponent<SoundRecorderDevice>();
        if (device == null)
        {
            device = GetComponentInChildren<SoundRecorderDevice>();
        }
    }

    public void Interact()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[SoundRecorderPickup] Main Camera not found!");
            return;
        }

        // --- Equip: Parent to camera ---
        transform.SetParent(mainCam.transform);
        transform.localPosition = new Vector3(0.4f, -0.3f, 0.6f);
        transform.localRotation = Quaternion.Euler(0, -10, 0);

        // --- Disable physics ---
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // --- Activate device ---
        if (device != null)
        {
            device.Pickup(); // This also creates the light if missing
        }

        // --- UI feedback ---
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.ShowMessage("Device Acquired! [Scroll] Frequency", 3.0f);
        }

        // Disable pickup script
        this.enabled = false;
    }

    public string GetInteractionPrompt()
    {
        return prompt;
    }
}
