using UnityEngine;
using UnityEditor;
using Unity.Netcode;

/// <summary>
/// Player prefab'ýný otomatik olarak ayarlayan editor tool
/// Unity Editor menüsünden Tools > ECHOES > Setup Player Prefab ile çalýþtýrýlýr
/// </summary>
public class PlayerPrefabSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/ECHOES/Setup Player Prefab")]
    static void SetupPlayer()
    {
        // Player prefab'ý yükle
        string prefabPath = "Assets/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab bulunamadý: " + prefabPath);
            return;
        }

        // Prefab'ý düzenleme modunda aç
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);

        // 1. Rigidbody varsa kaldýr, CharacterController ekle
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Object.DestroyImmediate(rb);
            Debug.Log("? Rigidbody kaldýrýldý");
        }

        CharacterController cc = instance.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = instance.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1, 0);
            Debug.Log("? CharacterController eklendi");
        }

        // 2. Kamera oluþtur veya güncelle
        Transform cameraTransform = instance.transform.Find("PlayerCamera");
        GameObject cameraObj;

        if (cameraTransform == null)
        {
            cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(instance.transform);
            Debug.Log("? PlayerCamera oluþturuldu");
        }
        else
        {
            cameraObj = cameraTransform.gameObject;
        }

        // Kamera pozisyonunu ayarla (göz seviyesinde)
        cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
        cameraObj.transform.localRotation = Quaternion.identity;

        // Camera component ekle
        Camera cam = cameraObj.GetComponent<Camera>();
        if (cam == null)
        {
            cam = cameraObj.AddComponent<Camera>();
        }
        cam.fieldOfView = 80f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        cam.enabled = false; // Baþlangýçta kapalý (NetworkSpawn'da açýlacak)

        // AudioListener ekle
        AudioListener listener = cameraObj.GetComponent<AudioListener>();
        if (listener == null)
        {
            listener = cameraObj.AddComponent<AudioListener>();
        }
        listener.enabled = false; // Baþlangýçta kapalý

        Debug.Log("? Kamera ve AudioListener ayarlandý");

        // 3. Ýsim etiketi oluþtur
        Transform nameTagTransform = instance.transform.Find("NameTag");
        GameObject nameTagObj;

        if (nameTagTransform == null)
        {
            nameTagObj = new GameObject("NameTag");
            nameTagObj.transform.SetParent(instance.transform);
            Debug.Log("? NameTag oluþturuldu");
        }
        else
        {
            nameTagObj = nameTagTransform.gameObject;
        }

        // Ýsim etiketi pozisyonu (baþ üstü)
        nameTagObj.transform.localPosition = new Vector3(0, 2.2f, 0);
        nameTagObj.transform.localRotation = Quaternion.identity;

        // Canvas ekle (World Space)
        Canvas canvas = nameTagObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = nameTagObj.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.WorldSpace;
        
        var canvasScaler = nameTagObj.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = nameTagObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        }

        // RectTransform ayarla
        RectTransform rectTransform = nameTagObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);
        rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // TextMeshPro ekle
        Transform textTransform = nameTagObj.transform.Find("NameText");
        GameObject textObj;

        if (textTransform == null)
        {
            textObj = new GameObject("NameText");
            textObj.transform.SetParent(nameTagObj.transform);
        }
        else
        {
            textObj = textTransform.gameObject;
        }

        var tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        }
        
        tmp.text = "Oyuncu";
        tmp.fontSize = 36;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log("? Ýsim etiketi UI'ý oluþturuldu");

        // 4. PlayerNameTag script ekle
        PlayerNameTag nameTag = instance.GetComponent<PlayerNameTag>();
        if (nameTag == null)
        {
            nameTag = instance.AddComponent<PlayerNameTag>();
        }
        
        // Referanslarý ayarla
        SerializedObject so = new SerializedObject(nameTag);
        so.FindProperty("nameText").objectReferenceValue = tmp;
        so.FindProperty("nameTagTransform").objectReferenceValue = nameTagObj.transform;
        so.ApplyModifiedProperties();

        Debug.Log("? PlayerNameTag scripti eklendi ve baðlandý");

        // Deðiþiklikleri kaydet
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);

        Debug.Log("<color=green>??? Player prefab baþarýyla ayarlandý! ???</color>");
        AssetDatabase.Refresh();
    }
#endif
}
