using UnityEngine;
using UnityEditor;

public class AddMeshColliders : EditorWindow
{
    [MenuItem("Tools/Add MeshColliders to Selected Objects")]
    static void AddColliders()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects in the Hierarchy.", "OK");
            return;
        }

        int count = 0;
        foreach (GameObject go in selected)
        {
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<MeshCollider>() != null) continue;

                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                count++;
            }
        }

        Debug.Log($"[AddMeshColliders] Added {count} MeshCollider(s) to selected objects and their children.");
        EditorUtility.DisplayDialog("Done", $"Added {count} MeshCollider(s).", "OK");
    }
}
