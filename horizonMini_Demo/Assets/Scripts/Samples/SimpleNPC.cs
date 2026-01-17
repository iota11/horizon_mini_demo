using UnityEngine;

namespace HorizonMini.Samples
{
    /// <summary>
    /// Simple NPC behavior for demo worlds - idle animation with random movement
    /// </summary>
    public class SimpleNPC : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float changeDirectionInterval = 3f;
        [SerializeField] private float movementRadius = 5f;

        private Vector3 startPosition;
        private Vector3 targetDirection;
        private float directionTimer;

        private void Start()
        {
            startPosition = transform.position;
            PickNewDirection();
        }

        private void Update()
        {
            directionTimer -= Time.deltaTime;

            if (directionTimer <= 0)
            {
                PickNewDirection();
            }

            // Move
            Vector3 movement = targetDirection * moveSpeed * Time.deltaTime;
            Vector3 newPos = transform.position + movement;

            // Keep within radius
            if (Vector3.Distance(newPos, startPosition) > movementRadius)
            {
                PickNewDirection();
            }
            else
            {
                transform.position = newPos;
            }

            // Rotate toward movement direction
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        private void PickNewDirection()
        {
            targetDirection = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;

            directionTimer = changeDirectionInterval;
        }
    }
}
