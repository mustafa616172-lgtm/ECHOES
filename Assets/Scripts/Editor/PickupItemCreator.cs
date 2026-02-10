using UnityEngine;
using UnityEditor;

public class PickupItemCreator : Editor
{
    [MenuItem("ECHOES/Create Pickup Item/Key")]
    static void CreateKey()
    {
        CreatePickup("Key_Item", InventorySystem.ItemType.Key, "Pasli Anahtar",
            new Color(1f, 0.85f, 0.2f), PrimitiveType.Cube, new Vector3(0.15f, 0.05f, 0.4f));
    }

    [MenuItem("ECHOES/Create Pickup Item/Battery")]
    static void CreateBattery()
    {
        CreatePickup("Battery_Item", InventorySystem.ItemType.Battery, "Pil",
            new Color(0.3f, 0.9f, 1f), PrimitiveType.Cylinder, new Vector3(0.08f, 0.15f, 0.08f));
    }

    [MenuItem("ECHOES/Create Pickup Item/Note")]
    static void CreateNote()
    {
        CreatePickup("Note_Item", InventorySystem.ItemType.Note, "Gizemli Not",
            new Color(0.9f, 0.85f, 0.7f), PrimitiveType.Cube, new Vector3(0.3f, 0.01f, 0.4f),
            "Bu notu okuyan herkes lanetlenir...");
    }

    [MenuItem("ECHOES/Create Pickup Item/Health Kit")]
    static void CreateHealthKit()
    {
        CreatePickup("HealthKit_Item", InventorySystem.ItemType.HealthKit, "Ilk Yardim Cantasi",
            new Color(0.2f, 1f, 0.2f), PrimitiveType.Cube, new Vector3(0.3f, 0.2f, 0.15f));
    }

    static void CreatePickup(string name, InventorySystem.ItemType type, string displayName,
        Color color, PrimitiveType shape, Vector3 scale, string noteContent = "")
    {
        GameObject obj = GameObject.CreatePrimitive(shape);
        obj.name = name;
        obj.transform.localScale = scale;

        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            obj.transform.position = sv.camera.transform.position + sv.camera.transform.forward * 3f;
        }

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            rend.material = mat;
        }

        PickupItem pickup = obj.AddComponent<PickupItem>();
        pickup.itemID = name.ToLower();
        pickup.displayName = displayName;
        pickup.itemType = type;
        if (!string.IsNullOrEmpty(noteContent))
            pickup.noteContent = noteContent;

        Selection.activeGameObject = obj;
        Undo.RegisterCreatedObjectUndo(obj, "Create Pickup Item");
    }
}
