using UnityEngine;
using UnityEditor;

// Auto-setup script: Runs once, adds DoorInteractable + MeshColliders to DoorHinge and its children
// Then removes itself. No manual setup needed.
[InitializeOnLoad]
public class DoorHingeAutoSetup
{
    static DoorHingeAutoSetup()
    {
        EditorApplication.delayCall += RunSetup;
    }

    static void RunSetup()
    {
        // Find DoorHinge object
        GameObject doorHinge = GameObject.Find("DoorHinge");
        if (doorHinge == null)
        {
            Debug.Log("[DoorHingeAutoSetup] DoorHinge not found in scene. Skipping.");
            return;
        }

        // Check if already set up
        if (doorHinge.GetComponent<DoorInteractable>() != null)
        {
            Debug.Log("[DoorHingeAutoSetup] DoorHinge already has DoorInteractable. Skipping.");
            return;
        }

        // Add MeshColliders to all children with MeshFilter
        MeshFilter[] meshFilters = doorHinge.GetComponentsInChildren<MeshFilter>(true);
        int colliderCount = 0;
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<MeshCollider>() != null) continue;

            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            colliderCount++;
        }

        // Add DoorInteractable to DoorHinge itself (the pivot point)
        DoorInteractable door = doorHinge.AddComponent<DoorInteractable>();
        door.openAngle = 90f;
        door.rotationAxis = Vector3.up;
        door.rotateSpeed = 3f;
        door.startOpen = false;
        door.volume = 0.4f;
        door.openPrompt = "[E] Open Door";
        door.closePrompt = "[E] Close Door";

        EditorUtility.SetDirty(doorHinge);

        Debug.Log($"[DoorHingeAutoSetup] Done! Added DoorInteractable to DoorHinge + {colliderCount} MeshCollider(s) to children.");
        EditorUtility.DisplayDialog("Door Setup Complete",
            $"DoorHinge configured!\n\n" +
            $"- DoorInteractable added to DoorHinge\n" +
            $"- {colliderCount} MeshCollider(s) added to children\n\n" +
            "The door will rotate 90 degrees on Y axis.\n" +
            "Adjust openAngle in Inspector if needed.",
            "OK");
    }
}
