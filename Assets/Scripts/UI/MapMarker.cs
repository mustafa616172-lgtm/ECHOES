using UnityEngine;

public class MapMarker : MonoBehaviour
{
    [Header("Marker Settings")]
    public MarkerType markerType = MarkerType.POI;
    public Color markerColor = Color.white;
    public float iconScale = 4f;
    public bool rotateWithObject = false;

    public enum MarkerType
    {
        Player,
        SaveRoom,
        Item,
        Enemy,
        Exit,
        POI
    }

    private GameObject iconObj;
    private SpriteRenderer sr;
    private float iconHeight = 140f;

    void Start()
    {
        // Get dynamic height from MapSystem if available
        if (MapSystem.Instance != null)
            iconHeight = MapSystem.Instance.GetMarkerIconHeight();

        CreateIcon();

        if (MapSystem.Instance != null)
            MapSystem.Instance.RegisterMarker(this);
    }

    void LateUpdate()
    {
        if (iconObj == null) return;

        Vector3 pos = transform.position;
        pos.y = iconHeight;
        iconObj.transform.position = pos;

        if (rotateWithObject)
        {
            float yaw = transform.eulerAngles.y;
            iconObj.transform.rotation = Quaternion.Euler(90f, 0f, -yaw);
        }
    }

    void OnEnable()
    {
        // Show icon when object becomes active
        if (iconObj != null)
            iconObj.SetActive(true);
    }

    void OnDisable()
    {
        // Hide icon when object becomes inactive
        if (iconObj != null)
            iconObj.SetActive(false);
    }

    void OnDestroy()
    {
        if (MapSystem.Instance != null)
            MapSystem.Instance.UnregisterMarker(this);
        if (iconObj != null)
            Destroy(iconObj);
    }

    void CreateIcon()
    {
        int layer = 31;
        int found = LayerMask.NameToLayer("Minimap");
        if (found != -1) layer = found;

        iconObj = new GameObject("MapIcon_" + gameObject.name);
        iconObj.layer = layer;
        iconObj.hideFlags = HideFlags.DontSave;

        sr = iconObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = (markerType == MarkerType.Player) ? 10 : 1;

        SetDefaultColors();
        sr.sprite = GenerateSprite();
        sr.color = markerColor;

        iconObj.transform.localScale = Vector3.one * iconScale;

        Vector3 pos = transform.position;
        pos.y = iconHeight;
        iconObj.transform.position = pos;

        if (!rotateWithObject)
            iconObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void SetDefaultColors()
    {
        if (markerColor != Color.white) return;

        switch (markerType)
        {
            case MarkerType.Player: markerColor = new Color(0.1f, 1f, 0.3f, 1f); break;
            case MarkerType.SaveRoom: markerColor = new Color(0.3f, 0.5f, 1f, 1f); break;
            case MarkerType.Item: markerColor = new Color(1f, 0.9f, 0.3f, 1f); break;
            case MarkerType.Enemy: markerColor = new Color(1f, 0.2f, 0.2f, 1f); break;
            case MarkerType.Exit: markerColor = new Color(0.9f, 0.9f, 1f, 1f); break;
            case MarkerType.POI: markerColor = new Color(0.3f, 0.9f, 0.9f, 1f); break;
        }
    }

    Sprite GenerateSprite()
    {
        switch (markerType)
        {
            case MarkerType.Player: return MakeArrow();
            case MarkerType.SaveRoom: return MakeSquare();
            case MarkerType.Enemy: return MakeDiamond();
            case MarkerType.Exit: return MakeDiamond();
            default: return MakeCircle();
        }
    }

    Sprite MakeArrow()
    {
        int s = 64;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] px = new Color[s * s];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        Vector2 top = new Vector2(s / 2f, s - 4);
        Vector2 bL = new Vector2(12, 6);
        Vector2 bR = new Vector2(s - 12, 6);

        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                if (InTriangle(new Vector2(x, y), top, bL, bR))
                    px[y * s + x] = Color.white;

        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite MakeSquare()
    {
        int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                px[y * s + x] = (x >= 4 && x < s - 4 && y >= 4 && y < s - 4) ? Color.white : Color.clear;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite MakeCircle()
    {
        int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] px = new Color[s * s];
        float c = s / 2f;
        float r = s / 2f - 3;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                px[y * s + x] = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) <= r ? Color.white : Color.clear;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite MakeDiamond()
    {
        int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] px = new Color[s * s];
        float c = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                px[y * s + x] = (Mathf.Abs(x - c) + Mathf.Abs(y - c) <= c - 3) ? Color.white : Color.clear;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
    }
}