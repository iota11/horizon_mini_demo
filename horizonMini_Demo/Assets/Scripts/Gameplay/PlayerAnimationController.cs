using UnityEngine;

namespace HorizonMini.Gameplay
{
    /// <summary>
    /// Player animation controller - same parameters as enemy
    /// Controls Speed, IsChasing, Attack, IsDead animation parameters
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParamName = "Speed";
        [SerializeField] private string isChasingParamName = "IsChasing";
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string isDeadParamName = "IsDead";

        [Header("Settings")]
        [SerializeField] private float movementThreshold = 0.1f; // Minimum speed to be considered "moving"
        [SerializeField] private float chasingSpeedThreshold = 3f; // Speed threshold for "chasing" animation

        private CharacterController controller;
        private PlayerHealth playerHealth;
        private Vector3 lastPosition;
        private float currentSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerHealth = GetComponent<PlayerHealth>();

            // Auto-find Animator if not assigned
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogWarning("[PlayerAnimationController] No Animator found!");
                }
            }

            lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null)
                return;

            // Calculate current movement speed
            Vector3 currentPosition = transform.position;
            Vector3 movement = currentPosition - lastPosition;
            currentSpeed = movement.magnitude / Time.deltaTime;
            lastPosition = currentPosition;

            // Update animation parameters
            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            // Set Speed parameter (normalized 0-1 based on movement)
            float normalizedSpeed = currentSpeed;
            animator.SetFloat(speedParamName, normalizedSpeed);

            // Set IsChasing parameter (true when moving fast)
            bool isChasing = currentSpeed >= chasingSpeedThreshold;
            animator.SetBool(isChasingParamName, isChasing);

            // Set IsDead parameter
            bool isDead = (playerHealth != null && playerHealth.IsDead);
            animator.SetBool(isDeadParamName, isDead);
        }

        /// <summary>
        /// Trigger attack animation (called by PlayerAttack or B button)
        /// </summary>
        public void TriggerAttack()
        {
            if (animator != null)
            {
                animator.SetTrigger(attackTriggerName);
                Debug.Log("[PlayerAnimationController] Attack animation triggered");
            }
        }

        /// <summary>
        /// Trigger death animation (called by PlayerHealth)
        /// </summary>
        public void TriggerDeath()
        {
            if (animator != null)
            {
                animator.SetBool(isDeadParamName, true);
                Debug.Log("[PlayerAnimationController] Death animation triggered");
            }
        }

        // Expose current speed for debugging
        public float CurrentSpeed => currentSpeed;
    }
}
