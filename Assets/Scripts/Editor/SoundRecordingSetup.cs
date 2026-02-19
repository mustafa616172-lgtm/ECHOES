using UnityEngine;
using UnityEditor;

/// <summary>
/// ECHOES - Sound Recording Scene Setup Tool
/// Menu: Tools > ECHOES > Setup Sound Recording
/// 
/// Automates the setup of the Sound Recording system in a scene:
/// - Adds "GhostSound" tag to project
/// - Creates SoundRecordingUI in the scene
/// - Validates existing SoundRecorderDevice setup
/// - Can create sample GhostAudioSource and VoiceLockDoor objects
/// </summary>
public class SoundRecordingSetup : EditorWindow
{
    private bool createSampleGhost = true;
    private bool createSampleDoor = true;
    private bool createUI = true;
    private string sampleClipID = "doctor_voice";
    private string sampleClipName = "Doktorun Sesi";

    [MenuItem("Tools/ECHOES/Setup Sound Recording")]
    public static void ShowWindow()
    {
        GetWindow<SoundRecordingSetup>("Sound Recording Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("ECHOES - Sound Recording System Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Tag setup
        EditorGUILayout.LabelField("1. Tag Setup", EditorStyles.boldLabel);
        if (GUILayout.Button("Add 'GhostSound' Tag"))
        {
            AddGhostSoundTag();
        }
        EditorGUILayout.Space(5);

        // UI Setup
        EditorGUILayout.LabelField("2. UI Setup", EditorStyles.boldLabel);
        createUI = EditorGUILayout.Toggle("Create SoundRecordingUI", createUI);
        EditorGUILayout.Space(5);

        // Sample objects
        EditorGUILayout.LabelField("3. Sample Objects (Optional)", EditorStyles.boldLabel);
        createSampleGhost = EditorGUILayout.Toggle("Create Sample Ghost Source", createSampleGhost);
        createSampleDoor = EditorGUILayout.Toggle("Create Sample Voice Lock Door", createSampleDoor);
        
        if (createSampleGhost || createSampleDoor)
        {
            EditorGUILayout.Space(3);
            sampleClipID = EditorGUILayout.TextField("Clip ID", sampleClipID);
            sampleClipName = EditorGUILayout.TextField("Clip Display Name", sampleClipName);
        }
        EditorGUILayout.Space(10);

        // Execute
        if (GUILayout.Button("Setup Scene", GUILayout.Height(30)))
        {
            SetupScene();
        }

        EditorGUILayout.Space(10);

        // Validation
        EditorGUILayout.LabelField("4. Validation", EditorStyles.boldLabel);
        if (GUILayout.Button("Validate Scene Setup"))
        {
            ValidateScene();
        }
    }

    private void SetupScene()
    {
        int stepCount = 0;

        // 1. Add tag
        AddGhostSoundTag();
        stepCount++;

        // 2. Create UI
        if (createUI)
        {
            CreateSoundRecordingUI();
            stepCount++;
        }

        // 3. Sample ghost source
        if (createSampleGhost)
        {
            CreateSampleGhostSource();
            stepCount++;
        }

        // 4. Sample door
        if (createSampleDoor)
        {
            CreateSampleVoiceLockDoor();
            stepCount++;
        }

        Debug.Log("[SoundRecordingSetup] Scene setup complete! " + stepCount + " steps executed.");
        EditorUtility.DisplayDialog("Setup Complete",
            stepCount + " setup steps completed successfully.\n\nRemember to assign AudioClips to GhostAudioSource components!",
            "OK");
    }

    private void AddGhostSoundTag()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        bool tagExists = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "GhostSound")
            {
                tagExists = true;
                break;
            }
        }

        if (!tagExists)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "GhostSound";
            tagManager.ApplyModifiedProperties();
            Debug.Log("[SoundRecordingSetup] Added 'GhostSound' tag to project.");
        }
        else
        {
            Debug.Log("[SoundRecordingSetup] 'GhostSound' tag already exists.");
        }
    }

    private void CreateSoundRecordingUI()
    {
        // Check if already exists
        SoundRecordingUI existing = Object.FindObjectOfType<SoundRecordingUI>();
        if (existing != null)
        {
            Debug.Log("[SoundRecordingSetup] SoundRecordingUI already exists in scene.");
            return;
        }

        GameObject uiObj = new GameObject("SoundRecordingUI");
        uiObj.AddComponent<SoundRecordingUI>();
        Undo.RegisterCreatedObjectUndo(uiObj, "Create SoundRecordingUI");
        Selection.activeGameObject = uiObj;

        Debug.Log("[SoundRecordingSetup] Created SoundRecordingUI in scene.");
    }

    private void CreateSampleGhostSource()
    {
        GameObject ghostObj = new GameObject("GhostAudioSource_Sample");
        GhostAudioSource ghost = ghostObj.AddComponent<GhostAudioSource>();
        ghost.clipID = sampleClipID;
        ghost.clipDisplayName = sampleClipName;

        // Add a sphere collider for OverlapSphere detection
        SphereCollider col = ghostObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // Position in front of scene camera
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            ghostObj.transform.position = sceneView.camera.transform.position +
                                           sceneView.camera.transform.forward * 3f;
        }

        Undo.RegisterCreatedObjectUndo(ghostObj, "Create Sample Ghost Source");
        Selection.activeGameObject = ghostObj;

        Debug.Log("[SoundRecordingSetup] Created sample GhostAudioSource. Assign an AudioClip to 'ghostClip'!");
    }

    private void CreateSampleVoiceLockDoor()
    {
        // Create door parent
        GameObject doorParent = new GameObject("VoiceLockDoor_Sample");
        VoiceLockDoor voiceDoor = doorParent.AddComponent<VoiceLockDoor>();
        voiceDoor.requiredClipID = sampleClipID;
        voiceDoor.requiredVoiceName = sampleClipName;

        // Add collider for interaction raycast
        BoxCollider col = doorParent.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 2.5f, 0.15f);
        col.center = new Vector3(0f, 1.25f, 0f);

        // Create door model (simple cube visual)
        GameObject doorModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorModel.name = "DoorModel";
        doorModel.transform.SetParent(doorParent.transform, false);
        doorModel.transform.localScale = new Vector3(1f, 2.5f, 0.1f);
        doorModel.transform.localPosition = new Vector3(0f, 1.25f, 0f);

        // Remove the primitive's collider (we use the parent's)
        Object.DestroyImmediate(doorModel.GetComponent<BoxCollider>());

        voiceDoor.doorModel = doorModel.transform;

        // Position
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            doorParent.transform.position = sceneView.camera.transform.position +
                                             sceneView.camera.transform.forward * 5f;
        }

        Undo.RegisterCreatedObjectUndo(doorParent, "Create Sample Voice Lock Door");
        Selection.activeGameObject = doorParent;

        Debug.Log("[SoundRecordingSetup] Created sample VoiceLockDoor. ClipID='" + sampleClipID + "'");
    }

    private void ValidateScene()
    {
        int issues = 0;
        string report = "=== Scene Validation Report ===\n\n";

        // Check SoundRecorderDevice
        SoundRecorderDevice device = Object.FindObjectOfType<SoundRecorderDevice>();
        if (device != null)
        {
            report += "OK: SoundRecorderDevice found on '" + device.gameObject.name + "'\n";
        }
        else
        {
            report += "WARNING: No SoundRecorderDevice in scene (will be on pickup item)\n";
        }

        // Check GhostAudioSources
        GhostAudioSource[] ghosts = Object.FindObjectsOfType<GhostAudioSource>();
        report += "INFO: " + ghosts.Length + " GhostAudioSource(s) found\n";
        foreach (GhostAudioSource g in ghosts)
        {
            if (g.ghostClip == null)
            {
                report += "  ERROR: '" + g.gameObject.name + "' has no ghostClip assigned!\n";
                issues++;
            }
            if (g.GetComponent<Collider>() == null)
            {
                report += "  ERROR: '" + g.gameObject.name + "' has no Collider (OverlapSphere needs one)!\n";
                issues++;
            }
            else
            {
                report += "  OK: '" + g.gameObject.name + "' (clipID='" + g.clipID + "')\n";
            }
        }

        // Check VoiceLockDoors
        VoiceLockDoor[] doors = Object.FindObjectsOfType<VoiceLockDoor>();
        report += "INFO: " + doors.Length + " VoiceLockDoor(s) found\n";
        foreach (VoiceLockDoor d in doors)
        {
            bool hasMatchingGhost = false;
            foreach (GhostAudioSource g in ghosts)
            {
                if (g.clipID == d.requiredClipID)
                {
                    hasMatchingGhost = true;
                    break;
                }
            }

            if (!hasMatchingGhost)
            {
                report += "  WARNING: '" + d.gameObject.name + "' requires clipID '" + d.requiredClipID + "' but no GhostAudioSource has this ID!\n";
                issues++;
            }
            else
            {
                report += "  OK: '" + d.gameObject.name + "' (requires='" + d.requiredClipID + "')\n";
            }
        }

        // Check UI
        SoundRecordingUI ui = Object.FindObjectOfType<SoundRecordingUI>();
        if (ui != null)
        {
            report += "OK: SoundRecordingUI found\n";
        }
        else
        {
            report += "WARNING: No SoundRecordingUI in scene\n";
            issues++;
        }

        // Check tag exists
        bool tagFound = false;
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "GhostSound")
            {
                tagFound = true;
                break;
            }
        }

        if (tagFound)
        {
            report += "OK: 'GhostSound' tag exists\n";
        }
        else
        {
            report += "ERROR: 'GhostSound' tag not found! Click 'Add GhostSound Tag' to fix.\n";
            issues++;
        }

        report += "\n=== " + issues + " issue(s) found ===";
        Debug.Log(report);

        if (issues == 0)
        {
            EditorUtility.DisplayDialog("Validation Passed",
                "All checks passed! Scene is ready for Sound Recording.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Validation Issues",
                issues + " issue(s) found. Check Console for details.", "OK");
        }
    }
}
