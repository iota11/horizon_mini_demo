using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using HorizonMini.Core;
using HorizonMini.Build;
using HorizonMini.UI;
using HorizonMini.Data;
using HorizonMini.Gameplay;
using System.Reflection;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Main controller for Play scene - loads world and manages third-person gameplay
    /// </summary>
    public class PlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playCamera;
        [SerializeField] private Transform worldContainer;

        [Header("Player Settings")]
        [SerializeField] private GameObject playerPrefab; // Top Down Engine character prefab

        [Header("UI References")]
        [SerializeField] private GameObject virtualJoystickUI;
        [SerializeField] private VirtualJoystick virtualJoystick;

        [Header("Camera Settings")]
        [SerializeField] private float cameraHeight = 5f;
        [SerializeField] private float cameraDistance = 8f;
        [SerializeField] private float cameraAngle = 45f;
        [SerializeField] private float cameraSmoothSpeed = 5f;
        [SerializeField] private float cameraRotationSpeed = 100f;
        [SerializeField] private float cameraVerticalRotationSpeed = 50f;
        [SerializeField] private float cameraZoomSpeed = 2f;
        [SerializeField] private float minCameraDistance = 3f;
        [SerializeField] private float maxCameraDistance = 20f;
        [SerializeField] private float minCameraAngle = 20f;
        [SerializeField] private float maxCameraAngle = 80f;

        [Header("Action Buttons")]
        [SerializeField] private UnityEngine.UI.Button jumpButton;
        [SerializeField] private UnityEngine.UI.Button attackButton;

        [Header("Standalone Mode")]
        [SerializeField] private bool autoInitialize = true;

        [Header("World Loading (Required)")]
        [SerializeField] private GameObject volumePrefab;
        [SerializeField] private GridSettings gridSettings;
        [SerializeField] private AssetCatalog assetCatalog;

        private AppRoot appRoot;
        private string currentWorldId;
        private WorldInstance currentWorldInstance;
        private GameObject spawnedPlayer;
        private Vector3 cameraOffset;
        private float currentCameraRotation = 0f;

        // Touch input state
        private bool isDraggingCamera = false;
        private Vector2 lastTouchPosition;

        private void Start()
        {
            Debug.Log($"[PlayController] Start called. autoInitialize={autoInitialize}");

            if (!autoInitialize)
            {
                Debug.LogWarning("[PlayController] autoInitialize is false, skipping initialization");
                return;
            }

            // Get world ID from scene transition first
            if (!SceneTransitionData.HasWorldToPlay())
            {
                Debug.LogWarning("[PlayController] No world ID provided. " +
                    "Play scene should be entered from Build scene via 'Go' button.\n" +
                    "For testing, you can set a world ID via: SceneTransitionData.SetWorldToPlay(\"your_world_id\")");

                // Don't automatically return to Main - let developer stay in scene for testing
                // ReturnToMain();
                return;
            }

            currentWorldId = SceneTransitionData.GetAndClearWorldToPlay();
            Debug.Log($"[PlayController] Retrieved world ID: {currentWorldId}");

            // Initialize AppRoot if needed (lightweight version for Play scene)
            appRoot = FindFirstObjectByType<AppRoot>();
            if (appRoot == null)
            {
                Debug.Log("[PlayController] Creating minimal AppRoot");
                // Create minimal AppRoot for Play scene (no UI, no controllers)
                InitializeMinimalAppRoot();
            }
            else
            {
                Debug.Log("[PlayController] Found existing AppRoot");
            }

            // Load world
            LoadWorld(currentWorldId);
        }

        /// <summary>
        /// Initialize minimal AppRoot for Play scene (no UI/controllers)
        /// </summary>
        private void InitializeMinimalAppRoot()
        {
            Debug.Log("[PlayController] InitializeMinimalAppRoot: Creating AppRoot GameObject");
            // Create AppRoot without triggering Awake by adding components manually
            GameObject appRootObj = new GameObject("AppRoot");

            Debug.Log("[PlayController] InitializeMinimalAppRoot: Adding SaveService");
            // Add SaveService first
            SaveService saveService = appRootObj.AddComponent<SaveService>();

            Debug.Log("[PlayController] InitializeMinimalAppRoot: Adding WorldLibrary");
            // Add WorldLibrary
            WorldLibrary worldLibrary = appRootObj.AddComponent<WorldLibrary>();

            // Use reflection to set WorldLibrary's required fields
            var volumePrefabField = typeof(WorldLibrary).GetField("volumePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            var gridSettingsField = typeof(WorldLibrary).GetField("gridSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            var assetCatalogField = typeof(WorldLibrary).GetField("assetCatalog", BindingFlags.NonPublic | BindingFlags.Instance);

            if (volumePrefabField != null && volumePrefab != null)
            {
                volumePrefabField.SetValue(worldLibrary, volumePrefab);
                Debug.Log("[PlayController] Set WorldLibrary.volumePrefab");
            }
            else
            {
                Debug.LogError("[PlayController] Failed to set volumePrefab!");
            }

            if (gridSettingsField != null && gridSettings != null)
            {
                gridSettingsField.SetValue(worldLibrary, gridSettings);
                Debug.Log("[PlayController] Set WorldLibrary.gridSettings");
            }
            else
            {
                Debug.LogError("[PlayController] Failed to set gridSettings!");
            }

            if (assetCatalogField != null && assetCatalog != null)
            {
                assetCatalogField.SetValue(worldLibrary, assetCatalog);
                Debug.Log("[PlayController] Set WorldLibrary.assetCatalog");
            }
            else
            {
                Debug.LogWarning("[PlayController] assetCatalog not set (optional)");
            }

            worldLibrary.Initialize(saveService);

            Debug.Log("[PlayController] InitializeMinimalAppRoot: Adding AppRoot component");
            // Add AppRoot last (this will trigger Awake, but we suppress the initialization)
            appRoot = appRootObj.AddComponent<AppRoot>();

            // Use reflection to set the private fields so AppRoot.Awake doesn't initialize again
            Debug.Log("[PlayController] InitializeMinimalAppRoot: Setting up reflection");
            var saveServiceField = typeof(AppRoot).GetField("saveService",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var worldLibraryField = typeof(AppRoot).GetField("worldLibrary",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (saveServiceField != null)
            {
                saveServiceField.SetValue(appRoot, saveService);
                Debug.Log("[PlayController] InitializeMinimalAppRoot: SaveService field set");
            }
            else
            {
                Debug.LogError("[PlayController] InitializeMinimalAppRoot: SaveService field not found!");
            }

            if (worldLibraryField != null)
            {
                worldLibraryField.SetValue(appRoot, worldLibrary);
                Debug.Log("[PlayController] InitializeMinimalAppRoot: WorldLibrary field set");
            }
            else
            {
                Debug.LogError("[PlayController] InitializeMinimalAppRoot: WorldLibrary field not found!");
            }

            Debug.Log($"[PlayController] Initialized minimal AppRoot. WorldLibrary={appRoot.WorldLibrary != null}, SaveService={appRoot.SaveService != null}");
        }

        private void LoadWorld(string worldId)
        {
            Debug.Log($"[PlayController] Loading world: {worldId}");

            // Create world container if needed
            if (worldContainer == null)
            {
                Debug.Log("[PlayController] Creating WorldContainer");
                GameObject containerObj = new GameObject("WorldContainer");
                worldContainer = containerObj.transform;
                worldContainer.position = Vector3.zero;
            }

            // Check if AppRoot and WorldLibrary are available
            if (appRoot == null)
            {
                Debug.LogError("[PlayController] AppRoot is null! Cannot load world.");
                return;
            }

            if (appRoot.WorldLibrary == null)
            {
                Debug.LogError("[PlayController] WorldLibrary is null! Cannot load world.");
                return;
            }

            Debug.Log($"[PlayController] Calling WorldLibrary.InstantiateWorld({worldId})");

            // First check if world data exists
            var worldData = appRoot.WorldLibrary.GetWorldData(worldId);
            if (worldData == null)
            {
                Debug.LogError($"[PlayController] WorldData not found for world ID: {worldId}");
                Debug.LogError($"[PlayController] This usually means the world wasn't saved before entering Play mode.");
                return;
            }
            Debug.Log($"[PlayController] Found WorldData: {worldData.worldTitle}, Volumes: {worldData.volumes.Count}, Props: {worldData.props?.Count ?? 0}");

            // IMPORTANT: Create NavMeshSurface BEFORE loading world
            // This ensures NavMeshAgent components can initialize when props are instantiated
            Debug.Log("[PlayController] Creating NavMeshSurface before world loading...");
            EnsureNavMeshSurfaceExists();

            // Load world using WorldLibrary
            currentWorldInstance = appRoot.WorldLibrary.InstantiateWorld(worldId, worldContainer);

            if (currentWorldInstance == null)
            {
                Debug.LogError($"[PlayController] Failed to load world: {worldId}");
                return;
            }

            currentWorldInstance.SetActivationLevel(ActivationLevel.FullyActive);
            Debug.Log($"[PlayController] World loaded: {currentWorldInstance.WorldData.worldTitle}");

            // Hide all edit cursors (terrain, walls, spawn points)
            var terrainMgr = SmartTerrainManager.Instance;
            if (terrainMgr != null) terrainMgr.EnterViewMode();

            var wallMgr = SmartWallManager.Instance;
            if (wallMgr != null) wallMgr.EnterViewMode();

            var spawnMgr = SpawnPointManager.Instance;
            if (spawnMgr != null) spawnMgr.EnterViewMode();

            Debug.Log("[PlayController] Spawning player");
            // Spawn player at spawn point
            SpawnPlayer();

            Debug.Log("[PlayController] Setting up camera");
            // Setup camera
            SetupCamera();

            // Show virtual joystick
            if (virtualJoystickUI != null)
            {
                Debug.Log("[PlayController] Showing virtual joystick");
                virtualJoystickUI.SetActive(true);
            }

            // Auto-find joystick if not assigned
            if (virtualJoystick == null && virtualJoystickUI != null)
                virtualJoystick = virtualJoystickUI.GetComponentInChildren<VirtualJoystick>();

            // Setup action buttons
            SetupActionButtons();

            // Build NavMesh for AI navigation AFTER world is loaded
            Debug.Log("[PlayController] Building NavMesh for AI navigation");
            BuildNavMesh();

            // Notify all AI agents that NavMesh is ready
            Debug.Log("[PlayController] Initializing AI agents");
            var aiAgents = FindObjectsByType<AIEnemyBehavior>(FindObjectsSortMode.None);
            Debug.Log($"[PlayController] Found {aiAgents.Length} AI agent(s)");
            foreach (var ai in aiAgents)
            {
                ai.OnNavMeshReady();
            }

            // Generate invisible boundary colliders to prevent player from leaving volume
            Debug.Log("[PlayController] Generating volume boundary colliders");
            GenerateVolumeBoundaryColliders();

            Debug.Log("[PlayController] World loading complete");
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayController] Player prefab not assigned!");
                return;
            }

            // Find spawn point in the loaded world
            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            Debug.Log($"[PlayController] Found {spawnPoints.Length} spawn point(s) in scene");

            SpawnPoint playerSpawn = null;

            foreach (var sp in spawnPoints)
            {
                Debug.Log($"[PlayController] SpawnPoint found: {sp.name}, Type: {sp.SpawnType}, IsInitial: {sp.IsInitialSpawn}");
                if (sp.SpawnType == SpawnType.Player)
                {
                    playerSpawn = sp;
                    break;
                }
            }

            if (playerSpawn == null)
            {
                Debug.LogError("[PlayController] No player spawn point found!");

                // Try to find ANY spawn point as fallback
                if (spawnPoints.Length > 0)
                {
                    Debug.LogWarning($"[PlayController] Using first available spawn point as fallback: {spawnPoints[0].name}");
                    playerSpawn = spawnPoints[0];
                }
                else
                {
                    Debug.LogError("[PlayController] No spawn points exist in the world at all!");
                    return;
                }
            }

            Vector3 spawnPosition = playerSpawn.transform.position;
            Quaternion spawnRotation = playerSpawn.transform.rotation;

            spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            spawnedPlayer.name = "Player";
            spawnedPlayer.tag = "Player"; // Set tag for AI detection

            // Set player layer (so it's excluded from NavMesh)
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                spawnedPlayer.layer = playerLayer;
                Debug.Log("[PlayController] Set player layer to Player");
            }
            else
            {
                Debug.LogWarning("[PlayController] 'Player' layer not found. Please create it in Project Settings > Tags and Layers");
            }

            // Add CharacterController if not present
            CharacterController charController = spawnedPlayer.GetComponent<CharacterController>();
            if (charController == null)
            {
                charController = spawnedPlayer.AddComponent<CharacterController>();
                charController.radius = 0.5f;
                charController.height = 2f;
                charController.center = new Vector3(0, 1f, 0);
                Debug.Log("[PlayController] Added CharacterController to player");
            }

            // Add SimplePlayerController if not present (temporary until Top Down Engine integration)
            SimplePlayerController playerController = spawnedPlayer.GetComponent<SimplePlayerController>();
            if (playerController == null)
            {
                spawnedPlayer.AddComponent<SimplePlayerController>();
                Debug.Log("[PlayController] Added SimplePlayerController to player");
            }

            Debug.Log($"[PlayController] Spawned player at {spawnPosition}");
        }

        private void SetupCamera()
        {
            if (playCamera == null)
            {
                playCamera = Camera.main;
            }

            if (playCamera == null)
            {
                Debug.LogError("[PlayController] Play camera not found!");
                return;
            }

            // Position camera behind and above player
            if (spawnedPlayer != null)
            {
                currentCameraRotation = spawnedPlayer.transform.eulerAngles.y;
                CalculateCameraOffset();
                UpdateCameraPosition(immediate: true);
            }
        }

        private void Update()
        {
            if (spawnedPlayer == null)
                return;

            // Update camera to follow player
            UpdateCameraFollow();

            // Handle camera rotation input
            HandleCameraRotationInput();

            // Handle camera zoom input (mouse scroll wheel)
            HandleCameraZoomInput();
        }

        private void CalculateCameraOffset()
        {
            float angleRad = currentCameraRotation * Mathf.Deg2Rad;
            float heightAngleRad = cameraAngle * Mathf.Deg2Rad;

            // Calculate camera offset maintaining constant distance from player
            // Horizontal plane distance (XZ)
            float horizontalDistance = cameraDistance * Mathf.Cos(heightAngleRad);
            // Vertical height (Y)
            float verticalHeight = cameraDistance * Mathf.Sin(heightAngleRad);

            cameraOffset = new Vector3(
                -Mathf.Sin(angleRad) * horizontalDistance,
                verticalHeight,
                -Mathf.Cos(angleRad) * horizontalDistance
            );
        }

        private void UpdateCameraFollow()
        {
            UpdateCameraPosition(immediate: false);
        }

        private void UpdateCameraPosition(bool immediate)
        {
            if (playCamera == null || spawnedPlayer == null)
                return;

            Vector3 targetPosition = spawnedPlayer.transform.position + cameraOffset;

            if (immediate)
            {
                playCamera.transform.position = targetPosition;
            }
            else
            {
                playCamera.transform.position = Vector3.Lerp(
                    playCamera.transform.position,
                    targetPosition,
                    Time.deltaTime * cameraSmoothSpeed
                );
            }

            // Look at player
            Vector3 lookAtPosition = spawnedPlayer.transform.position + Vector3.up * 1.5f;
            playCamera.transform.LookAt(lookAtPosition);
        }

        private void HandleCameraRotationInput()
        {
            // Handle touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                // Check if touch is over UI (joystick area - left side of screen)
                bool isTouchOverJoystick = touch.position.x < Screen.width * 0.3f;

                if (touch.phase == TouchPhase.Began && !isTouchOverJoystick)
                {
                    isDraggingCamera = true;
                    lastTouchPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved && isDraggingCamera)
                {
                    Vector2 delta = touch.position - lastTouchPosition;
                    RotateCamera(delta.x, delta.y);
                    lastTouchPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isDraggingCamera = false;
                }
            }
            // Handle mouse input (for editor testing)
            else if (Input.GetMouseButton(0))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // Check if mouse is over UI (left side)
                    bool isMouseOverJoystick = Input.mousePosition.x < Screen.width * 0.3f;
                    if (!isMouseOverJoystick)
                    {
                        isDraggingCamera = true;
                        lastTouchPosition = Input.mousePosition;
                    }
                }
                else if (isDraggingCamera)
                {
                    Vector2 currentMousePosition = Input.mousePosition;
                    Vector2 delta = currentMousePosition - lastTouchPosition;
                    RotateCamera(delta.x, delta.y);
                    lastTouchPosition = currentMousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingCamera = false;
            }
        }

        private void RotateCamera(float deltaX, float deltaY)
        {
            // Horizontal rotation
            currentCameraRotation += deltaX * cameraRotationSpeed * Time.deltaTime;

            // Vertical rotation (pitch)
            cameraAngle -= deltaY * cameraVerticalRotationSpeed * Time.deltaTime;
            cameraAngle = Mathf.Clamp(cameraAngle, minCameraAngle, maxCameraAngle);

            CalculateCameraOffset();
        }

        private void HandleCameraZoomInput()
        {
            // Use Input.mouseScrollDelta for scroll wheel zoom
            Vector2 scrollDelta = Input.mouseScrollDelta;

            if (Mathf.Abs(scrollDelta.y) > 0.01f)
            {
                // Adjust camera distance
                cameraDistance -= scrollDelta.y * cameraZoomSpeed;
                cameraDistance = Mathf.Clamp(cameraDistance, minCameraDistance, maxCameraDistance);

                // Recalculate camera offset with new distance
                CalculateCameraOffset();
            }
        }

        /// <summary>
        /// Return to Main scene
        /// </summary>
        public void ReturnToMain()
        {
            Debug.Log("[PlayController] Returning to Main");
            SceneManager.LoadScene("Main");
        }

        /// <summary>
        /// Return to Home scene (alias for ReturnToMain)
        /// </summary>
        [System.Obsolete("Use ReturnToMain instead")]
        public void ReturnToHome()
        {
            ReturnToMain();
        }

        /// <summary>
        /// Return to Build scene to edit this world
        /// </summary>
        public void ReturnToBuild()
        {
            Debug.Log("[PlayController] Returning to Build mode");
            SceneTransitionData.SetWorldToEdit(currentWorldId);
            SceneManager.LoadScene("Build");
        }

        /// <summary>
        /// Ensure NavMeshSurface exists and has initial NavMesh data
        /// This prevents "Failed to create agent" errors when instantiating NavMeshAgent components
        /// </summary>
        private void EnsureNavMeshSurfaceExists()
        {
            // Find all NavMeshSurface components in the scene
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);

            // If no NavMeshSurface exists, create one automatically
            if (surfaces.Length == 0)
            {
                Debug.Log("[PlayController] Creating NavMeshSurface...");

                // Create NavMesh GameObject
                GameObject navMeshObj = new GameObject("NavMeshSurface");
                NavMeshSurface surface = navMeshObj.AddComponent<NavMeshSurface>();

                // Configure NavMeshSurface - use PhysicsColliders to avoid mesh read/write issues
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

                // Exclude dynamic objects and UI from NavMesh
                // Include: Default, Terrain, Ground layers (modify as needed)
                // Exclude: Player, Enemies, UI layers
                surface.layerMask = ~0; // Start with all layers

                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1)
                    surface.layerMask &= ~(1 << playerLayer); // Exclude Player layer

                int enemyLayer = LayerMask.NameToLayer("Enemies");
                if (enemyLayer == -1)
                    enemyLayer = LayerMask.NameToLayer("Enemy"); // Fallback to singular
                if (enemyLayer != -1)
                    surface.layerMask &= ~(1 << enemyLayer); // Exclude Enemies layer

                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer != -1)
                    surface.layerMask &= ~(1 << uiLayer); // Exclude UI layer

                // IMPORTANT: Bake an initial empty NavMesh immediately
                // This allows NavMeshAgent components to initialize without errors
                // We'll rebake with actual terrain after world loads
                try
                {
                    surface.BuildNavMesh();
                    Debug.Log("[PlayController] ✓ Created and baked initial NavMeshSurface (will rebake after world loads)");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayController] Failed to bake initial NavMesh: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Build NavMesh for AI navigation
        /// </summary>
        private void BuildNavMesh()
        {
            // Find all NavMeshSurface components in the scene
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);

            if (surfaces.Length == 0)
            {
                Debug.LogError("[PlayController] No NavMeshSurface found! Call EnsureNavMeshSurfaceExists() first.");
                return;
            }

            // Build all NavMesh surfaces
            foreach (NavMeshSurface surface in surfaces)
            {
                if (surface != null)
                {
                    // Ensure using PhysicsColliders to avoid mesh read/write issues
                    if (surface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
                    {
                        Debug.LogWarning($"[PlayController] NavMeshSurface '{surface.gameObject.name}' is using {surface.useGeometry}. Switching to PhysicsColliders to avoid mesh read errors.");
                        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                    }

                    try
                    {
                        surface.BuildNavMesh();
                        Debug.Log($"[PlayController] ✓ Built NavMesh for surface: {surface.gameObject.name}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[PlayController] Failed to build NavMesh: {e.Message}");
                    }
                }
            }
        }

        private void SetupActionButtons()
        {
            // Setup jump button
            if (jumpButton != null)
            {
                jumpButton.onClick.AddListener(OnJumpButtonPressed);
                Debug.Log("[PlayController] Jump button (A) setup complete");
            }
            else
            {
                Debug.LogWarning("[PlayController] Jump button (A) not assigned");
            }

            // Setup attack button
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(OnAttackButtonPressed);
                Debug.Log("[PlayController] Attack button (B) setup complete");
            }
            else
            {
                Debug.LogWarning("[PlayController] Attack button (B) not assigned");
            }
        }

        private void OnJumpButtonPressed()
        {
            if (spawnedPlayer == null)
                return;

            Debug.Log("[PlayController] Jump button pressed");

            // Get player controller
            SimplePlayerController playerController = spawnedPlayer.GetComponent<SimplePlayerController>();
            if (playerController != null)
            {
                playerController.Jump();
            }

            // If using CharacterController directly
            CharacterController charController = spawnedPlayer.GetComponent<CharacterController>();
            if (charController != null && charController.isGrounded)
            {
                // Simple jump implementation - you may want to add this to SimplePlayerController
                // For now just log
                Debug.Log("[PlayController] Player jump triggered");
            }
        }

        private void OnAttackButtonPressed()
        {
            if (spawnedPlayer == null)
                return;

            Debug.Log("[PlayController] Attack button pressed");

            // Get player attack component
            PlayerAttack playerAttack = spawnedPlayer.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.Attack();
            }
            else
            {
                Debug.LogWarning("[PlayController] Player has no PlayerAttack component!");
            }

            // Also trigger animator if present
            Animator animator = spawnedPlayer.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // TODO: Implement actual attack logic
            // For now just trigger animation
            Debug.Log("[PlayController] Player attack triggered");
        }

        /// <summary>
        /// Generate invisible boundary colliders around all volumes to prevent player from leaving
        /// </summary>
        private void GenerateVolumeBoundaryColliders()
        {
            Debug.Log("[PlayController] GenerateVolumeBoundaryColliders() called");

            // Always destroy existing boundaries to ensure correct size
            GameObject existingBoundaries = GameObject.Find("VolumeBoundaries");
            if (existingBoundaries != null && existingBoundaries)
            {
                Debug.Log("[PlayController] Destroying existing boundaries to regenerate with correct volume size");
                Destroy(existingBoundaries);
            }

            // Find all VolumeGrid instances in the loaded world
            VolumeGrid[] volumeGrids = FindObjectsByType<VolumeGrid>(FindObjectsSortMode.None);
            Debug.Log($"[PlayController] Found {volumeGrids.Length} VolumeGrid(s)");

            if (volumeGrids.Length == 0)
            {
                Debug.LogWarning("[PlayController] No VolumeGrid found, cannot generate boundaries");
                return;
            }

            const float VOLUME_UNIT_SIZE = 8f;
            const float WALL_THICKNESS = 0.1f;

            // Create parent container at root of scene
            GameObject boundariesContainer = new GameObject("VolumeBoundaries");
            boundariesContainer.transform.position = Vector3.zero;

            Debug.Log($"[PlayController] Creating boundaries for {volumeGrids.Length} volume(s)");

            foreach (VolumeGrid volumeGrid in volumeGrids)
            {
                // Hide volume bounds visual in Play mode
                volumeGrid.showBounds = false;
                Transform boundsVisual = volumeGrid.transform.Find("VolumeBounds");
                if (boundsVisual != null)
                {
                    boundsVisual.gameObject.SetActive(false);
                }

                Vector3 volumeCenter = volumeGrid.GetCenter();
                Vector3Int volumeSize = volumeGrid.volumeDimensions;

                Debug.Log($"[PlayController] VolumeGrid '{volumeGrid.name}' dimensions: {volumeSize.x}x{volumeSize.y}x{volumeSize.z}");

                // Calculate actual world size
                float width = volumeSize.x * VOLUME_UNIT_SIZE;
                float actualHeight = volumeSize.y * VOLUME_UNIT_SIZE;
                float depth = volumeSize.z * VOLUME_UNIT_SIZE;

                // Wall height is actual volume height
                float wallHeight = actualHeight;

                // VolumeGrid.GetCenter() returns half extents, so this is already the center position
                Vector3 center = volumeCenter;

                // Calculate bottom Y (floor level)
                float floorY = center.y - (actualHeight / 2f);

                // Walls extend from floor to top of volume
                float wallCenterY = floorY + (wallHeight / 2f);

                Debug.Log($"[PlayController] Volume: {volumeGrid.name}, Size: {width}x{actualHeight}x{depth}m, Center: {center}, Floor Y: {floorY}, Wall Center Y: {wallCenterY}");

                // Create 4 invisible walls (left, right, front, back)
                // Left/Right walls extend full depth
                CreateBoundaryWall(boundariesContainer.transform, $"BoundaryWall_Left_{volumeGrid.name}",
                    new Vector3(center.x - (width / 2f), wallCenterY, center.z),
                    new Vector3(WALL_THICKNESS, wallHeight, depth));

                CreateBoundaryWall(boundariesContainer.transform, $"BoundaryWall_Right_{volumeGrid.name}",
                    new Vector3(center.x + (width / 2f), wallCenterY, center.z),
                    new Vector3(WALL_THICKNESS, wallHeight, depth));

                // Front/Back walls are shorter to avoid corner overlap (width - 2 * WALL_THICKNESS)
                float frontBackWidth = width - (2f * WALL_THICKNESS);
                CreateBoundaryWall(boundariesContainer.transform, $"BoundaryWall_Front_{volumeGrid.name}",
                    new Vector3(center.x, wallCenterY, center.z - (depth / 2f)),
                    new Vector3(frontBackWidth, wallHeight, WALL_THICKNESS));

                CreateBoundaryWall(boundariesContainer.transform, $"BoundaryWall_Back_{volumeGrid.name}",
                    new Vector3(center.x, wallCenterY, center.z + (depth / 2f)),
                    new Vector3(frontBackWidth, wallHeight, WALL_THICKNESS));

                // Create floor collider at actual floor level
                CreateBoundaryWall(boundariesContainer.transform, $"BoundaryFloor_{volumeGrid.name}",
                    new Vector3(center.x, floorY, center.z),
                    new Vector3(width, 0.1f, depth));
            }

            Debug.Log($"[PlayController] Generated boundary colliders for {volumeGrids.Length} volume(s)");
        }

        /// <summary>
        /// Create a single invisible boundary wall with box collider
        /// </summary>
        private void CreateBoundaryWall(Transform parent, string name, Vector3 worldPosition, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = worldPosition;
            wall.transform.rotation = Quaternion.identity;

            // Add box collider (invisible barrier)
            // BoxCollider size is the actual size, and center is at (0,0,0) local space
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.center = Vector3.zero; // Center at GameObject position
            collider.size = size;

            // Set layer
            wall.layer = LayerMask.NameToLayer("Default");
        }
    }
}
