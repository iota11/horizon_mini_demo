using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Spawn point for characters (Player or NPC)
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private SpawnType spawnType = SpawnType.Player;
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private bool isInitialSpawn = false; // Cannot be deleted if true

        [Header("Visual")]
        [SerializeField] private GameObject visualModel; // Arrow or marker showing spawn direction

        private Vector3 savedPosition;
        private Quaternion savedRotation;

        public SpawnType SpawnType => spawnType;
        public GameObject CharacterPrefab => characterPrefab;
        public bool IsInitialSpawn => isInitialSpawn;

        private void Awake()
        {
            // Ensure there's a collider for raycasting
            if (GetComponent<Collider>() == null)
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.5f;
                collider.isTrigger = true; // Don't interfere with physics
            }
        }

        private void Start()
        {
            UpdateSavedTransform();

            // Register with SpawnPointManager
            var manager = SpawnPointManager.Instance;
            if (manager != null)
            {
                manager.RegisterSpawnPoint(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from SpawnPointManager
            if (SpawnPointManager.Instance != null)
            {
                SpawnPointManager.Instance.UnregisterSpawnPoint(this);
            }
        }

        /// <summary>
        /// Set spawn type
        /// </summary>
        public void SetSpawnType(SpawnType type)
        {
            spawnType = type;
        }

        /// <summary>
        /// Set character prefab to spawn
        /// </summary>
        public void SetCharacterPrefab(GameObject prefab)
        {
            characterPrefab = prefab;
        }

        /// <summary>
        /// Mark this as initial spawn point (cannot be deleted)
        /// </summary>
        public void SetInitialSpawn(bool isInitial)
        {
            isInitialSpawn = isInitial;
        }

        /// <summary>
        /// Save current transform for serialization
        /// </summary>
        public void UpdateSavedTransform()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
        }

        /// <summary>
        /// Get saved position
        /// </summary>
        public Vector3 GetSavedPosition()
        {
            return savedPosition;
        }

        /// <summary>
        /// Get saved rotation
        /// </summary>
        public Quaternion GetSavedRotation()
        {
            return savedRotation;
        }

        /// <summary>
        /// Spawn character at this point
        /// </summary>
        public GameObject SpawnCharacter()
        {
            if (characterPrefab == null)
            {
                Debug.LogWarning($"SpawnPoint {name}: No character prefab assigned!");
                return null;
            }

            GameObject spawnedCharacter = Instantiate(characterPrefab, transform.position, transform.rotation);
            Debug.Log($"Spawned {spawnType} character: {spawnedCharacter.name} at {transform.position}");

            return spawnedCharacter;
        }

        private void OnDrawGizmos()
        {
            // Draw arrow showing spawn direction
            Gizmos.color = isInitialSpawn ? Color.cyan : (spawnType == SpawnType.Player ? Color.green : Color.red);

            Vector3 position = transform.position;
            Vector3 direction = transform.forward;

            // Draw arrow
            Gizmos.DrawRay(position, direction * 1.5f);
            Gizmos.DrawWireSphere(position, 0.3f);

            // Draw cone for direction
            Vector3 right = transform.right * 0.3f;
            Vector3 arrowTip = position + direction * 1.5f;
            Gizmos.DrawLine(arrowTip, arrowTip - direction * 0.5f + right);
            Gizmos.DrawLine(arrowTip, arrowTip - direction * 0.5f - right);
        }
    }

    /// <summary>
    /// Type of spawn point
    /// </summary>
    public enum SpawnType
    {
        Player,
        NPC
    }
}
