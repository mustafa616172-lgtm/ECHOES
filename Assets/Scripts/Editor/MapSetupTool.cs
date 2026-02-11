#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ECHOES - Map Setup Tool
/// Creates the "Minimap" layer and configures main camera to ignore it.
/// Access via: Tools > ECHOES > Setup Map Layer
/// </summary>
public class MapSetupTool : Editor
{
    [MenuItem("Tools/ECHOES/Setup Map Layer")]
    public static void SetupMapLayer()
    {
        // Open TagManager
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Check if Minimap layer already exists
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (layerProp.stringValue == "Minimap")
            {
                EditorUtility.DisplayDialog("Map Setup",
                    "Minimap layer already exists at index " + i + "!\n\n" +
                    "Setup is complete. The MapSystem will use this layer automatically.",
                    "OK");
                return;
            }
        }

        // Find an empty layer slot (prefer 31, then search from 8 upwards)
        int targetSlot = -1;

        // Try slot 31 first
        if (layersProp.arraySize > 31)
        {
            SerializedProperty slot31 = layersProp.GetArrayElementAtIndex(31);
            if (string.IsNullOrEmpty(slot31.stringValue))
            {
                targetSlot = 31;
            }
        }

        // If 31 is taken, search from 8 upwards
        if (targetSlot == -1)
        {
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    targetSlot = i;
                    break;
                }
            }
        }

        if (targetSlot == -1)
        {
            EditorUtility.DisplayDialog("Map Setup Error",
                "No empty layer slots available!\n" +
                "Please manually create a layer named 'Minimap' in Project Settings > Tags and Layers.",
                "OK");
            return;
        }

        // Assign the layer
        SerializedProperty targetLayer = layersProp.GetArrayElementAtIndex(targetSlot);
        targetLayer.stringValue = "Minimap";
        tagManager.ApplyModifiedProperties();

        // Configure main camera to NOT render the Minimap layer
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.cullingMask &= ~(1 << targetSlot);
            Debug.Log("[MapSetup] Main camera updated to ignore Minimap layer.");
        }

        EditorUtility.DisplayDialog("Map Setup Complete",
            "Minimap layer created at index " + targetSlot + "!\n\n" +
            "IMPORTANT: Make sure your main game camera does NOT render this layer.\n" +
            "Go to your main camera > Culling Mask > uncheck 'Minimap'.\n\n" +
            "The MapSystem will handle everything else automatically.\n\n" +
            "To add map markers to objects:\n" +
            "1. Select any GameObject\n" +
            "2. Add Component > MapMarker\n" +
            "3. Set the marker type (SaveRoom, Item, Enemy, Exit, POI)",
            "OK");

        Debug.Log("[MapSetup] Minimap layer created at index " + targetSlot);
    }

    [MenuItem("Tools/ECHOES/Setup Map Layer", true)]
    public static bool ValidateSetupMapLayer()
    {
        return true; // Always available in editor
    }
}
#endif