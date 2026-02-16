using UnityEngine;
using UnityEditor;

public class SpeakerNoiseSetup : EditorWindow
{
    [MenuItem("ECHOES/Setup Speaker System")]
    static void SetupSpeakerSystem()
    {
        int speakerCount = 0;
        bool doorLinked = false;
        bool switchLinked = false;

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (!obj.name.Contains("Hoparlor")) continue;
            if (obj.name == "HoparlorSwitch") continue;

            MeshRenderer mesh = obj.GetComponent<MeshRenderer>();
            if (mesh == null)
            {
                bool hasChildren = false;
                foreach (Transform child in obj.transform)
                {
                    if (child.name.Contains("Hoparlor"))
                    {
                        hasChildren = true;
                        break;
                    }
                }
                if (hasChildren) continue;
            }

            if (obj.GetComponent<AudioSource>() == null)
                Undo.AddComponent<AudioSource>(obj);
            if (obj.GetComponent<SpeakerStaticNoise>() == null)
                Undo.AddComponent<SpeakerStaticNoise>(obj);

            speakerCount++;
        }

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Kapimentese"))
            {
                DoorInteractable door = obj.GetComponent<DoorInteractable>();
                if (door == null) door = obj.GetComponentInChildren<DoorInteractable>();
                if (door != null && door.GetComponent<SpeakerDoorLink>() == null)
                {
                    Undo.AddComponent<SpeakerDoorLink>(door.gameObject);
                    doorLinked = true;
                }
                break;
            }
        }

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "HoparlorSwitch")
            {
                if (obj.GetComponent<SpeakerSwitch>() == null)
                    Undo.AddComponent<SpeakerSwitch>(obj);
                if (obj.GetComponent<Collider>() == null)
                {
                    BoxCollider col = Undo.AddComponent<BoxCollider>(obj);
                    col.size = Vector3.one * 0.3f;
                }
                switchLinked = true;
                break;
            }
        }

        string msg = speakerCount + " hoparlore statik ses eklendi\n";
        msg += doorLinked ? "Kapi baglantisi kuruldu\n" : "";
        msg += switchLinked ? "Anahtar baglantisi kuruldu\n" : "";
        EditorUtility.DisplayDialog("Speaker System Setup", msg, "Tamam");
    }

    [MenuItem("ECHOES/Setup Story System")]
    static void SetupStorySystem()
    {
        string result = "";

        // 1. Create StorySequenceManager
        StorySequenceManager existingManager = GameObject.FindObjectOfType<StorySequenceManager>();
        if (existingManager == null)
        {
            GameObject managerObj = new GameObject("StorySequenceManager");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create StorySequenceManager");
            Undo.AddComponent<StorySequenceManager>(managerObj);
            result += "StorySequenceManager olusturuldu\n";
        }
        else
        {
            result += "StorySequenceManager zaten var\n";
        }

        // 2. Create SoundRoomTrigger (user needs to position it)
        SoundRoomTrigger existingTrigger = GameObject.FindObjectOfType<SoundRoomTrigger>();
        if (existingTrigger == null)
        {
            GameObject triggerObj = new GameObject("SoundRoomTrigger");
            Undo.RegisterCreatedObjectUndo(triggerObj, "Create SoundRoomTrigger");
            Undo.AddComponent<SoundRoomTrigger>(triggerObj);
            result += "SoundRoomTrigger olusturuldu (kapiya yerlestirin!)\n";
        }

        // 3. Create FirstRoomTrigger
        FirstRoomTrigger existingReturn = GameObject.FindObjectOfType<FirstRoomTrigger>();
        if (existingReturn == null)
        {
            GameObject returnObj = new GameObject("FirstRoomTrigger");
            Undo.RegisterCreatedObjectUndo(returnObj, "Create FirstRoomTrigger");
            Undo.AddComponent<FirstRoomTrigger>(returnObj);
            result += "FirstRoomTrigger olusturuldu (ilk odaya yerlestirin!)\n";
        }

        // 4. Create RoomStateChanger
        RoomStateChanger existingChanger = GameObject.FindObjectOfType<RoomStateChanger>();
        if (existingChanger == null)
        {
            GameObject changerObj = new GameObject("RoomStateChanger");
            Undo.RegisterCreatedObjectUndo(changerObj, "Create RoomStateChanger");
            Undo.AddComponent<RoomStateChanger>(changerObj);
            result += "RoomStateChanger olusturuldu (yatak ve lambayi atayiniz!)\n";
        }

        result += "\nSONRAKI ADIMLAR:\n";
        result += "1. SoundRoomTrigger -> ses odasinin girisine konumlandir\n";
        result += "2. FirstRoomTrigger -> ilk odanin girisine konumlandir\n";
        result += "3. RoomStateChanger -> Changes arrayine yatak ve lambayi ekle\n";
        result += "4. StorySequenceManager -> Inspector'dan referanslari ata\n";
        result += "5. Hayalet figuru olusturup EchoVisibleObject ekle\n";

        EditorUtility.DisplayDialog("Story System Setup", result, "Tamam");
    }
}
