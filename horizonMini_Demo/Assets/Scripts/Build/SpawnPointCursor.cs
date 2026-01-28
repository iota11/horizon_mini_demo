using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Cursor for controlling SpawnPoint position and rotation
    /// Allows XZ dragging and Y-axis rotation, optional delete button
    /// </summary>
    public class SpawnPointCursor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpawnPoint spawnPoint;
        [SerializeField] private Camera buildCamera;

        [Header("Visual Elements")]
        [SerializeField] public GameObject xzHandlePrefab; // XZ drag handle
        [SerializeField] public GameObject rotateHandlePrefab; // Rotation handle
        [SerializeField] public GameObject deleteButtonPrefab; // Delete button (hidden for initial spawn)
        [SerializeField] private LayerMask uiLayer;

        [Header("Settings")]
        [SerializeField] private float dragSensitivity = 1f;
        [SerializeField] private float snapInterval = 0.5f;
        [SerializeField] private float rotationSnapDegrees = 15f;

        private GameObject xzHandle;
        private GameObject rotateHandle;
        private GameObject deleteButton;

        private bool isDraggingXZ = false;
        private bool isDraggingRotate = false;
        private Vector3 dragStartWorldPos;
        private Vector3 dragStartPosition;
        private float dragStartRotation;
        private Plane dragPlaneXZ;
        private Ray currentRay;

        private bool isVisible = true;

        private void Awake()
        {
            // Auto-find SpawnPoint if not assigned
            if (spawnPoint == null)
            {
                spawnPoint = GetComponentInParent<SpawnPoint>();
                if (spawnPoint == null)
                {
                    Debug.LogError("SpawnPointCursor: Cannot find SpawnPoint component in parent!");
                }
            }
        }

        private void Start()
        {
            Debug.Log($"[SpawnPointCursor] Start - spawnPoint: {spawnPoint?.name}");

            // Auto-find camera if not assigned
            if (buildCamera == null)
            {
                buildCamera = Camera.main;
            }

            // Validate spawnPoint reference
            if (spawnPoint == null)
            {
                Debug.LogError($"[SpawnPointCursor] SpawnPoint reference is null! Cannot create cursor.");
                return;
            }

            // Create handle visuals
            CreateXZHandle();
            CreateRotateHandle();
            CreateDeleteButton();

            // Set UI layer for raycasting priority
            if (uiLayer == 0)
                uiLayer = LayerMask.GetMask("UI");

            // Register with SpawnPointManager
            var manager = SpawnPointManager.Instance;
            if (manager != null)
            {
                manager.RegisterCursor(this);
            }

            // Start hidden (View mode by default)
            SetVisible(false);
        }

        private void OnDestroy()
        {
            // Unregister from manager
            if (SpawnPointManager.Instance != null)
            {
                SpawnPointManager.Instance.UnregisterCursor(this);
            }
        }

        private void CreateXZHandle()
        {
            if (xzHandlePrefab != null)
            {
                xzHandle = Instantiate(xzHandlePrefab, transform);
                xzHandle.name = "XZHandle";
            }
            else
            {
                // Fallback: Create simple sphere
                xzHandle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                xzHandle.name = "XZHandle";
                xzHandle.transform.SetParent(transform);
                xzHandle.transform.localScale = Vector3.one * 0.4f;

                var renderer = xzHandle.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = new Color(0f, 0.5f, 1f, 0.8f); // Blue
                }
            }

            xzHandle.transform.localPosition = Vector3.zero;
            SetLayerRecursively(xzHandle, LayerMask.NameToLayer("UI"));
            ApplyAlwaysOnTopShader(xzHandle);
        }

        private void CreateRotateHandle()
        {
            if (rotateHandlePrefab != null)
            {
                rotateHandle = Instantiate(rotateHandlePrefab, transform);
                rotateHandle.name = "RotateHandle";
            }
            else
            {
                // Fallback: Create simple cube
                rotateHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rotateHandle.name = "RotateHandle";
                rotateHandle.transform.SetParent(transform);
                rotateHandle.transform.localScale = new Vector3(0.3f, 0.3f, 0.8f);

                var renderer = rotateHandle.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
                }
            }

            rotateHandle.transform.localPosition = new Vector3(0, 0, 1f); // In front
            SetLayerRecursively(rotateHandle, LayerMask.NameToLayer("UI"));
            ApplyAlwaysOnTopShader(rotateHandle);
        }

        private void CreateDeleteButton()
        {
            // Only create delete button if this is NOT an initial spawn point
            if (spawnPoint != null && spawnPoint.IsInitialSpawn)
            {
                Debug.Log("[SpawnPointCursor] Initial spawn point - no delete button");
                return; // Don't create delete button for initial spawn
            }

            if (deleteButtonPrefab != null)
            {
                deleteButton = Instantiate(deleteButtonPrefab, transform);
                deleteButton.name = "DeleteButton";
            }
            else
            {
                // Fallback: Create simple cube
                deleteButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deleteButton.name = "DeleteButton";
                deleteButton.transform.SetParent(transform);
                deleteButton.transform.localScale = Vector3.one * 0.3f;

                var renderer = deleteButton.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = new Color(1f, 0f, 0f, 0.9f); // Red
                }
            }

            deleteButton.transform.localPosition = new Vector3(-1f, 0, 0);
            SetLayerRecursively(deleteButton, LayerMask.NameToLayer("UI"));
            ApplyAlwaysOnTopShader(deleteButton);
        }

        private void ApplyAlwaysOnTopShader(GameObject obj)
        {
            Shader alwaysOnTopShader = Shader.Find("HorizonMini/AlwaysOnTop_URP");
            if (alwaysOnTopShader == null)
                alwaysOnTopShader = Shader.Find("Custom/AlwaysOnTop_URP");
            if (alwaysOnTopShader == null)
                alwaysOnTopShader = Shader.Find("Universal Render Pipeline/Lit");

            if (alwaysOnTopShader == null)
            {
                Debug.LogError("No suitable shader found for SpawnPointCursor!");
                return;
            }

            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    Color oldColor = renderer.material.color;
                    renderer.material.shader = alwaysOnTopShader;
                    renderer.material.color = oldColor;
                    renderer.material.renderQueue = 3000;
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void Update()
        {
            if (spawnPoint == null || buildCamera == null)
                return;

            // Update cursor position to match spawn point
            transform.position = spawnPoint.transform.position;

            // Billboard effect - always face camera
            Vector3 toCamera = buildCamera.transform.position - transform.position;
            toCamera.y = 0;
            if (toCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(toCamera);
            }

            if (isVisible)
            {
                HandleInput();
                UpdateVisuals();
            }
        }

        private void HandleInput()
        {
            bool isTouching = Input.GetMouseButton(0) || (Input.touchCount > 0);
            bool touchStarted = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool touchEnded = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

            Vector2 screenPos = Input.mousePosition;
            if (Input.touchCount > 0)
                screenPos = Input.GetTouch(0).position;

            currentRay = buildCamera.ScreenPointToRay(screenPos);

            // Start drag
            if (touchStarted && !isDraggingXZ && !isDraggingRotate)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    Transform hitTransform = hit.transform;

                    // Check for delete button
                    if (deleteButton != null && IsChildOf(hitTransform, deleteButton.transform))
                    {
                        OnDeleteButtonClicked();
                        return;
                    }

                    // Check for rotate handle
                    if (IsChildOf(hitTransform, rotateHandle.transform))
                    {
                        StartDragRotate(hit.point);
                        return;
                    }

                    // Check for XZ handle
                    if (IsChildOf(hitTransform, xzHandle.transform))
                    {
                        StartDragXZ(hit.point);
                        return;
                    }
                }
            }

            // Continue dragging
            if (isTouching)
            {
                if (isDraggingXZ)
                {
                    UpdateDragXZ();
                }
                else if (isDraggingRotate)
                {
                    UpdateDragRotate();
                }
            }

            // End drag
            if (touchEnded)
            {
                if (isDraggingXZ || isDraggingRotate)
                {
                    spawnPoint.UpdateSavedTransform();

                    // Also update PlacedObject transform for saving
                    PlacedObject placedObj = spawnPoint.GetComponent<PlacedObject>();
                    if (placedObj != null)
                    {
                        placedObj.UpdateSavedTransform();
                        Debug.Log($"[SpawnPointCursor] Updated PlacedObject transform: {spawnPoint.transform.position}");
                    }
                }
                isDraggingXZ = false;
                isDraggingRotate = false;
            }
        }

        private bool IsChildOf(Transform child, Transform parent)
        {
            if (child == parent)
                return true;
            if (child.parent == null)
                return false;
            return IsChildOf(child.parent, parent);
        }

        private void StartDragXZ(Vector3 hitPoint)
        {
            isDraggingXZ = true;
            dragStartWorldPos = hitPoint;
            dragStartPosition = spawnPoint.transform.position;

            // Create horizontal plane at spawn point level
            dragPlaneXZ = new Plane(Vector3.up, spawnPoint.transform.position);
        }

        private void UpdateDragXZ()
        {
            float enter;
            if (dragPlaneXZ.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                Vector3 newPosition = dragStartPosition + worldDelta * dragSensitivity;

                // Snap to grid
                newPosition.x = Mathf.Round(newPosition.x / snapInterval) * snapInterval;
                newPosition.z = Mathf.Round(newPosition.z / snapInterval) * snapInterval;

                spawnPoint.transform.position = newPosition;
            }
        }

        private void StartDragRotate(Vector3 hitPoint)
        {
            isDraggingRotate = true;
            dragStartWorldPos = hitPoint;
            dragStartRotation = spawnPoint.transform.eulerAngles.y;
        }

        private void UpdateDragRotate()
        {
            // Get current mouse position on XZ plane
            Plane xzPlane = new Plane(Vector3.up, spawnPoint.transform.position);
            float enter;
            if (xzPlane.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 startDir = (dragStartWorldPos - spawnPoint.transform.position).normalized;
                Vector3 currentDir = (currentWorldPos - spawnPoint.transform.position).normalized;

                // Calculate angle change
                float angle = Vector3.SignedAngle(startDir, currentDir, Vector3.up);
                float newRotation = dragStartRotation + angle;

                // Snap to rotation intervals
                newRotation = Mathf.Round(newRotation / rotationSnapDegrees) * rotationSnapDegrees;

                spawnPoint.transform.rotation = Quaternion.Euler(0, newRotation, 0);
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (spawnPoint == null)
                return;

            // Double check - should never delete initial spawn
            if (spawnPoint.IsInitialSpawn)
            {
                Debug.LogWarning("Cannot delete initial spawn point!");
                return;
            }

            Debug.Log($"Deleting spawn point: {spawnPoint.name}");
            Destroy(spawnPoint.gameObject);
        }

        private void UpdateVisuals()
        {
            // Position rotate handle in front based on spawn point's forward direction
            if (rotateHandle != null)
            {
                rotateHandle.transform.localPosition = new Vector3(0, 0, 1f);
            }

            // Position delete button to the left
            if (deleteButton != null)
            {
                deleteButton.transform.localPosition = new Vector3(-1f, 0, 0);
            }
        }

        /// <summary>
        /// Set spawn point this cursor controls
        /// </summary>
        public void SetSpawnPoint(SpawnPoint point)
        {
            spawnPoint = point;
        }

        /// <summary>
        /// Get spawn point this cursor controls
        /// </summary>
        public SpawnPoint GetSpawnPoint()
        {
            return spawnPoint;
        }

        /// <summary>
        /// Show or hide the cursor
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;

            if (xzHandle != null)
                xzHandle.SetActive(visible);
            if (rotateHandle != null)
                rotateHandle.SetActive(visible);
            if (deleteButton != null)
                deleteButton.SetActive(visible);

            if (!visible)
            {
                isDraggingXZ = false;
                isDraggingRotate = false;
            }
        }

        /// <summary>
        /// Check if cursor is currently visible
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }

        /// <summary>
        /// Check if cursor is currently being dragged
        /// </summary>
        public bool IsDragging()
        {
            return isDraggingXZ || isDraggingRotate;
        }
    }
}
