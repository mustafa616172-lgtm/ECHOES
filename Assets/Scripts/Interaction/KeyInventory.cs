using UnityEngine;
using System.Collections.Generic;

public class KeyInventory : MonoBehaviour
{
    public static KeyInventory Instance;
    public List<string> collectedKeys = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    public void AddKey(string keyID)
    {
        if (!collectedKeys.Contains(keyID))
        {
            collectedKeys.Add(keyID);
            Debug.Log($"Key Added: {keyID}");
        }
    }

    public bool HasKey(string keyID)
    {
        return collectedKeys.Contains(keyID);
    }
}
