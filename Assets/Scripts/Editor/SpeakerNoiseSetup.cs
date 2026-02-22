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
}
