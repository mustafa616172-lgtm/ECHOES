using UnityEngine;

/// <summary>
/// ECHOES - Item Thumbnail Generator
/// Renders 3D objects to a Sprite for inventory display.
/// Creates a temporary camera to capture a clean preview of any GameObject.
/// </summary>
public static class ItemThumbnailGenerator
{
    private static readonly int THUMBNAIL_SIZE = 128;
    private static readonly Color BACKGROUND_COLOR = new Color(0, 0, 0, 0); // Transparent

    /// <summary>
    /// Generates a Sprite thumbnail from a 3D GameObject.
    /// Creates a temporary clone, renders it with an isolated camera, captures to texture.
    /// </summary>
    /// <param name="sourceObject">The GameObject to capture (original is NOT modified)</param>
    /// <param name="size">Pixel size of the thumbnail (square)</param>
    /// <returns>Sprite with the rendered thumbnail</returns>
    public static Sprite GenerateThumbnail(GameObject sourceObject, int size = 0)
    {
        if (sourceObject == null)
        {
            Debug.LogWarning("[ItemThumbnailGenerator] Source object is null!");
            return null;
        }

        if (size <= 0) size = THUMBNAIL_SIZE;

        // Create temporary layer-isolated clone for rendering
        GameObject clone = Object.Instantiate(sourceObject);
        clone.name = "ThumbnailClone_Temp";
        clone.transform.position = new Vector3(1000f, 1000f, 1000f); // Move far away
        clone.transform.rotation = Quaternion.Euler(15f, -135f, 0f);  // Nice 3/4 view angle
        
        // Ensure clone is active and visible
        clone.SetActive(true);
        SetLayerRecursive(clone, 0); // Default layer
        
        // Disable any scripts to prevent side effects
        MonoBehaviour[] scripts = clone.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            script.enabled = false;
        }

        // Remove physics from clone
        Rigidbody[] rbs = clone.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs) Object.DestroyImmediate(rb);
        Collider[] cols = clone.GetComponentsInChildren<Collider>();
        foreach (var col in cols) Object.DestroyImmediate(col);

        // Calculate bounds to frame the object
        Bounds bounds = CalculateBounds(clone);
        
        // Create temporary render camera
        GameObject camObj = new GameObject("ThumbnailCamera_Temp");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BACKGROUND_COLOR;
        cam.orthographic = true;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        cam.enabled = false; // We render manually
        
        // Position camera to frame the object
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        cam.orthographicSize = maxExtent * 1.3f; // Add margin
        camObj.transform.position = bounds.center + new Vector3(0f, 0.2f, -maxExtent * 3f);
        camObj.transform.LookAt(bounds.center);

        // Add light for the preview
        GameObject lightObj = new GameObject("ThumbnailLight_Temp");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.9f, 0.95f, 1f);
        light.intensity = 1.2f;
        lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        // Create RenderTexture and capture
        RenderTexture rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        cam.Render();

        // Convert RenderTexture to Texture2D
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        // Create Sprite from Texture2D
        Sprite sprite = Sprite.Create(
            texture, 
            new Rect(0, 0, size, size), 
            new Vector2(0.5f, 0.5f),
            100f
        );
        sprite.name = $"Thumbnail_{sourceObject.name}";

        // Cleanup temporary objects
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camObj);
        Object.DestroyImmediate(lightObj);
        Object.DestroyImmediate(clone);

        Debug.Log($"[ItemThumbnailGenerator] Generated {size}x{size} thumbnail for: {sourceObject.name}");
        return sprite;
    }

    /// <summary>
    /// Generates a simple colored icon sprite with a symbol (fallback for objects without renderers)
    /// </summary>
    public static Sprite GenerateColorIcon(Color color, int size = 64)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Color borderColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 1f);
        Color centerColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.9f);
        
        int border = 3;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bool isBorder = x < border || x >= size - border || y < border || y >= size - border;
                
                // Create rounded corner effect
                float cx = (float)x / size - 0.5f;
                float cy = (float)y / size - 0.5f;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                
                if (dist > 0.48f)
                    texture.SetPixel(x, y, Color.clear);
                else if (isBorder || dist > 0.42f)
                    texture.SetPixel(x, y, borderColor);
                else
                    texture.SetPixel(x, y, centerColor);
            }
        }
        
        texture.Apply();
        
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
        
        return sprite;
    }

    static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one * 0.5f);
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        return bounds;
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
