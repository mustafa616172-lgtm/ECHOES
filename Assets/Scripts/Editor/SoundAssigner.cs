using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to assign sound files to game components.
/// </summary>
public class SoundAssigner : EditorWindow
{
    [MenuItem("ECHOES/Assign Sounds to Player")]
    public static void AssignSoundsToPlayer()
    {
        // Find Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try to find by name
            player = GameObject.Find("Player");
        }
        
        if (player == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Player not found!\n\nMake sure there's a GameObject with tag 'Player' in the scene.", 
                "OK");
            return;
        }
        
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "PlayerController not found on Player!", 
                "OK");
            return;
        }
        
        // Load footstep sound
        AudioClip footstep = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Ses/Ses Efektleri/Adim/data_pion-st1-footstep-sfx-323053.mp3");
        
        if (footstep == null)
        {
            // Try alternative path
            footstep = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Ses/Ses Efektleri/Adým/data_pion-st1-footstep-sfx-323053.mp3");
        }
        
        // Load breath sound
        AudioClip breath = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Ses/Ses Efektleri/Nefes/stamina az nefes sesi.MP3");
        
        if (breath == null)
        {
            breath = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Ses/Ses Efektleri/Nefes/freesound_community-breathing-6811.mp3");
        }
        
        // Use SerializedObject to modify private serialized fields
        SerializedObject so = new SerializedObject(pc);
        
        if (footstep != null)
        {
            SerializedProperty footstepProp = so.FindProperty("footstepSound");
            if (footstepProp != null)
            {
                footstepProp.objectReferenceValue = footstep;
                Debug.Log($"[SoundAssigner] Assigned footstep sound: {footstep.name}");
            }
        }
        
        if (breath != null)
        {
            SerializedProperty breathProp = so.FindProperty("breathSound");
            if (breathProp != null)
            {
                breathProp.objectReferenceValue = breath;
                Debug.Log($"[SoundAssigner] Assigned breath sound: {breath.name}");
            }
        }
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(pc);
        
        string message = "Sounds assigned to Player:\n\n";
        message += footstep != null ? $"Footstep: {footstep.name}\n" : "Footstep: NOT FOUND\n";
        message += breath != null ? $"Breath: {breath.name}" : "Breath: NOT FOUND";
        
        EditorUtility.DisplayDialog("Sound Assignment", message, "OK");
    }
    
    [MenuItem("ECHOES/List Available Sounds")]
    public static void ListAvailableSounds()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Ses" });
        
        string message = $"Found {guids.Length} audio clips:\n\n";
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileName(path);
            message += $"- {fileName}\n";
        }
        
        Debug.Log(message);
        EditorUtility.DisplayDialog("Available Sounds", message, "OK");
    }
}
