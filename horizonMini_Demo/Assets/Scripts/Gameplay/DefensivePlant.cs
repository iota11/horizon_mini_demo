using UnityEngine;

namespace HorizonMini.Gameplay
{
    /// <summary>
    /// Defensive plant that attacks enemies within range
    /// Has 1 second windup before attacking
    /// </summary>
    public class DefensivePlant : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Detection")]
        [SerializeField] private float detectionRange = 3f;
        [SerializeField] private LayerMask enemyLayer = -1; // All layers by default

        [Header("Attack")]
        [SerializeField] private float attackDamage = 50f;
        [SerializeField] private float attackWindup = 1f; // 1 second windup
        [SerializeField] private float attackCooldown = 2f; // Time between attacks

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string windupTriggerName = "Windup";
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string isDeadParamName = "IsDead";

        [Header("Visual Feedback")]
        [SerializeField] private GameObject attackIndicator; // Optional visual effect during windup

        private enum State { Idle, Windup, Attack, Dead }
        private State currentState = State.Idle;

        private Transform currentTarget;
        private float windupTimer = 0f;
        private float attackCooldownTimer = 0f;

        private void Awake()
        {
            currentHealth = maxHealth;

            // Auto-find Animator if not assigned
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Hide attack indicator initially
            if (attackIndicator != null)
            {
                attackIndicator.SetActive(false);
            }
        }

        private void Update()
        {
            if (currentState == State.Dead)
                return;

            // Check for death
            if (currentHealth <= 0)
            {
                EnterDeathState();
                return;
            }

            // Update cooldown timer
            if (attackCooldownTimer > 0)
            {
                attackCooldownTimer -= Time.deltaTime;
            }

            // State machine
            switch (currentState)
            {
                case State.Idle:
                    UpdateIdle();
                    break;

                case State.Windup:
                    UpdateWindup();
                    break;

                case State.Attack:
                    // After attack, return to idle
                    currentState = State.Idle;
                    attackCooldownTimer = attackCooldown;
                    break;
            }
        }

        private void UpdateIdle()
        {
            // Can't attack if on cooldown
            if (attackCooldownTimer > 0)
                return;

            // Find nearest enemy in range
            currentTarget = FindNearestEnemy();

            if (currentTarget != null)
            {
                EnterWindupState();
            }
        }

        private Transform FindNearestEnemy()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);

            Transform nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in hitColliders)
            {
                // Check if it's an enemy
                AIEnemyBehavior enemy = col.GetComponent<AIEnemyBehavior>();
                if (enemy != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = col.transform;
                    }
                }
            }

            return nearestEnemy;
        }

        private void EnterWindupState()
        {
            currentState = State.Windup;
            windupTimer = 0f;

            Debug.Log($"[DefensivePlant] Starting windup for target: {currentTarget.name}");

            // Trigger windup animation
            if (animator != null)
            {
                animator.SetTrigger(windupTriggerName);
            }

            // Show attack indicator
            if (attackIndicator != null)
            {
                attackIndicator.SetActive(true);
            }
        }

        private void UpdateWindup()
        {
            windupTimer += Time.deltaTime;

            // Check if target is still in range
            if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.position) > detectionRange)
            {
                Debug.Log("[DefensivePlant] Target lost during windup");
                CancelWindup();
                return;
            }

            // Rotate to face target
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), Time.deltaTime * 5f);
            }

            // After windup time, perform attack
            if (windupTimer >= attackWindup)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            currentState = State.Attack;

            Debug.Log($"[DefensivePlant] Attacking target: {currentTarget.name}");

            // Trigger attack animation
            if (animator != null)
            {
                animator.SetTrigger(attackTriggerName);
            }

            // Hide attack indicator
            if (attackIndicator != null)
            {
                attackIndicator.SetActive(false);
            }

            // Deal damage to target
            if (currentTarget != null)
            {
                AIEnemyBehavior enemy = currentTarget.GetComponent<AIEnemyBehavior>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                    Debug.Log($"[DefensivePlant] ✓ Dealt {attackDamage} damage to {currentTarget.name}");
                }
            }

            currentTarget = null;
        }

        private void CancelWindup()
        {
            currentState = State.Idle;
            currentTarget = null;
            windupTimer = 0f;

            // Hide attack indicator
            if (attackIndicator != null)
            {
                attackIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Take damage from external source (e.g., player or enemy attack)
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (currentState == State.Dead)
                return;

            currentHealth -= damage;
            Debug.Log($"[DefensivePlant] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                EnterDeathState();
            }
        }

        private void EnterDeathState()
        {
            if (currentState == State.Dead)
                return;

            Debug.Log("[DefensivePlant] Entering death state");
            currentState = State.Dead;

            // Trigger death animation
            if (animator != null)
            {
                animator.SetBool(isDeadParamName, true);
            }

            // Hide attack indicator
            if (attackIndicator != null)
            {
                attackIndicator.SetActive(false);
            }

            // Disable collider after a delay
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col, 2f);
            }

            // Destroy gameobject after death animation
            Destroy(gameObject, 5f);
        }

        // Visualize detection range in editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw line to current target during windup
            if (Application.isPlaying && currentTarget != null && currentState == State.Windup)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
    }
}
