using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to set up the Sound Attraction System in the scene.
/// </summary>
public class SoundSystemSetup : EditorWindow
{
    [MenuItem("ECHOES/Setup Sound System")]
    public static void SetupSoundSystem()
    {
        // Check if SoundManager already exists
        SoundManager existingManager = Object.FindObjectOfType<SoundManager>();
        if (existingManager != null)
        {
            Debug.Log("[SoundSystemSetup] SoundManager already exists in scene!");
            Selection.activeGameObject = existingManager.gameObject;
            return;
        }
        
        // Create SoundManager
        GameObject soundManagerObj = new GameObject("SoundManager");
        soundManagerObj.AddComponent<SoundManager>();
        
        Debug.Log("[SoundSystemSetup] SoundManager created!");
        
        // Select it
        Selection.activeGameObject = soundManagerObj;
        
        EditorUtility.DisplayDialog("Sound System Setup", 
            "SoundManager created!\n\n" +
            "Now you can:\n" +
            "1. Add NoiseMaker + ThrowableObject to objects\n" +
            "2. Set NoiseType (Glass=25m, Metal=15m, Plastic=8m)\n" +
            "3. The mutant will investigate sounds!", 
            "OK");
    }
    
    [MenuItem("ECHOES/Make Object Throwable")]
    public static void MakeObjectThrowable()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject first!", "OK");
            return;
        }
        
        // Add Rigidbody if missing
        if (selected.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = selected.AddComponent<Rigidbody>();
            rb.mass = 1f;
        }
        
        // Add Collider if missing
        if (selected.GetComponent<Collider>() == null)
        {
            selected.AddComponent<BoxCollider>();
        }
        
        // Add NoiseMaker if missing
        if (selected.GetComponent<NoiseMaker>() == null)
        {
            selected.AddComponent<NoiseMaker>();
        }
        
        // Add ThrowableObject if missing
        if (selected.GetComponent<ThrowableObject>() == null)
        {
            selected.AddComponent<ThrowableObject>();
        }
        
        Debug.Log($"[SoundSystemSetup] Made '{selected.name}' throwable with noise!");
        
        EditorUtility.DisplayDialog("Success", 
            $"'{selected.name}' is now throwable!\n\n" +
            "Set the NoiseType in NoiseMaker component:\n" +
            "- Glass = 25m detection\n" +
            "- Metal = 15m detection\n" +
            "- Plastic = 8m detection", 
            "OK");
    }
    
    [MenuItem("ECHOES/Setup Stealth UI")]
    public static void SetupStealthUI()
    {
        // Check if AwarenessIndicator already exists
        AwarenessIndicator existing = Object.FindObjectOfType<AwarenessIndicator>();
        if (existing != null)
        {
            Debug.Log("[SoundSystemSetup] AwarenessIndicator already exists!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        // Create AwarenessIndicator
        GameObject indicatorObj = new GameObject("AwarenessIndicator");
        indicatorObj.AddComponent<AwarenessIndicator>();
        
        Debug.Log("[SoundSystemSetup] AwarenessIndicator created!");
        
        EditorUtility.DisplayDialog("Stealth UI Setup", 
            "AwarenessIndicator created!\n\n" +
            "This shows the mutant's awareness:\n" +
            "• Gray = Unaware\n" +
            "• Yellow = Suspicious (?)\n" +
            "• Orange = Alert (!)\n" +
            "• Red = Hostile (!!!)", 
            "OK");
    }
    
    [MenuItem("ECHOES/Setup Complete System")]
    public static void SetupCompleteSystem()
    {
        SetupSoundSystem();
        SetupStealthUI();
        
        EditorUtility.DisplayDialog("Complete Setup", 
            "Sound + Stealth system ready!\n\n" +
            "Controls:\n" +
            "• Left Ctrl = Crouch (silent movement)\n" +
            "• Space = Hold Breath (stay silent)\n" +
            "• E = Pick up objects\n" +
            "• Left Click = Throw\n" +
            "• Right Click = Drop", 
            "OK");
    }
}

