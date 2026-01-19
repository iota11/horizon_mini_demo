using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Core;
using HorizonMini.Data;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Manages the home view with two horizontal scrollable rows of 3D world previews
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Camera homeCamera;
        [SerializeField] private Transform worldContainer;
        [SerializeField] private float worldSpacing = 20f; // Spacing between worlds
        [SerializeField] private float worldScale = 1f;

        [Header("Camera Controls")]
        [SerializeField] private float panSpeed = 0.5f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 100f;
        [SerializeField] private float cameraHeight = 20f;

        private AppRoot appRoot;
        private List<WorldInstance> allWorldInstances = new List<WorldInstance>();

        // Camera control state
        private Vector3 cameraTargetPosition;
        private float currentZoom = 30f;
        private Vector2 touchStartPos;
        private bool isDragging = false;

        private bool isActive = false;

        public void Initialize(AppRoot root)
        {
            appRoot = root;

            if (homeCamera == null)
            {
                homeCamera = Camera.main;
            }

            if (worldContainer == null)
            {
                GameObject container = new GameObject("WorldContainer");
                worldContainer = container.transform;
                worldContainer.SetParent(transform);
                worldContainer.localPosition = Vector3.zero;
            }

            currentZoom = 30f;
            cameraTargetPosition = Vector3.zero;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (active)
            {
                RefreshRows();
                PositionCamera();
            }
            else
            {
                ClearAllWorlds();
            }

            if (homeCamera != null)
            {
                homeCamera.gameObject.SetActive(active);
            }
        }

        private void RefreshRows()
        {
            ClearAllWorlds();
            LoadAllWorlds();
        }

        private void LoadAllWorlds()
        {
            // Load all created worlds in a continuous line
            List<string> worldIds = appRoot.SaveService.GetCreatedWorldIds();

            float currentX = 0f;

            for (int i = 0; i < worldIds.Count; i++)
            {
                WorldInstance instance = appRoot.WorldLibrary.InstantiateWorld(worldIds[i], worldContainer);
                if (instance != null)
                {
                    // Position worlds in a continuous line
                    instance.transform.localPosition = new Vector3(currentX, 0, 0);
                    instance.transform.localScale = Vector3.one * worldScale;
                    instance.SetActivationLevel(ActivationLevel.FullyActive);
                    allWorldInstances.Add(instance);

                    // Calculate next position based on world bounds
                    Bounds worldBounds = instance.GetWorldBounds();
                    currentX += worldBounds.size.x + worldSpacing;

                    Debug.Log($"Loaded world '{instance.WorldData.worldTitle}' at x={instance.transform.localPosition.x}");
                }
            }

            // If no worlds, show a placeholder message
            if (allWorldInstances.Count == 0)
            {
                Debug.Log("No created worlds yet. Click Build to create your first world!");
            }
            else
            {
                Debug.Log($"Loaded {allWorldInstances.Count} worlds in continuous layout");
            }
        }

        private void ClearAllWorlds()
        {
            foreach (var instance in allWorldInstances)
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }
            allWorldInstances.Clear();
        }

        private void PositionCamera()
        {
            if (homeCamera != null)
            {
                Vector3 targetPos = cameraTargetPosition + new Vector3(0, cameraHeight, -currentZoom);
                homeCamera.transform.position = targetPos;
                homeCamera.transform.LookAt(cameraTargetPosition);
            }
        }

        private void Update()
        {
            if (!isActive)
                return;

            HandleInput();
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            if (homeCamera != null)
            {
                Vector3 targetPos = cameraTargetPosition + new Vector3(0, cameraHeight, -currentZoom);
                homeCamera.transform.position = Vector3.Lerp(homeCamera.transform.position, targetPos, Time.deltaTime * 5f);
                homeCamera.transform.LookAt(cameraTargetPosition);
            }
        }

        private void HandleInput()
        {
            // Mouse wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom -= scroll * zoomSpeed * 10f;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }

            // Touch/Mouse pan controls
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    touchStartPos = touch.position;
                    isDragging = true;
                }
                else if (touch.phase == TouchPhase.Moved && isDragging)
                {
                    Vector2 delta = touch.position - touchStartPos;
                    touchStartPos = touch.position;

                    // Pan camera
                    cameraTargetPosition -= new Vector3(delta.x * panSpeed * Time.deltaTime, 0, 0);
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    isDragging = false;

                    // Check if it was a tap (not a drag)
                    if ((touch.position - touchStartPos).magnitude < 50f)
                    {
                        HandleWorldSelection(touch.position);
                    }
                }
            }
            // Mouse controls
            else if (Input.GetMouseButtonDown(0))
            {
                touchStartPos = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 delta = currentPos - touchStartPos;
                touchStartPos = currentPos;

                // Pan camera
                cameraTargetPosition -= new Vector3(delta.x * panSpeed * Time.deltaTime, 0, 0);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Vector2 currentPos = Input.mousePosition;
                isDragging = false;

                // Check if it was a click (not a drag)
                if ((currentPos - touchStartPos).magnitude < 5f)
                {
                    HandleWorldSelection(currentPos);
                }
            }
        }


        private void HandleWorldSelection(Vector2 screenPos)
        {
            Ray ray = homeCamera.ScreenPointToRay(screenPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                WorldInstance instance = hit.collider.GetComponentInParent<WorldInstance>();
                if (instance != null)
                {
                    OnWorldSelected(instance);
                }
            }
        }

        private void OnWorldSelected(WorldInstance instance)
        {
            Debug.Log($"Selected world: {instance.WorldData.worldTitle}");

            // Enter Build mode to edit this world
            string worldId = instance.WorldId;
            if (!string.IsNullOrEmpty(worldId))
            {
                Debug.Log($"Opening world '{instance.WorldData.worldTitle}' (ID: {worldId}) for editing...");

                // Pass worldId to Build scene via SceneTransitionData
                HorizonMini.Core.SceneTransitionData.SetWorldToEdit(worldId);

                // Load Build scene
                UnityEngine.SceneManagement.SceneManager.LoadScene("Build");
            }
            else
            {
                Debug.LogError("WorldInstance has no WorldId!");
            }
        }
    }
}
