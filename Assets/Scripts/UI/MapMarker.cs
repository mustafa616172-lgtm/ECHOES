using UnityEngine;

public class MapMarker : MonoBehaviour
{
    [Header("Marker Settings")]
    public Sprite icon;
    public Color color = Color.white;
    public float iconSize = 4f; // Increased default size
    public bool rotateWithObject = false;
    
    [Header("Filter")]
    public MarkerType type = MarkerType.POI;
    
    private GameObject markerObj;
    private SpriteRenderer spriteRenderer;
    
    public enum MarkerType
    {
        Player,
        POI, // Point of Interest
        Item,
        Enemy
    }
    
    void Start()
    {
        CreateMarkerVisuals();
        MapSystem.Instance?.RegisterMarker(this);
    }
    
    void CreateMarkerVisuals()
    {
        if (icon == null) return;
        
        // Create a child object for the marker
        markerObj = new GameObject("MapMarker_Visual");
        markerObj.transform.SetParent(transform);
        markerObj.transform.localPosition = new Vector3(0, 50, 0); // High above object
        markerObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Face up (for top-down camera)
        markerObj.transform.localScale = Vector3.one * iconSize;
        
        // Set Layer to 'Ignore Raycast' (2) - MapCamera will render this
        markerObj.layer = 2; 

        // Add SpriteRenderer
        spriteRenderer = markerObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = icon;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 10; // Ensure on top of map geometry
    }
    
    void LateUpdate()
    {
        if (markerObj == null) return;
        
        // Keep marker rotation fixed (always facing North/Up on map) unless specified
        if (!rotateWithObject)
        {
            markerObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        else
        {
            // For player arrow, rotate around Y
            markerObj.transform.rotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);
        }
    }

    void OnDestroy()
    {
        MapSystem.Instance?.UnregisterMarker(this);
        if (markerObj != null) Destroy(markerObj);
    }

    public void SetVisible(bool state)
    {
        if (markerObj != null) markerObj.SetActive(state);
    }
}
