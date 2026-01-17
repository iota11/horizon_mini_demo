using UnityEngine;
using HorizonMini.Core;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages first-person play mode within a world
    /// </summary>
    public class PlayController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera playCamera;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float lookSmoothness = 10f;

        [Header("Movement")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 2f;

        [Header("Mobile Controls")]
        [SerializeField] private bool useMobileControls = true;
        [SerializeField] private float joystickRadius = 100f;

        private AppRoot appRoot;
        private WorldInstance currentWorld;
        private string currentWorldId;

        private Vector3 velocity;
        private float verticalRotation = 0f;
        private Vector2 lookInput;
        private Vector2 moveInput;

        // Touch controls
        private int leftTouchId = -1;
        private int rightTouchId = -1;
        private Vector2 leftTouchStartPos;
        private Vector2 rightTouchStartPos;

        private bool isActive = false;

        public void Initialize(AppRoot root)
        {
            appRoot = root;

            if (playCamera == null)
            {
                GameObject camObj = new GameObject("PlayCamera");
                camObj.transform.SetParent(transform);
                playCamera = camObj.AddComponent<Camera>();
            }

            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.height = 2f;
                characterController.radius = 0.5f;
                characterController.center = new Vector3(0, 1f, 0);
            }

            playCamera.transform.SetParent(transform);
            playCamera.transform.localPosition = new Vector3(0, 1.6f, 0); // Eye level
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (playCamera != null)
            {
                playCamera.gameObject.SetActive(active);
            }

            if (characterController != null)
            {
                characterController.enabled = active;
            }
        }

        public void EnterWorld(string worldId)
        {
            currentWorldId = worldId;

            // Load world if not already loaded
            currentWorld = appRoot.WorldLibrary.InstantiateWorld(worldId);

            if (currentWorld != null)
            {
                currentWorld.SetActivationLevel(ActivationLevel.FullyActive);
                currentWorld.OnWorldEnter();

                // Position player at spawn point (center bottom of world)
                Bounds bounds = currentWorld.GetWorldBounds();
                Vector3 spawnPos = new Vector3(bounds.center.x, bounds.min.y + 2f, bounds.center.z);
                transform.position = spawnPos;

                // Reset camera rotation
                verticalRotation = 0f;
                transform.rotation = Quaternion.identity;
                playCamera.transform.localRotation = Quaternion.identity;
            }
        }

        public void ExitWorld()
        {
            if (currentWorld != null)
            {
                currentWorld.OnWorldExit();
                Destroy(currentWorld.gameObject);
                currentWorld = null;
            }

            currentWorldId = null;
        }

        private void Update()
        {
            if (!isActive || currentWorld == null)
                return;

            HandleInput();
            ApplyMovement();
            ApplyLook();
        }

        private void HandleInput()
        {
            if (useMobileControls)
            {
                HandleTouchInput();
            }
            else
            {
                HandleKeyboardMouse();
            }

            // Exit button (always available)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                appRoot.ExitPlayMode();
            }
        }

        private void HandleKeyboardMouse()
        {
            // WASD movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveInput = new Vector2(horizontal, vertical);

            // Mouse look
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
            else
            {
                lookInput = Vector2.zero;
            }

            // Jump
            if (Input.GetButtonDown("Jump") && characterController.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void HandleTouchInput()
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Left side = movement joystick
                if (touch.position.x < Screen.width / 2)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        leftTouchId = touch.fingerId;
                        leftTouchStartPos = touch.position;
                    }
                    else if (touch.fingerId == leftTouchId)
                    {
                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            Vector2 delta = touch.position - leftTouchStartPos;
                            moveInput = new Vector2(
                                Mathf.Clamp(delta.x / joystickRadius, -1f, 1f),
                                Mathf.Clamp(delta.y / joystickRadius, -1f, 1f)
                            );
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            leftTouchId = -1;
                            moveInput = Vector2.zero;
                        }
                    }
                }
                // Right side = look
                else
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        rightTouchId = touch.fingerId;
                        rightTouchStartPos = touch.position;
                    }
                    else if (touch.fingerId == rightTouchId)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            lookInput = touch.deltaPosition * 0.1f;
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            rightTouchId = -1;
                            lookInput = Vector2.zero;
                        }
                    }
                }
            }
        }

        private void ApplyMovement()
        {
            // Ground check and gravity
            if (characterController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;

            // Calculate move direction relative to camera
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;
            Vector3 move = moveDir * moveSpeed * Time.deltaTime;

            // Add vertical velocity
            move.y = velocity.y * Time.deltaTime;

            // Move
            characterController.Move(move);
        }

        private void ApplyLook()
        {
            if (lookInput.sqrMagnitude < 0.01f)
                return;

            // Horizontal rotation (Y-axis)
            float horizontalRotation = lookInput.x * lookSensitivity;
            transform.Rotate(Vector3.up * horizontalRotation);

            // Vertical rotation (X-axis, camera only)
            verticalRotation -= lookInput.y * lookSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

            Quaternion targetRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            playCamera.transform.localRotation = Quaternion.Slerp(
                playCamera.transform.localRotation,
                targetRotation,
                Time.deltaTime * lookSmoothness
            );
        }

        // Called by UI Exit button
        public void OnExitButtonPressed()
        {
            appRoot.ExitPlayMode();
        }
    }
}
