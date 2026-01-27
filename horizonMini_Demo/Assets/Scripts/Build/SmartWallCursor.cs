using UnityEngine;
using UnityEngine.EventSystems;

namespace HorizonMini.Build
{
    /// <summary>
    /// Cursor for controlling SmartWall control points
    /// Allows XZ dragging for control points, Y dragging for height, and add/delete buttons
    /// </summary>
    public class SmartWallCursor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SmartWall smartWall;
        [SerializeField] private Camera buildCamera;
        [SerializeField] private int controlPointIndex = -1;

        [Header("Visual Elements")]
        [SerializeField] public GameObject xzHandlePrefab; // Control point XZ drag handle (exposed for customization)
        [SerializeField] public GameObject yHandlePrefab;   // Height drag handle (exposed for customization)
        [SerializeField] public GameObject addButtonPrefab; // Add control point button (exposed for customization)
        [SerializeField] public GameObject deleteButtonPrefab; // Delete control point button (exposed for customization)
        [SerializeField] private LayerMask uiLayer;

        [Header("Settings")]
        [SerializeField] private float dragSensitivity = 1f;
        [SerializeField] private float snapInterval = 0.25f;

        private GameObject xzHandle;
        private GameObject yHandle;
        private GameObject addButton;
        private GameObject deleteButton;

        private bool isDraggingXZ = false;
        private bool isDraggingY = false;
        private Vector3 dragStartWorldPos;
        private Vector3 dragStartCPPos;
        private float dragStartHeight;
        private Plane dragPlaneXZ;
        private Plane dragPlaneY;
        private Ray currentRay;

        private bool isVisible = true;

        private void Awake()
        {
            // Auto-find SmartWall if not assigned
            if (smartWall == null)
            {
                smartWall = GetComponentInParent<SmartWall>();
                if (smartWall == null)
                {
                    Debug.LogError("SmartWallCursor: Cannot find SmartWall component in parent!");
                }
            }
        }

        private void Start()
        {
            Debug.Log($"[SmartWallCursor] Start - index {controlPointIndex}, wall: {smartWall?.name}");

            // Auto-find camera if not assigned
            if (buildCamera == null)
            {
                buildCamera = Camera.main;
                Debug.Log($"[SmartWallCursor] Found camera: {buildCamera?.name}");
            }

            // Validate smartWall reference
            if (smartWall == null)
            {
                Debug.LogError($"[SmartWallCursor] SmartWall reference is null! Cannot create cursor.");
                return;
            }

            Debug.Log($"[SmartWallCursor] Creating handles...");

            // Create handle visuals
            CreateXZHandle();

            // Only first control point gets height handler
            if (controlPointIndex == 0)
            {
                Debug.Log($"[SmartWallCursor] Creating Y handle for first CP");
                CreateYHandle();
            }

            Debug.Log($"[SmartWallCursor] Creating buttons...");
            CreateAddButton();
            CreateDeleteButton();

            // Set UI layer for raycasting priority
            if (uiLayer == 0)
                uiLayer = LayerMask.GetMask("UI");

            Debug.Log($"[SmartWallCursor] Registering with manager...");
            // Register with manager
            var manager = SmartWallManager.Instance;
            if (manager != null)
            {
                manager.RegisterCursor(this);
            }
            else
            {
                Debug.LogWarning($"[SmartWallCursor] SmartWallManager.Instance is null, cannot register");
            }

            Debug.Log($"[SmartWallCursor] Setting visible false...");
            // Start hidden (View mode by default)
            SetVisible(false);

            Debug.Log($"[SmartWallCursor] Start complete for index {controlPointIndex}");
        }

        private void OnDestroy()
        {
            // Unregister from manager
            if (SmartWallManager.Instance != null)
            {
                SmartWallManager.Instance.UnregisterCursor(this);
            }
        }

        private void CreateXZHandle()
        {
            Debug.Log($"[SmartWallCursor] CreateXZHandle for index {controlPointIndex}");

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

            Debug.Log($"[SmartWallCursor] XZHandle created successfully");
        }

