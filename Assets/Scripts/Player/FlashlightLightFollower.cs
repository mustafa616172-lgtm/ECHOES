using UnityEngine;

/// <summary>
/// Quick fix - manually make light follow flashlight direction
/// Add this to the Light object if automatic tracking doesn't work
/// </summary>
public class FlashlightLightFollower : MonoBehaviour
{
    private Transform flashlightParent;
    
    void Start()
    {
        flashlightParent = transform.parent;
        Debug.Log("[LightFollower] Will sync with flashlight rotation");
    }
    
    void LateUpdate()
    {
        if (flashlightParent != null)
        {
            // Ensure light always points forward relative to flashlight
            transform.rotation = flashlightParent.rotation;
        }
    }
}
