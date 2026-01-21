using UnityEngine;
using HorizonMini.UI;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Simple player controller using VirtualJoystick input
    /// Temporary solution until Top Down Engine integration
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f; // Rotation lerp speed (higher = faster)
        [SerializeField] private bool rotateTowardsCameraOnInput = true; // Rotate to camera forward when input starts

        [Header("Gravity")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer = ~0;

        [Header("Visual (Optional)")]
        [Tooltip("Assign the visual model child GameObject here if your player has a separate visual mesh. This prevents rotation conflicts with CharacterController.")]
        [SerializeField] private Transform visualTransform; // If player model is a child object

        private CharacterController controller;
        private VirtualJoystick virtualJoystick;
        private Vector3 velocity;
        private bool isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            // Find virtual joystick in scene
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>();
            if (virtualJoystick == null)
            {
                Debug.LogWarning("[SimplePlayerController] VirtualJoystick not found!");
            }
        }

        private void Update()
        {
            // Ground check
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small negative value to keep grounded
            }

            // Get input from virtual joystick
            Vector2 input = Vector2.zero;
            if (virtualJoystick != null)
            {
                input = virtualJoystick.InputVector;
            }

            bool hasInput = input.magnitude > 0.01f;
            Camera cam = Camera.main;
            Vector3 moveDirection = Vector3.zero;

            // Calculate movement direction relative to camera
            if (hasInput && cam != null)
            {
                // Get camera forward and right, flatten to ground plane
                Vector3 forward = cam.transform.forward;
                Vector3 right = cam.transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                // Calculate move direction
                moveDirection = (forward * input.y + right * input.x).normalized;

                // Rotate player to face movement direction
                if (moveDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

                    if (visualTransform != null)
                    {
                        visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                }

                // Move character
                Vector3 move = moveDirection * moveSpeed * Time.deltaTime;
                controller.Move(move);
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw ground check ray
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
        }
    }
}
