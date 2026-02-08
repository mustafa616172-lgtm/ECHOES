using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy AI for the Cursed Priest in ECHOES horror game.
/// Uses NavMesh for navigation - enemy stays within walkable areas only.
/// Features: Player detection, chase behavior, patrol mode, and creepy movement.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyWalker : MonoBehaviour
{
    [Header("=== PLAYER DETECTION ===")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float chaseRange = 25f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private string playerTag = "Player";
    
    [Header("=== MOVEMENT SPEEDS ===")]
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    
    [Header("=== PATROL SETTINGS ===")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float pauseDuration = 3f;
    [SerializeField] private float minPatrolDistance = 5f;
    
    [Header("=== HORROR EFFECTS ===")]
    [SerializeField] private bool creepyMovement = true;
    [SerializeField] private float creepySwayAmount = 0.02f;
    [SerializeField] private float creepySwaySpeed = 2f;
    
    [Header("=== ANIMATION (Optional) ===")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkParameterName = "IsWalking";
    [SerializeField] private string runParameterName = "IsRunning";
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string attackParameterName = "IsAttacking";
    [SerializeField] private string roarParameterName = "IsRoaring";
    
    [Header("=== ATTACK SETTINGS ===")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float roarDuration = 1.5f;
    [SerializeField] private float attackDamage = 25f;
    private float lastAttackTime = -99f;
    private bool isRoaring = false;
    private float roarTimer = 0f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugGizmos = true;
    
    // State
    public enum EnemyState { Idle, Patrol, Chase, Attack }
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;
    
    // Private variables
    private Transform targetPlayer;
    private Vector3 startPosition;
    private Vector3 currentPatrolTarget;
    private float pauseTimer = 0f;
    
    private NavMeshAgent navAgent;
    
    public EnemyState CurrentState => currentState;
    public Transform TargetPlayer => targetPlayer;
    
    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
    }
    
    private void Start()
    {
        startPosition = transform.position;
        
        // Configure NavMeshAgent
        navAgent.speed = patrolSpeed;
        navAgent.angularSpeed = 120f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = 0.5f;
        navAgent.autoBraking = true;
        
        // IMPORTANT: Prevent character from sinking into ground
        navAgent.baseOffset = 0f;
        navAgent.updatePosition = true;
        navAgent.updateRotation = true;
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Find player by tag initially
        FindPlayer();
        
        // Set first patrol target
        if (enablePatrol)
        {
            SetNewPatrolTarget();
        }
    }
    
    private void Update()
    {
        // Always try to find player if we don't have one
        if (targetPlayer == null)
        {
            FindPlayer();
        }
        
        // Update state based on player distance
        UpdateState();
        
        // Execute current state behavior
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Patrol:
                HandlePatrol();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Attack:
                HandleAttack();
                break;
        }
        
        // Apply creepy effects
        if (creepyMovement && navAgent.velocity.magnitude > 0.1f)
        {
            ApplyCreepySway();
        }
        
        // Update animations
        UpdateAnimations();
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            targetPlayer = player.transform;
        }
    }
    
    private void UpdateState()
    {
        if (targetPlayer == null)
        {
            if (enablePatrol && currentState != EnemyState.Patrol && currentState != EnemyState.Idle)
            {
                currentState = EnemyState.Patrol;
                navAgent.speed = patrolSpeed;
            }
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        
        EnemyState previousState = currentState;
        
        // State transitions based on distance
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase && distanceToPlayer <= chaseRange)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = enablePatrol ? EnemyState.Patrol : EnemyState.Idle;
        }
        
        // Update speed on state change
        if (previousState != currentState)
        {
            switch (currentState)
            {
                case EnemyState.Patrol:
                    navAgent.speed = patrolSpeed;
                    break;
                case EnemyState.Chase:
                    navAgent.speed = chaseSpeed;
                    break;
                case EnemyState.Attack:
                case EnemyState.Idle:
                    navAgent.speed = 0;
                    break;
            }
        }
    }
    
    private void HandleIdle()
    {
        navAgent.isStopped = true;
    }
    
    private void HandlePatrol()
    {
        navAgent.isStopped = false;
        
        if (pauseTimer > 0)
        {
            navAgent.isStopped = true;
            pauseTimer -= Time.deltaTime;
            return;
        }
        
        navAgent.isStopped = false;
        
        // Check if reached patrol target
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            pauseTimer = pauseDuration;
            SetNewPatrolTarget();
        }
    }
    
    private void HandleChase()
    {
        if (targetPlayer == null) return;
        
        navAgent.isStopped = false;
        navAgent.SetDestination(targetPlayer.position);
    }
    
    private void HandleAttack()
    {
        navAgent.isStopped = true;
        
        // Face the player
        if (targetPlayer != null)
        {
            Vector3 direction = (targetPlayer.position - transform.position).normalized;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            }
        }
        
        // Attack logic
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // First roar, then attack
            if (!isRoaring)
            {
                StartRoar();
            }
        }
        
        // Update roar timer
        if (isRoaring)
        {
            roarTimer -= Time.deltaTime;
            if (roarTimer <= 0)
            {
                EndRoarAndAttack();
            }
        }
    }
    
    private void StartRoar()
    {
        isRoaring = true;
        roarTimer = roarDuration;
        
        if (animator != null && HasParameter(roarParameterName))
        {
            animator.SetBool(roarParameterName, true);
        }
    }
    
    private void EndRoarAndAttack()
    {
        isRoaring = false;
        lastAttackTime = Time.time;
        
        if (animator != null)
        {
            if (HasParameter(roarParameterName))
                animator.SetBool(roarParameterName, false);
            
            if (HasParameter(attackParameterName))
                animator.SetTrigger(attackParameterName);
        }
        
        // Deal damage to player if still in range
        if (targetPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist <= attackRange)
            {
                DealDamageToPlayer();
            }
        }
    }
    
    private void DealDamageToPlayer()
    {
        // Try to find PlayerHealth component
        var playerHealth = targetPlayer.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        
        Debug.Log("[Mutant] Attack hit player! Damage: " + attackDamage);
    }
    
    private void SetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            // Make sure the new point is far enough from current position
            if (Vector3.Distance(transform.position, hit.position) >= minPatrolDistance)
            {
                currentPatrolTarget = hit.position;
                navAgent.SetDestination(currentPatrolTarget);
            }
            else
            {
                // Try again with a different random point
                SetNewPatrolTarget();
            }
        }
        else
        {
            // Fallback to start position
            currentPatrolTarget = startPosition;
            navAgent.SetDestination(currentPatrolTarget);
        }
    }
    
    private void ApplyCreepySway()
    {
        float sway = Mathf.Sin(Time.time * creepySwaySpeed) * creepySwayAmount;
        transform.Rotate(0, sway, 0);
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        float speed = navAgent.velocity.magnitude;
        bool isWalking = currentState == EnemyState.Patrol && speed > 0.1f;
        bool isRunning = currentState == EnemyState.Chase && speed > 0.1f;
        bool isAttacking = currentState == EnemyState.Attack && !isRoaring;
        
        if (HasParameter(walkParameterName))
            animator.SetBool(walkParameterName, isWalking);
        
        if (HasParameter(runParameterName))
            animator.SetBool(runParameterName, isRunning);
        
        if (HasParameter(speedParameterName))
            animator.SetFloat(speedParameterName, speed);
        
        // Attack state is handled in HandleAttack() for proper sequencing
    }
    
    private bool HasParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;
            
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
    
    // Public methods
    public void SetTarget(Transform newTarget)
    {
        targetPlayer = newTarget;
    }
    
    public void StopChasing()
    {
        currentState = enablePatrol ? EnemyState.Patrol : EnemyState.Idle;
    }
    
    public void StartChasing(Transform target)
    {
        targetPlayer = target;
        currentState = EnemyState.Chase;
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        
        // Patrol radius (blue - like NavMesh)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(pos, patrolRadius);
        
        // Detection range (yellow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Chase range (orange)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Attack range (red)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (Application.isPlaying)
        {
            // Show current patrol target
            if (currentState == EnemyState.Patrol)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, currentPatrolTarget);
                Gizmos.DrawWireSphere(currentPatrolTarget, 0.5f);
            }
            
            // Show player direction if chasing
            if (targetPlayer != null && currentState == EnemyState.Chase)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetPlayer.position);
            }
        }
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"State: {currentState}");
        #endif
    }
}
