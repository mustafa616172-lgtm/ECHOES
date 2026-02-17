using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ECHOES - Sound Wave UI
/// Visualizes sound frequencies as sine waves on a UI Canvas.
/// Used for the Resonance Door unlocking mechanic.
/// </summary>
public class SoundWaveUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SoundRecorderDevice soundRecorder;
    [SerializeField] private ResonanceDoor targetDoor;
    
    [Header("Wave Settings")]
    [SerializeField] private RectTransform waveContainer;
    [SerializeField] private Color playerWaveColor = Color.green;
    [SerializeField] private Color targetWaveColor = Color.gray;
    [SerializeField] private float waveThickness = 2f;
    [SerializeField] private int resolution = 50; // Points in the line
    [SerializeField] private float amplitude = 50f; // Height of wave
    [SerializeField] private float animationSpeed = 2f;
    
    // Using UI Extensions or LineRenderer? 
    // Since this is UI (Canvas), LineRenderer can be tricky with sorting layers.
    // We'll use a custom UI Line Renderer approach or simple multiple Image segments for robustness if no library is present.
    // For simplicity and performance, we'll use a LineRenderer but screen-space overlay might need World Space UI.
    // Better approach: Use UILineRenderer if available, or create a simple mesh.
    // Let's use Unity's LineRenderer and set the Canvas to Screen Space - Camera or World Space, 
    // OR just use a raw image with a shader. 
    // BUT for maximum compatibility without shaders: We'll generate a mesh.
    
    [Header("Visualization")]
    [SerializeField] private LineRenderer playerLineRenderer;
    [SerializeField] private LineRenderer targetLineRenderer;
    
    private float phase = 0f;
    
    public static SoundWaveUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (soundRecorder == null) soundRecorder = FindObjectOfType<SoundRecorderDevice>();
        
        SetupLineRenderers();
    }
    
    void SetupLineRenderers()
    {
        // Ensure we have LineRenderers if not assigned
        if (playerLineRenderer == null)
        {
            GameObject pObj = new GameObject("PlayerWave");
            pObj.transform.SetParent(transform, false);
            playerLineRenderer = pObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(playerLineRenderer, playerWaveColor);
        }
        
        if (targetLineRenderer == null)
        {
            GameObject tObj = new GameObject("TargetWave");
            tObj.transform.SetParent(transform, false);
            targetLineRenderer = tObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(targetLineRenderer, targetWaveColor);
        }
    }
    
    void ConfigureLineRenderer(LineRenderer lr, Color c)
    {
        lr.useWorldSpace = false; // Important for UI
        lr.startWidth = 0.05f; // Adjust scale
        lr.endWidth = 0.05f;
        lr.positionCount = resolution;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = c;
        lr.endColor = c;
        // Sorting order for UI
        lr.sortingOrder = 10; 
    }
    
    void Update()
    {
        if (soundRecorder == null) return;
        
        // Ensure this UI is only visible when needed (interacting)
        if (targetDoor == null) 
        {
            // If no door assigned, maybe hide or disable
            // For now, let's keep it running if active to show current frequency
        }
        
        DrawWaves();
    }
    
    public void SetTargetDoor(ResonanceDoor door)
    {
        targetDoor = door;
        Show();
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public void UpdateFrequencies(float targetFreq, float currentFreq)
    {
        // For now, we mainly use this to ensure the UI is refreshed or parameters are updated if we move away from direct reference
        // But since we have references, we can just ensure we are active
        if (!gameObject.activeSelf) Show();
    }
    
    void DrawWaves()
    {
        phase += Time.deltaTime * animationSpeed;
        
        // 1. Player Wave (Dynamic based on SoundRecorderDevice frequency)
        float playerFreqNormalized = soundRecorder.CurrentFrequency / 100f; // Scale for visual
        DrawSineWave(playerLineRenderer, playerFreqNormalized, phase, 1f);
        
        // 2. Target Wave (Static/Ghost based on Door frequency)
        if (targetDoor != null)
        {
            float targetFreqNormalized = targetDoor.requiredFrequency / 100f;
            DrawSineWave(targetLineRenderer, targetFreqNormalized, phase, 0.5f); // Lower alpha/amplitude?
        }
        else
        {
            targetLineRenderer.positionCount = 0;
        }
    }
    
    void DrawSineWave(LineRenderer lr, float frequency, float offset, float alphaScale)
    {
        lr.positionCount = resolution;
        
        float width = 400f; // Width of the wave area in local units
        float startX = -width / 2f;
        float step = width / resolution;
        
        for (int i = 0; i < resolution; i++)
        {
            float x = startX + (i * step);
            // Sine wave formula: y = Amplitude * sin(Frequency * x + Phase)
            // Normalize x for frequency calculation
            float normalizedX = (float)i / resolution * 2f * Mathf.PI;
            
            float y = Mathf.Sin(normalizedX * frequency + offset) * amplitude;
            
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}
