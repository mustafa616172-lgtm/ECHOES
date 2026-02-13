using UnityEngine;
using UnityEditor;

public class LightSwitchSetupTool : EditorWindow
{
    private Transform lampTransform;
    private Color lightColor = new Color(1f, 0.95f, 0.8f, 1f);
    private float lightIntensity = 1.5f;
    private float lightRange = 12f;
    private bool startOn = false;

    [MenuItem("Tools/Setup LightSwitch on Selected")]
    static void ShowWindow()
    {
        GetWindow<LightSwitchSetupTool>("LightSwitch Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Light Switch Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        lampTransform = (Transform)EditorGUILayout.ObjectField("Lamp Transform", lampTransform, typeof(Transform), true);
        lightColor = EditorGUILayout.ColorField("Light Color", lightColor);
        lightIntensity = EditorGUILayout.FloatField("Intensity", lightIntensity);
        lightRange = EditorGUILayout.FloatField("Range", lightRange);
        startOn = EditorGUILayout.Toggle("Start On", startOn);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. Select the LightSwitch object in Hierarchy\n" +
            "2. Assign the Lamp empty object as Light Source\n" +
            "3. Click Setup",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Selected as LightSwitch", GUILayout.Height(35)))
        {
            SetupSwitch();
        }
    }

    void SetupSwitch()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Select a LightSwitch object in Hierarchy.", "OK");
            return;
        }

        if (lampTransform == null)
        {
            EditorUtility.DisplayDialog("No Lamp", "Please assign the Lamp transform.", "OK");
            return;
        }

        int count = 0;
        foreach (GameObject go in selected)
        {
            Undo.RecordObject(go, "Setup LightSwitch");

            // Add MeshCollider if needed
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && go.GetComponent<Collider>() == null)
            {
                MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
                mc.sharedMesh = mf.sharedMesh;
            }

            // Add to children too
            MeshFilter[] childMeshes = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter childMf in childMeshes)
            {
                if (childMf.sharedMesh == null) continue;
                if (childMf.GetComponent<Collider>() != null) continue;
                MeshCollider mc = Undo.AddComponent<MeshCollider>(childMf.gameObject);
                mc.sharedMesh = childMf.sharedMesh;
            }

            // Add LightSwitchInteractable
            LightSwitchInteractable ls = go.GetComponent<LightSwitchInteractable>();
            if (ls == null)
            {
                ls = Undo.AddComponent<LightSwitchInteractable>(go);
            }

            ls.lightSourceTransform = lampTransform;
            ls.lightColor = lightColor;
            ls.lightIntensity = lightIntensity;
            ls.lightRange = lightRange;
            ls.startOn = startOn;

            EditorUtility.SetDirty(go);
            count++;
        }

        EditorUtility.DisplayDialog("Done", count + " LightSwitch(es) configured!", "OK");
    }
}
