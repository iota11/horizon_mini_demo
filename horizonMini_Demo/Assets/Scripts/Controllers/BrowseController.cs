using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Core;
using HorizonMini.Data;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages the browsing feed with vertical paging and preloading
    /// </summary>
    public class BrowseController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform worldContainer;
        [SerializeField] private float worldSpacing = 20f;
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float transitionSpeed = 5f;
        [SerializeField] private float rotationSpeed = 1f; // Degrees per pixel

        [Header("Camera")]
        [SerializeField] private Camera browseCamera;
        [SerializeField] private float cameraDistance = 30f;
        [SerializeField] private float cameraAngle = 45f; // 45 degree top-down view

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
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (active)
            {
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
            for (int i = 0; i < loadedWorlds.Length; i++)
            {
                if (loadedWorlds[i] != null)
                {
                    float yPos = (i - 1) * worldSpacing; // -1, 0, +1
                    loadedWorlds[i].transform.localPosition = new Vector3(0, yPos, 0);
                }
            }

            targetYPosition = 0f;
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
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            // Mouse wheel scrolling - vertical = switch worlds
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (scroll > 0)
                {
                    NavigateToPrevious();
                }
                else
                {
                    NavigateToNext();
                }
                return; // Don't process other input when scrolling
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

        private void HandleDrag(Vector2 frameDelta)
        {
            // Horizontal drag - orbit camera around current world's volume grid center
            // frameDelta is per-frame movement, so we don't multiply by Time.deltaTime
            if (Mathf.Abs(frameDelta.x) > Mathf.Abs(frameDelta.y))
            {
                // Horizontal movement dominates - rotate camera around world
                if (loadedWorlds[1] != null && browseCamera != null)
                {
                    float rotationDelta = -frameDelta.x * rotationSpeed; // Negative to reverse direction
                    currentOrbitAngle += rotationDelta;

                    // Get volume grid center - this is the pivot point for camera orbit
                    Vector3 gridCenter;
                    try
                    {
                        gridCenter = loadedWorlds[1].GetVolumeGridCenter();
                    }
                    catch
                    {
                        return;
                    }

                    // Calculate new camera position orbiting around grid center
                    // Keep the same distance and vertical angle (45 degrees)
                    float angleRad = cameraAngle * Mathf.Deg2Rad;
                    float horizontalDist = cameraDistance * Mathf.Cos(angleRad);
                    float verticalDist = cameraDistance * Mathf.Sin(angleRad);

                    // Calculate position on orbit circle
                    float orbitRad = currentOrbitAngle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Sin(orbitRad) * horizontalDist,
                        verticalDist,
                        -Mathf.Cos(orbitRad) * horizontalDist
                    );

                    browseCamera.transform.position = gridCenter + offset;
                    browseCamera.transform.LookAt(gridCenter);
                }
            }
            // Vertical drag - just for visual feedback, actual navigation happens on swipe end
        }

        private void HandleSwipe(Vector2 delta)
        {
            if (delta.magnitude < swipeThreshold)
                return;

            float angle = Mathf.Abs(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            // Vertical swipe = page
            if (angle > 45f && angle < 135f)
            {
                if (delta.y > 0)
                {
                    // Swipe down = previous world
                    NavigateToPrevious();
                }
                else
                {
                    // Swipe up = next world
                    NavigateToNext();
                }
            }
        }

        private void NavigateToNext()
        {
            if (currentIndex >= worldFeed.Count - 1)
                return;

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

            UpdateWorldPositions();
            UpdateActivationLevels();

            // Reset orbit angle when switching worlds
            currentOrbitAngle = 0f;

            // Camera will smoothly move to new world in UpdateCameraPosition()
        }

        private void NavigateToPrevious()
        {
            if (currentIndex <= 0)
                return;

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

            UpdateWorldPositions();
            UpdateActivationLevels();

            // Reset orbit angle when switching worlds
            currentOrbitAngle = 0f;

            // Camera will smoothly move to new world in UpdateCameraPosition()
        }

        private void UpdateActivationLevels()
        {
            for (int i = 0; i < loadedWorlds.Length; i++)
            {
                if (loadedWorlds[i] != null)
                {
                    if (i == 1)
                    {
                        loadedWorlds[i].SetActivationLevel(ActivationLevel.FullyActive);
                    }
                    else
                    {
                        loadedWorlds[i].SetActivationLevel(ActivationLevel.Preloaded);
                    }
                }
            }
        }

        private void UpdateCameraPosition()
        {
            if (browseCamera != null && loadedWorlds[1] != null)
            {
                // Get current world's volume grid center
                Vector3 gridCenter;
                try
                {
                    gridCenter = loadedWorlds[1].GetVolumeGridCenter();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to get volume grid center: {e.Message}");
                    return;
                }

                // Calculate camera position based on orbit angle
                // Keep the same distance and vertical angle (45 degrees)
                float angleRad = cameraAngle * Mathf.Deg2Rad;
                float horizontalDist = cameraDistance * Mathf.Cos(angleRad);
                float verticalDist = cameraDistance * Mathf.Sin(angleRad);

                // Calculate position on orbit circle
                float orbitRad = currentOrbitAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Sin(orbitRad) * horizontalDist,
                    verticalDist,
                    -Mathf.Cos(orbitRad) * horizontalDist
                );

                Vector3 targetPos = gridCenter + offset;

                // Smooth camera movement when switching worlds
                browseCamera.transform.position = Vector3.Lerp(
                    browseCamera.transform.position,
                    targetPos,
                    Time.deltaTime * transitionSpeed
                );

                browseCamera.transform.LookAt(gridCenter);
            }
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
