using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ECHOES - System Setup Tool
/// Yeni sistemin kurulumunu otomatiklestirir.
/// </summary>
public class EchoesSetupTool : Editor
{
    [MenuItem("Tools/ECHOES/Setup/1. Setup MainMenu Scene")]
    public static void SetupMainMenuScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("MainMenu"))
        {
            if (!EditorUtility.DisplayDialog("Warning", 
                "Current scene is not MainMenu. Continue anyway?", "Yes", "No"))
                return;
        }
        
        var gmm = FindObjectOfType<GameModeManager>();
        if (gmm == null)
        {
            GameObject go = new GameObject("GameModeManager");
            go.AddComponent<GameModeManager>();
            Debug.Log("[Setup] GameModeManager created");
        }
        
        var sm = FindObjectOfType<SettingsManager>();
        if (sm == null)
        {
            GameObject go = new GameObject("SettingsManager");
            go.AddComponent<SettingsManager>();
            Debug.Log("[Setup] SettingsManager created");
        }
        
        EditorUtility.DisplayDialog("MainMenu Setup Complete", 
            "Created GameModeManager and SettingsManager.\n\nNow connect MenuManager buttons in Inspector and save the scene!", "OK");
    }
    
    [MenuItem("Tools/ECHOES/Setup/2. Setup Game Scene (Echoes)")]
    public static void SetupGameScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Echoes"))
        {
            if (!EditorUtility.DisplayDialog("Warning", 
                "Current scene is not Echoes. Continue anyway?", "Yes", "No"))
                return;
        }
        
        var gsm = FindObjectOfType<GameSceneManager>();
        if (gsm == null)
        {
            GameObject go = new GameObject("GameSceneManager");
            go.AddComponent<GameSceneManager>();
            Debug.Log("[Setup] GameSceneManager created");
        }
        
        var spm = FindObjectOfType<SinglePlayerManager>();
        if (spm == null)
        {
            GameObject go = new GameObject("SinglePlayerManager");
            go.AddComponent<SinglePlayerManager>();
            
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(go.transform);
            spawnPoint.transform.position = new Vector3(0, 2, 0);
            
            Debug.Log("[Setup] SinglePlayerManager created");
        }
        
        var mpm = FindObjectOfType<MultiplayerManager>();
        if (mpm == null)
        {
            GameObject go = new GameObject("MultiplayerManager");
            go.AddComponent<MultiplayerManager>();
            Debug.Log("[Setup] MultiplayerManager created");
        }
        
        EditorUtility.DisplayDialog("Game Scene Setup Complete", 
            "Created GameSceneManager, SinglePlayerManager, and MultiplayerManager.\n\nConnect references in Inspector and save the scene!", "OK");
    }
    
    [MenuItem("Tools/ECHOES/Setup/3. Fix Build Settings")]
    public static void FixBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Echoes.unity", true)
        };
        
        EditorBuildSettings.scenes = scenes;
        
        EditorUtility.DisplayDialog("Build Settings Updated", 
            "Scene order:\n0. MainMenu\n1. Echoes", "OK");
    }
    
    [MenuItem("Tools/ECHOES/Debug/Check System Status")]
    public static void CheckSystemStatus()
    {
        string report = "=== ECHOES SYSTEM STATUS ===\n\n";
        
        var gmm = FindObjectOfType<GameModeManager>();
        report += gmm != null ? "[OK] GameModeManager\n" : "[!] GameModeManager NOT found\n";
        
        var gsm = FindObjectOfType<GameSceneManager>();
        report += gsm != null ? "[OK] GameSceneManager\n" : "[!] GameSceneManager NOT found\n";
        
        var spm = FindObjectOfType<SinglePlayerManager>();
        report += spm != null ? "[OK] SinglePlayerManager\n" : "[!] SinglePlayerManager NOT found\n";
        
        var mpm = FindObjectOfType<MultiplayerManager>();
        report += mpm != null ? "[OK] MultiplayerManager\n" : "[!] MultiplayerManager NOT found\n";
        
        var mm = FindObjectOfType<MenuManager>();
        report += mm != null ? "[OK] MenuManager\n" : "[!] MenuManager NOT found\n";
        
        var sm = FindObjectOfType<SettingsManager>();
        report += sm != null ? "[OK] SettingsManager\n" : "[!] SettingsManager NOT found\n";
        
        var nm = FindObjectOfType<Unity.Netcode.NetworkManager>();
        report += nm != null ? "[OK] NetworkManager\n" : "[!] NetworkManager NOT found\n";
        
        Debug.Log(report);
        EditorUtility.DisplayDialog("System Status", report, "OK");
    }
}
