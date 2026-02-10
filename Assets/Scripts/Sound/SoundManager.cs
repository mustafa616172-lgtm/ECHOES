using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central sound event manager for ECHOES.
/// Tracks all sounds in the game and provides them to AI enemies.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float soundMemoryDuration = 5f; // How long sounds are remembered
    [SerializeField] private bool showDebugGizmos = false;
    
    // Active sounds in the world
    private List<SoundEvent> activeSounds = new List<SoundEvent>();
    
    public enum SoundType
    {
        Footstep,       // Player running
        ObjectDrop,     // Generic object drop
        Glass,          // Glass breaking/dropping (loudest)
        Metal,          // Metal clanking
        Plastic,        // Plastic (quietest)
        Throw           // Object thrown and landed
    }
    
    [System.Serializable]
    public class SoundEvent
    {
        public Vector3 position;
        public float radius;
        public SoundType type;
        public float timestamp;
        public float priority; // Higher = more attractive to AI
        
        public SoundEvent(Vector3 pos, float rad, SoundType soundType)
        {
            position = pos;
            radius = rad;
            type = soundType;
            timestamp = Time.time;
            priority = GetPriorityForType(soundType);
        }
        
        private float GetPriorityForType(SoundType t)
        {
            switch (t)
            {
                case SoundType.Glass: return 1.0f;
                case SoundType.Metal: return 0.8f;
                case SoundType.Throw: return 0.7f;
                case SoundType.ObjectDrop: return 0.6f;
                case SoundType.Footstep: return 0.4f;
                case SoundType.Plastic: return 0.3f;
                default: return 0.5f;
            }
        }
        
        public bool IsExpired(float memoryDuration)
        {
            return Time.time - timestamp > memoryDuration;
        }
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Update()
    {
        // Clean up expired sounds
        activeSounds.RemoveAll(s => s.IsExpired(soundMemoryDuration));
    }
    
    /// <summary>
    /// Emit a sound at a position with given radius.
    /// AI within radius can detect this sound.
    /// </summary>
    public void EmitSound(Vector3 position, float radius, SoundType type)
    {
        SoundEvent newSound = new SoundEvent(position, radius, type);
        activeSounds.Add(newSound);
        
        Debug.Log($"[SoundManager] Sound emitted: {type} at {position}, radius: {radius}m");
    }
    
    /// <summary>
    /// Get the most relevant sound for an AI at given position.
    /// Returns null if no sounds are within detection range.
    /// </summary>
    public SoundEvent GetMostRelevantSound(Vector3 listenerPosition)
    {
        SoundEvent bestSound = null;
        float bestScore = 0f;
        
        foreach (var sound in activeSounds)
        {
            float distance = Vector3.Distance(listenerPosition, sound.position);
            
            // Check if listener is within sound radius
            if (distance <= sound.radius)
            {
                // Score based on priority and proximity (closer = higher score)
                float proximityScore = 1f - (distance / sound.radius);
                float totalScore = sound.priority * proximityScore;
                
                // Fresher sounds are more attractive
                float age = Time.time - sound.timestamp;
                float freshnessMultiplier = 1f - (age / soundMemoryDuration);
                totalScore *= freshnessMultiplier;
                
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestSound = sound;
                }
            }
        }
        
        return bestSound;
    }
    
    /// <summary>
    /// Check if there's any sound within range of listener.
    /// </summary>
    public bool HasSoundInRange(Vector3 listenerPosition, float maxRange)
    {
        foreach (var sound in activeSounds)
        {
            float distance = Vector3.Distance(listenerPosition, sound.position);
            if (distance <= sound.radius && distance <= maxRange)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get all sounds within a certain range.
    /// </summary>
    public List<SoundEvent> GetSoundsInRange(Vector3 position, float range)
    {
        List<SoundEvent> result = new List<SoundEvent>();
        foreach (var sound in activeSounds)
        {
            float distance = Vector3.Distance(position, sound.position);
            if (distance <= sound.radius && distance <= range)
            {
                result.Add(sound);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Clear a specific sound (e.g., when AI reaches it).
    /// </summary>
    public void ClearSound(SoundEvent sound)
    {
        activeSounds.Remove(sound);
    }
    
    /// <summary>
    /// Get default radius for a sound type.
    /// </summary>
    public static float GetDefaultRadius(SoundType type)
    {
        switch (type)
        {
            case SoundType.Glass: return 25f;
            case SoundType.Metal: return 15f;
            case SoundType.Throw: return 12f;
            case SoundType.ObjectDrop: return 10f;
            case SoundType.Footstep: return 10f;
            case SoundType.Plastic: return 8f;
            default: return 10f;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        
        foreach (var sound in activeSounds)
        {
            // Color based on type
            Color c = GetColorForType(sound.type);
            c.a = 0.3f;
            Gizmos.color = c;
            Gizmos.DrawWireSphere(sound.position, sound.radius);
            
            // Draw solid center
            c.a = 0.6f;
            Gizmos.color = c;
            Gizmos.DrawSphere(sound.position, 0.5f);
        }
    }
    
    private Color GetColorForType(SoundType type)
    {
        switch (type)
        {
            case SoundType.Glass: return Color.cyan;
            case SoundType.Metal: return Color.gray;
            case SoundType.Footstep: return Color.yellow;
            case SoundType.Throw: return Color.magenta;
            case SoundType.Plastic: return Color.green;
            default: return Color.white;
        }
    }
}
