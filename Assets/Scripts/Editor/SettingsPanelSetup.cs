using UnityEngine;
using UnityEditor;

/// <summary>
/// ECHOES - Settings Panel Setup Tool
/// Editor araci - Settings Panel UI olusturur.
/// </summary>
public class SettingsPanelSetup : Editor
{
    [MenuItem("Tools/ECHOES/Create Settings Panel")]
    public static void CreateSettingsPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SettingsSetup] No Canvas found!");
            return;
        }
        
        GameObject settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = settingsPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        UnityEngine.UI.Image bg = settingsPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        settingsPanel.AddComponent<SettingsPanel>();
        
        settingsPanel.SetActive(false);
        
        MenuManager mm = FindObjectOfType<MenuManager>();
        if (mm != null)
        {
            SerializedObject so = new SerializedObject(mm);
            var prop = so.FindProperty("settingsPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = settingsPanel;
                so.ApplyModifiedProperties();
                Debug.Log("[SettingsSetup] Connected to MenuManager");
            }
        }
        
        Selection.activeGameObject = settingsPanel;
        EditorUtility.DisplayDialog("Success", "SettingsPanel created! Add UI elements as needed.", "OK");
    }
}
