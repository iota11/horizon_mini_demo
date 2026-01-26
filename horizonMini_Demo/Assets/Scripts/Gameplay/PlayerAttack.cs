using UnityEngine;

namespace HorizonMini.Gameplay
{
    /// <summary>
    /// Player attack system - deals damage to enemies in front of player
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 100f;
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private float attackAngle = 60f; // Cone angle in front of player
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private LayerMask enemyLayer = -1; // All layers by default

        private float lastAttackTime = -999f;
        private PlayerAnimationController animationController;

        /// <summary>
        /// Perform attack (called by B button)
        /// </summary>
        public void Attack()
        {
            // Check cooldown
            if (Time.time - lastAttackTime < attackCooldown)
            {
                Debug.Log("[PlayerAttack] Attack on cooldown");
                return;
            }

            lastAttackTime = Time.time;

            // Get animation controller if not cached
            if (animationController == null)
            {
                animationController = GetComponent<PlayerAnimationController>();
            }

            // Trigger attack animation
            if (animationController != null)
            {
                animationController.TriggerAttack();
            }

            // Find enemies in range
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

            int enemiesHit = 0;
            foreach (Collider col in hitColliders)
            {
                // Skip self
                if (col.gameObject == gameObject)
                    continue;

                // Check if enemy is in front of player (within attack angle cone)
                Vector3 directionToEnemy = (col.transform.position - transform.position).normalized;
                Vector3 forward = transform.forward;
                forward.y = 0;
                directionToEnemy.y = 0;

                float angle = Vector3.Angle(forward, directionToEnemy);
                if (angle <= attackAngle / 2f)
                {
                    // Try to damage enemy
                    AIEnemyBehavior enemy = col.GetComponent<AIEnemyBehavior>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(attackDamage);
                        enemiesHit++;
                        Debug.Log($"[PlayerAttack] Hit enemy: {col.gameObject.name} for {attackDamage} damage");
                    }

                    // Also check for defensive plants (they can be attacked too)
                    // Using SendMessage to avoid compilation order issues
                    if (col.gameObject.name.Contains("Plant") || col.CompareTag("Plant"))
                    {
                        col.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
                        enemiesHit++;
                        Debug.Log($"[PlayerAttack] Hit plant: {col.gameObject.name} for {attackDamage} damage");
                    }
                }
            }

            if (enemiesHit == 0)
            {
                Debug.Log("[PlayerAttack] Attack missed - no enemies in range");
            }
            else
            {
                Debug.Log($"[PlayerAttack] Attack hit {enemiesHit} target(s)!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw attack cone
            Gizmos.color = Color.yellow;
            Vector3 forward = transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, attackAngle / 2f, 0) * forward * attackRange;
            Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle / 2f, 0) * forward * attackRange;

            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        }
    }
}
