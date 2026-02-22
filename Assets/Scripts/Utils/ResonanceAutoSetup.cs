using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ECHOES - Resonance Auto Setup Tool
/// Automates the assignment of scripts and UI for the Resonance System.
/// </summary>
public class ResonanceAutoSetup : MonoBehaviour
{
    [Header("Setup Options")]
    public bool setupPlayer = true;
    public bool setupDoors = true;
    
    [ContextMenu("Run Auto Setup")]
    public void RunSetup()
    {
        if (setupPlayer) SetupPlayerDevice();
        if (setupDoors) SetupResonanceDoors();
        
        Debug.Log("Resonance System Setup Complete!");
    }
    
    void SetupPlayerDevice()
    {
        // 1. Find SoundRecorderDevice
        SoundRecorderDevice device = FindObjectOfType<SoundRecorderDevice>();
        if (device == null)
        {
            Debug.LogError("Could not find SoundRecorderDevice component in scene! Please add it to your player or device model first.");
            return;
        }
        
        Debug.Log($"Found SoundRecorderDevice on {device.name}");
        
        // 2. Check for UI Canvas
        Canvas existingCanvas = device.GetComponentInChildren<Canvas>();
        if (existingCanvas == null)
        {
            GameObject canvasObj = new GameObject("DeviceCanvas");
            canvasObj.transform.SetParent(device.transform);
            canvasObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            canvasObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
            canvasObj.transform.localScale = Vector3.one * 0.001f; // World space scale
            
            existingCanvas = canvasObj.AddComponent<Canvas>();
            existingCanvas.renderMode = RenderMode.WorldSpace;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("Created new DeviceCanvas");
        }
        
        // 3. Add SoundWaveUI (SoundRecorder handles visualization directly)
        SoundWaveUI waveUI = device.GetComponentInChildren<SoundWaveUI>();
        if (waveUI == null)
        {
            GameObject waveObj = new GameObject("SoundWaveUI");
            waveObj.transform.SetParent(existingCanvas.transform, false);
            waveUI = waveObj.AddComponent<SoundWaveUI>();
            
            // Create RectTransform params
            RectTransform rt = waveObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 200);
            rt.anchoredPosition = new Vector3(0, 150, 0); // Position above other UI
            
            Debug.Log("Added SoundWaveUI component");
        }
        
        // Cleanup old EchoDeviceUI if exists (optional, or just ignore it)
        

        
#if UNITY_EDITOR
        if (waveUI != null)
        {
            SerializedObject soWave = new SerializedObject(waveUI);
            soWave.Update();
            SerializedProperty propRecorder = soWave.FindProperty("soundRecorder");
            if (propRecorder.objectReferenceValue == null) propRecorder.objectReferenceValue = device;
            soWave.ApplyModifiedProperties();
        }
#endif
    }
    
    void SetupResonanceDoors()
    {
        ResonanceDoor[] doors = FindObjectsOfType<ResonanceDoor>();
        Debug.Log($"Found {doors.Length} Resonance Doors");
        
        foreach (var door in doors)
        {
            // Ensure door has audio sources setup
            // The Start() method does this, but we can verify
            if (door.GetComponent<AudioSource>() == null)
            {
                // Door script handles creating them at runtime
            }
        }
    }
}
