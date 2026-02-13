using UnityEngine;
using UnityEditor;

/// <summary>
/// Seçili objelere DrawerInteractable ve MeshCollider ekleyen Editor aracý.
/// </summary>
public class DrawerSetupTool : EditorWindow
{
    private Vector3 openOffset = new Vector3(0f, 0f, -0.4f);
    private float moveSpeed = 3f;

    [MenuItem("Tools/Setup Drawers on Selected")]
    static void ShowWindow()
    {
        GetWindow<DrawerSetupTool>("Drawer Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Çekmece Kurulum Aracý", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        openOffset = EditorGUILayout.Vector3Field("Açýlma Ofseti", openOffset);
        moveSpeed = EditorGUILayout.FloatField("Hareket Hýzý", moveSpeed);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Hierarchy'de çekmece objelerini seçin, sonra 'Setup' butonuna basýn.\n" +
            "Bu araç seçili objelere DrawerInteractable ve MeshCollider ekler.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Selected Drawers", GUILayout.Height(35)))
        {
            SetupDrawers();
        }
    }

    void SetupDrawers()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Seçim Yok", "Lütfen Hierarchy'den çekmece objelerini seçin.", "OK");
            return;
        }

        int count = 0;
        foreach (GameObject go in selected)
        {
            Undo.RecordObject(go, "Setup Drawer");

            // MeshCollider ekle (yoksa)
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && go.GetComponent<MeshCollider>() == null)
            {
                MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
                mc.sharedMesh = mf.sharedMesh;
            }

            // Alt objelere de MeshCollider ekle
            MeshFilter[] childMeshes = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter childMf in childMeshes)
            {
                if (childMf.sharedMesh == null) continue;
                if (childMf.GetComponent<MeshCollider>() != null) continue;
                MeshCollider mc = Undo.AddComponent<MeshCollider>(childMf.gameObject);
                mc.sharedMesh = childMf.sharedMesh;
            }

            // DrawerInteractable ekle (yoksa)
            DrawerInteractable drawer = go.GetComponent<DrawerInteractable>();
            if (drawer == null)
            {
                drawer = Undo.AddComponent<DrawerInteractable>(go);
            }
            drawer.openOffset = openOffset;
            drawer.moveSpeed = moveSpeed;

            EditorUtility.SetDirty(go);
            count++;
        }

        Debug.Log($"[DrawerSetup] {count} çekmece kuruldu!");
        EditorUtility.DisplayDialog("Tamamlandý", $"{count} çekmece baþarýyla kuruldu!", "OK");
    }
}
