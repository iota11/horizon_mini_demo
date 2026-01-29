using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Core;
using HorizonMini.Data;
using HorizonMini.UI;
using HorizonMini.MiniGames;
using HorizonMini.Build;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages the browsing feed with vertical paging and preloading
    /// </summary>
    public class BrowseController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform worldContainer;
        [SerializeField] private float worldSpacing = 100f; // Large fixed spacing between worlds
        [SerializeField] private float swipeThreshold = 50f;
        #pragma warning disable 0414
        [SerializeField] private float transitionSpeed = 5f; // Reserved for future smooth transitions
        #pragma warning restore 0414
        [SerializeField] private float rotationSpeed = 1f; // Degrees per pixel

        [Header("Scroll Settings")]
        [Tooltip("How much the view scrolls per pixel dragged (higher = more sensitive)")]
        [SerializeField] private float scrollSensitivity = 0.02f;
        [Tooltip("30% of world spacing to trigger snap to next world")]
        [SerializeField] private float snapThreshold = 0.3f;
        [Tooltip("SmoothDamp time for snap animation")]
        [SerializeField] private float snapSpeed = 0.15f;
        [Tooltip("Minimum velocity to trigger snap")]
        [SerializeField] private float velocityThreshold = 5f;

        [Header("Auto Rotation")]
        [SerializeField] private bool enableAutoRotation = true;
        [SerializeField] private float autoRotationSpeed = 5f; // Degrees per second
        [SerializeField] private float autoRotationDelay = 1f; // Seconds of inactivity before auto-rotation starts

        [Header("Camera")]
        [SerializeField] private Camera browseCamera;
        [SerializeField] private float cameraDistance = 30f;
        [SerializeField] private float cameraAngle = 45f; // 45 degree top-down view
        [SerializeField] private float fieldOfView = 60f; // Perspective camera FOV
        [SerializeField] private float minCameraDistance = 15f; // Minimum distance for zoom in
        [SerializeField] private float maxCameraDistance = 50f; // Maximum distance for zoom out
        [SerializeField] private float cameraZoomSpeed = 0.2f; // SmoothDamp time for camera zoom
        [SerializeField] private float scrollWheelZoomSpeed = 5f; // How much scroll wheel affects zoom
        [SerializeField] private float pinchZoomSpeed = 0.5f; // How much pinch affects zoom

        private AppRoot appRoot;
        private List<WorldMeta> worldFeed;
        private int currentIndex = 0;

        // Preload pool: previous, current, next
        private WorldInstance[] loadedWorlds = new WorldInstance[3];
        private int[] loadedIndices = new int[3] { -1, -1, -1 };

        // Gesture tracking
        private Vector2 touchStartPos;
        private Vector2 lastTouchPos;
        private bool isDragging = false;
        private bool isRotatingCamera = false; // True if current drag is horizontal rotation, false if vertical scroll
        private float currentOrbitAngle = 0f; // Camera's orbit angle around world
        private float targetYPosition = 0f;

        // Vertical scroll tracking for feed-style browsing
        private float currentScrollOffset = 0f; // Current Y offset from snapped position
        private float scrollVelocity = 0f; // Vertical scroll velocity
        private bool isSnapping = false; // Currently animating snap to world
        private float snapAnimationVelocity = 0f; // For SmoothDamp
        private float snapToTargetOffset = 0f; // Target offset for snap animation
        private float lastDragTime = 0f;

        // Camera zoom tracking (perspective distance)
        private float currentCameraDistance = 30f;
        private float targetCameraDistance = 30f;
        private float cameraDistanceVelocity = 0f;
        private float manualZoomOffset = 0f; // User's manual zoom adjustment

        // Pinch gesture tracking
        private float lastPinchDistance = 0f;
        private bool isPinching = false;

        // Auto rotation tracking
        private bool isAutoRotating = false;
        private float lastInteractionTime = 0f;

        // Player visibility tracking
        private float lastWorldStopTime = 0f;
        private bool isWorldStable = false;
        private bool hasShownPlayer = false; // Track if we've already shown player for current stable period
        private const float PLAYER_SHOW_DELAY = 0.5f;

        private bool isActive = false;

        private void OnEnable()
        {
            // When script component is enabled, ensure isActive is true
            // This handles the case where PlayModeManager uses .enabled instead of SetActive()
            if (appRoot != null && worldFeed != null)
            {
                Debug.Log("[BrowseController] OnEnable - setting isActive = true");
                isActive = true;

                // Reset player visibility state when re-enabled (e.g., returning from Play Mode)
                lastWorldStopTime = Time.time;
                isWorldStable = true;
                hasShownPlayer = false;
            }
        }

        private void OnDisable()
        {
            // When script component is disabled, set isActive to false
            Debug.Log("[BrowseController] OnDisable - setting isActive = false");
            isActive = false;
        }

        public void Initialize(AppRoot root)
        {
            appRoot = root;

            if (worldContainer == null)
            {
                GameObject container = new GameObject("BrowseWorldContainer");
                worldContainer = container.transform;
                worldContainer.SetParent(transform);
            }

            if (browseCamera == null)
            {
                browseCamera = Camera.main;
            }

            // Set camera to perspective
            if (browseCamera != null)
            {
                browseCamera.orthographic = false;
                browseCamera.fieldOfView = fieldOfView;
            }

            // Initialize camera distance
            currentCameraDistance = cameraDistance;
            targetCameraDistance = cameraDistance;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (active)
            {
                // Ensure camera is set to perspective when activating
                if (browseCamera != null)
                {
                    browseCamera.orthographic = false;
                    browseCamera.fieldOfView = fieldOfView;

                    // Sync camera distance from current camera position
                    SyncCameraDistanceFromPosition();
                }

                RefreshFeed();
                LoadInitialWorlds();
            }
            else
            {
                ClearAllWorlds();
            }

            if (browseCamera != null)
            {
                browseCamera.gameObject.SetActive(active);
            }
        }

        private void RefreshFeed()
        {
            worldFeed = appRoot.WorldLibrary.GetAllWorlds();
            currentIndex = 0;
        }

        private void LoadInitialWorlds()
        {
            if (worldFeed == null || worldFeed.Count == 0)
            {
                Debug.LogWarning("No worlds in feed");
                return;
            }

            currentIndex = Mathf.Clamp(currentIndex, 0, worldFeed.Count - 1);
            LoadWorldsAroundIndex(currentIndex);

            // Calculate optimal camera distance for initial world
            CalculateOptimalCameraDistance();
            currentCameraDistance = targetCameraDistance; // Set immediately for first world

            PositionCamera();

            // Update mini game button visibility for first world
            UpdateMiniGameButtonVisibility();
        }

        private void LoadWorldsAroundIndex(int centerIndex)
        {
            // Load: previous (0), current (1), next (2)
            int prevIndex = centerIndex - 1;
            int nextIndex = centerIndex + 1;

            LoadWorldAtSlot(0, prevIndex);
            LoadWorldAtSlot(1, centerIndex);
            LoadWorldAtSlot(2, nextIndex);

            UpdateWorldPositions();
        }

        private void LoadWorldAtSlot(int slot, int worldIndex)
        {
            // If already loaded, no need to reload
            if (loadedIndices[slot] == worldIndex && loadedWorlds[slot] != null)
            {
                Debug.Log($"[BrowseController] World at slot {slot} already loaded");
                return;
            }

            // Unload existing
            if (loadedWorlds[slot] != null)
            {
                Destroy(loadedWorlds[slot].gameObject);
                loadedWorlds[slot] = null;
            }

            // Load new world
            if (worldIndex >= 0 && worldIndex < worldFeed.Count)
            {
                WorldMeta meta = worldFeed[worldIndex];
                WorldInstance instance = appRoot.WorldLibrary.InstantiateWorld(meta.id, worldContainer);

                if (instance != null)
                {
                    loadedWorlds[slot] = instance;
                    loadedIndices[slot] = worldIndex;

                    // Disable volume visuals in Browse mode
                    DisableVolumeVisuals(instance);

                    // Set activation level
                    if (slot == 1)
                    {
                        instance.SetActivationLevel(ActivationLevel.FullyActive);
                    }
                    else
                    {
                        instance.SetActivationLevel(ActivationLevel.Preloaded);
                    }

                    // Don't load mini game here - it will be loaded by UpdateMiniGameButtonVisibility()
                    // after 0.5s delay when the world becomes center
                }
            }
            else
            {
                loadedIndices[slot] = -1;
            }
        }

        private void UpdateWorldPositions()
        {
            // Position worlds along Y axis for smooth scrolling
            // Worlds move based on currentScrollOffset for smooth transitions
            for (int i = 0; i < loadedWorlds.Length; i++)
            {
                if (loadedWorlds[i] != null && loadedIndices[i] >= 0)
                {
                    // Calculate base position: previous (-worldSpacing), current (0), next (+worldSpacing)
                    int relativeIndex = i - 1; // -1, 0, 1
                    float baseY = relativeIndex * worldSpacing;

                    // Apply scroll offset - when scrolling down (positive offset), worlds move up
                    float targetY = baseY - currentScrollOffset;

                    // Check if this world has a mini game - if so, position at origin
                    string worldId = loadedWorlds[i].WorldId;
                    WorldData worldData = appRoot.WorldLibrary.GetWorldData(worldId);
                    bool hasMiniGame = worldData != null && worldData.miniGames != null && worldData.miniGames.Count > 0;

                    if (hasMiniGame)
                    {
                        // For mini game worlds, position directly at target (no volume center offset)
                        loadedWorlds[i].transform.position = new Vector3(0, targetY, 0);
                    }
                    else
                    {
                        // For normal worlds, align by volume center
                        Vector3 volumeCenter;
                        try
                        {
                            volumeCenter = loadedWorlds[i].GetVolumeGridCenter();
                        }
                        catch
                        {
                            volumeCenter = loadedWorlds[i].transform.position;
                        }

                        // Calculate offset: move world so its volume center is at target Y position
                        Vector3 currentWorldPos = loadedWorlds[i].transform.position;
                        Vector3 offsetFromPivotToCenter = volumeCenter - currentWorldPos;

                        // Position world so volume center ends up at (0, targetY, 0)
                        Vector3 targetCenterPos = new Vector3(0, targetY, 0);
                        loadedWorlds[i].transform.position = targetCenterPos - offsetFromPivotToCenter;
                    }

                    // All worlds visible for smooth scrolling
                    loadedWorlds[i].gameObject.SetActive(true);
                    loadedWorlds[i].SetActivationLevel(ActivationLevel.FullyActive);
                }
            }
        }

        private void PositionCamera()
        {
            if (browseCamera != null && worldContainer != null)
            {
                Vector3 worldCenter = worldContainer.position;

                if (loadedWorlds[1] != null)
                {
                    try
                    {
                        worldCenter = loadedWorlds[1].GetWorldBounds().center;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to get world bounds in PositionCamera: {e.Message}");
                    }
                }

                // Calculate 45-degree top-down camera position
                float angleRad = cameraAngle * Mathf.Deg2Rad;
                float horizontalDist = cameraDistance * Mathf.Cos(angleRad);
                float verticalDist = cameraDistance * Mathf.Sin(angleRad);

                browseCamera.transform.position = worldCenter + new Vector3(0, verticalDist, -horizontalDist);
                browseCamera.transform.LookAt(worldCenter);
            }
        }

        private void Update()
        {
            if (!isActive || worldFeed == null || worldFeed.Count == 0)
                return;

            HandleInput();
            UpdateAutoRotation();
            UpdateSnapAnimation();
            UpdateWorldPositions();
            UpdateCameraPosition();
            UpdatePlayerVisibility();
        }

        private void UpdatePlayerVisibility()
        {
            // Check if world is stable
            // World is stable when: not snapping AND (not dragging OR only rotating camera) AND scroll offset is near 0
            bool isScrolling = isDragging && !isRotatingCamera; // Vertical scrolling
            bool currentlyStable = !isSnapping && !isScrolling && Mathf.Abs(currentScrollOffset) < 1f;

            if (currentlyStable && !isWorldStable)
            {
                // Just became stable - start timer
                lastWorldStopTime = Time.time;
                isWorldStable = true;
                hasShownPlayer = false; // Reset flag
            }
            else if (!currentlyStable && isWorldStable)
            {
                // Just started scrolling (not rotating) - hide player immediately
                isWorldStable = false;
                hasShownPlayer = false;
                HidePlayer();
            }
            else if (isWorldStable && !hasShownPlayer)
            {
                // Still stable and haven't shown player yet - check if enough time has passed
                float stableTime = Time.time - lastWorldStopTime;
                if (stableTime >= PLAYER_SHOW_DELAY)
                {
                    ShowPlayer();
                    hasShownPlayer = true; // Mark as shown
                }
            }
        }

        private void ShowPlayer()
        {
            if (PlayModeManager.Instance != null)
            {
                // Re-spawn player at current world's spawn point
                WorldInstance currentWorld = loadedWorlds[1];
                if (currentWorld != null)
                {
                    Debug.Log($"<color=cyan>[BrowseController] ShowPlayer - currentWorld: {currentWorld.WorldId}</color>");
                    PlayModeManager.Instance.RespawnPlayerAtCurrentWorld(currentWorld);
                }
            }
        }

        private void HidePlayer()
        {
            if (PlayModeManager.Instance != null)
            {
                Debug.Log("<color=yellow>[BrowseController] HidePlayer</color>");
                PlayModeManager.Instance.ShowPlayerInBrowse(false);
            }
        }

        private void UpdateSnapAnimation()
        {
            if (isSnapping)
            {
                // Smoothly animate scrollOffset to target
                currentScrollOffset = Mathf.SmoothDamp(
                    currentScrollOffset,
                    snapToTargetOffset,
                    ref snapAnimationVelocity,
                    snapSpeed
                );

                // Stop snapping when close enough
                if (Mathf.Abs(currentScrollOffset - snapToTargetOffset) < 0.1f)
                {
                    currentScrollOffset = snapToTargetOffset;
                    isSnapping = false;
                    snapAnimationVelocity = 0f;
                }
            }
        }

        private void HandleInput()
        {
            // Mouse wheel zoom
            Vector2 scrollDelta = Input.mouseScrollDelta;
            if (Mathf.Abs(scrollDelta.y) > 0.01f)
            {
                float scroll = scrollDelta.y * 0.1f;
                HandleScrollWheelZoom(scroll);
                return;
            }

            // Check if mouse is over UI - if so, skip other input processing
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Handle pinch gesture (two finger touch)
            if (Input.touchCount == 2)
            {
                HandlePinchGesture();
                return; // Don't process other input when pinching
            }
            else if (isPinching)
            {
                // Pinch ended
                isPinching = false;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStartPos = touch.position;
                        lastTouchPos = touch.position;
                        isDragging = true;
                        break;

                    case TouchPhase.Moved:
                        if (isDragging)
                        {
                            Vector2 currentPos = touch.position;
                            Vector2 frameDelta = currentPos - lastTouchPos;
                            lastTouchPos = currentPos;
                            HandleDrag(frameDelta);
                        }
                        break;

                    case TouchPhase.Ended:
                        if (isDragging)
                        {
                            Vector2 totalDelta = touch.position - touchStartPos;
                            HandleSwipe(totalDelta);
                        }
                        isDragging = false;
                        isRotatingCamera = false;
                        break;
                }
            }
            // Mouse fallback for editor testing
            else if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
                lastTouchPos = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 frameDelta = currentPos - lastTouchPos;
                lastTouchPos = currentPos;
                HandleDrag(frameDelta);
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                Vector2 totalDelta = (Vector2)Input.mousePosition - touchStartPos;
                HandleSwipe(totalDelta);
                isDragging = false;
                isRotatingCamera = false;
            }
        }

        private void HandleScrollWheelZoom(float scrollDelta)
        {
            RecordInteraction();

            manualZoomOffset -= scrollDelta * scrollWheelZoomSpeed;
            manualZoomOffset = Mathf.Clamp(manualZoomOffset,
                minCameraDistance - cameraDistance,
                maxCameraDistance - cameraDistance);
        }

        private void HandlePinchGesture()
        {
            // Record interaction
            RecordInteraction();

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Calculate current distance between touches
            float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);

            if (!isPinching)
            {
                // Start pinching
                isPinching = true;
                lastPinchDistance = currentPinchDistance;
            }
            else
            {
                // Calculate pinch delta
                float pinchDelta = currentPinchDistance - lastPinchDistance;

                // Pinch in = zoom in (decrease distance), pinch out = zoom out (increase distance)
                manualZoomOffset -= pinchDelta * pinchZoomSpeed * 0.01f;
                manualZoomOffset = Mathf.Clamp(manualZoomOffset,
                    minCameraDistance - cameraDistance,
                    maxCameraDistance - cameraDistance);

                lastPinchDistance = currentPinchDistance;
            }
        }

        private void HandleDrag(Vector2 frameDelta)
        {
            // Record interaction
            RecordInteraction();

            Debug.Log($"[BrowseController] HandleDrag - frameDelta: {frameDelta}, abs X: {Mathf.Abs(frameDelta.x)}, abs Y: {Mathf.Abs(frameDelta.y)}");

            // Determine if horizontal or vertical drag dominates
            if (Mathf.Abs(frameDelta.x) > Mathf.Abs(frameDelta.y))
            {
                // Horizontal movement dominates - rotate camera around world
                isRotatingCamera = true;
                Debug.Log("[BrowseController] Horizontal drag - rotating camera");
                if (loadedWorlds[1] != null && browseCamera != null)
                {
                    float rotationDelta = -frameDelta.x * rotationSpeed; // Negative to reverse direction
                    currentOrbitAngle += rotationDelta;

                    // Immediately update camera for responsive rotation
                    UpdateCameraOrbitPosition();
                }
            }
            else
            {
                // Vertical movement dominates - track for world switching
                isRotatingCamera = false;
                float scrollDelta = -frameDelta.y * scrollSensitivity; // Use original sensitivity
                currentScrollOffset += scrollDelta;

                Debug.Log($"[BrowseController] Vertical drag - scrollDelta: {scrollDelta}, currentScrollOffset: {currentScrollOffset}");

                // Calculate velocity for later snap decision
                float deltaTime = Time.time - lastDragTime;
                if (deltaTime > 0)
                {
                    scrollVelocity = scrollDelta / deltaTime;
                }
                lastDragTime = Time.time;
            }
        }

        private void HandleSwipe(Vector2 delta)
        {
            // Simple swipe threshold for world switching
            float swipeThresholdValue = 30f; // Lower threshold for easier switching
            float absVelocity = Mathf.Abs(scrollVelocity);

            bool shouldSnapToNext = false;
            bool shouldSnapToPrevious = false;

            Debug.Log($"[BrowseController] HandleSwipe - scrollOffset: {currentScrollOffset}, velocity: {scrollVelocity}");

            // Check vertical swipe distance or velocity
            if (Mathf.Abs(currentScrollOffset) > swipeThresholdValue || absVelocity > velocityThreshold)
            {
                if (currentScrollOffset > 0 || scrollVelocity > 0)
                {
                    shouldSnapToNext = (currentIndex < worldFeed.Count - 1);
                    Debug.Log($"[BrowseController] Should snap to NEXT: {shouldSnapToNext}");
                }
                else
                {
                    shouldSnapToPrevious = (currentIndex > 0);
                    Debug.Log($"[BrowseController] Should snap to PREVIOUS: {shouldSnapToPrevious}");
                }
            }
            else
            {
                Debug.Log("[BrowseController] Swipe too small, snapping back to current world");
            }

            // Execute world switch or snap back
            if (shouldSnapToNext)
            {
                NavigateToNext();
            }
            else if (shouldSnapToPrevious)
            {
                NavigateToPrevious();
            }
            else
            {
                // Snap back to current world (scrollOffset = 0)
                isSnapping = true;
                snapToTargetOffset = 0f;
            }

            // Reset velocity
            scrollVelocity = 0f;
        }

        private void RecordInteraction()
        {
            lastInteractionTime = Time.time;
            isAutoRotating = false;
        }

        private void UpdateAutoRotation()
        {
            if (!enableAutoRotation) return;

            // Check if enough time has passed since last interaction
            float timeSinceInteraction = Time.time - lastInteractionTime;

            if (timeSinceInteraction >= autoRotationDelay && !isSnapping)
            {
                isAutoRotating = true;
            }

            // Apply auto rotation
            if (isAutoRotating)
            {
                currentOrbitAngle += autoRotationSpeed * Time.deltaTime;
                // Keep angle in 0-360 range
                if (currentOrbitAngle >= 360f)
                {
                    currentOrbitAngle -= 360f;
                }
            }
        }

        private void NavigateToNext()
        {
            if (currentIndex >= worldFeed.Count - 1)
                return;

            Debug.Log($"<color=cyan>[BrowseController] Switching to next world (index {currentIndex} -> {currentIndex + 1})</color>");

            // Pause any active mini game before switching
            if (BrowseMiniGameTrigger.Instance != null && BrowseMiniGameTrigger.Instance.IsPlaying)
            {
                BrowseMiniGameTrigger.Instance.PauseGame();
                Debug.Log("[BrowseController] Paused mini game before switching to next world");
            }

            // Adjust scrollOffset for smooth transition
            // Current offset is relative to old currentIndex
            // After index++, we need to snap to 0 (which means old position + worldSpacing)
            currentScrollOffset -= worldSpacing;

            currentIndex++;

            // Recycle: previous becomes next
            WorldInstance recycled = loadedWorlds[0];
            loadedWorlds[0] = loadedWorlds[1];
            loadedWorlds[1] = loadedWorlds[2];
            loadedWorlds[2] = recycled;

            loadedIndices[0] = loadedIndices[1];
            loadedIndices[1] = loadedIndices[2];

            // Load new next world
            int newNextIndex = currentIndex + 1;
            if (recycled != null)
            {
                Destroy(recycled.gameObject);
            }
            LoadWorldAtSlot(2, newNextIndex);

            // Update world positions
            UpdateWorldPositions();

            // Calculate optimal camera distance for new world
            CalculateOptimalCameraDistance();

            // Snap to new world center (offset = 0)
            isSnapping = true;
            snapToTargetOffset = 0f;

            // Reset orbit
            currentOrbitAngle = 0f;

            // Start auto rotation immediately after switching
            isAutoRotating = true;

            // Update mini game button visibility
            UpdateMiniGameButtonVisibility();

            Debug.Log($"<color=green>[BrowseController] ✓ Switched to world index {currentIndex}</color>");
        }

        private void NavigateToPrevious()
        {
            if (currentIndex <= 0)
                return;

            Debug.Log($"<color=cyan>[BrowseController] Switching to previous world (index {currentIndex} -> {currentIndex - 1})</color>");

            // Pause any active mini game before switching
            if (BrowseMiniGameTrigger.Instance != null && BrowseMiniGameTrigger.Instance.IsPlaying)
            {
                BrowseMiniGameTrigger.Instance.PauseGame();
                Debug.Log("[BrowseController] Paused mini game before switching to previous world");
            }

            // Adjust scrollOffset for smooth transition
            currentScrollOffset += worldSpacing;

            currentIndex--;

            // Recycle: next becomes previous
            WorldInstance recycled = loadedWorlds[2];
            loadedWorlds[2] = loadedWorlds[1];
            loadedWorlds[1] = loadedWorlds[0];
            loadedWorlds[0] = recycled;

            loadedIndices[2] = loadedIndices[1];
            loadedIndices[1] = loadedIndices[0];

            // Load new previous world
            int newPrevIndex = currentIndex - 1;
            if (recycled != null)
            {
                Destroy(recycled.gameObject);
            }
            LoadWorldAtSlot(0, newPrevIndex);

            // Update world positions
            UpdateWorldPositions();

            // Calculate optimal camera distance for new world
            CalculateOptimalCameraDistance();

            // Snap to new world center (offset = 0)
            isSnapping = true;
            snapToTargetOffset = 0f;

            // Reset orbit
            currentOrbitAngle = 0f;

            // Start auto rotation immediately after switching
            isAutoRotating = true;

            // Update mini game button visibility
            UpdateMiniGameButtonVisibility();

            Debug.Log($"<color=green>[BrowseController] ✓ Switched to world index {currentIndex}</color>");
        }

        private void UpdateActivationLevels()
        {
            // Calculate scroll progress as percentage of worldSpacing
            float scrollProgress = currentScrollOffset / worldSpacing;
            float visibilityThreshold = 0.3f; // 30% threshold

            for (int i = 0; i < loadedWorlds.Length; i++)
            {
                if (loadedWorlds[i] != null)
                {
                    if (i == 1)
                    {
                        // Current world is always fully active
                        loadedWorlds[i].SetActivationLevel(ActivationLevel.FullyActive);
                    }
                    else if (i == 0)
                    {
                        // Previous world (上面的): show only if scrolling up >= 30%
                        // scrollProgress < 0 means scrolling up (toward previous)
                        if (scrollProgress < -visibilityThreshold)
                        {
                            loadedWorlds[i].SetActivationLevel(ActivationLevel.Preloaded);
                        }
                        else
                        {
                            loadedWorlds[i].SetActivationLevel(ActivationLevel.Inactive);
                        }
                    }
                    else if (i == 2)
                    {
                        // Next world (下面的): show only if scrolling down >= 30%
                        // scrollProgress > 0 means scrolling down (toward next)
                        if (scrollProgress > visibilityThreshold)
                        {
                            loadedWorlds[i].SetActivationLevel(ActivationLevel.Preloaded);
                        }
                        else
                        {
                            loadedWorlds[i].SetActivationLevel(ActivationLevel.Inactive);
                        }
                    }
                }
            }
        }


        private void CalculateOptimalCameraDistance()
        {
            if (loadedWorlds[1] == null) return;

            try
            {
                // Get world bounds
                Bounds worldBounds = loadedWorlds[1].GetWorldBounds();

                // For perspective camera, calculate distance based on world size and FOV
                float worldSizeXZ = Mathf.Max(worldBounds.size.x, worldBounds.size.z);

                // Base values are calibrated for 8x8x8 volume
                const float baseVolumeSize = 8f; // 8m x 8m x 8m reference
                const float baseMinDistance = 15f;
                const float baseMaxDistance = 50f;

                // Calculate scale factor relative to base volume
                float scaleFactor = worldSizeXZ / baseVolumeSize;

                // Scale min/max distances proportionally to world size
                float dynamicMinDistance = baseMinDistance * scaleFactor;
                float dynamicMaxDistance = baseMaxDistance * scaleFactor;

                // Calculate distance needed to fit world in view
                // Using half FOV and some margin
                float margin = 1.3f;
                float halfFOV = fieldOfView * 0.5f * Mathf.Deg2Rad;
                float distance = (worldSizeXZ * margin) / (2f * Mathf.Tan(halfFOV));

                targetCameraDistance = Mathf.Clamp(distance, dynamicMinDistance, dynamicMaxDistance);

                // Update min/max camera distance for this world (for zoom limits)
                minCameraDistance = dynamicMinDistance;
                maxCameraDistance = dynamicMaxDistance;

                Debug.Log($"[BrowseController] World size: {worldSizeXZ:F1}m, Scale: {scaleFactor:F2}x, Distance range: {minCameraDistance:F1}-{maxCameraDistance:F1}, Target: {targetCameraDistance:F1}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to calculate optimal camera distance: {e.Message}");
                targetCameraDistance = cameraDistance; // Fallback to default
            }
        }



        private void UpdateCameraPosition()
        {
            if (browseCamera == null)
                return;

            // Skip camera updates if PlayModeManager is transitioning camera
            if (PlayModeManager.Instance != null && PlayModeManager.Instance.IsCameraTransitioning)
                return;

            Vector3 lookAtPoint = Vector3.zero;

            float baseTargetDistance = targetCameraDistance + manualZoomOffset;
            baseTargetDistance = Mathf.Clamp(baseTargetDistance, minCameraDistance, maxCameraDistance);

            currentCameraDistance = Mathf.SmoothDamp(
                currentCameraDistance,
                baseTargetDistance,
                ref cameraDistanceVelocity,
                cameraZoomSpeed
            );

            float angleRad = cameraAngle * Mathf.Deg2Rad;
            float horizontalDist = currentCameraDistance * Mathf.Cos(angleRad);
            float verticalDist = currentCameraDistance * Mathf.Sin(angleRad);

            float orbitRad = currentOrbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(orbitRad) * horizontalDist,
                verticalDist,
                -Mathf.Cos(orbitRad) * horizontalDist
            );

            browseCamera.transform.position = lookAtPoint + offset;
            browseCamera.transform.LookAt(lookAtPoint);
        }

        private void UpdateCameraOrbitPosition()
        {
            // Immediate camera update for responsive rotation during drag
            if (browseCamera == null)
                return;

            // All worlds at origin
            Vector3 lookAtPoint = Vector3.zero;

            float angleRad = cameraAngle * Mathf.Deg2Rad;
            float horizontalDist = currentCameraDistance * Mathf.Cos(angleRad);
            float verticalDist = currentCameraDistance * Mathf.Sin(angleRad);

            float orbitRad = currentOrbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(orbitRad) * horizontalDist,
                verticalDist,
                -Mathf.Cos(orbitRad) * horizontalDist
            );

            browseCamera.transform.position = lookAtPoint + offset;
            browseCamera.transform.LookAt(lookAtPoint);
        }

        /// <summary>
        /// Syncs internal camera distance from actual camera position
        /// Called when returning from Play Mode to ensure zoom continues to work
        /// </summary>
        private void SyncCameraDistanceFromPosition()
        {
            if (browseCamera == null)
                return;

            // Calculate actual distance from camera to origin (world center)
            Vector3 lookAtPoint = Vector3.zero;
            float actualDistance = Vector3.Distance(browseCamera.transform.position, lookAtPoint);

            // Update both current and target distance
            currentCameraDistance = actualDistance;
            targetCameraDistance = actualDistance;

            // Reset manual zoom offset to 0
            manualZoomOffset = 0f;

            Debug.Log($"[BrowseController] Synced camera distance from position: {actualDistance}");
        }

        private void ClearAllWorlds()
        {
            for (int i = 0; i < loadedWorlds.Length; i++)
            {
                if (loadedWorlds[i] != null)
                {
                    Destroy(loadedWorlds[i].gameObject);
                    loadedWorlds[i] = null;
                }
                loadedIndices[i] = -1;
            }
        }

        public WorldInstance GetCurrentWorld()
        {
            return loadedWorlds[1];
        }

        public string GetCurrentWorldId()
        {
            return loadedWorlds[1]?.WorldId;
        }

        /// <summary>
        /// Navigate to a specific world by ID (for returning from Play Mode)
        /// </summary>
        public void NavigateToWorldId(string worldId)
        {
            if (worldFeed == null || string.IsNullOrEmpty(worldId))
                return;

            // Find the index of this world in the feed
            int targetIndex = worldFeed.FindIndex(w => w.id == worldId);
            if (targetIndex >= 0 && targetIndex != currentIndex)
            {
                Debug.Log($"[BrowseController] Navigating to world {worldId} at index {targetIndex}");
                currentIndex = targetIndex;
                LoadWorldsAroundIndex(currentIndex);
                UpdateWorldPositions();
                CalculateOptimalCameraDistance();
                UpdateMiniGameButtonVisibility();
            }
        }

        // Called by UI buttons
        public void OnGoButtonPressed()
        {
            string worldId = GetCurrentWorldId();
            if (!string.IsNullOrEmpty(worldId))
            {
                appRoot.EnterPlayMode(worldId);
            }
        }

        public void OnCollectButtonPressed()
        {
            string worldId = GetCurrentWorldId();
            if (!string.IsNullOrEmpty(worldId))
            {
                appRoot.SaveService.CollectWorld(worldId);
                Debug.Log($"Collected world: {worldId}");
            }
        }

        public void OnLikeButtonPressed()
        {
            string worldId = GetCurrentWorldId();
            if (!string.IsNullOrEmpty(worldId))
            {
                appRoot.SaveService.ToggleLike(worldId);
                Debug.Log($"Toggled like for world: {worldId}");
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        /// <summary>
        /// Disables volume grid visuals (wireframe) in Browse mode
        /// </summary>
        private void DisableVolumeVisuals(WorldInstance worldInstance)
        {
            if (worldInstance == null) return;

            // Find all VolumeGrid components and disable their visuals
            VolumeGrid[] volumeGrids = worldInstance.GetComponentsInChildren<VolumeGrid>();
            foreach (var grid in volumeGrids)
            {
                grid.showBounds = false;

                // Also directly disable the boundsVisual GameObject if it exists
                Transform boundsVisual = grid.transform.Find("VolumeBounds");
                if (boundsVisual != null)
                {
                    boundsVisual.gameObject.SetActive(false);
                }
            }

            Debug.Log($"[BrowseController] Disabled volume visuals for world {worldInstance.WorldId} ({volumeGrids.Length} grids)");
        }

        /// <summary>
        /// Ensures mini game exists in the given world instance (creates if missing)
        /// </summary>
        private void EnsureMiniGameExists(WorldInstance worldInstance)
        {
            if (worldInstance == null) return;

            string worldId = worldInstance.WorldId;
            WorldData worldData = appRoot.WorldLibrary.GetWorldData(worldId);

            if (worldData == null || worldData.miniGames == null || worldData.miniGames.Count == 0)
            {
                // No mini game in this world
                return;
            }

            // Check if mini game preview already exists
            var existingPreview = worldInstance.GetComponentInChildren<HorizonMini.MiniGames.MiniGamePreview>();

            if (existingPreview == null)
            {
                // Mini game not loaded yet - create it now!
                MiniGameData gameData = worldData.miniGames[0];
                Debug.Log($"<color=yellow>[BrowseController] EnsureMiniGameExists - Creating mini game for world {worldId}</color>");
                appRoot.WorldLibrary.CreateMiniGameInWorld(worldInstance, gameData);
            }
            else
            {
                Debug.Log($"<color=green>[BrowseController] EnsureMiniGameExists - Mini game already exists in world {worldId}</color>");
                Debug.Log($"[BrowseController] Preview GameObject: {existingPreview.gameObject.name}");
                Debug.Log($"[BrowseController] Preview GameObject path: {GetGameObjectPath(existingPreview.gameObject)}");

                // Try CubeStack GameController
                GameController gc = existingPreview.GetComponent<GameController>();
                if (gc == null)
                {
                    Debug.Log("[BrowseController] GameController not on preview root, searching children...");
                    gc = existingPreview.GetComponentInChildren<GameController>();
                }

                if (gc != null)
                {
                    Debug.Log($"[BrowseController] Found GameController (CubeStack) on: {gc.gameObject.name}");
                    Debug.Log($"[BrowseController] GameController state: {gc.State}");

                    // Always start the game after a delay to ensure Start() has executed
                    Debug.Log($"<color=yellow>[BrowseController] Scheduling auto-start after initialization...</color>");
                    StartCoroutine(AutoStartGameAfterInit(gc, existingPreview.gameObject));
                    return;
                }

                // Try KotobaMatch KotobaMatchController
                KotobaMatchController kotobaController = existingPreview.GetComponent<KotobaMatchController>();
                if (kotobaController == null)
                {
                    Debug.Log("[BrowseController] KotobaMatchController not on preview root, searching children...");
                    kotobaController = existingPreview.GetComponentInChildren<KotobaMatchController>();
                }

                if (kotobaController != null)
                {
                    Debug.Log($"[BrowseController] Found KotobaMatchController on: {kotobaController.gameObject.name}");
                    Debug.Log($"[BrowseController] KotobaMatchController state: {kotobaController.State}");

                    // Always start the game after a delay to ensure Start() has executed
                    Debug.Log($"<color=yellow>[BrowseController] Scheduling auto-start after initialization (KotobaMatch)...</color>");
                    StartCoroutine(AutoStartKotobaGameAfterInit(kotobaController, existingPreview.gameObject));
                    return;
                }

                Debug.LogError("[BrowseController] Neither GameController nor KotobaMatchController found in mini game preview!");
            }
        }

        private System.Collections.IEnumerator LoadMiniGameAfterDelay(WorldInstance worldInstance, float delay)
        {
            Debug.Log($"<color=cyan>[BrowseController] Waiting {delay}s before loading mini game...</color>");
            yield return new WaitForSeconds(delay);

            Debug.Log($"<color=cyan>[BrowseController] Delay finished, loading mini game now</color>");
            EnsureMiniGameExists(worldInstance);
        }

        private System.Collections.IEnumerator AutoStartGameAfterInit(GameController gc, GameObject gameInstance)
        {
            // Wait multiple frames to ensure Start() and all initialization is complete
            Debug.Log($"<color=cyan>[BrowseController] Waiting for GameController to initialize...</color>");

            // Wait 10 frames to be safe
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            // Also wait for end of frame to ensure everything is settled
            yield return new WaitForEndOfFrame();

            Debug.Log($"<color=cyan>[BrowseController] After waiting, GameController state: {gc.State}</color>");

            if (gc.State == GameState.Menu)
            {
                Debug.Log($"<color=yellow>[BrowseController] Starting game now...</color>");
                gc.OnPlayerInput(); // This will call StartGame()
            }
            else
            {
                Debug.Log($"<color=cyan>[BrowseController] Game already in state: {gc.State}, not auto-starting</color>");
            }

            // Disable input so it's preview-only
            yield return null;
            var inputHandler = gameInstance.GetComponentInChildren<InputHandler>();
            if (inputHandler != null)
            {
                inputHandler.enabled = false;
                Debug.Log($"[BrowseController] Disabled InputHandler - game is now in preview mode");
            }
        }

        private System.Collections.IEnumerator AutoStartKotobaGameAfterInit(KotobaMatchController kotobaController, GameObject gameInstance)
        {
            // Wait multiple frames to ensure Start() and all initialization is complete
            Debug.Log($"<color=cyan>[BrowseController] Waiting for KotobaMatchController to initialize...</color>");

            // Wait 10 frames to be safe
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            // Also wait for end of frame to ensure everything is settled
            yield return new WaitForEndOfFrame();

            Debug.Log($"<color=cyan>[BrowseController] After waiting, KotobaMatchController state: {kotobaController.State}</color>");

            // Always reset to menu first to ensure clean state
            kotobaController.ResetToMenu();
            Debug.Log($"[BrowseController] Reset KotobaMatch to menu");

            // Wait a frame for reset to complete
            yield return null;

            if (kotobaController.State == KotobaGameState.Menu)
            {
                Debug.Log($"<color=yellow>[BrowseController] Starting KotobaMatch game now...</color>");
                kotobaController.OnPlayerTap(); // This will call StartGame() and make cards drop
            }

            // Wait for cards to drop (about 2 seconds)
            yield return new WaitForSeconds(2.5f);

            // Check if still in browse mode (user might have clicked "Go" while waiting)
            HorizonMini.MiniGames.MiniGamePreview preview = gameInstance.GetComponent<HorizonMini.MiniGames.MiniGamePreview>();
            bool isActiveInPlayMode = (preview != null && preview.isActive);

            if (!isActiveInPlayMode)
            {
                // Still in browse/preview mode - pause the game
                kotobaController.PauseForPreview();
                Debug.Log($"[BrowseController] Paused KotobaMatch for preview mode");

                // Disable input so it's preview-only
                var kotobaInputHandler = gameInstance.GetComponentInChildren<KotobaInputHandler>();
                if (kotobaInputHandler != null)
                {
                    kotobaInputHandler.enabled = false;
                    Debug.Log($"[BrowseController] Disabled KotobaInputHandler - game is now in preview mode");
                }
            }
            else
            {
                Debug.Log($"[BrowseController] Game already active in Play Mode - skipping preview pause");
            }
        }

        private System.Collections.IEnumerator DisableInputAfterFrame(GameObject gameInstance)
        {
            yield return null;

            var inputHandler = gameInstance.GetComponentInChildren<InputHandler>();
            if (inputHandler != null)
            {
                inputHandler.enabled = false;
                Debug.Log($"[BrowseController] Disabled InputHandler after auto-start");
            }
        }

        /// <summary>
        /// Updates mini game button visibility based on current world's MiniGameData
        /// </summary>
        private void UpdateMiniGameButtonVisibility()
        {
            Debug.Log("<color=yellow>[BrowseController] UpdateMiniGameButtonVisibility called</color>");

            // Get current world
            WorldInstance currentWorld = loadedWorlds[1];
            if (currentWorld == null)
            {
                Debug.LogWarning("[BrowseController] Current world is null");
                return;
            }

            // Get world data
            string worldId = currentWorld.WorldId;
            WorldData worldData = appRoot.WorldLibrary.GetWorldData(worldId);

            if (worldData == null)
            {
                Debug.LogWarning($"[BrowseController] WorldData is null for world {worldId}");
                return;
            }

            Debug.Log($"[BrowseController] World browsed: {worldId}, miniGames: {(worldData.miniGames != null ? worldData.miniGames.Count : 0)}");

            // Notify PlayModeManager that a world is being browsed
            if (PlayModeManager.Instance != null)
            {
                PlayModeManager.Instance.OnWorldBrowsed(currentWorld, worldData);
            }

            // Handle mini game loading if needed
            if (worldData.miniGames != null && worldData.miniGames.Count > 0)
            {
                // Load mini game with delay (0.5s after world becomes center)
                StartCoroutine(LoadMiniGameAfterDelay(currentWorld, 0.5f));

                // If game is currently paused, resume it
                if (BrowseMiniGameTrigger.Instance != null &&
                    BrowseMiniGameTrigger.Instance.IsPlaying &&
                    BrowseMiniGameTrigger.Instance.IsPaused)
                {
                    BrowseMiniGameTrigger.Instance.ResumeGame();
                    Debug.Log($"<color=green>[BrowseController] ✓ Resumed paused mini game</color>");
                }
            }
        }
    }
}
