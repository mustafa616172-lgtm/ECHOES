using UnityEngine;
using UnityEditor;

/// <summary>
/// ECHOES - Player Prefab Setup Tool
/// Oyuncu prefab olusturma araci.
/// </summary>
public class PlayerPrefabSetup : Editor
{
    [MenuItem("Tools/ECHOES/Create Player Prefab")]
    public static void CreatePlayerPrefab()
    {
        GameObject player = new GameObject("Player");
        
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1, 0);
        
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 1, 0);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
        
        GameObject camHolder = new GameObject("CameraHolder");
        camHolder.transform.SetParent(player.transform);
        camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);
        
        Camera cam = camHolder.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.fieldOfView = 70f;
        cam.tag = "MainCamera";
        
        camHolder.AddComponent<AudioListener>();
        
        player.AddComponent<PlayerController>();
        
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        
        string prefabPath = "Assets/Prefabs/Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        
        Debug.Log("[PlayerPrefabSetup] Player prefab created at: " + prefabPath);
        EditorUtility.DisplayDialog("Success", "Player Prefab created!", "OK");
        
        Selection.activeGameObject = player;
    }
}
