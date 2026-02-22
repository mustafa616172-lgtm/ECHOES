using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ECHOES - Simple Echo Pulse Effect
/// Simplified version using particle system and line renderer instead of shaders.
/// Creates expanding rings and highlights objects briefly.
/// </summary>
public class SimpleEchoPulseEffect : MonoBehaviour
{
    [Header("Pulse Visual")]
    [SerializeField] private Color pulseColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private float ringThickness = 0.3f;
    [SerializeField] private int ringSegments = 64;
    
    [Header("Object Highlight")]
    [SerializeField] private LayerMask highlightLayers = ~0;
    [SerializeField] private float highlightDuration = 2f;
    [SerializeField] private Color highlightColor = new Color(0.1f, 0.9f, 1f, 0.6f);
    
    private bool isPulseActive = false;
    private float pulseProgress = 0f;
    private float currentRadius = 0f;
    private float currentMaxRadius = 30f;
    private float currentDuration = 3f;
    private GameObject pulseRingObject;
    private LineRenderer lineRenderer;
    
    private List<Renderer> highlightedObjects = new List<Renderer>();
    private Dictionary<Renderer, Color> originalEmissions = new Dictionary<Renderer, Color>();
    
    void Start()
    {
        CreatePulseRing();
    }
    
    void CreatePulseRing()
    {
        // Create ring GameObject
        pulseRingObject = new GameObject("EchoPulseRing");
        pulseRingObject.transform.SetParent(transform);
        pulseRingObject.transform.localPosition = Vector3.zero;
        
        // Add LineRenderer
        lineRenderer = pulseRingObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = ringThickness;
        lineRenderer.endWidth = ringThickness;
        lineRenderer.positionCount = ringSegments;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = pulseColor;
        lineRenderer.endColor = pulseColor;
        
        // Start invisible
        pulseRingObject.SetActive(false);
    }
    
    void Update()
    {
        if (isPulseActive)
        {
            pulseProgress += Time.deltaTime;
            float normalizedTime = pulseProgress / currentDuration;
            
            // Expand pulse radius
            currentRadius = Mathf.Lerp(0, currentMaxRadius, normalizedTime);
            
            // Update ring size and fade
            UpdateRingVisual(normalizedTime);
            
            // Highlight nearby objects
            if (normalizedTime < 0.5f) // Only during first half
            {
                HighlightObjectsAtRadius(currentRadius);
            }
            
            // End pulse when complete
            if (normalizedTime >= 1f)
            {
                EndPulse();
            }
        }
    }
    
    void UpdateRingVisual(float normalizedTime)
    {
        if (lineRenderer == null) return;
        
        // Update ring positions
        float angleStep = 360f / ringSegments;
        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * currentRadius;
            float z = Mathf.Sin(angle) * currentRadius;
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
        }
        
        // Fade out
        float alpha = 1f - normalizedTime;
        Color color = pulseColor;
        color.a *= alpha;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
    
    void HighlightObjectsAtRadius(float radius)
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, radius + 2f, highlightLayers);
        
        foreach (Collider col in nearbyObjects)
        {
            Renderer rend = col.GetComponent<Renderer>();
            if (rend != null && !highlightedObjects.Contains(rend))
            {
                StartCoroutine(HighlightObject(rend));
            }
        }
    }
    
    IEnumerator HighlightObject(Renderer rend)
    {
        highlightedObjects.Add(rend);
        
        // Save original emission
        Material mat = rend.material;
        Color originalEmission = Color.black;
        bool hasEmission = mat.HasProperty("_EmissionColor");
        
        if (hasEmission)
        {
            originalEmission = mat.GetColor("_EmissionColor");
            originalEmissions[rend] = originalEmission;
            
            // Enable emission and set highlight color
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", highlightColor);
        }
        else
        {
            yield break; // Can't highlight without emission property
        }
        
        // Wait a bit before fading start (keep bright for a moment)
        yield return new WaitForSeconds(0.2f);
        
        // FADE OUT
        float elapsed = 0f;
        float fadeDuration = highlightDuration; // Use the duration for the fade
        Color startColor = highlightColor;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            // Smooth step for nicer fade
            t = t * t * (3f - 2f * t);
            
            if (rend != null && mat != null)
            {
                mat.SetColor("_EmissionColor", Color.Lerp(startColor, originalEmission, t));
            }
            else
            {
                break; // Renderer destroyed
            }
            yield return null;
        }
        
        // Restore original emission exactly
        if (rend != null && mat != null && originalEmissions.ContainsKey(rend))
        {
            mat.SetColor("_EmissionColor", originalEmissions[rend]);
            originalEmissions.Remove(rend);
        }
        
        highlightedObjects.Remove(rend);
    }
    
    public void TriggerPulse(float radius, float speed, float duration, float frequency)
    {
        // Stop current pulse if active
        if (isPulseActive)
        {
            EndPulse();
        }
        
        isPulseActive = true;
        pulseProgress = 0f;
        currentRadius = 0f;
        currentMaxRadius = radius;
        currentDuration = duration;
        
        // Adjust color based on frequency
        float normalizedFreq = Mathf.InverseLerp(20f, 20000f, frequency);
        pulseColor = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 1f), normalizedFreq);
        
        // Show ring
        if (pulseRingObject != null)
        {
            pulseRingObject.SetActive(true);
        }
        
        Debug.Log($"[SimpleEchoPulseEffect] Pulse triggered! Radius: {radius}, Duration: {duration}, Frequency: {frequency}Hz");
    }
    
    void EndPulse()
    {
        isPulseActive = false;
        pulseProgress = 0f;
        currentRadius = 0f;
        
        if (pulseRingObject != null)
        {
            pulseRingObject.SetActive(false);
        }
        
        // Clear all highlighted objects
        StopAllCoroutines();
        foreach (var kvp in originalEmissions)
        {
            if (kvp.Key != null && kvp.Key.material.HasProperty("_EmissionColor"))
            {
                kvp.Key.material.SetColor("_EmissionColor", kvp.Value);
            }
        }
        highlightedObjects.Clear();
        originalEmissions.Clear();
    }
    
    void OnDestroy()
    {
        EndPulse();
        if (pulseRingObject != null)
        {
            Destroy(pulseRingObject);
        }
    }
}
