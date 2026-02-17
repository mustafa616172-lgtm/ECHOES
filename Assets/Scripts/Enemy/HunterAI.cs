using UnityEngine;
using UnityEngine.AI;
using System.Collections;   

/// <summary>
/// "Avcı" (Hunter) AI for the Mutant Enemy in ECHOES.
/// completely BLIND. Relies on SOUND (Noise Events) to detect the player.
/// responding to "Noise Events" from SoundManager.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HunterAI : MonoBehaviour
{
    [Header("=== HEARING SETTINGS ===")]
    [SerializeField] private float hearingRange = 30f; // Ne kadar uzaktan duyabilir
    [SerializeField] private float investigateSpeed = 3.5f; // Ses kaynağına gitme hızı
    [SerializeField] private float chaseSpeed = 5.0f; // Kesin yer tespiti sonrası kovalama hızı
    [SerializeField] private float patrolSpeed = 1.5f; // Devriye hızı
    
    [Header("=== LISTENING BEHAVIOR ===")]
    [SerializeField] private float listenDuration = 3f; // Ses kaynağına vardığında ne kadar dinleyecek
    [SerializeField] private bool showDebugGizmos = true;

    [Header("=== ATTACK SETTINGS ===")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 35f;

    [Header("=== ANIMATION ===")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkParam = "IsWalking";
    [SerializeField] private string runParam = "IsRunning";
    [SerializeField] private string attackParam = "Attack";
    [SerializeField] private string listenParam = "IsListening"; // Yeni dinleme animasyonu için
    [SerializeField] private string roarParam = "Roar";
    
    // State Machine
    public enum State { Patrol, Investigate, Listen, Chase, Attack }
    [SerializeField] private State currentState = State.Patrol;

    private NavMeshAgent navAgent;
    private Vector3 currentTargetPos;
    private float stateTimer;
    private float lastAttackTime;
    private Transform playerTransform; // Sadece temas veya çok yüksek sesle kesinleşir

    [Header("=== PATROL SETTINGS ===")]
    [SerializeField] private PatrolPath patrolPath; // Assign in Inspector
    [SerializeField] private float waypointWaitTime = 2f;
    private int currentWaypointIndex;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        // Initialize patrol
        if (patrolPath != null)
        {
            patrolPath.RefreshWaypoints();
            // Start at closest point
            currentWaypointIndex = patrolPath.GetClosestWaypointIndex(transform.position);
            SetPatrolDestination();
        }
    }

    private void Update()
    {
        // 1. Sesleri Dinle (Her zaman aktif)
        ListenForSounds();

        // 2. Durum Makinesi
        switch (currentState)
        {
            case State.Patrol:
                HandlePatrol();
                break;
            case State.Investigate:
                HandleInvestigate();
                break;
            case State.Listen:
                HandleListen();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Attack:
                HandleAttack();
                break;
        }

        // 3. Animasyonları Güncelle
        UpdateAnimations();
        
        // 4. Temas Kontrolü (Kör olduğu için çarpışma önemli)
        CheckPhysicalContact();
    }

    private void ListenForSounds()
    {
        // SoundManager yoksa işlem yapma
        if (SoundManager.Instance == null) return;

        // En önemli sesi al
        var loudSound = SoundManager.Instance.GetMostRelevantSound(transform.position);

        if (loudSound != null)
        {
            float dist = Vector3.Distance(transform.position, loudSound.position);
            
            // Duyma menzili içindeyse
            if (dist <= hearingRange)
            {
                // Eğer zaten saldırıyorsak veya kovalıyorsak, sadece çok yakın sesler fikrimizi değiştirir
                if ((currentState == State.Chase || currentState == State.Attack) && dist > 5f)
                    return;

                // Yeni bir ses duydu! Oraya git.
                // Eğer ses çok yeniyse ve bizden farklı bir yerdeyse
                if (Vector3.Distance(currentTargetPos, loudSound.position) > 2f)
                {
                    Debug.Log($"[HunterAI] Ses duyuldu! Tür: {loudSound.type}, Mesafe: {dist}");
                    GoToNoiseSource(loudSound.position);
                }
            }
        }
    }

    private void GoToNoiseSource(Vector3 pos)
    {
        currentTargetPos = pos;
        navAgent.SetDestination(currentTargetPos);
        navAgent.speed = investigateSpeed;
        currentState = State.Investigate;
    }

    private void HandlePatrol()
    {
        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            // Hedefe vardı, biraz bekle
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                if (patrolPath != null)
                {
                    // Sonraki noktaya geç (Tanımlı Rota)
                    currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
                    SetPatrolDestination();
                }
                else
                {
                    // Rastgele Rota (NavMesh üzerinde)
                    SetRandomPatrolPoint();
                }
            }
        }
    }

    private void SetPatrolDestination()
    {
        if (patrolPath == null) return;
        
        Transform wp = patrolPath.GetWaypoint(currentWaypointIndex);
        if (wp != null)
        {
            SetDestinationSafe(wp.position);
            currentState = State.Patrol;
            stateTimer = waypointWaitTime;
        }
    }

    private void SetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * 15f;
        randomDir += transform.position;
        NavMeshHit hit;
        
        // NavMesh üzerinde rastgele bir nokta bul
        if (NavMesh.SamplePosition(randomDir, out hit, 15f, NavMesh.AllAreas))
        {
            SetDestinationSafe(hit.position);
            currentState = State.Patrol;
            stateTimer = Random.Range(2f, 5f);
        }
    }
    
    private void SetDestinationSafe(Vector3 target)
    {
        navAgent.speed = patrolSpeed;
        navAgent.SetDestination(target);
    }

    private void HandleInvestigate()
    {
        // Ses kaynağına gidiyor
        if (!navAgent.pathPending && navAgent.remainingDistance < 1.0f)
        {
            // Vardı, şimdi dinleme moduna geç
            Debug.Log("[HunterAI] Ses kaynağına ulaştı. Dinliyor...");
            currentState = State.Listen;
            stateTimer = listenDuration;
            navAgent.isStopped = true; // Dur
        }
    }

    private void HandleListen()
    {
        // Etrafı dinliyor (Animasyon oynayacak)
        stateTimer -= Time.deltaTime;
        
        // Hafifçe etrafa dönme eklenebilir
        transform.Rotate(0, 10f * Time.deltaTime, 0);

        if (stateTimer <= 0)
        {
            // Bir şey duyamadı, devriyeye dön
            Debug.Log("[HunterAI] Tehdit yok. Devriyeye dönüyor.");
            navAgent.isStopped = false;
            
            if (patrolPath != null)
            {
                // En yakın devriye noktasına dön
                currentWaypointIndex = patrolPath.GetClosestWaypointIndex(transform.position);
                SetPatrolDestination();
            }
            else
            {
                // Rastgele gezmeye devam et
                SetRandomPatrolPoint();
            }
        }
    }

    private void HandleChase()
    {
        // Oyuncuyu kesin olarak tespit etti (temas veya sürekli gürültü)
        if (playerTransform != null)
        {
            navAgent.SetDestination(playerTransform.position);
            
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= attackRange)
            {
                currentState = State.Attack;
            }
            else if (dist > hearingRange * 1.5f) // Çok uzaklaştı
            {
                playerTransform = null; // İzini kaybettik
                currentState = State.Listen; // Durup dinleyelim
                stateTimer = 2f;
            }
        }
        else
        {
            currentState = State.Listen;
        }
    }

    private void HandleAttack()
    {
        navAgent.isStopped = true;
        if (playerTransform != null)
        {
            // Oyuncuya dön
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) 
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > attackRange)
            {
                currentState = State.Chase;
                navAgent.isStopped = false;
                return;
            }
        }

        if (Time.time - lastAttackTime > attackCooldown)
        {
            // Saldır
            lastAttackTime = Time.time;
            if (animator != null) animator.SetTrigger(attackParam);
            
            // Hasar ver
            if (playerTransform != null)
            {
                var hp = playerTransform.GetComponent<PlayerHealth>();
                if (hp != null) hp.TakeDamage(attackDamage);
            }
        }
    }

    private void CheckPhysicalContact()
    {
        // Basit birOverlapSphere ile oyuncuya değdi mi kontrol et
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.0f, LayerMask.GetMask("Player", "Default"));
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Ahaa! Dokunduk!
                playerTransform = hit.transform;
                if (currentState != State.Attack && currentState != State.Chase)
                {
                    Debug.Log("[HunterAI] Oyuncuya çarptı! Saldırı başlıyor!");
                    currentState = State.Attack;
                    if(animator) animator.SetTrigger(roarParam);
                }
            }
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Reset triggers if needed, or rely on state
        // For boolean parameters:
        animator.SetBool(walkParam, currentState == State.Patrol && navAgent.velocity.magnitude > 0.1f);
        animator.SetBool(runParam, (currentState == State.Chase || currentState == State.Investigate) && navAgent.velocity.magnitude > 0.1f);
        animator.SetBool(listenParam, currentState == State.Listen);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Duyma alanı
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Durum
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Durum: {currentState}");
        #endif
    }
}
