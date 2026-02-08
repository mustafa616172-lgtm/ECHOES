using UnityEngine;

/// <summary>
/// Player health system for single player mode.
/// Handles damage, death, and health regeneration.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Regeneration")]
    [SerializeField] private bool enableRegeneration = true;
    [SerializeField] private float regenRate = 5f;
    [SerializeField] private float regenDelay = 3f;
    
    [Header("Effects")]
    [SerializeField] private bool showDamageScreen = true;
    
    private float lastDamageTime;
    private bool isDead = false;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    
    public delegate void HealthChanged(float current, float max);
    public event HealthChanged OnHealthChanged;
    
    public delegate void PlayerDied();
    public event PlayerDied OnPlayerDied;
    
    private void Start()
    {
        currentHealth = maxHealth;
    }
    
    private void Update()
    {
        if (isDead) return;
        
        // Health regeneration
        if (enableRegeneration && currentHealth < maxHealth)
        {
            if (Time.time - lastDamageTime >= regenDelay)
            {
                currentHealth += regenRate * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        lastDamageTime = Time.time;
        
        Debug.Log("[PlayerHealth] Took damage: " + damage + ", Current health: " + currentHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (showDamageScreen)
        {
            ShowDamageEffect();
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void SetMaxHealth(float newMax, bool healToFull = false)
    {
        maxHealth = newMax;
        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void ShowDamageEffect()
    {
        // TODO: Implement screen damage effect (red vignette, etc.)
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        Debug.Log("[PlayerHealth] Player died!");
        
        OnPlayerDied?.Invoke();
        
        // Disable player controls
        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        // Show death screen or restart game
        // This can be handled by subscribing to OnPlayerDied event
    }
    
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
