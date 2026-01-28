using UnityEngine;
using UnityEngine.AI;

namespace HorizonMini.Gameplay
{
    /// <summary>
    /// AI Enemy behavior with full state machine: Idle, Wander, Chase, Attack, Death
    /// Includes animation control and health system
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIEnemyBehavior : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Detection")]
        [SerializeField] private float detectionRange = 50f;
        [SerializeField] private LayerMask playerLayer = -1; // All layers by default

        [Header("Chase Behavior")]
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float chaseUpdateInterval = 0.5f; // Update path every 0.5s when chasing

        [Header("Attack Behavior")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackInterval = 1.5f;
        [SerializeField] private float attackDamage = 30f;

        [Header("Wander Behavior")]
        [SerializeField] private float wanderSpeed = 2f;
        [SerializeField] private float wanderRadius = 20f; // How far to wander from spawn point
        [SerializeField] private float wanderWaitTimeMin = 2f;
        [SerializeField] private float wanderWaitTimeMax = 5f;
        [SerializeField] private float destinationReachedThreshold = 1f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParamName = "Speed";
        [SerializeField] private string isChasingParamName = "IsChasing";
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string isDeadParamName = "IsDead";

        private enum State { Idle, Wandering, Chasing, Attacking, Dead }
        private State currentState = State.Idle;

        private NavMeshAgent agent;
        private Transform playerTransform;
        private Vector3 spawnPosition;

        // Timers
        private float chaseUpdateTimer = 0f;
        private float wanderWaitTimer = 0f;
        private float nextWanderWaitTime = 0f;
        private float attackTimer = 0f;

        // Initialization flag
        private bool isInitialized = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            spawnPosition = transform.position;
            currentHealth = maxHealth;

            // Auto-find Animator if not assigned
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogWarning($"[AIEnemy] No Animator found on {gameObject.name}. Animations will not play.");
                }
            }

            // Ensure enemy is on Enemies layer (so it's excluded from NavMesh)
            int enemyLayer = LayerMask.NameToLayer("Enemies");
            if (enemyLayer == -1)
                enemyLayer = LayerMask.NameToLayer("Enemy"); // Fallback to singular

            if (enemyLayer == -1)
            {
                Debug.LogWarning("[AIEnemy] 'Enemies' or 'Enemy' layer not found. Please create it in Project Settings > Tags and Layers");
            }
            else if (gameObject.layer != enemyLayer)
            {
                gameObject.layer = enemyLayer;
                Debug.Log($"[AIEnemy] Set layer to {LayerMask.LayerToName(enemyLayer)}");
            }
        }

        private void Start()
        {
            // Don't initialize yet - wait for NavMesh to be ready
            // PlayController will call OnNavMeshReady() when NavMesh is baked
            Debug.Log($"[AIEnemy] {gameObject.name} awaiting NavMesh initialization...");
        }

        /// <summary>
        /// Called by PlayController when NavMesh is ready
        /// </summary>
        public void OnNavMeshReady()
        {
            if (isInitialized)
                return;

            Debug.Log($"[AIEnemy] Initializing {gameObject.name}...");

            // Check if agent is on NavMesh
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning($"[AIEnemy] {gameObject.name} is NOT on NavMesh at position {transform.position}! Trying to find nearest NavMesh point...");

                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log($"[AIEnemy] ✓ Warped to NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogError($"[AIEnemy] ✗ No NavMesh found within 10m of {gameObject.name}! AI will not move.");
                    return;
                }
            }
            else
            {
                Debug.Log($"[AIEnemy] ✓ {gameObject.name} is on NavMesh at {transform.position}");
            }

            // Start in wander state
            isInitialized = true;
            EnterWanderState();
        }

        private void Update()
        {
            // Don't update until initialized or if dead
            if (!isInitialized || currentState == State.Dead)
                return;

            // Check for death
            if (currentHealth <= 0)
            {
                EnterDeathState();
                return;
            }

            // Try to find player if we don't have a reference
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    Debug.Log($"[AIEnemy] Found player: {playerObj.name}");
                }
            }

            // Check distances
            bool playerInDetectionRange = IsPlayerInRange();
            bool playerInAttackRange = IsPlayerInAttackRange();

            // State machine
            switch (currentState)
            {
                case State.Idle:
                    if (playerInDetectionRange)
                    {
                        EnterChaseState();
                    }
                    else
                    {
                        EnterWanderState();
                    }
                    break;

                case State.Chasing:
                    if (!playerInDetectionRange)
                    {
                        EnterWanderState();
                    }
                    else if (playerInAttackRange)
                    {
                        EnterAttackState();
                    }
                    else
                    {
                        UpdateChase();
                    }
                    break;

                case State.Attacking:
                    if (!playerInDetectionRange)
                    {
                        EnterWanderState();
                    }
                    else if (!playerInAttackRange)
                    {
                        EnterChaseState();
                    }
                    else
                    {
                        UpdateAttack();
                    }
                    break;

                case State.Wandering:
                    if (playerInDetectionRange)
                    {
                        EnterChaseState();
                    }
                    else
                    {
                        UpdateWander();
                    }
                    break;
            }

            // Update animation parameters
            UpdateAnimations();
        }

        private bool IsPlayerInRange()
        {
            if (playerTransform == null)
                return false;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            return distance <= detectionRange;
        }

        private void EnterChaseState()
        {
            if (currentState == State.Chasing)
                return;

            Debug.Log($"[AIEnemy] Entering CHASE state");
            currentState = State.Chasing;
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            chaseUpdateTimer = 0f;
        }

        private void UpdateChase()
        {
            if (playerTransform == null)
            {
                EnterWanderState();
                return;
            }

            // Update path to player periodically
            chaseUpdateTimer += Time.deltaTime;
            if (chaseUpdateTimer >= chaseUpdateInterval)
            {
                chaseUpdateTimer = 0f;
                agent.SetDestination(playerTransform.position);
            }
        }

        private void EnterWanderState()
        {
            if (currentState == State.Wandering)
                return;

            Debug.Log($"[AIEnemy] Entering WANDER state");
            currentState = State.Wandering;
            agent.speed = wanderSpeed;
            agent.isStopped = false;
            wanderWaitTimer = 0f;
            nextWanderWaitTime = 0f;

            // Pick first wander destination
            PickNewWanderDestination();
        }

        private void UpdateWander()
        {
            // Check if we reached destination
            if (!agent.pathPending && agent.remainingDistance <= destinationReachedThreshold)
            {
                // Wait before picking new destination
                if (wanderWaitTimer < nextWanderWaitTime)
                {
                    wanderWaitTimer += Time.deltaTime;
                }
                else
                {
                    PickNewWanderDestination();
                    wanderWaitTimer = 0f;
                }
            }
        }

        private void PickNewWanderDestination()
        {
            // Pick random point around spawn position
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                bool success = agent.SetDestination(hit.position);
                nextWanderWaitTime = Random.Range(wanderWaitTimeMin, wanderWaitTimeMax);

                if (success)
                {
                    Debug.Log($"[AIEnemy] ✓ Set wander destination: {hit.position} (distance: {Vector3.Distance(transform.position, hit.position):F2}m), wait time: {nextWanderWaitTime:F1}s");
                }
                else
                {
                    Debug.LogWarning($"[AIEnemy] ✗ Failed to set destination to {hit.position}. PathStatus: {agent.pathStatus}");
                }
            }
            else
            {
                Debug.LogWarning($"[AIEnemy] Failed to find valid NavMesh position for wandering (tried at {randomDirection})");
                // Try again after a short wait
                nextWanderWaitTime = 1f;
            }
        }

        private bool IsPlayerInAttackRange()
        {
            if (playerTransform == null)
                return false;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            return distance <= attackRange;
        }

        private void EnterAttackState()
        {
            if (currentState == State.Attacking)
                return;

            Debug.Log($"[AIEnemy] Entering ATTACK state");
            currentState = State.Attacking;
            agent.isStopped = true; // Stop moving while attacking
            attackTimer = 0f;
        }

        private void UpdateAttack()
        {
            if (playerTransform == null)
            {
                EnterWanderState();
                return;
            }

            // Face the player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0; // Keep on horizontal plane
            if (directionToPlayer != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToPlayer), Time.deltaTime * 5f);
            }

            // Attack periodically
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            // Trigger attack animation
            if (animator != null)
            {
                animator.SetTrigger(attackTriggerName);
            }

            Debug.Log($"[AIEnemy] Attacking player for {attackDamage} damage!");

            // Apply damage to player
            if (playerTransform != null)
            {
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"[AIEnemy] ✓ Damaged player for {attackDamage}");
                }
                else
                {
                    Debug.LogWarning("[AIEnemy] Player has no PlayerHealth component!");
                }
            }
        }

        private void EnterDeathState()
        {
            if (currentState == State.Dead)
                return;

            Debug.Log($"[AIEnemy] Entering DEATH state");
            currentState = State.Dead;

            // Stop agent
            agent.isStopped = true;
            agent.enabled = false;

            // Trigger death animation
            if (animator != null)
            {
                animator.SetBool(isDeadParamName, true);
            }

            // Disable collider after a delay (allow death animation to play)
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col, 2f);
            }

            // Destroy gameobject after death animation
            Destroy(gameObject, 5f);
        }

        private void UpdateAnimations()
        {
            if (animator == null)
                return;

            // Set speed parameter based on agent velocity
            float speed = agent.velocity.magnitude;
            animator.SetFloat(speedParamName, speed);

            // Set isChasing parameter
            bool isChasing = (currentState == State.Chasing);
            animator.SetBool(isChasingParamName, isChasing);
        }

        /// <summary>
        /// Take damage from external source (e.g., player attack)
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (currentState == State.Dead)
                return;

            currentHealth -= damage;
            Debug.Log($"[AIEnemy] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                EnterDeathState();
            }
        }

        /// <summary>
        /// Heal enemy (for testing or game mechanics)
        /// </summary>
        public void Heal(float amount)
        {
            if (currentState == State.Dead)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"[AIEnemy] Healed {amount}. Health: {currentHealth}/{maxHealth}");
        }

        // Visualize detection and attack ranges in editor
        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (Application.isPlaying)
            {
                // Wander radius
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(spawnPosition, wanderRadius);
            }
        }
    }
}
