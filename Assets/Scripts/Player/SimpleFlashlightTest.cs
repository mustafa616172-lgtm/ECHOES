using UnityEngine;

/// <summary>
/// SUPER SIMPLE - Just adds a big glowing sphere in front of camera
/// If you can't see this, there's a bigger problem!
/// </summary>
public class SimpleFlashlightTest : MonoBehaviour
{
    private GameObject testSphere;
    private Light testLight;
    private bool isOn = false;
    
    void Start()
    {
        // Create a BIG obvious sphere
        testSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        testSphere.name = "TEST_FLASHLIGHT_SPHERE";
        testSphere.transform.SetParent(transform);
        testSphere.transform.localPosition = new Vector3(0.5f, -0.3f, 1f); // In front of camera
        testSphere.transform.localScale = Vector3.one * 0.3f; // BIG
        
        // Make it BRIGHT YELLOW
        var renderer = testSphere.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.yellow;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.yellow * 5f);
        renderer.material = mat;
        
        // Remove collider
        Destroy(testSphere.GetComponent<Collider>());
        
        // Add a light
        GameObject lightObj = new GameObject("TEST_LIGHT");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, 0, 0.5f);
        testLight = lightObj.AddComponent<Light>();
        testLight.type = LightType.Spot;
        testLight.intensity = 10f;
        testLight.range = 25f;
        testLight.spotAngle = 60f;
        testLight.color = Color.white;
        
        // Start OFF
        testSphere.SetActive(false);
        testLight.enabled = false;
        
        Debug.Log("===== SIMPLE TEST FLASHLIGHT READY =====");
        Debug.Log("Press L to toggle - You MUST see a BIG YELLOW SPHERE!");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            isOn = !isOn;
            testSphere.SetActive(isOn);
            testLight.enabled = isOn;
            
            if (isOn)
            {
                Debug.Log("===== FLASHLIGHT ON - LOOK FOR BIG YELLOW SPHERE! =====");
            }
            else
            {
                Debug.Log("===== FLASHLIGHT OFF =====");
            }
        }
    }
}
