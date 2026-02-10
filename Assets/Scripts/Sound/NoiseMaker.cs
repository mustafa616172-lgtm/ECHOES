using UnityEngine;

/// <summary>
/// Component for objects that make noise when dropped or collided.
/// Attach this to any object that should attract the enemy when dropped.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class NoiseMaker : MonoBehaviour
{
    public enum NoiseType
    {
        Plastic,    // Quiet - 8m
        Metal,      // Medium - 15m
        Glass       // Loud - 25m
    }
    
    [Header("Noise Settings")]
    [SerializeField] private NoiseType noiseType = NoiseType.Plastic;
    [SerializeField] private float customRadius = 0f; // 0 = use default for type
    [SerializeField] private float minImpactVelocity = 2f; // Minimum velocity to make sound
    [SerializeField] private float noiseCooldown = 0.5f; // Prevent spam
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float volume = 1f;
    
    private float lastNoiseTime;
    private Rigidbody rb;
    private AudioSource audioSource;
    
    public NoiseType Type => noiseType;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Create audio source if we have a clip
        if (impactSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = impactSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = volume;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check cooldown
        if (Time.time - lastNoiseTime < noiseCooldown) return;
        
        // Check velocity
        float impactVelocity = collision.relativeVelocity.magnitude;
        if (impactVelocity < minImpactVelocity) return;
        
        // Don't make noise when colliding with player
        if (collision.gameObject.CompareTag("Player")) return;
        
        // Make the noise!
        MakeNoise();
        lastNoiseTime = Time.time;
    }
    
    /// <summary>
    /// Manually trigger noise (called when thrown/dropped).
    /// </summary>
    public void MakeNoise()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[NoiseMaker] SoundManager not found in scene!");
            return;
        }
        
        float radius = GetNoiseRadius();
        SoundManager.SoundType soundType = ConvertToSoundType(noiseType);
        
        SoundManager.Instance.EmitSound(transform.position, radius, soundType);
        
        // Play audio if available
        if (audioSource != null && impactSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight variation
            audioSource.Play();
        }
        
        Debug.Log($"[NoiseMaker] {noiseType} noise at {transform.position}, radius: {radius}m");
    }
    
    private float GetNoiseRadius()
    {
        if (customRadius > 0) return customRadius;
        
        switch (noiseType)
        {
            case NoiseType.Glass: return 25f;
            case NoiseType.Metal: return 15f;
            case NoiseType.Plastic: return 8f;
            default: return 10f;
        }
    }
    
    private SoundManager.SoundType ConvertToSoundType(NoiseType type)
    {
        switch (type)
        {
            case NoiseType.Glass: return SoundManager.SoundType.Glass;
            case NoiseType.Metal: return SoundManager.SoundType.Metal;
            case NoiseType.Plastic: return SoundManager.SoundType.Plastic;
            default: return SoundManager.SoundType.ObjectDrop;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Show noise radius in editor
        float radius = GetNoiseRadius();
        
        Color c = noiseType switch
        {
            NoiseType.Glass => Color.cyan,
            NoiseType.Metal => Color.gray,
            NoiseType.Plastic => Color.green,
            _ => Color.white
        };
        
        c.a = 0.2f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
