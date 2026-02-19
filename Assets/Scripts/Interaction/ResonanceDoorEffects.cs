using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural visual and audio effects for the Resonance Door.
/// Auto-creates LineRenderer cracks, dust particles, groan audio, and emissive glow.
/// Attach to the same GameObject as ResonanceDoor or it auto-attaches.
/// </summary>
public class ResonanceDoorEffects : MonoBehaviour
{
    [Header("Crack Settings")]
    [Tooltip("Max number of crack lines")]
    public int maxCracks = 6;
    [Tooltip("Max length of each crack")]
    public float maxCrackLength = 1.5f;
    public float crackWidth = 0.008f;
    public Color crackColor = new Color(0f, 1f, 1f, 0.9f);

    [Header("Dust Particles")]
    [Tooltip("Quality threshold to start emitting dust")]
    public float dustThreshold = 0.4f;
    public int maxDustParticles = 30;
    public float dustLifetime = 1.5f;

    [Header("Groan Audio")]
    public AudioClip groanClip;
    [Range(0f, 1f)] public float groanMaxVolume = 0.5f;
    public float groanMinPitch = 0.5f;
    public float groanMaxPitch = 1.2f;

    [Header("Emissive Glow")]
    public Color glowColor = new Color(0f, 0.8f, 1f, 1f);
    public float maxGlowIntensity = 2f;

    // References
    private ResonanceDoor door;
    private List<LineRenderer> crackLines = new List<LineRenderer>();
    private ParticleSystem dustSystem;
    private AudioSource groanSource;
    private Renderer doorRenderer;
    private MaterialPropertyBlock propBlock;

    // Crack geometry (pre-generated once)
    private List<Vector3[]> crackPaths = new List<Vector3[]>();

    // State
    private float lastQuality = 0f;
    private bool initialized = false;

    void Start()
    {
        door = GetComponent<ResonanceDoor>();
        if (door == null)
        {
            Debug.LogWarning("[ResonanceDoorEffects] No ResonanceDoor found on " + gameObject.name);
            enabled = false;
            return;
        }

        doorRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        GenerateCrackPaths();
        CreateCrackLines();
        CreateDustSystem();
        CreateGroanAudio();

        initialized = true;
    }

    void Update()
    {
        if (!initialized || door == null) return;

        float quality = door.CurrentQuality;
        lastQuality = quality;

        UpdateCracks(quality);
        UpdateDust(quality);
        UpdateGroan(quality);
        UpdateGlow(quality);
    }

    // ==========================================
    // CRACK GENERATION
    // ==========================================

    private void GenerateCrackPaths()
    {
        crackPaths.Clear();
        Bounds bounds = GetDoorBounds();
        
        for (int i = 0; i < maxCracks; i++)
        {
            // Start from random edge point
            Vector3 start = GetRandomEdgePoint(bounds);
            
            // Generate jagged path inward
            int segments = Random.Range(4, 8);
            Vector3[] path = new Vector3[segments];
            path[0] = start;

            Vector3 direction = (bounds.center - start).normalized;
            float segmentLength = maxCrackLength / segments;

            for (int j = 1; j < segments; j++)
            {
                // Move inward with random jitter
                Vector3 jitter = new Vector3(
                    Random.Range(-0.15f, 0.15f),
                    Random.Range(-0.15f, 0.15f),
                    Random.Range(-0.02f, 0.02f)
                );
                path[j] = path[j - 1] + direction * segmentLength + jitter;
            }

            crackPaths.Add(path);
        }
    }

    private Bounds GetDoorBounds()
    {
        if (doorRenderer != null)
            return doorRenderer.bounds;
        
        // Fallback: estimate from collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            return col.bounds;

        return new Bounds(transform.position, Vector3.one);
    }

    private Vector3 GetRandomEdgePoint(Bounds bounds)
    {
        // Pick a random point on the door's edge (local space)
        int edge = Random.Range(0, 4);
        Vector3 point = bounds.center;
        Vector3 ext = bounds.extents;

        switch (edge)
        {
            case 0: point += new Vector3(-ext.x, Random.Range(-ext.y, ext.y), 0); break; // Left
            case 1: point += new Vector3(ext.x, Random.Range(-ext.y, ext.y), 0); break;  // Right
            case 2: point += new Vector3(Random.Range(-ext.x, ext.x), ext.y, 0); break;  // Top
            case 3: point += new Vector3(Random.Range(-ext.x, ext.x), -ext.y, 0); break; // Bottom
        }

        return point;
    }

