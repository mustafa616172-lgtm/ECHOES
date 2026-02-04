using UnityEngine;
using UnityEditor;
using Unity.Netcode;

/// <summary>
/// ECHOES - Multiplayer Scene Setup
/// Oyun sahnesinde gerekli multiplayer objelerini olusturur.
/// </summary>
public class MultiplayerSceneSetup : Editor
{
    [MenuItem("ECHOES/Setup Multiplayer in Game Scene")]
    public static void SetupMultiplayerScene()
    {
        // Check if we're in the right scene (not MainMenu)
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.ToLower().Contains("menu"))
        {
            EditorUtility.DisplayDialog("Warning", 
                "This tool should be run in the GAME scene (Echoes), not MainMenu!", "OK");
            return;
        }
        
        int created = 0;
        
        // 1. Check/Create GameSceneManager
        GameSceneManager gsm = FindObjectOfType<GameSceneManager>();
        if (gsm == null)
        {
            GameObject gsmObj = new GameObject("GameSceneManager");
            gsm = gsmObj.AddComponent<GameSceneManager>();
            created++;
            Debug.Log("[Setup] Created GameSceneManager");
        }
        
        // 2. Check/Create MultiplayerManager
        MultiplayerManager mm = FindObjectOfType<MultiplayerManager>();
        if (mm == null)
        {
            GameObject mmObj = new GameObject("MultiplayerManager");
            mm = mmObj.AddComponent<MultiplayerManager>();
            created++;
            Debug.Log("[Setup] Created MultiplayerManager");
        }
        
        // 3. Check/Create SinglePlayerManager
        SinglePlayerManager spm = FindObjectOfType<SinglePlayerManager>();
        if (spm == null)
        {
            GameObject spmObj = new GameObject("SinglePlayerManager");
            spm = spmObj.AddComponent<SinglePlayerManager>();
            created++;
            Debug.Log("[Setup] Created SinglePlayerManager");
        }
        
        // 4. Check NetworkManager
        NetworkManager nm = FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            EditorUtility.DisplayDialog("Warning", 
                "NetworkManager not found!\n\nPlease add a NetworkManager to the scene via:\nGameObject ? Netcode ? NetworkManager", "OK");
        }
        
        // 5. Check InGameMenu
        InGameMenu igm = FindObjectOfType<InGameMenu>();
        if (igm == null)
        {
            Debug.LogWarning("[Setup] InGameMenu not found - ESC menu won't work in multiplayer");
        }
        
        // Mark everything dirty
        if (gsm != null) EditorUtility.SetDirty(gsm);
        if (mm != null) EditorUtility.SetDirty(mm);
        if (spm != null) EditorUtility.SetDirty(spm);
        
        string message = "";
        if (created > 0)
        {
            message = $"Created {created} missing manager(s).\n\n";
        }
        
        message += "Scene Status:\n";
        message += $"• GameSceneManager: {(gsm != null ? "OK" : "MISSING")}\n";
        message += $"• MultiplayerManager: {(mm != null ? "OK" : "MISSING")}\n";
        message += $"• SinglePlayerManager: {(spm != null ? "OK" : "MISSING")}\n";
        message += $"• NetworkManager: {(nm != null ? "OK" : "MISSING")}\n";
        message += $"• InGameMenu: {(igm != null ? "OK" : "NOT FOUND")}\n";
        message += "\nDon't forget to SAVE THE SCENE!";
        
        EditorUtility.DisplayDialog("Multiplayer Scene Setup", message, "OK");
    }
}
