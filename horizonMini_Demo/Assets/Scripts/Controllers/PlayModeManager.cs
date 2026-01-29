using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HorizonMini.Core;
using HorizonMini.Data;
using HorizonMini.MiniGames;
using HorizonMini.Build;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages the transition between Browse Mode and Play Mode
    /// Handles unified "Go" button and Play Mode UI (Back/Build buttons)
    /// </summary>
    public class PlayModeManager : MonoBehaviour
    {
        public enum Mode
        {
            Browse,
            Play
        }

        [Header("References")]
        [SerializeField] private BrowseController browseController;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Camera mainCamera;

        [Header("UI - Browse Mode")]
        [SerializeField] private Button goButton;
        [SerializeField] private GameObject goButtonObject;

        [Header("UI - Play Mode")]
        [SerializeField] private GameObject playModeUI;
        [SerializeField] private Button backButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private GameObject virtualJoystickUI;
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button attackButton;

        [Header("Camera Settings (Play Mode)")]
        [Tooltip("Initial camera height above player")]
        [SerializeField] private float cameraHeight = 5f;
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
        [Tooltip("Zoom smoothness (SmoothDamp time)")]
        [SerializeField] private float cameraZoomSpeed = 2f;

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
        [Tooltip("Time in seconds to transition between Browse/Play modes")]
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

        private Mode currentMode = Mode.Browse;
        private GameObject spawnedPlayer;
        private SimplePlayerController playerController;
        private WorldInstance currentWorld;
        private bool isCurrentWorldMiniGame = false;

        // Camera state for Play Mode
        private Vector3 cameraOffset;
        private float currentCameraRotation = 0f;
        private Vector3 browseCameraPosition;
        private Quaternion browseCameraRotation;

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

        private static PlayModeManager _instance;
        public static PlayModeManager Instance => _instance;

        /// <summary>
        /// Check if camera is currently transitioning (for BrowseController to skip camera updates)
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
            if (browseController == null)
                browseController = FindFirstObjectByType<BrowseController>();

            if (mainCamera == null)
                mainCamera = Camera.main;

            // Ensure BrowseController is initialized
            if (browseController != null && AppRoot.Instance != null)
            {
                // Check if BrowseController has been initialized by calling a test
                // If not initialized, initialize it now
                try
                {
                    string testWorldId = browseController.GetCurrentWorldId();
                    if (string.IsNullOrEmpty(testWorldId))
                    {
                        Debug.Log("[PlayModeManager] BrowseController not initialized, initializing now...");
                        browseController.Initialize(AppRoot.Instance);
                        browseController.SetActive(true);
                    }
                }
                catch
                {
                    Debug.Log("[PlayModeManager] BrowseController not initialized, initializing now...");
                    browseController.Initialize(AppRoot.Instance);
                    browseController.SetActive(true);
                }
            }

            // Auto-find VirtualJoystick if not assigned
            if (virtualJoystickUI == null)
            {
                virtualJoystickUI = GameObject.Find("VirtualJoystick");
                if (virtualJoystickUI != null)
                    Debug.Log("[PlayModeManager] Found VirtualJoystick");
            }

            // Auto-find action buttons if not assigned
            if (jumpButton == null)
            {
                GameObject jumpObj = GameObject.Find("JumpButton");
                if (jumpObj != null) jumpButton = jumpObj.GetComponent<Button>();
            }

            if (attackButton == null)
            {
                GameObject attackObj = GameObject.Find("AttackButton");
                if (attackObj != null) attackButton = attackObj.GetComponent<Button>();
            }

            // Create UI if not assigned
            if (goButton == null)
                CreateGoButton();

            if (playModeUI == null)
                CreatePlayModeUI();

            // Setup button listeners
            if (goButton != null)
                goButton.onClick.AddListener(OnGoButtonClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            if (buildButton != null)
                buildButton.onClick.AddListener(OnBuildButtonClicked);

            // Initially hide all UI
            ShowGoButton(false);
            ShowPlayModeUI(false);
            ShowGameplayUI(false); // Hide VirtualJoystick and AB buttons in Browse mode
        }

        /// <summary>
        /// Called when a world becomes the center world in Browse mode
        /// </summary>
        public void OnWorldBrowsed(WorldInstance world, WorldData worldData)
        {
            Debug.Log($"<color=cyan>[PlayModeManager] OnWorldBrowsed called - world={world?.WorldId}, worldData={(worldData != null ? "exists" : "null")}</color>");

            // DON'T update currentWorld here - let RespawnPlayerAtCurrentWorld() handle it
            // so it can detect world changes properly

            // Check if this world has a mini game
            isCurrentWorldMiniGame = worldData != null &&
                                      worldData.miniGames != null &&
                                      worldData.miniGames.Count > 0;

            Debug.Log($"[PlayModeManager] isCurrentWorldMiniGame={isCurrentWorldMiniGame}, currentMode={currentMode}");

            if (currentMode == Mode.Browse)
            {
                // Show Go button
                ShowGoButton(true);

                // Don't spawn player here - BrowseController will handle it
                // after 0.5s of stability via RespawnPlayerAtCurrentWorld()

                // For mini games, despawn any existing player
                if (isCurrentWorldMiniGame)
                {
                    DespawnPlayer();
                }
            }
        }

        /// <summary>
        /// Show/hide the unified "Go" button
        /// </summary>
        public void ShowGoButton(bool show)
        {
            if (goButtonObject != null)
            {
                goButtonObject.SetActive(show);
            }
        }

        /// <summary>
        /// Show/hide Play Mode UI (Back + Build buttons)
        /// </summary>
        public void ShowPlayModeUI(bool show)
        {
            if (playModeUI != null)
            {
                playModeUI.SetActive(show);
            }
        }

        private void OnGoButtonClicked()
        {
            EnterPlayMode();
        }

        private void OnBackButtonClicked()
        {
            ExitPlayMode();
        }

        private void OnBuildButtonClicked()
        {
            // TODO: Implement transition to Build Mode
            Debug.Log("[PlayModeManager] Build button clicked - transitioning to Build Mode");

            // This should load Build scene or switch to Build mode
            // For now, just log
        }

        /// <summary>
        /// Enter Play Mode from Browse Mode
        /// </summary>
        public void EnterPlayMode()
        {
            if (currentMode == Mode.Play)
                return;

            Debug.Log($"<color=cyan>[PlayModeManager] Entering Play Mode - isMiniGame: {isCurrentWorldMiniGame}</color>");

            currentMode = Mode.Play;

            // Hide Browse UI
            ShowGoButton(false);

            // Show Play Mode UI
            ShowPlayModeUI(true);

            // Disable BrowseController for both mini games and normal worlds
            if (browseController != null)
            {
                browseController.enabled = false;
                Debug.Log("[PlayModeManager] Disabled BrowseController");
            }

            if (isCurrentWorldMiniGame)
            {
                // Mini game - start the game
                if (BrowseMiniGameTrigger.Instance != null)
                {
                    BrowseMiniGameTrigger.Instance.StartGame();
                }
            }
            else
            {
                // Normal world - enable player control

                if (playerController != null)
                {
                    playerController.enabled = true;
                    Debug.Log($"[PlayModeManager] Enabled PlayerController - enabled={playerController.enabled}, gameObject.activeInHierarchy={playerController.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.LogError("[PlayModeManager] PlayerController is NULL! Cannot enable player control.");
                }

                // Save Browse camera state and transition to Play Mode camera
                if (mainCamera != null && spawnedPlayer != null)
                {
                    browseCameraPosition = mainCamera.transform.position;
                    browseCameraRotation = mainCamera.transform.rotation;

                    // Initialize camera for player following
                    currentCameraRotation = spawnedPlayer.transform.eulerAngles.y;
                    CalculateCameraOffset();

                    // Calculate target position for Play Mode
                    Vector3 targetPos = spawnedPlayer.transform.position + cameraOffset;
                    Vector3 lookAtPos = spawnedPlayer.transform.position + Vector3.up * 1.5f;
                    Quaternion targetRot = Quaternion.LookRotation(lookAtPos - targetPos);

                    // Start smooth transition
                    StartCameraTransition(browseCameraPosition, browseCameraRotation, targetPos, targetRot);

                    Debug.Log("[PlayModeManager] Starting camera transition to Play Mode");
                }

                // Show VirtualJoystick and action buttons
                ShowGameplayUI(true);
            }

            Debug.Log("<color=green>[PlayModeManager] ✓ Entered Play Mode</color>");
        }

        /// <summary>
        /// Exit Play Mode back to Browse Mode
        /// </summary>
        public void ExitPlayMode()
        {
            if (currentMode == Mode.Browse)
                return;

            Debug.Log($"<color=yellow>[PlayModeManager] Exiting Play Mode - isMiniGame: {isCurrentWorldMiniGame}</color>");

            currentMode = Mode.Browse;

            // Hide Play Mode UI
            ShowPlayModeUI(false);

            // Show Browse UI
            ShowGoButton(true);

            // Re-enable BrowseController
            if (browseController != null)
            {
                browseController.enabled = true;
                Debug.Log("[PlayModeManager] Re-enabled BrowseController");
            }

            if (isCurrentWorldMiniGame)
            {
                // Mini game - stop the game
                if (BrowseMiniGameTrigger.Instance != null)
                {
                    BrowseMiniGameTrigger.Instance.StopGame();
                }
            }
            else
            {
                // Normal world - disable player control
                if (playerController != null)
                {
                    playerController.enabled = false;
                    Debug.Log("[PlayModeManager] Disabled PlayerController");
                }

                // DON'T hide player here - keep it visible during transition
                // BrowseController will manage visibility after transition completes

                // Start smooth transition back to saved Browse camera state
                if (mainCamera != null)
                {
                    Vector3 currentPos = mainCamera.transform.position;
                    Quaternion currentRot = mainCamera.transform.rotation;

                    // Transition back to the saved Browse camera position
                    StartCameraTransition(currentPos, currentRot, browseCameraPosition, browseCameraRotation);

                    Debug.Log("[PlayModeManager] Starting camera transition to Browse Mode");
                }

                // Hide VirtualJoystick and action buttons
                ShowGameplayUI(false);
            }

            Debug.Log("<color=green>[PlayModeManager] ✓ Exited Play Mode</color>");
        }

        /// <summary>
        /// Show/hide player in Browse Mode
        /// </summary>
        public void ShowPlayerInBrowse(bool show)
        {
            if (spawnedPlayer != null)
            {
                spawnedPlayer.SetActive(show);
            }
        }

        /// <summary>
        /// Re-spawn player at current world's spawn point
        /// Called when world changes in Browse Mode
        /// </summary>
        public void RespawnPlayerAtCurrentWorld(WorldInstance world)
        {
            if (currentMode != Mode.Browse || world == null)
                return;

            Debug.Log($"<color=green>[PlayModeManager] RespawnPlayerAtCurrentWorld - world: {world.WorldId}</color>");

            // Check if this world has a mini game
            HorizonMini.Core.AppRoot appRoot = FindFirstObjectByType<HorizonMini.Core.AppRoot>();
            if (appRoot != null && appRoot.WorldLibrary != null)
            {
                HorizonMini.Data.WorldData worldData = appRoot.WorldLibrary.GetWorldData(world.WorldId);
                bool hasMiniGame = worldData != null && worldData.miniGames != null && worldData.miniGames.Count > 0;

                if (hasMiniGame)
                {
                    Debug.Log($"[PlayModeManager] World {world.WorldId} has mini game - skipping player spawn");
                    // Despawn any existing player
                    DespawnPlayer();
                    // Update current world reference
                    currentWorld = world;
                    return;
                }
            }

            // Always despawn old player to trigger respawn effect
            DespawnPlayer();

            // Update current world reference
            currentWorld = world;

            // Spawn player with fall effect and particles
            Debug.Log($"[PlayModeManager] Spawning player at world {world.WorldId}");
            SpawnPlayerInIdle(world);

            // Show the player
            ShowPlayerInBrowse(true);
        }

        /// <summary>
        /// Show/hide gameplay UI (VirtualJoystick and AB buttons)
        /// </summary>
        private void ShowGameplayUI(bool show)
        {
            if (virtualJoystickUI != null)
            {
                virtualJoystickUI.SetActive(show);
                Debug.Log($"[PlayModeManager] VirtualJoystick {(show ? "shown" : "hidden")}");
            }

            if (jumpButton != null)
            {
                jumpButton.gameObject.SetActive(show);
            }

            if (attackButton != null)
            {
                attackButton.gameObject.SetActive(show);
            }
        }

        private void Update()
        {
            // Update camera transition if active
            if (isCameraTransitioning)
            {
                UpdateCameraTransition();
            }
            // Only update camera in Play Mode when not transitioning
            else if (currentMode == Mode.Play)
            {
                if (!isCurrentWorldMiniGame && spawnedPlayer != null)
                {
                    // Normal world - follow player
                    UpdateCameraFollow();
                    HandleCameraRotationInput();
                    HandleCameraZoomInput();
                }
                else if (isCurrentWorldMiniGame)
                {
                    // Mini game mode - allow free camera orbit and zoom
                    HandleMiniGameCameraControl();
                }
            }
        }

        /// <summary>
        /// Spawn player at spawn point in idle state (Browse mode)
        /// </summary>
        private void SpawnPlayerInIdle(WorldInstance world)
        {
            Debug.Log($"<color=yellow>[PlayModeManager] SpawnPlayerInIdle called - world={world?.WorldId}</color>");

            // Remove existing player if any
            DespawnPlayer();

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayModeManager] Player prefab not assigned!");
                return;
            }

            Debug.Log($"[PlayModeManager] Player prefab: {playerPrefab.name}");

            // Find spawn point in the world
            Transform spawnPoint = FindSpawnPointInWorld(world);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[PlayModeManager] No spawn point found in world {world.WorldId}");
                return;
            }

            // Get spawn point's local position and rotation relative to world
            Vector3 spawnLocalPosition = spawnPoint.localPosition;
            Quaternion spawnLocalRotation = spawnPoint.localRotation;

            // Spawn 3 meters above the spawn point for a falling effect
            Vector3 spawnAbovePosition = spawnLocalPosition + new Vector3(0, 3f, 0);

            // Instantiate player as child of world with local position
            spawnedPlayer = Instantiate(playerPrefab, world.transform);
            spawnedPlayer.transform.localPosition = spawnAbovePosition;
            spawnedPlayer.transform.localRotation = spawnLocalRotation;

            // Create spawn particle effect
            CreateSpawnParticles(spawnedPlayer.transform.position);

            playerController = spawnedPlayer.GetComponent<SimplePlayerController>();

            if (playerController == null)
            {
                // Add SimplePlayerController if missing
                playerController = spawnedPlayer.AddComponent<SimplePlayerController>();

                // Also add CharacterController if missing
                if (spawnedPlayer.GetComponent<CharacterController>() == null)
                {
                    CharacterController cc = spawnedPlayer.AddComponent<CharacterController>();
                    cc.radius = 0.5f;
                    cc.height = 2f;
                    cc.center = new Vector3(0, 1f, 0);
                }
            }

            if (playerController != null)
            {
                // In Browse mode, let player fall first, then disable control
                if (currentMode == Mode.Browse)
                {
                    StartCoroutine(DisablePlayerAfterFalling());
                }
                else
                {
                    // In Play mode, keep control disabled initially
                    playerController.enabled = false;
                }
            }

            // Setup action button listeners
            SetupActionButtons();
        }

        /// <summary>
        /// Wait for player to finish falling in Browse mode, then disable control
        /// </summary>
        private System.Collections.IEnumerator DisablePlayerAfterFalling()
        {
            if (playerController == null)
                yield break;

            // Keep controller enabled during fall
            playerController.enabled = true;

            // Wait for player to land (check if grounded for 2 consecutive frames)
            int groundedFrames = 0;
            CharacterController cc = spawnedPlayer.GetComponent<CharacterController>();

            while (groundedFrames < 2)
            {
                yield return null;

                if (cc != null && cc.isGrounded)
                {
                    groundedFrames++;
                }
                else
                {
                    groundedFrames = 0;
                }

                // Safety timeout after 3 seconds
                if (Time.time > Time.realtimeSinceStartup + 3f)
                    break;
            }

            // Player has landed, disable control
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }

        /// <summary>
        /// Remove spawned player
        /// </summary>
        private void DespawnPlayer()
        {
            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer);
                spawnedPlayer = null;
                playerController = null;
                Debug.Log("[PlayModeManager] Despawned player");
            }
        }

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

            // Configure main module
            var main = ps.main;
            main.duration = 1f;
            main.loop = false; // Don't loop - single burst only
            main.startLifetime = particleLifetime;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = spawnParticleColor;
            main.gravityModifier = particleGravity;
            main.maxParticles = spawnParticleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // World space for URP

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

        /// <summary>
        /// Find spawn point in world (looks for SpawnPoint component or "SpawnPoint" named object)
        /// </summary>
        private Transform FindSpawnPointInWorld(WorldInstance world)
        {
            // Try to find SpawnPoint component
            var spawnPoint = world.GetComponentInChildren<SpawnPoint>();
            if (spawnPoint != null)
                return spawnPoint.transform;

            // Fallback: look for GameObject named "SpawnPoint"
            Transform sp = world.transform.Find("SpawnPoint");
            if (sp != null)
                return sp;

            // Ultimate fallback: world center at ground level
            Debug.LogWarning("[PlayModeManager] Using world center as spawn point");
            GameObject tempSpawn = new GameObject("TempSpawnPoint");
            tempSpawn.transform.SetParent(world.transform);
            tempSpawn.transform.localPosition = new Vector3(0, 0.5f, 0);
            return tempSpawn.transform;
        }

        private void CreateGoButton()
        {
            // Find or create canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("PlayModeCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create Go button
            goButtonObject = new GameObject("GoButton");
            goButtonObject.transform.SetParent(canvas.transform);

            RectTransform rect = goButtonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.1f);
            rect.anchorMax = new Vector2(0.5f, 0.1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(200, 80);

            Image image = goButtonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.8f, 0.3f, 0.9f);

            goButton = goButtonObject.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(goButtonObject.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Go";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 32;
            text.fontStyle = FontStyles.Bold;

            Debug.Log("[PlayModeManager] Created Go button");
        }

        private void CreatePlayModeUI()
        {
            // Find canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PlayModeManager] No canvas found for Play Mode UI");
                return;
            }

            // Create container
            playModeUI = new GameObject("PlayModeUI");
            playModeUI.transform.SetParent(canvas.transform);

            // Create Back button (top-left)
            GameObject backObj = new GameObject("BackButton");
            backObj.transform.SetParent(playModeUI.transform);

            RectTransform backRect = backObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 1);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.anchoredPosition = new Vector2(20, -20);
            backRect.sizeDelta = new Vector2(120, 50);

            Image backImage = backObj.AddComponent<Image>();
            backImage.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);

            backButton = backObj.AddComponent<Button>();

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backObj.transform);
            RectTransform backTextRect = backTextObj.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI backText = backTextObj.AddComponent<TextMeshProUGUI>();
            backText.text = "Back";
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = Color.white;
            backText.fontSize = 20;

            // Create Build button (top-right)
            GameObject buildObj = new GameObject("BuildButton");
            buildObj.transform.SetParent(playModeUI.transform);

            RectTransform buildRect = buildObj.AddComponent<RectTransform>();
            buildRect.anchorMin = new Vector2(1, 1);
            buildRect.anchorMax = new Vector2(1, 1);
            buildRect.pivot = new Vector2(1, 1);
            buildRect.anchoredPosition = new Vector2(-20, -20);
            buildRect.sizeDelta = new Vector2(120, 50);

            Image buildImage = buildObj.AddComponent<Image>();
            buildImage.color = new Color(0.3f, 0.5f, 0.8f, 0.9f);

            buildButton = buildObj.AddComponent<Button>();

            GameObject buildTextObj = new GameObject("Text");
            buildTextObj.transform.SetParent(buildObj.transform);
            RectTransform buildTextRect = buildTextObj.AddComponent<RectTransform>();
            buildTextRect.anchorMin = Vector2.zero;
            buildTextRect.anchorMax = Vector2.one;
            buildTextRect.offsetMin = Vector2.zero;
            buildTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buildText = buildTextObj.AddComponent<TextMeshProUGUI>();
            buildText.text = "Build";
            buildText.alignment = TextAlignmentOptions.Center;
            buildText.color = Color.white;
            buildText.fontSize = 20;

            Debug.Log("[PlayModeManager] Created Play Mode UI (Back + Build buttons)");
        }

        private void SetupActionButtons()
        {
            // Clear existing listeners to avoid duplicates
            if (jumpButton != null)
            {
                jumpButton.onClick.RemoveAllListeners();
                jumpButton.onClick.AddListener(OnJumpButtonPressed);
                Debug.Log("[PlayModeManager] Jump button listener added");
            }

            if (attackButton != null)
            {
                attackButton.onClick.RemoveAllListeners();
                attackButton.onClick.AddListener(OnAttackButtonPressed);
                Debug.Log("[PlayModeManager] Attack button listener added");
            }
        }

        private void OnJumpButtonPressed()
        {
            if (playerController != null && playerController.enabled)
            {
                playerController.Jump();
                Debug.Log("[PlayModeManager] Jump button pressed");
            }
        }

        private void OnAttackButtonPressed()
        {
            // TODO: Implement attack
            Debug.Log("[PlayModeManager] Attack button pressed");
        }

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
            if (!isCameraTransitioning || mainCamera == null)
                return;

            // Increment progress
            cameraTransitionProgress += Time.deltaTime / cameraTransitionSpeed;

            // Use smoothstep for easing
            float t = Mathf.SmoothStep(0f, 1f, cameraTransitionProgress);

            // Interpolate position and rotation
            mainCamera.transform.position = Vector3.Lerp(cameraTransitionStartPos, cameraTransitionTargetPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(cameraTransitionStartRot, cameraTransitionTargetRot, t);

            // Check if transition complete
            if (cameraTransitionProgress >= 1f)
            {
                isCameraTransitioning = false;
                mainCamera.transform.position = cameraTransitionTargetPos;
                mainCamera.transform.rotation = cameraTransitionTargetRot;
                Debug.Log("[PlayModeManager] Camera transition complete");

                // If transitioning back to Browse Mode, enable BrowseController now
                if (currentMode == Mode.Browse && browseController != null)
                {
                    browseController.enabled = true;

                    // Navigate back to the current world
                    if (currentWorld != null)
                    {
                        string currentWorldId = currentWorld.WorldId;
                        browseController.NavigateToWorldId(currentWorldId);
                        Debug.Log($"[PlayModeManager] Navigated back to world {currentWorldId}");
                    }

                    // Sync BrowseController camera state from actual camera position
                    browseController.SendMessage("SyncCameraDistanceFromPosition", SendMessageOptions.DontRequireReceiver);
                    Debug.Log("[PlayModeManager] Enabled BrowseController and synced camera state");
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
            UpdateCameraPosition(immediate: false);
        }

        private void UpdateCameraPosition(bool immediate)
        {
            if (mainCamera == null || spawnedPlayer == null)
                return;

            Vector3 targetPosition = spawnedPlayer.transform.position + cameraOffset;

            if (immediate)
            {
                mainCamera.transform.position = targetPosition;
            }
            else
            {
                mainCamera.transform.position = Vector3.Lerp(
                    mainCamera.transform.position,
                    targetPosition,
                    Time.deltaTime * cameraSmoothSpeed
                );
            }

            // Look at player
            Vector3 lookAtPosition = spawnedPlayer.transform.position + Vector3.up * 1.5f;
            mainCamera.transform.LookAt(lookAtPosition);
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
        /// Handle camera control for mini games (orbit and zoom around game center)
        /// </summary>
        private void HandleMiniGameCameraControl()
        {
            if (mainCamera == null) return;

            // Mouse scroll wheel zoom
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                // Move camera closer/further along its forward direction
                Vector3 forward = mainCamera.transform.forward;
                mainCamera.transform.position += forward * scrollDelta * 2f;
            }

            // Touch input for camera orbit
            if (Input.touchCount == 1)
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

                    // Orbit camera around current look-at point
                    OrbitMiniGameCamera(delta * 0.3f);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isDraggingCamera = false;
                }
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

                // Orbit camera around current look-at point
                OrbitMiniGameCamera(delta * 0.3f);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingCamera = false;
            }
        }

        /// <summary>
        /// Orbit camera around a point in mini game mode
        /// </summary>
        private void OrbitMiniGameCamera(Vector2 delta)
        {
            if (mainCamera == null) return;

            // Calculate orbit center (raycast to find what camera is looking at)
            Vector3 orbitCenter = mainCamera.transform.position + mainCamera.transform.forward * 10f;

            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 50f))
            {
                orbitCenter = hit.point;
            }

            // Rotate around orbit center
            Vector3 camToCenter = orbitCenter - mainCamera.transform.position;
            float distance = camToCenter.magnitude;

            // Horizontal rotation (around Y axis)
            Quaternion yawRotation = Quaternion.AngleAxis(delta.x, Vector3.up);

            // Vertical rotation (around camera's right axis)
            Quaternion pitchRotation = Quaternion.AngleAxis(-delta.y, mainCamera.transform.right);

            // Apply rotations
            mainCamera.transform.position = orbitCenter - (yawRotation * pitchRotation * camToCenter);
            mainCamera.transform.LookAt(orbitCenter);
        }

        #endregion
    }
}
