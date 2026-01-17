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
        [SerializeField] private float rotationSpeed = 100f;

        [Header("Camera")]
        [SerializeField] private Camera browseCamera;
        [SerializeField] private float cameraDistance = 30f;

        private AppRoot appRoot;
        private List<WorldMeta> worldFeed;
        private int currentIndex = 0;

        // Preload pool: previous, current, next
        private WorldInstance[] loadedWorlds = new WorldInstance[3];
        private int[] loadedIndices = new int[3] { -1, -1, -1 };

        // Gesture tracking
        private Vector2 touchStartPos;
        private bool isDragging = false;
        private float currentRotation = 0f;
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
            if (browseCamera != null && loadedWorlds[1] != null)
            {
                Vector3 worldCenter = loadedWorlds[1].GetWorldBounds().center;
                browseCamera.transform.position = worldCenter + Vector3.back * cameraDistance;
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
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchStartPos = touch.position;
                        isDragging = true;
                        break;

                    case TouchPhase.Moved:
                        if (isDragging)
                        {
                            Vector2 delta = touch.position - touchStartPos;
                            HandleDrag(delta);
                        }
                        break;

                    case TouchPhase.Ended:
                        if (isDragging)
                        {
                            Vector2 delta = touch.position - touchStartPos;
                            HandleSwipe(delta);
                        }
                        isDragging = false;
                        break;
                }
            }
            // Mouse fallback for editor testing
            else if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;
                HandleDrag(delta);
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;
                HandleSwipe(delta);
                isDragging = false;
            }
        }

        private void HandleDrag(Vector2 delta)
        {
            // Determine if vertical or horizontal swipe
            float angle = Mathf.Abs(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            if (angle > 45f && angle < 135f)
            {
                // Vertical drag - preview paging
                // (Can add visual feedback here)
            }
            else
            {
                // Horizontal drag - rotate current world
                if (loadedWorlds[1] != null)
                {
                    float rotationDelta = delta.x * Time.deltaTime * rotationSpeed * 0.1f;
                    currentRotation += rotationDelta;
                    loadedWorlds[1].transform.localRotation = Quaternion.Euler(0, currentRotation, 0);
                }
            }
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
            PositionCamera();
            currentRotation = 0f;
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
            PositionCamera();
            currentRotation = 0f;
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
            if (browseCamera != null)
            {
                // Smooth camera movement
                Vector3 targetPos = worldContainer.position + Vector3.up * targetYPosition + Vector3.back * cameraDistance;
                browseCamera.transform.position = Vector3.Lerp(
                    browseCamera.transform.position,
                    targetPos,
                    Time.deltaTime * transitionSpeed
                );

                // Look at current world
                if (loadedWorlds[1] != null)
                {
                    Vector3 lookTarget = loadedWorlds[1].GetWorldBounds().center;
                    browseCamera.transform.LookAt(lookTarget);
                }
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
