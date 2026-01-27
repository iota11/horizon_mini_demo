using UnityEngine;
using UnityEngine.EventSystems;

namespace HorizonMini.Build
{
    /// <summary>
    /// Cursor for controlling SmartTerrain control points
    /// Allows dragging in XZ plane and adjusting Y height
    /// </summary>
    public class SmartTerrainCursor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SmartTerrain smartTerrain;
        [SerializeField] private Camera buildCamera;

        [Header("Visual Elements")]
        [SerializeField] private GameObject xzHandlePrefab; // Handler for XZ plane movement
        [SerializeField] private GameObject yHandlePrefab;   // Handler for Y axis movement
        [SerializeField] private LayerMask uiLayer;

        [Header("Settings")]
        [SerializeField] private float dragSensitivity = 1f;
        [SerializeField] private float snapInterval = 0.25f; // Snap interval in meters

        private GameObject xzHandle;
        private GameObject yHandle;
        private Transform xzHandleTransform;
        private Transform yHandleTransform;
        private bool isDraggingXZ = false;
        private bool isDraggingY = false;
        private Vector3 dragStartWorldPos;
        private Vector3 dragStartLocalPos;
        private Plane dragPlaneXZ;
        private Plane dragPlaneY;
        private Ray currentRay;

        // For hover detection (reserved for future use)
        #pragma warning disable 0414
        private bool isHoveringXZHandle = false;
        private bool isHoveringYHandle = false;
        #pragma warning restore 0414

        // Visibility control
        private bool isVisible = true;

        private void Awake()
        {
            // Auto-find SmartTerrain if not assigned
            if (smartTerrain == null)
            {
                smartTerrain = GetComponentInParent<SmartTerrain>();
                if (smartTerrain == null)
                {
                    Debug.LogError("SmartTerrainCursor: Cannot find SmartTerrain component in parent!");
                }
            }
        }

        private void Start()
        {
            // Auto-find camera if not assigned
            if (buildCamera == null)
                buildCamera = Camera.main;

            // Create handle visuals
            CreateXZHandle();
            CreateYHandle();

            // Set UI layer for raycasting priority
            if (uiLayer == 0)
                uiLayer = LayerMask.GetMask("UI");

            // Register with SmartTerrainManager
            var manager = SmartTerrainManager.Instance;
            if (manager != null)
            {
                manager.RegisterCursor(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from SmartTerrainManager
            if (SmartTerrainManager.Instance != null)
            {
                SmartTerrainManager.Instance.UnregisterCursor(this);
            }
        }

        private void CreateXZHandle()
        {
            if (xzHandlePrefab != null)
            {
                // Use prefab
                xzHandle = Instantiate(xzHandlePrefab, transform);
                xzHandle.name = "XZHandle";
            }
            else
            {
                // Fallback: Create simple sphere
                xzHandle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                xzHandle.name = "XZHandle";
                xzHandle.transform.SetParent(transform);
                xzHandle.transform.localScale = Vector3.one * 0.5f;

                // Default color (shader will be applied later)
                var renderer = xzHandle.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = new Color(0f, 0.5f, 1f, 0.8f); // Blue
                }
            }

            xzHandle.transform.localPosition = Vector3.zero;
            xzHandleTransform = xzHandle.transform;

            // Set layer to UI for priority raycasting
            SetLayerRecursively(xzHandle, LayerMask.NameToLayer("UI"));

            // Apply AlwaysOnTop shader to all renderers
            ApplyAlwaysOnTopShader(xzHandle);
        }

        private void CreateYHandle()
        {
            if (yHandlePrefab != null)
            {
                // Use prefab
                yHandle = Instantiate(yHandlePrefab, transform);
                yHandle.name = "YHandle";
            }
            else
            {
                // Fallback: Create simple cylinder
                yHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                yHandle.name = "YHandle";
                yHandle.transform.SetParent(transform);
                yHandle.transform.localScale = new Vector3(0.2f, 1f, 0.2f);

                // Default color (shader will be applied later)
                var renderer = yHandle.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = new Color(0f, 1f, 0f, 0.8f); // Green
                }
            }

            yHandle.transform.localPosition = Vector3.zero;
            yHandleTransform = yHandle.transform;

            // Set layer to UI for priority raycasting
            SetLayerRecursively(yHandle, LayerMask.NameToLayer("UI"));

            // Apply AlwaysOnTop shader to all renderers
            ApplyAlwaysOnTopShader(yHandle);
        }