        private void CreateYHandle()
        {
            if (yHandlePrefab != null)
            {
                yHandle = Instantiate(yHandlePrefab, transform);
                yHandle.name = "YHandle";
            }
            else
            {
                // Fallback: Create simple cylinder
                yHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                yHandle.name = "YHandle";
                yHandle.transform.SetParent(transform);
                yHandle.transform.localScale = new Vector3(0.15f, 0.8f, 0.15f);

                var renderer = yHandle.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = new Color(0f, 1f, 0f, 0.8f); // Green
                }
            }

            yHandle.transform.localPosition = Vector3.zero;
            SetLayerRecursively(yHandle, LayerMask.NameToLayer("UI"));
            ApplyAlwaysOnTopShader(yHandle);
        }

        private void CreateAddButton()
        {
            // Create simple cube for add button
            addButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
            addButton.name = "AddButton";
            addButton.transform.SetParent(transform);
            addButton.transform.localScale = Vector3.one * 0.3f;

            var renderer = addButton.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = new Color(0f, 1f, 0f, 0.9f); // Green for add
            }

            SetLayerRecursively(addButton, LayerMask.NameToLayer("UI"));
            ApplyAlwaysOnTopShader(addButton);
        }

        private void CreateDeleteButton()
        {
            // Create simple cube for delete button
            deleteButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deleteButton.name = "DeleteButton";
            deleteButton.transform.SetParent(transform);
            deleteButton.transform.localScale = Vector3.one * 0.3f;

            var renderer = deleteButton.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = new Color(1f, 0f, 0f, 0.9f); // Red for delete
            }

            SetLayerRecursively(deleteButton, LayerMask.NameToLayer("UI"));
            ApplyAlwaysOnTopShader(deleteButton);
        }

        private void ApplyAlwaysOnTopShader(GameObject obj)
        {
            Shader alwaysOnTopShader = Shader.Find("HorizonMini/AlwaysOnTop_URP");
            if (alwaysOnTopShader == null)
            {
                alwaysOnTopShader = Shader.Find("Custom/AlwaysOnTop_URP");
            }
            if (alwaysOnTopShader == null)
            {
                alwaysOnTopShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (alwaysOnTopShader == null)
            {
                Debug.LogError("No suitable shader found for SmartWallCursor!");
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
            if (smartWall == null || buildCamera == null || controlPointIndex < 0)
                return;

            // Update cursor position
            UpdateCursorPosition();

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

        private void UpdateCursorPosition()
        {
            if (controlPointIndex >= 0 && controlPointIndex < smartWall.GetControlPointCount())
            {
                Vector3 cpPos = smartWall.transform.TransformPoint(smartWall.GetControlPointPosition(controlPointIndex));
                float height = smartWall.GetWallHeight();
                transform.position = cpPos + new Vector3(0, height * 0.5f, 0);
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
            if (touchStarted && !isDraggingXZ && !isDraggingY)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    Transform hitTransform = hit.transform;

                    // Check for add button
                    if (IsChildOf(hitTransform, addButton.transform))
                    {
                        OnAddButtonClicked();
                        return;
                    }

                    // Check for delete button
                    if (IsChildOf(hitTransform, deleteButton.transform))
                    {
                        OnDeleteButtonClicked();
                        return;
                    }

                    // Check for Y handle (only exists for first control point)
                    if (yHandle != null && IsChildOf(hitTransform, yHandle.transform))
                    {
                        StartDragY(hit.point);
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
                else if (isDraggingY)
                {
                    UpdateDragY();
                }
            }

            // End drag
            if (touchEnded)
            {
                isDraggingXZ = false;
                isDraggingY = false;
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
            dragStartCPPos = smartWall.GetControlPointPosition(controlPointIndex);

            // Create horizontal plane at control point level (y=0)
            Vector3 planePoint = smartWall.transform.TransformPoint(dragStartCPPos);
            dragPlaneXZ = new Plane(Vector3.up, planePoint);
        }

        private void UpdateDragXZ()
        {
            float enter;
            if (dragPlaneXZ.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                // Convert to local space
                Vector3 localDelta = smartWall.transform.InverseTransformDirection(worldDelta);

                // Only XZ movement
                Vector3 newLocalPos = dragStartCPPos;
                newLocalPos.x += localDelta.x * dragSensitivity;
                newLocalPos.z += localDelta.z * dragSensitivity;

                // Snap to grid
                newLocalPos.x = Mathf.Round(newLocalPos.x / snapInterval) * snapInterval;
                newLocalPos.z = Mathf.Round(newLocalPos.z / snapInterval) * snapInterval;

                smartWall.SetControlPointPosition(controlPointIndex, newLocalPos);
            }
        }

        private void StartDragY(Vector3 hitPoint)
        {
            isDraggingY = true;
            dragStartWorldPos = hitPoint;
            dragStartHeight = smartWall.GetWallHeight();

            // Create vertical plane perpendicular to camera
            Vector3 cameraForward = buildCamera.transform.forward;
            cameraForward.y = 0;
            if (cameraForward.sqrMagnitude < 0.001f)
            {
                cameraForward = buildCamera.transform.right;
                cameraForward.y = 0;
            }
            cameraForward.Normalize();

            Vector3 cpWorldPos = smartWall.transform.TransformPoint(smartWall.GetControlPointPosition(controlPointIndex));
            dragPlaneY = new Plane(cameraForward, cpWorldPos);
        }

        private void UpdateDragY()
        {
            float enter;
            if (dragPlaneY.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                float yDelta = worldDelta.y;

                float newHeight = dragStartHeight + yDelta * dragSensitivity;

                // Snap to grid
                newHeight = Mathf.Round(newHeight / snapInterval) * snapInterval;
                newHeight = Mathf.Max(snapInterval, newHeight);

                smartWall.SetWallHeight(newHeight);
            }
        }

        private void OnAddButtonClicked()
        {
            if (smartWall == null)
                return;

            int newIndex = -1;

            // Check if this is the last control point
            if (controlPointIndex == smartWall.GetControlPointCount() - 1)
            {
                // Extend wall at the end
                newIndex = smartWall.ExtendWall(controlPointIndex);
            }
            else
            {
                // Insert between current and next
                newIndex = smartWall.InsertControlPointAfter(controlPointIndex);
            }

            Debug.Log($"Added control point at index {newIndex}");
        }

        private void OnDeleteButtonClicked()
        {
            if (smartWall == null)
                return;

            bool success = smartWall.DeleteControlPoint(controlPointIndex);
            if (!success)
            {
                Debug.LogWarning("Cannot delete control point - minimum 2 required");
            }
            else
            {
                Debug.Log($"Deleted control point at index {controlPointIndex}");
                // The manager should handle updating which cursor is active
            }
        }

        private void UpdateVisuals()
        {
            if (controlPointIndex < 0 || controlPointIndex >= smartWall.GetControlPointCount())
                return;

            // Position buttons relative to cursor
            if (addButton != null)
            {
                addButton.transform.localPosition = new Vector3(0.8f, 0, 0);
            }

            if (deleteButton != null)
            {
                deleteButton.transform.localPosition = new Vector3(-0.8f, 0, 0);
            }
        }

        /// <summary>
        /// Set which control point this cursor is controlling
        /// </summary>
        public void SetControlPointIndex(int index)
        {
            controlPointIndex = index;
            UpdateCursorPosition();
        }

        /// <summary>
        /// Get the control point index this cursor is controlling
        /// </summary>
        public int GetControlPointIndex()
        {
            return controlPointIndex;
        }

        /// <summary>
        /// Set the smart wall this cursor controls
        /// </summary>
        public void SetSmartWall(SmartWall wall)
        {
            smartWall = wall;
        }

        /// <summary>
        /// Get the smart wall this cursor controls
        /// </summary>
        public SmartWall GetSmartWall()
        {
            return smartWall;
        }

        /// <summary>
        /// Show or hide the cursor
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;

            if (xzHandle != null)
                xzHandle.SetActive(visible);
            if (yHandle != null)
                yHandle.SetActive(visible);
            if (addButton != null)
                addButton.SetActive(visible);
            if (deleteButton != null)
                deleteButton.SetActive(visible);

            if (!visible)
            {
                isDraggingXZ = false;
                isDraggingY = false;
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
            return isDraggingXZ || isDraggingY;
        }
    }
}
