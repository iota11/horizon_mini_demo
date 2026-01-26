using UnityEngine;
using UnityEngine.UI;

namespace HorizonMini.Gameplay
{
    /// <summary>
    /// Player health system with HP bar UI
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("UI")]
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private GameObject healthBarCanvas;

        [Header("Damage Feedback")]
        [SerializeField] private float damageFlashDuration = 0.2f;

        private bool isDead = false;

        private void Awake()
        {
            currentHealth = maxHealth;

            // Hide health bar initially
            if (healthBarCanvas != null)
            {
                healthBarCanvas.SetActive(false);
            }
        }

        /// <summary>
        /// Take damage from external source
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead)
                return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(currentHealth, 0);

            Debug.Log($"[PlayerHealth] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

            // Show health bar when taking damage
            ShowHealthBar();

            // Update health bar UI
            UpdateHealthBar();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal player
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"[PlayerHealth] Healed {amount}. Health: {currentHealth}/{maxHealth}");

            UpdateHealthBar();

            // Hide health bar if at full health
            if (currentHealth >= maxHealth && healthBarCanvas != null)
            {
                healthBarCanvas.SetActive(false);
            }
        }

        private void ShowHealthBar()
        {
            if (healthBarCanvas != null)
            {
                healthBarCanvas.SetActive(true);
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBarSlider != null)
            {
                healthBarSlider.value = currentHealth / maxHealth;
            }
        }

        private void Die()
        {
            if (isDead)
                return;

            isDead = true;
            Debug.Log("[PlayerHealth] Player died!");

            // Trigger death animation
            var animationController = GetComponent<PlayerAnimationController>();
            if (animationController != null)
            {
                animationController.TriggerDeath();
            }

            // Disable movement
            var playerController = GetComponent<Controllers.SimplePlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }

        public bool IsDead => isDead;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
    }
}