        private void ApplyAlwaysOnTopShader(GameObject obj)
        {
            Shader alwaysOnTopShader = Shader.Find("HorizonMini/AlwaysOnTop_URP");
            if (alwaysOnTopShader == null)
            {
                Debug.LogWarning("HorizonMini/AlwaysOnTop_URP shader not found! Trying Custom/AlwaysOnTop_URP...");
                alwaysOnTopShader = Shader.Find("Custom/AlwaysOnTop_URP");
            }

            if (alwaysOnTopShader == null)
            {
                Debug.LogWarning("AlwaysOnTop_URP shader not found! Using URP/Lit as fallback (no always-on-top effect).");
                alwaysOnTopShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (alwaysOnTopShader == null)
            {
                Debug.LogError("No suitable shader found!");
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
                    renderer.material.renderQueue = 3000; // Overlay queue
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
            if (smartTerrain == null || buildCamera == null)
                return;

            // Update cursor position to match control point
            if (smartTerrain.controlPoint != null)
            {
                transform.position = smartTerrain.controlPoint.position;
            }

            // Always face camera (billboard)
            Vector3 toCamera = buildCamera.transform.position - transform.position;
            toCamera.y = 0; // Keep upright
            if (toCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(toCamera);
            }

            // Only handle input and update visuals if visible
            if (isVisible)
            {
                HandleInput();
                UpdateVisuals();
            }
        }

        private void HandleInput()
        {
            // Check for touch or mouse input
            bool isTouching = Input.GetMouseButton(0) || (Input.touchCount > 0);
            bool touchStarted = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool touchEnded = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

            Vector2 screenPos = Input.mousePosition;
            if (Input.touchCount > 0)
                screenPos = Input.GetTouch(0).position;

            currentRay = buildCamera.ScreenPointToRay(screenPos);

            // Hover detection
            isHoveringXZHandle = false;
            isHoveringYHandle = false;
            if (!isDraggingXZ && !isDraggingY)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    if (hit.transform == xzHandleTransform || hit.transform.IsChildOf(xzHandleTransform))
                    {
                        isHoveringXZHandle = true;
                    }
                    else if (hit.transform == yHandleTransform || hit.transform.IsChildOf(yHandleTransform))
                    {
                        isHoveringYHandle = true;
                    }
                }
            }

            // Start drag
            if (touchStarted && !isDraggingXZ && !isDraggingY)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    // Check if clicking on Y handle
                    if (hit.transform == yHandleTransform || hit.transform.IsChildOf(yHandleTransform))
                    {
                        // Set this terrain as active when starting to drag
                        SmartTerrainManager.Instance.SetActiveTerrain(smartTerrain);
                        StartDragY(hit.point);
                        return;
                    }
                    // Check if clicking on XZ handle
                    else if (hit.transform == xzHandleTransform || hit.transform.IsChildOf(xzHandleTransform))
                    {
                        // Set this terrain as active when starting to drag
                        SmartTerrainManager.Instance.SetActiveTerrain(smartTerrain);
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

        private void StartDragXZ(Vector3 hitPoint)
        {
            isDraggingXZ = true;
            dragStartWorldPos = hitPoint;
            dragStartLocalPos = smartTerrain.GetControlPointPosition();

            // Create drag plane at control point height, parallel to XZ
            Vector3 planePoint = smartTerrain.controlPoint.position;
            dragPlaneXZ = new Plane(Vector3.up, planePoint);
        }

        private void UpdateDragXZ()
        {
            float enter;
            if (dragPlaneXZ.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                // Convert world delta to local space
                Vector3 localDelta = smartTerrain.transform.InverseTransformDirection(worldDelta);

                // Only apply XZ movement
                Vector3 newLocalPos = dragStartLocalPos;
                newLocalPos.x += localDelta.x * dragSensitivity;
                newLocalPos.z += localDelta.z * dragSensitivity;

                // Snap to grid
                newLocalPos.x = Mathf.Round(newLocalPos.x / snapInterval) * snapInterval;
                newLocalPos.z = Mathf.Round(newLocalPos.z / snapInterval) * snapInterval;

                smartTerrain.SetControlPointPosition(newLocalPos);
            }
        }

        private void StartDragY(Vector3 hitPoint)
        {
            isDraggingY = true;
            dragStartWorldPos = hitPoint;
            dragStartLocalPos = smartTerrain.GetControlPointPosition();

            // Create vertical drag plane perpendicular to camera view direction
            Vector3 cameraForward = buildCamera.transform.forward;
            cameraForward.y = 0; // Keep horizontal
            if (cameraForward.sqrMagnitude < 0.001f)
            {
                // Camera is looking straight down/up, use camera right
                cameraForward = buildCamera.transform.right;
                cameraForward.y = 0;
            }
            cameraForward.Normalize();

            // Plane normal is horizontal direction perpendicular to camera
            dragPlaneY = new Plane(cameraForward, smartTerrain.controlPoint.position);
        }

        private void UpdateDragY()
        {
            float enter;
            if (dragPlaneY.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                // Extract only Y component (no negation - normal direction)
                float yDelta = worldDelta.y;

                Vector3 newLocalPos = dragStartLocalPos;
                newLocalPos.y += yDelta * dragSensitivity;

                // Snap to grid
                newLocalPos.y = Mathf.Round(newLocalPos.y / snapInterval) * snapInterval;
                newLocalPos.y = Mathf.Max(snapInterval, newLocalPos.y); // Clamp to positive (minimum 1 snap interval)

                smartTerrain.SetControlPointPosition(newLocalPos);
            }
        }

        private void UpdateVisuals()
        {
            // Update handle positions (always at control point)
            if (smartTerrain.controlPoint != null)
            {
                if (xzHandleTransform != null)
                {
                    xzHandleTransform.position = smartTerrain.controlPoint.position;
                }
                if (yHandleTransform != null)
                {
                    yHandleTransform.position = smartTerrain.controlPoint.position;
                }
            }
        }

        /// <summary>
        /// Set the smart terrain this cursor controls
        /// </summary>
        public void SetSmartTerrain(SmartTerrain terrain)
        {
            smartTerrain = terrain;
        }

        /// <summary>
        /// Get the smart terrain this cursor controls
        /// </summary>
        public SmartTerrain GetSmartTerrain()
        {
            return smartTerrain;
        }

        /// <summary>
        /// Get whether cursor is currently being dragged
        /// </summary>
        public bool IsDragging()
        {
            return isDraggingXZ || isDraggingY;
        }

        /// <summary>
        /// Show or hide the cursor and control point (for Edit/View mode)
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;

            // Hide/show handles
            if (xzHandle != null)
            {
                xzHandle.SetActive(visible);
            }
            if (yHandle != null)
            {
                yHandle.SetActive(visible);
            }

            // Also control the smart terrain's control point visibility
            if (smartTerrain != null)
            {
                smartTerrain.SetControlPointVisible(visible);
            }

            // Cancel any ongoing drag when hiding
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

        private void OnDrawGizmos()
        {
            if (isDraggingXZ)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }

            if (isDraggingY)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
            }
        }
    }
}
