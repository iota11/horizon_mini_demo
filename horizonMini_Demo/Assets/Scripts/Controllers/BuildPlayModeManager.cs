using UnityEngine;
using UnityEngine.UI;
using HorizonMini.Build;
using HorizonMini.UI;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages Play Mode in Build Scene (similar to PlayModeManager in Main Scene)
    /// Handles View Mode ↔ Play Mode transitions
    /// </summary>
    public class BuildPlayModeManager : MonoBehaviour
    {
        public enum Mode
        {
            View,  // Editing/viewing world (BuildController active)
            Play   // Testing gameplay (player control active)
        }

        [Header("References")]
        [SerializeField] private BuildController buildController;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Camera buildCamera;
        [SerializeField] private AssetCatalogUI assetCatalogUI;

        [Header("UI - View Mode")]
        [SerializeField] private Button playTestButton;
        [SerializeField] private GameObject playTestButtonObject;

        [Header("UI - Play Mode")]
        [SerializeField] private GameObject playModeUI;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject virtualJoystickUI;
        [SerializeField] private Button jumpButton;

        private UI.VirtualJoystick virtualJoystick;

        [Header("Camera Settings (Play Mode)")]
        [Tooltip("Initial camera distance from player")]
        [SerializeField] private float cameraDistance = 8f;
        [Tooltip("Camera angle (degrees) - 0=horizontal, 90=top-down")]
        [SerializeField] private float cameraAngle = 20f;
        [Tooltip("Camera follow smoothness (higher = smoother but slower)")]
        [SerializeField] private float cameraSmoothSpeed = 5f;
        [Tooltip("Horizontal camera rotation speed (drag left/right)")]
        [SerializeField] private float cameraRotationSpeed = 100f;
        [Tooltip("Vertical camera rotation speed (drag up/down)")]
        [SerializeField] private float cameraVerticalRotationSpeed = 50f;

        [Header("Camera Zoom Range")]
        [Tooltip("Minimum zoom distance (closest to player)")]
        [SerializeField] private float minCameraDistance = 3f;
        [Tooltip("Maximum zoom distance (farthest from player)")]
        [SerializeField] private float maxCameraDistance = 20f;
        [Tooltip("Minimum camera angle (most horizontal)")]
        [SerializeField] private float minCameraAngle = 20f;
        [Tooltip("Maximum camera angle (most top-down)")]
        [SerializeField] private float maxCameraAngle = 80f;

        [Header("Camera Transition")]
        [Tooltip("Time in seconds to transition between View/Play modes")]
        [SerializeField] private float cameraTransitionSpeed = 0.3f;

        [Header("Spawn Particle Settings")]
        [Tooltip("Number of particles to emit on spawn")]
        [SerializeField] private int spawnParticleCount = 60;
        [Tooltip("Color of spawn particles")]
        [SerializeField] private Color spawnParticleColor = Color.white;
        [Tooltip("Particle lifetime in seconds")]
        [SerializeField] private float particleLifetime = 0.8f;
        [Tooltip("Particle speed")]
        [SerializeField] private float particleSpeed = 3f;
        [Tooltip("Particle size")]
        [SerializeField] private float particleSize = 0.3f;
        [Tooltip("Gravity effect on particles")]
        [SerializeField] private float particleGravity = 0.5f;
        [Tooltip("Emission sphere radius")]
        [SerializeField] private float emissionRadius = 0.5f;

        private Mode currentMode = Mode.View;
        private GameObject spawnedPlayer;
        private SimplePlayerController playerController;

        // Camera state for Play Mode
        private Vector3 cameraOffset;
        private float currentCameraRotation = 0f;
        private float targetCameraRotation = 0f; // Target rotation for smoothing
        private float targetCameraAngle = 0f; // Target pitch angle for smoothing
        private Vector3 viewCameraPosition;
        private Quaternion viewCameraRotation;

        // Camera transition state
        private bool isCameraTransitioning = false;
        private Vector3 cameraTransitionStartPos;
        private Quaternion cameraTransitionStartRot;
        private Vector3 cameraTransitionTargetPos;
        private Quaternion cameraTransitionTargetRot;
        private float cameraTransitionProgress = 0f;

        // Touch input state for camera control
        private bool isDraggingCamera = false;
        private Vector2 lastTouchPosition;

        private static BuildPlayModeManager _instance;
        public static BuildPlayModeManager Instance => _instance;

        /// <summary>
        /// Check if camera is currently transitioning
        /// </summary>
        public bool IsCameraTransitioning => isCameraTransitioning;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Auto-find references if not assigned
            if (buildController == null)
                buildController = FindFirstObjectByType<BuildController>();

            if (buildCamera == null)
                buildCamera = Camera.main;

            if (assetCatalogUI == null)
                assetCatalogUI = FindFirstObjectByType<AssetCatalogUI>();
        }

        private void Start()
        {
            // Setup button listeners
            if (playTestButton != null)
                playTestButton.onClick.AddListener(OnPlayTestButtonClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            if (jumpButton != null)
                jumpButton.onClick.AddListener(OnJumpButtonPressed);

            // Get VirtualJoystick component
            if (virtualJoystickUI != null)
                virtualJoystick = virtualJoystickUI.GetComponentInChildren<UI.VirtualJoystick>();

            // Initialize in View mode
            ShowPlayTestButton(true);
            ShowPlayModeUI(false);
            ShowGameplayUI(false);

            // Spawn player in idle state
            SpawnPlayerInView();
        }

        private void Update()
        {
            if (currentMode == Mode.Play && !isCameraTransitioning)
            {
                HandlePlayModeCameraControl();
                UpdateCameraFollow();
            }

            if (isCameraTransitioning)
            {
                UpdateCameraTransition();
            }
        }

        #region Mode Switching

        private void OnPlayTestButtonClicked()
        {
            EnterPlayMode();
        }

        private void OnBackButtonClicked()
        {
            ExitPlayMode();
        }

        /// <summary>
        /// Enter Play Mode from View Mode
        /// </summary>
        public void EnterPlayMode()
        {
            if (currentMode == Mode.Play)
                return;

            Debug.Log("<color=cyan>[BuildPlayModeManager] Entering Play Mode</color>");

            // If player hasn't been spawned yet, try to spawn now
            if (spawnedPlayer == null || playerController == null)
            {
                Debug.Log("[BuildPlayModeManager] Player not spawned, spawning now...");
                SpawnPlayer();

                // Check if spawn was successful
                if (spawnedPlayer == null || playerController == null)
                {
                    Debug.LogError("[BuildPlayModeManager] Failed to spawn player! Cannot enter Play Mode.");
                    Debug.LogError("[BuildPlayModeManager] Make sure: 1) Player prefab is assigned, 2) It has SimplePlayerController component, 3) World/VolumeGrid is loaded");
                    return;
                }
            }

            currentMode = Mode.Play;

            // Collapse Asset Catalog panel
            if (assetCatalogUI == null)
                assetCatalogUI = FindFirstObjectByType<AssetCatalogUI>();

            if (assetCatalogUI != null)
            {
                assetCatalogUI.Collapse();
            }

            // Hide View UI
            ShowPlayTestButton(false);

            // Show Play Mode UI
            ShowPlayModeUI(true);

            // Disable BuildController camera and input
            if (buildController != null)
            {
                // Disable BuildController's camera control
                var cameraController = buildController.GetComponent<BuildCameraController>();
                if (cameraController != null)
                    cameraController.SetEnabled(false);

                // Disable selection and placement systems
                var selectionSystem = buildController.GetComponent<SelectionSystem>();
                if (selectionSystem != null)
                    selectionSystem.SetEnabled(false);

                var placementSystem = buildController.GetComponent<PlacementSystem>();
                if (placementSystem != null)
                    placementSystem.SetEnabled(false);

                // Disable gesture detector
                var gestureDetector = buildController.GetComponent<TouchGestureDetector>();
                if (gestureDetector != null)
                    gestureDetector.enabled = false;

                Debug.Log("[BuildPlayModeManager] Disabled BuildController input systems");
            }

            // Enable player control
            if (playerController != null)
            {
                playerController.enabled = true;
                Debug.Log("[BuildPlayModeManager] Enabled PlayerController");
            }
            else
            {
                Debug.LogError("[BuildPlayModeManager] PlayerController is NULL!");
            }

            // Save View camera state and transition to Play Mode camera
            if (buildCamera != null && spawnedPlayer != null)
            {
                viewCameraPosition = buildCamera.transform.position;
                viewCameraRotation = buildCamera.transform.rotation;

                // Initialize camera for player following
                currentCameraRotation = spawnedPlayer.transform.eulerAngles.y;
                targetCameraRotation = currentCameraRotation;
                targetCameraAngle = cameraAngle;
                CalculateCameraOffset();

                // Calculate target position for Play Mode
                Vector3 targetPos = spawnedPlayer.transform.position + cameraOffset;
                Vector3 lookAtPos = spawnedPlayer.transform.position + Vector3.up * 1.5f;
                Quaternion targetRot = Quaternion.LookRotation(lookAtPos - targetPos);

                // Start smooth transition
                StartCameraTransition(viewCameraPosition, viewCameraRotation, targetPos, targetRot);

                Debug.Log("[BuildPlayModeManager] Starting camera transition to Play Mode");
            }

            // Show VirtualJoystick and action buttons
            ShowGameplayUI(true);

            Debug.Log("<color=green>[BuildPlayModeManager] ✓ Entered Play Mode</color>");
        }

        /// <summary>
        /// Exit Play Mode back to View Mode
        /// </summary>
        public void ExitPlayMode()
        {
            if (currentMode == Mode.View)
                return;

            Debug.Log("<color=yellow>[BuildPlayModeManager] Exiting Play Mode</color>");

            currentMode = Mode.View;

            // Hide Play Mode UI
            ShowPlayModeUI(false);

            // Show View UI
            ShowPlayTestButton(true);

            // Disable player control
            if (playerController != null)
            {
                playerController.enabled = false;
                Debug.Log("[BuildPlayModeManager] Disabled PlayerController");
            }

            // Start smooth transition back to View camera state
            if (buildCamera != null)
            {
                Vector3 currentPos = buildCamera.transform.position;
                Quaternion currentRot = buildCamera.transform.rotation;

                // Transition back to the saved View camera position
                StartCameraTransition(currentPos, currentRot, viewCameraPosition, viewCameraRotation);

                Debug.Log("[BuildPlayModeManager] Starting camera transition to View Mode");
            }

            // Hide VirtualJoystick and action buttons
            ShowGameplayUI(false);

            Debug.Log("<color=green>[BuildPlayModeManager] ✓ Exited View Mode</color>");
        }

        #endregion

        #region UI Management

        /// <summary>
        /// Show/hide the "Play Test" button
        /// </summary>
        public void ShowPlayTestButton(bool show)
        {
            if (playTestButtonObject != null)
            {
                playTestButtonObject.SetActive(show);
            }
        }

        /// <summary>
        /// Show/hide Play Mode UI (Back button)
        /// </summary>
        public void ShowPlayModeUI(bool show)
        {
            if (playModeUI != null)
            {
                playModeUI.SetActive(show);
            }
        }

        /// <summary>
        /// Show/hide gameplay UI (VirtualJoystick, Jump button)
        /// </summary>
        private void ShowGameplayUI(bool show)
        {
            if (virtualJoystickUI != null)
                virtualJoystickUI.SetActive(show);

            if (jumpButton != null)
                jumpButton.gameObject.SetActive(show);
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Spawn player in View Mode (can be called manually after world is loaded)
        /// </summary>
        public void SpawnPlayer()
        {
            // Despawn existing player if any
            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer);
                spawnedPlayer = null;
                playerController = null;
            }

            SpawnPlayerInView();
        }

        /// <summary>
        /// Spawn player in View Mode (idle state)
        /// </summary>
        private void SpawnPlayerInView()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[BuildPlayModeManager] Player prefab not assigned");
                return;
            }

            if (buildController == null || buildController.VolumeGrid == null)
            {
                Debug.LogWarning("[BuildPlayModeManager] BuildController or VolumeGrid not ready, will spawn player later");
                return;
            }

            // Find spawn point
            Transform spawnPoint = FindSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogWarning("[BuildPlayModeManager] No spawn point found");
                return;
            }

            // Spawn player at spawn point with 3m height offset
            Vector3 spawnPosition = spawnPoint.position + Vector3.up * 3f;
            Quaternion spawnRotation = spawnPoint.rotation;

            spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            spawnedPlayer.name = "Player";

            // Get player controller
            playerController = spawnedPlayer.GetComponent<SimplePlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false; // Start disabled in View mode
                Debug.Log($"[BuildPlayModeManager] Spawned player with controller at {spawnPosition}");
            }
            else
            {
                Debug.LogError($"[BuildPlayModeManager] Player prefab missing SimplePlayerController component! Prefab: {playerPrefab.name}");
                Destroy(spawnedPlayer);
                spawnedPlayer = null;
                return;
            }

            // Create spawn particle effect
            CreateSpawnParticles(spawnPosition);
        }

        /// <summary>
        /// Find spawn point in the current world
        /// </summary>
        private Transform FindSpawnPoint()
        {
            // Look for SpawnPoint component in the scene
            SpawnPoint spawnPoint = FindFirstObjectByType<SpawnPoint>();
            if (spawnPoint != null)
                return spawnPoint.transform;

            // Fallback: use volume grid center at ground level
            if (buildController != null && buildController.VolumeGrid != null)
            {
                Debug.LogWarning("[BuildPlayModeManager] Using volume grid center as spawn point");
                Vector3 center = buildController.VolumeGrid.transform.position;
                GameObject tempSpawn = new GameObject("TempSpawnPoint");
                tempSpawn.transform.position = new Vector3(center.x, 0.5f, center.z);
                return tempSpawn.transform;
            }

            return null;
        }

        #endregion

        #region Camera Transition

        private void StartCameraTransition(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot)
        {
            cameraTransitionStartPos = fromPos;
            cameraTransitionStartRot = fromRot;
            cameraTransitionTargetPos = toPos;
            cameraTransitionTargetRot = toRot;
            cameraTransitionProgress = 0f;
            isCameraTransitioning = true;
        }

        private void UpdateCameraTransition()
        {
            if (!isCameraTransitioning || buildCamera == null)
                return;

            // Increment progress
            cameraTransitionProgress += Time.deltaTime / cameraTransitionSpeed;

            // Use smoothstep for easing
            float t = Mathf.SmoothStep(0f, 1f, cameraTransitionProgress);

            // Interpolate position and rotation
            buildCamera.transform.position = Vector3.Lerp(cameraTransitionStartPos, cameraTransitionTargetPos, t);
            buildCamera.transform.rotation = Quaternion.Slerp(cameraTransitionStartRot, cameraTransitionTargetRot, t);

            // Check if transition complete
            if (cameraTransitionProgress >= 1f)
            {
                isCameraTransitioning = false;
                buildCamera.transform.position = cameraTransitionTargetPos;
                buildCamera.transform.rotation = cameraTransitionTargetRot;
                Debug.Log("[BuildPlayModeManager] Camera transition complete");

                // If transitioning back to View Mode, re-enable BuildController systems
                if (currentMode == Mode.View && buildController != null)
                {
                    var cameraController = buildController.GetComponent<BuildCameraController>();
                    if (cameraController != null)
                        cameraController.SetEnabled(true);

                    var selectionSystem = buildController.GetComponent<SelectionSystem>();
                    if (selectionSystem != null)
                        selectionSystem.SetEnabled(true);

                    var placementSystem = buildController.GetComponent<PlacementSystem>();
                    if (placementSystem != null)
                        placementSystem.SetEnabled(true);

                    var gestureDetector = buildController.GetComponent<TouchGestureDetector>();
                    if (gestureDetector != null)
                        gestureDetector.enabled = true;

                    Debug.Log("[BuildPlayModeManager] Re-enabled BuildController input systems");
                }
            }
        }

        #endregion

        #region Camera Control (Play Mode)

        private void CalculateCameraOffset()
        {
            float angleRad = currentCameraRotation * Mathf.Deg2Rad;
            float heightAngleRad = cameraAngle * Mathf.Deg2Rad;

            // Calculate camera offset maintaining constant distance from player
            float horizontalDistance = cameraDistance * Mathf.Cos(heightAngleRad);
            float verticalHeight = cameraDistance * Mathf.Sin(heightAngleRad);

            cameraOffset = new Vector3(
                -Mathf.Sin(angleRad) * horizontalDistance,
                verticalHeight,
                -Mathf.Cos(angleRad) * horizontalDistance
            );
        }

        private void UpdateCameraFollow()
        {
            if (buildCamera == null || spawnedPlayer == null)
                return;

            // Smoothly interpolate rotation and angle (orbit smoothing)
            currentCameraRotation = Mathf.LerpAngle(currentCameraRotation, targetCameraRotation, Time.deltaTime * 10f);
            cameraAngle = Mathf.Lerp(cameraAngle, targetCameraAngle, Time.deltaTime * 10f);
            CalculateCameraOffset();

            // Calculate target camera position
            Vector3 targetPosition = spawnedPlayer.transform.position + cameraOffset;

            // Smoothly move camera (locomotion smoothing)
            buildCamera.transform.position = Vector3.Lerp(
                buildCamera.transform.position,
                targetPosition,
                Time.deltaTime * cameraSmoothSpeed
            );

            // Look at player (slightly above center)
            Vector3 lookAtPosition = spawnedPlayer.transform.position + Vector3.up * 1.5f;
            buildCamera.transform.LookAt(lookAtPosition);
        }

        private void HandlePlayModeCameraControl()
        {
            // Mouse scroll wheel zoom
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                // Zoom in/out by adjusting camera distance
                cameraDistance -= scrollDelta * 2f;
                cameraDistance = Mathf.Clamp(cameraDistance, minCameraDistance, maxCameraDistance);
                CalculateCameraOffset();
            }

            // Check if joystick is being used
            bool isJoystickActive = virtualJoystick != null && virtualJoystick.InputVector.magnitude > 0.01f;

            // Touch input for camera rotation (similar to PlayModeManager)
            if (Input.touchCount == 1 && !isJoystickActive)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    isDraggingCamera = true;
                    lastTouchPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved && isDraggingCamera)
                {
                    Vector2 delta = touch.position - lastTouchPosition;
                    lastTouchPosition = touch.position;

                    // Rotate camera horizontally and vertically
                    targetCameraRotation += delta.x * 0.3f;
                    targetCameraAngle -= delta.y * 0.15f; // Vertical drag changes pitch
                    targetCameraAngle = Mathf.Clamp(targetCameraAngle, minCameraAngle, maxCameraAngle);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isDraggingCamera = false;
                }
            }
            // Stop camera drag if joystick becomes active
            else if (isJoystickActive && isDraggingCamera)
            {
                isDraggingCamera = false;
            }
            // Mouse fallback for editor
            else if (Input.GetMouseButtonDown(0))
            {
                isDraggingCamera = true;
                lastTouchPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && isDraggingCamera)
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 delta = currentPos - lastTouchPosition;
                lastTouchPosition = currentPos;

                // Rotate camera horizontally and vertically
                targetCameraRotation += delta.x * 0.3f;
                targetCameraAngle -= delta.y * 0.15f; // Vertical drag changes pitch
                targetCameraAngle = Mathf.Clamp(targetCameraAngle, minCameraAngle, maxCameraAngle);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingCamera = false;
            }
        }

        private void OnJumpButtonPressed()
        {
            if (playerController != null)
            {
                playerController.Jump();
            }
        }

        #endregion

        #region Particle Effects

        /// <summary>
        /// Create spawn particle effect at player spawn position
        /// </summary>
        private void CreateSpawnParticles(Vector3 position)
        {
            // Create a temporary GameObject for particles
            GameObject particleObj = new GameObject("SpawnParticles");
            particleObj.transform.position = position;

            // Add particle system
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

            // Stop the system first to allow configuration
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Configure main module
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = particleLifetime;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = spawnParticleColor;
            main.gravityModifier = particleGravity;
            main.maxParticles = spawnParticleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Configure emission
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, spawnParticleCount) });

            // Configure shape (sphere)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = emissionRadius;

            // Configure color over lifetime (fade out)
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Configure size over lifetime (shrink)
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            // Configure renderer for URP compatibility
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Create URP-compatible material
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            particleMat.SetColor("_BaseColor", spawnParticleColor);
            particleMat.SetFloat("_Surface", 1); // Transparent
            particleMat.SetFloat("_Blend", 0); // Alpha blend
            particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            particleMat.renderQueue = 3000;

            renderer.material = particleMat;

            // Play the particle system
            ps.Play();

            // Destroy after particles finish
            Destroy(particleObj, main.duration + main.startLifetime.constantMax);
        }

        #endregion
    }
}
