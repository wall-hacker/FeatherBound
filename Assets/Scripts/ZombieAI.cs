using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class ZombieAI : MonoBehaviour
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float centerAttractionStrength = 0.3f; // 0-1, higher = more drawn to center
    [SerializeField] private float minIdleTime = 2f;
    [SerializeField] private float maxIdleTime = 6f;
    [SerializeField] private float minWalkTime = 3f;
    [SerializeField] private float maxWalkTime = 8f;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float loseTargetDistance = 20f;
    [SerializeField] private float fieldOfView = 120f;
    [SerializeField] private LayerMask detectionMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float detectionCheckInterval = 0.3f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 1f;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Scream Settings")]
    [SerializeField] private bool alwaysScreamOnDetection = false;
    [SerializeField] private float screamChance = 0.5f; // 50% chance to scream
    [SerializeField] private float screamDuration = 2f;
    
    [Header("Knife Detection Settings")]
    [SerializeField] private float knifeDetectionRadius = 2f;
    [SerializeField] private float behindAngleThreshold = 60f; // Degrees from behind
    
    [Header("References")]
    [SerializeField] private Animator zombieAnimator;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform player;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip idleSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip screamSound;

    [Header("Drop Items")]
    [SerializeField] Item coinItem;
    
    // Components
    private NavMeshAgent agent;
    private EnemyHealth healthScript;
    
    // State machine
    public enum ZombieState { Idle, Wandering, Chasing, Screaming, Attacking, Dead }
    private ZombieState currentState = ZombieState.Idle;
    
    // Spawn position (center of wander area)
    private Vector3 spawnPosition;
    
    // Timers
    private float stateTimer;
    private float nextAttackTime;
    private float detectionTimer;
    
    // Animation hashes
    private int speedHash;
    private int attackHash;
    private int dieHash;
    private int screamHash;
    
    // Target tracking
    private bool hasDetectedPlayer;
    private bool isAttacking;
    private bool hasScreamed;

    private PlayerStats playerStats;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        healthScript = GetComponent<EnemyHealth>();
        
        if (zombieAnimator == null)
            zombieAnimator = GetComponentInChildren<Animator>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Cache animation hashes
        speedHash = Animator.StringToHash("Speed");
        attackHash = Animator.StringToHash("Attack");
        dieHash = Animator.StringToHash("Die");
        screamHash = Animator.StringToHash("Scream");
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Configure NavMeshAgent
        agent.speed = walkSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Start in idle state
        SetState(ZombieState.Idle);
        
        // Register damage callback with EnemyHealth
        if (healthScript != null)
        {
            healthScript.OnDamageTaken += OnTookDamage;
        }
    }

    void OnDestroy()
    {
        // Unregister callback to prevent memory leaks
        if (healthScript != null)
        {
            healthScript.OnDamageTaken -= OnTookDamage;
        }
    }

    // Called when zombie takes damage
    public void OnTookDamage()
    {
        if (currentState == ZombieState.Dead) return;
        
        // Immediately detect and target the player
        if (!hasDetectedPlayer)
        {
            hasDetectedPlayer = true;
            
            // Decide whether to scream first
            bool shouldScream = alwaysScreamOnDetection || Random.value < screamChance;
            
            if (shouldScream && !hasScreamed)
            {
                SetState(ZombieState.Screaming);
            }
            else
            {
                SetState(ZombieState.Chasing);
            }
        }
        else if (currentState != ZombieState.Attacking && currentState != ZombieState.Screaming)
        {
            // Already detected, just make sure we're chasing
            SetState(ZombieState.Chasing);
        }
    }

    void Update()
    {
        // Check if dead
        if (healthScript.IsDead && currentState != ZombieState.Dead)
        {
            SetState(ZombieState.Dead);
            return;
        }
        
        if (currentState == ZombieState.Dead) return;
        
        // Check for knife kill opportunity
        CheckKnifeKill();
        
        // Update detection timer
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            DetectPlayer();
            detectionTimer = detectionCheckInterval;
        }
        
        // Update state machine
        UpdateState();
        
        // Update animator
        UpdateAnimator();
    }

    void UpdateState()
    {
        stateTimer -= Time.deltaTime;
        
        switch (currentState)
        {
            case ZombieState.Idle:
                HandleIdleState();
                break;
                
            case ZombieState.Wandering:
                HandleWanderingState();
                break;
                
            case ZombieState.Chasing:
                HandleChasingState();
                break;
                
            case ZombieState.Screaming:
                HandleScreamingState();
                break;
                
            case ZombieState.Attacking:
                HandleAttackingState();
                break;
        }
    }

    void HandleIdleState()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // Stop any residual movement
        
        if (stateTimer <= 0f)
        {
            // Randomly decide to wander
            SetState(ZombieState.Wandering);
        }
    }

    void HandleWanderingState()
    {
        agent.isStopped = false;
        
        // Check if reached destination or timer expired
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance || stateTimer <= 0f)
        {
            SetState(ZombieState.Idle);
        }
    }

    void HandleChasingState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if lost player
        if (distanceToPlayer > loseTargetDistance)
        {
            hasDetectedPlayer = false;
            hasScreamed = false;
            SetState(ZombieState.Idle);
            return;
        }
        
        // Check if in attack range
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            SetState(ZombieState.Attacking);
            return;
        }
        
        // Only move towards player if NOT in attack range
        if (distanceToPlayer > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // Stop moving if already in attack range (waiting for cooldown)
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            
            // Face the player while waiting
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction.magnitude > 0.1f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void HandleScreamingState()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // Completely stop movement
        
        // Look at player
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
        
        if (stateTimer <= 0f)
        {
            SetState(ZombieState.Chasing);
        }
    }

    void HandleAttackingState()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // Completely stop movement to prevent gliding
        
        // Look at player during attack
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * 2f * Time.deltaTime);
        }
        
        if (stateTimer <= 0f)
        {
            isAttacking = false;
            nextAttackTime = Time.time + attackCooldown;
            
            // After attack, check if player is still in range
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                // If player is still very close, stay in chasing state (which will keep zombie stationary)
                // The chasing state will handle attacking again when cooldown expires
                SetState(ZombieState.Chasing);
            }
            else
            {
                SetState(ZombieState.Idle);
            }
        }
    }

    public void SetState(ZombieState newState)
    {
        currentState = newState;
        
        switch (newState)
        {
            case ZombieState.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.speed = walkSpeed;
                stateTimer = Random.Range(minIdleTime, maxIdleTime);
                break;
                
            case ZombieState.Wandering:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                stateTimer = Random.Range(minWalkTime, maxWalkTime);
                SetRandomWanderDestination();
                break;
                
            case ZombieState.Chasing:
                agent.isStopped = false;
                agent.speed = runSpeed;
                break;
                
            case ZombieState.Screaming:
                agent.isStopped = true;
                agent.velocity = Vector3.zero; // Ensure no movement during scream
                stateTimer = screamDuration;
                zombieAnimator.SetTrigger(screamHash);
                PlaySound(screamSound);
                hasScreamed = true;
                break;
                
            case ZombieState.Attacking:
                agent.isStopped = true;
                agent.velocity = Vector3.zero; // Ensure no movement during attack
                stateTimer = attackAnimationDuration;
                isAttacking = true;
                zombieAnimator.SetTrigger(attackHash);
                PlaySound(attackSound);
                PerformAttack();
                break;
                
            case ZombieState.Dead:
                // agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.enabled = false;
                zombieAnimator.SetTrigger(dieHash);
                this.enabled = false;
                break;
        }
    }

    void SetRandomWanderDestination()
    {
        // Generate random point in wander radius
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Apply center attraction - bias towards spawn center
        Vector3 towardsCenter = (spawnPosition - randomPoint).normalized * (wanderRadius * centerAttractionStrength);
        randomPoint += towardsCenter;
        
        // Find nearest valid point on NavMesh
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Fallback: go to spawn position
            agent.SetDestination(spawnPosition);
        }
    }

    void DetectPlayer()
    {
        if (player == null || currentState == ZombieState.Dead) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // If already detected and chasing, keep chasing
        if (hasDetectedPlayer && distanceToPlayer <= loseTargetDistance)
        {
            if (currentState != ZombieState.Attacking && currentState != ZombieState.Screaming)
            {
                SetState(ZombieState.Chasing);
            }
            return;
        }
        
        // Check if player is in detection radius
        if (distanceToPlayer <= detectionRadius)
        {
            // Check if player is in field of view
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            
            if (angleToPlayer <= fieldOfView / 2f)
            {
                // Raycast to check if player is visible (no obstacles)
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, detectionRadius, detectionMask | obstacleMask))
                {
                    if (hit.transform == player)
                    {
                        // Player detected!
                        OnPlayerDetected();
                    }
                }
            }
        }
    }

    void OnPlayerDetected()
    {
        if (hasDetectedPlayer) return;
        
        hasDetectedPlayer = true;
        
        // Decide whether to scream first
        bool shouldScream = alwaysScreamOnDetection || Random.value < screamChance;
        
        if (shouldScream && !hasScreamed)
        {
            SetState(ZombieState.Screaming);
        }
        else
        {
            SetState(ZombieState.Chasing);
        }
    }

    void PerformAttack()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            playerStats.TakeDamage(attackDamage);
            Debug.Log($"Zombie attacked player for {attackDamage} damage!");
        }
    }

    void CheckKnifeKill()
    {
        if (player == null || hasDetectedPlayer) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is close enough
        if (distanceToPlayer <= knifeDetectionRadius)
        {
            // Check if player is behind zombie
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            
            // Player is behind if angle is > 180 - threshold
            if (angleToPlayer >= (180f - behindAngleThreshold))
            {
                // Check if player is crouching (sneaking)
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null && playerController.IsSneaking)
                {
                    // Show knife prompt (you can implement UI for this)
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        PerformKnifeKill();
                    }
                }
            }
        }
    }

    void PerformKnifeKill()
    {
        StartCoroutine(KnifeKillRoutine());
    }

    IEnumerator KnifeKillRoutine()
    {
        Debug.Log("Knife kill started");

        // Play knife animation
        playerAnimator.SetTrigger("Knife");

        yield return new WaitForSeconds(0.4f);

        Debug.Log("Zombie killed by knife!");

        SetState(ZombieState.Dead);

        // Instant kill
        if (healthScript != null)
        {
            healthScript.TakeDamage(healthScript.MaxHealth);
        }

        // Reward player with random coins amount
        if (playerStats.IsStealthCoinActive && playerStats.BuffedKillsRemaining > 0)
        {
            int randomizedCoinAmount = Random.Range(7, 9);
            InventorySystem.Instance.AddItem(coinItem, randomizedCoinAmount);
            playerStats.DecreaseBuffedKills();
        }
    }

    void UpdateAnimator()
    {
        if (zombieAnimator == null) return;
        
        // Calculate speed (0 = idle, 0.5 = walking, 1 = running)
        float speed = 0f;
        
        switch (currentState)
        {
            case ZombieState.Idle:
            case ZombieState.Screaming:
            case ZombieState.Attacking:
                speed = 0f;
                break;
                
            case ZombieState.Wandering:
                speed = 0.5f;
                break;
                
            case ZombieState.Chasing:
                // Only show running animation if actually moving
                float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : 0f;
                speed = distanceToPlayer > attackRange ? 1f : 0f;
                break;
        }
        
        zombieAnimator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        // Draw wander radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, wanderRadius);
        
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw field of view
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView / 2f, Vector3.up) * transform.forward * detectionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView / 2f, Vector3.up) * transform.forward * detectionRadius;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);
        
        // Draw knife detection area (behind)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position - transform.forward * 0.5f, knifeDetectionRadius);
    }

    public void SetLoseTargetDistance(float newDistance)
    {
        loseTargetDistance = newDistance;
    }
}