    private void CreateCrackLines()
    {
        // Create a parent object for all cracks
        GameObject crackParent = new GameObject("CrackEffects");
        crackParent.transform.SetParent(transform);
        crackParent.transform.localPosition = Vector3.zero;
        crackParent.transform.localRotation = Quaternion.identity;

        for (int i = 0; i < crackPaths.Count; i++)
        {
            GameObject crackObj = new GameObject("Crack_" + i);
            crackObj.transform.SetParent(crackParent.transform);
            crackObj.transform.localPosition = Vector3.zero;
            crackObj.transform.localRotation = Quaternion.identity;

            LineRenderer lr = crackObj.AddComponent<LineRenderer>();
            lr.positionCount = crackPaths[i].Length;
            lr.SetPositions(crackPaths[i]);

            lr.startWidth = crackWidth;
            lr.endWidth = crackWidth * 0.3f;
            lr.startColor = crackColor;
            lr.endColor = new Color(crackColor.r, crackColor.g, crackColor.b, 0.2f);

            // Use default particle material (works without setup)
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = crackColor;

            lr.useWorldSpace = true;
            lr.enabled = false; // Hidden until quality increases

            crackLines.Add(lr);
        }
    }

    private void UpdateCracks(float quality)
    {
        // Cracks start appearing at quality > 0.2
        float crackProgress = Mathf.Clamp01((quality - 0.2f) / 0.8f);

        for (int i = 0; i < crackLines.Count; i++)
        {
            LineRenderer lr = crackLines[i];
            
            // Progressive reveal: show more cracks as quality increases
            float crackThreshold = (float)i / crackLines.Count;
            bool shouldShow = crackProgress > crackThreshold;
            lr.enabled = shouldShow;

            if (shouldShow)
            {
                // Reveal segments progressively
                float segmentProgress = Mathf.Clamp01((crackProgress - crackThreshold) * crackLines.Count);
                int visibleSegments = Mathf.Max(2, Mathf.CeilToInt(segmentProgress * crackPaths[i].Length));
                lr.positionCount = Mathf.Min(visibleSegments, crackPaths[i].Length);
                
                for (int j = 0; j < lr.positionCount; j++)
                    lr.SetPosition(j, crackPaths[i][j]);

                // Pulse width based on quality
                float pulse = 1f + Mathf.Sin(Time.time * 8f + i) * 0.3f * quality;
                lr.startWidth = crackWidth * pulse;

                // Color intensity
                Color c = crackColor * (0.5f + quality * 0.5f);
                c.a = crackColor.a;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.2f);
            }
        }
    }

    // ==========================================
    // DUST PARTICLES
    // ==========================================

    private void CreateDustSystem()
    {
        GameObject dustObj = new GameObject("DustParticles");
        dustObj.transform.SetParent(transform);
        dustObj.transform.localPosition = Vector3.zero;

        dustSystem = dustObj.AddComponent<ParticleSystem>();
        
        var main = dustSystem.main;
        main.startLifetime = dustLifetime;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(0.6f, 0.6f, 0.5f, 0.6f);
        main.maxParticles = maxDustParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.3f;

        var emission = dustSystem.emission;
        emission.rateOverTime = 0; // Controlled manually

        var shape = dustSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        Bounds bounds = GetDoorBounds();
        shape.scale = bounds.size;
        shape.position = bounds.center - transform.position;

        // Color over lifetime: fade out
        var col = dustSystem.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.gray, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        // Use default particle material
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        dustSystem.Stop();
    }

    private void UpdateDust(float quality)
    {
        if (dustSystem == null) return;

        if (quality > dustThreshold)
        {
            if (!dustSystem.isPlaying) dustSystem.Play();

            var emission = dustSystem.emission;
            // More dust as quality increases
            float rate = Mathf.Lerp(0f, maxDustParticles, (quality - dustThreshold) / (1f - dustThreshold));
            emission.rateOverTime = rate;

            // Speed increases with quality
            var main = dustSystem.main;
            main.startSpeed = Mathf.Lerp(0.1f, 0.8f, quality);
        }
        else
        {
            if (dustSystem.isPlaying) dustSystem.Stop();
        }
    }

    // ==========================================
    // GROAN AUDIO
    // ==========================================

    private void CreateGroanAudio()
    {
        groanSource = gameObject.AddComponent<AudioSource>();
        groanSource.loop = true;
        groanSource.playOnAwake = false;
        groanSource.spatialBlend = 1f;
        groanSource.volume = 0f;
        groanSource.minDistance = 1f;
        groanSource.maxDistance = 15f;

        if (groanClip != null)
        {
            groanSource.clip = groanClip;
        }
        else
        {
            // Generate procedural groan-like audio using low frequency oscillation
            // This is a fallback if no clip is provided
            int sampleRate = 44100;
            int seconds = 3;
            AudioClip proceduralGroan = AudioClip.Create("ProceduralGroan", sampleRate * seconds, 1, sampleRate, false);
            float[] samples = new float[sampleRate * seconds];
            
            System.Random rng = new System.Random(42);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                // Low rumble (20-60 Hz) with harmonics
                float signal = Mathf.Sin(t * 30f * Mathf.PI * 2f) * 0.4f;
                signal += Mathf.Sin(t * 45f * Mathf.PI * 2f) * 0.2f;
                signal += Mathf.Sin(t * 17f * Mathf.PI * 2f) * 0.3f;
                // Add metallic stress creaking
                float creak = Mathf.Sin(t * 120f * Mathf.PI * 2f + Mathf.Sin(t * 3f) * 5f) * 0.1f;
                signal += creak;
                // Random crackling
                if (rng.NextDouble() < 0.005)
                    signal += (float)(rng.NextDouble() - 0.5) * 0.5f;
                    
                samples[i] = signal * 0.5f;
            }
            
            proceduralGroan.SetData(samples, 0);
            groanSource.clip = proceduralGroan;
        }
    }

    private void UpdateGroan(float quality)
    {
        if (groanSource == null || groanSource.clip == null) return;

        if (quality > 0.15f)
        {
            if (!groanSource.isPlaying) groanSource.Play();

            // Volume scales with quality
            float targetVol = Mathf.Lerp(0f, groanMaxVolume, (quality - 0.15f) / 0.85f);
            groanSource.volume = Mathf.Lerp(groanSource.volume, targetVol, Time.deltaTime * 5f);

            // Pitch increases with quality (more stressed sound)
            float targetPitch = Mathf.Lerp(groanMinPitch, groanMaxPitch, quality);
            groanSource.pitch = Mathf.Lerp(groanSource.pitch, targetPitch, Time.deltaTime * 3f);
        }
        else
        {
            groanSource.volume = Mathf.Lerp(groanSource.volume, 0f, Time.deltaTime * 8f);
            if (groanSource.volume < 0.01f && groanSource.isPlaying)
                groanSource.Stop();
        }
    }

    // ==========================================
    // EMISSIVE GLOW
    // ==========================================

    private void UpdateGlow(float quality)
    {
        if (doorRenderer == null || propBlock == null) return;

        if (quality > 0.1f)
        {
            // Pulsing glow
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.3f;
            float intensity = Mathf.Lerp(0f, maxGlowIntensity, quality) * pulse;
            
            Color emission = glowColor * intensity;

            doorRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", emission);
            doorRenderer.SetPropertyBlock(propBlock);

            // Enable emission keyword if material supports it
            if (doorRenderer.material != null)
            {
                doorRenderer.material.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            doorRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", Color.black);
            doorRenderer.SetPropertyBlock(propBlock);
        }
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    public void StopAllEffects()
    {
        foreach (var lr in crackLines)
        {
            if (lr != null) lr.enabled = false;
        }

        if (dustSystem != null) dustSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        if (groanSource != null)
        {
            groanSource.volume = 0f;
            groanSource.Stop();
        }

        if (doorRenderer != null && propBlock != null)
        {
            doorRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", Color.black);
            doorRenderer.SetPropertyBlock(propBlock);
        }
    }

    void OnDestroy()
    {
        StopAllEffects();
    }
}
