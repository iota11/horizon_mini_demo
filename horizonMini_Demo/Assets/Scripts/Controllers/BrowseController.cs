using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Core;
using HorizonMini.Data;
using HorizonMini.UI;

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
        [SerializeField] private float scrollSensitivity = 0.02f; // How much camera moves per pixel drag
        [SerializeField] private float snapThreshold = 0.3f; // 30% of worldSpacing to trigger snap to next
        [SerializeField] private float snapSpeed = 0.15f; // SmoothDamp time for snap animation
        [SerializeField] private float velocityThreshold = 5f; // Minimum velocity to trigger snap

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

        private bool isActive = false;

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
            // If already loaded, skip
            if (loadedIndices[slot] == worldIndex && loadedWorlds[slot] != null)
                return;

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

                    // Set activation level
                    if (slot == 1)
                    {
                        instance.SetActivationLevel(ActivationLevel.FullyActive);
                    }
                    else
                    {
                        instance.SetActivationLevel(ActivationLevel.Preloaded);
                    }
                }
            }
            else
            {
                loadedIndices[slot] = -1;
            }
        }

        private void UpdateWorldPositions()
        {
            // Position worlds at fixed Y coordinates based on their slot index
            // World positions are based on currentIndex, not slot position
            for (int i = 0; i < loadedWorlds.Length; i++)
            {
                if (loadedWorlds[i] != null && loadedIndices[i] >= 0)
                {
                    // Calculate Y position based on world index in feed
                    float yPos = loadedIndices[i] * worldSpacing;

                    // Get the volume grid center to align all worlds by their centers
                    Vector3 volumeCenter;
                    try
                    {
                        volumeCenter = loadedWorlds[i].GetVolumeGridCenter();
                    }
                    catch
                    {
                        volumeCenter = loadedWorlds[i].transform.position;
                    }

                    // Calculate offset: move world so its volume center is at (0, yPos, 0)
                    Vector3 currentWorldPos = loadedWorlds[i].transform.position;
                    Vector3 offsetFromPivotToCenter = volumeCenter - currentWorldPos;

                    // Position world so volume center ends up at target position
                    Vector3 targetCenterPos = new Vector3(0, yPos, 0);
                    loadedWorlds[i].transform.position = targetCenterPos - offsetFromPivotToCenter;
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
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            // Check if mouse is over UI - if so, skip input processing
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // Mouse is over UI, but still allow scroll wheel zoom
                try
                {
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if (Mathf.Abs(scroll) > 0.01f)
                    {
                        HandleScrollWheelZoom(scroll);
                    }
                }
                catch (System.ArgumentException) { }
                return;
            }

            // Mouse wheel zoom
            try
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    HandleScrollWheelZoom(scroll);
                    return; // Don't process other input when scrolling
                }
            }
            catch (System.ArgumentException)
            {
                // Mouse ScrollWheel axis not configured in Input Manager
                // This is expected on some platforms
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
            }
        }

        private void HandleScrollWheelZoom(float scrollDelta)
        {
            // Record interaction
            RecordInteraction();

            // Scroll up = zoom in (decrease distance), scroll down = zoom out (increase distance)
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

            // Determine if horizontal or vertical drag dominates
            if (Mathf.Abs(frameDelta.x) > Mathf.Abs(frameDelta.y))
            {
                // Horizontal movement dominates - rotate camera around world
                isSnapping = false; // Cancel any ongoing snap animation

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
                // Vertical movement dominates - scroll through worlds (feed-style)
                isSnapping = false; // Cancel any ongoing snap animation

                // Update scroll offset based on drag (negative because drag down = see previous world)
                float scrollDelta = -frameDelta.y * scrollSensitivity;
                currentScrollOffset += scrollDelta;

                // Clamp to prevent scrolling beyond bounds
                float maxScroll = (currentIndex < worldFeed.Count - 1) ? worldSpacing : 0;
                float minScroll = (currentIndex > 0) ? -worldSpacing : 0;
                currentScrollOffset = Mathf.Clamp(currentScrollOffset, minScroll, maxScroll);

                // Calculate velocity for later snap decision
                float deltaTime = Time.time - lastDragTime;
                if (deltaTime > 0)
                {
                    scrollVelocity = scrollDelta / deltaTime;
                }
                lastDragTime = Time.time;

                // Camera will update in UpdateCameraPosition with scroll offset
            }
        }

        private void HandleSwipe(Vector2 delta)
        {
            // New feed-style snap logic
            // Decide whether to snap to next/previous world or return to current

            // Check if scrolled enough distance or has enough velocity
            float scrollDistance = Mathf.Abs(currentScrollOffset);
            float absVelocity = Mathf.Abs(scrollVelocity);

            bool shouldSnapToNext = false;
            bool shouldSnapToPrevious = false;

            // Snap based on distance threshold
            if (scrollDistance > worldSpacing * snapThreshold)
            {
                if (currentScrollOffset > 0)
                {
                    shouldSnapToNext = (currentIndex < worldFeed.Count - 1);
                }
                else
                {
                    shouldSnapToPrevious = (currentIndex > 0);
                }
            }
            // Or snap based on velocity (flick gesture)
            else if (absVelocity > velocityThreshold)
            {
                if (scrollVelocity > 0)
                {
                    shouldSnapToNext = (currentIndex < worldFeed.Count - 1);
                }
                else
                {
                    shouldSnapToPrevious = (currentIndex > 0);
                }
            }

            // Execute snap
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
                // Snap back to current world
                isSnapping = true;
                snapToTargetOffset = 0f; // Snap back to zero offset
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

            // Before changing index, compensate scrollOffset
            // Camera is currently at: oldIndex * spacing + currentScrollOffset
            // After increment: newIndex * spacing + newScrollOffset
            // We want camera to stay in same Y position, so:
            // oldIndex * spacing + currentScrollOffset = newIndex * spacing + newScrollOffset
            // newScrollOffset = currentScrollOffset - worldSpacing
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

            // Update world positions (worlds stay at their fixed positions)
            UpdateWorldPositions();
            UpdateActivationLevels();

            // Calculate optimal orthographic size for new world
            CalculateOptimalCameraDistance();

            // Reset orbit and snap camera to new world center (scrollOffset = 0)
            currentOrbitAngle = 0f;
            isSnapping = true;
            snapToTargetOffset = 0f;

            // Start auto rotation immediately after switching
            isAutoRotating = true;
        }

        private void NavigateToPrevious()
        {
            if (currentIndex <= 0)
                return;

            // Before changing index, compensate scrollOffset
            // Camera is currently at: oldIndex * spacing + currentScrollOffset
            // After decrement: newIndex * spacing + newScrollOffset
            // We want camera to stay in same Y position, so:
            // oldIndex * spacing + currentScrollOffset = newIndex * spacing + newScrollOffset
            // newScrollOffset = currentScrollOffset + worldSpacing
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

            // Update world positions (worlds stay at their fixed positions)
            UpdateWorldPositions();
            UpdateActivationLevels();

            // Calculate optimal orthographic size for new world
            CalculateOptimalCameraDistance();

            // Reset orbit and snap camera to new world center (scrollOffset = 0)
            currentOrbitAngle = 0f;
            isSnapping = true;
            snapToTargetOffset = 0f;

            // Start auto rotation immediately after switching
            isAutoRotating = true;
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

                // Calculate distance needed to fit world in view
                // Using half FOV and some margin
                float margin = 1.3f;
                float halfFOV = fieldOfView * 0.5f * Mathf.Deg2Rad;
                float distance = (worldSizeXZ * margin) / (2f * Mathf.Tan(halfFOV));

                targetCameraDistance = Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
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

            // Handle snap animation
            if (isSnapping)
            {
                currentScrollOffset = Mathf.SmoothDamp(
                    currentScrollOffset,
                    snapToTargetOffset,
                    ref snapAnimationVelocity,
                    snapSpeed
                );

                // Stop snapping when close enough to target
                if (Mathf.Abs(currentScrollOffset - snapToTargetOffset) < 0.01f)
                {
                    currentScrollOffset = snapToTargetOffset;
                    isSnapping = false;
                    snapAnimationVelocity = 0f;
                }
            }

            // Calculate camera target Y position (in world space)
            // currentIndex * worldSpacing gives the Y position of current world
            float targetWorldY = currentIndex * worldSpacing;
            float cameraTargetY = targetWorldY + currentScrollOffset;

            // Get current world's XZ center (if available)
            Vector3 lookAtPoint = new Vector3(0, cameraTargetY, 0);
            if (loadedWorlds[1] != null)
            {
                try
                {
                    Vector3 gridCenter = loadedWorlds[1].GetVolumeGridCenter();
                    lookAtPoint.x = gridCenter.x;
                    lookAtPoint.z = gridCenter.z;
                }
                catch
                {
                    // Use default (0, 0)
                }
            }

            // Smooth camera distance to target (for zoom effect)
            float baseTargetDistance = targetCameraDistance + manualZoomOffset;
            baseTargetDistance = Mathf.Clamp(baseTargetDistance, minCameraDistance, maxCameraDistance);

            currentCameraDistance = Mathf.SmoothDamp(
                currentCameraDistance,
                baseTargetDistance,
                ref cameraDistanceVelocity,
                cameraZoomSpeed
            );

            // Calculate camera position based on orbit angle and current distance
            float angleRad = cameraAngle * Mathf.Deg2Rad;
            float horizontalDist = currentCameraDistance * Mathf.Cos(angleRad);
            float verticalDist = currentCameraDistance * Mathf.Sin(angleRad);

            // Calculate position on orbit circle
            float orbitRad = currentOrbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(orbitRad) * horizontalDist,
                verticalDist,
                -Mathf.Cos(orbitRad) * horizontalDist
            );

            Vector3 targetPos = lookAtPoint + offset;

            // Direct update - SmoothDamp already handles smoothing via currentScrollOffset
            browseCamera.transform.position = targetPos;
            browseCamera.transform.LookAt(lookAtPoint);

            // Update world activation levels based on scroll progress
            UpdateActivationLevels();
        }

        private void UpdateCameraOrbitPosition()
        {
            // Immediate camera update for responsive rotation during drag
            if (browseCamera == null)
                return;

            // Calculate camera target Y position
            float targetWorldY = currentIndex * worldSpacing;
            float cameraTargetY = targetWorldY + currentScrollOffset;

            // Get current world's XZ center
            Vector3 lookAtPoint = new Vector3(0, cameraTargetY, 0);
            if (loadedWorlds[1] != null)
            {
                try
                {
                    Vector3 gridCenter = loadedWorlds[1].GetVolumeGridCenter();
                    lookAtPoint.x = gridCenter.x;
                    lookAtPoint.z = gridCenter.z;
                }
                catch
                {
                    // Use default (0, 0)
                }
            }

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
    }
}
