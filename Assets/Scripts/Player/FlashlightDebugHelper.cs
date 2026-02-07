using UnityEngine;

/// <summary>
/// Add this to flashlight to make it more visible in Scene view
/// </summary>
public class FlashlightDebugHelper : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // Draw a sphere at flashlight position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);
        
        // Draw a line showing forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
        
        // Draw a wire cube showing the model
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.1f, 0.1f, 0.3f));
    }
}
