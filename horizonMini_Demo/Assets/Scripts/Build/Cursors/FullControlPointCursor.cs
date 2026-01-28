using UnityEngine;
using UnityEngine.Events;

namespace HorizonMini.Build.Cursors
{
    /// <summary>
    /// Full Control Point Cursor - for full 3D control
    /// Renders a sphere (configured in prefab - typically red)
    /// Handlers: XZ drag and Y drag (with minimum value)
    /// </summary>
    public class FullControlPointCursor : BaseCursor
    {
        [Header("Handler References")]
        [SerializeField] private Transform xzHandler;
        [SerializeField] private Transform yHandler;

        [Header("Visual")]
        [SerializeField] private GameObject sphere; // The visual sphere (configured in prefab)

        [Header("Settings")]
        [SerializeField] private float snapInterval = 0.5f;
        [SerializeField] private float yMin = 0.5f; // Minimum Y value

        [Header("Events")]
        public UnityEvent<Vector3> OnPositionChanged;

        private enum DragMode { None, XZ, Y }
        private DragMode currentDragMode = DragMode.None;

        private Vector3 dragStartWorldPos;
        private Vector3 dragStartPosition;
        private Plane dragPlane;
        private Ray currentRay;

        private Transform targetTransform; // The transform this cursor follows (e.g., SmartTerrain.controlPoint)

        protected override void HandleInput()
        {
            Vector2 screenPos = GetScreenPosition();
            currentRay = buildCamera.ScreenPointToRay(screenPos);

            // Start drag
            if (GetInputDown() && currentDragMode == DragMode.None)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    Transform hitTransform = hit.transform;

                    // Check for Y handler (check first for priority)
                    if (yHandler != null && IsChildOf(hitTransform, yHandler))
                    {
                        StartDragY(hit.point);
                        return;
                    }

                    // Check for XZ handler
                    if (xzHandler != null && IsChildOf(hitTransform, xzHandler))
                    {
                        StartDragXZ(hit.point);
                        return;
                    }
                }
            }

            // Continue dragging
            if (GetInput())
            {
                switch (currentDragMode)
                {
                    case DragMode.XZ:
                        UpdateDragXZ();
                        break;
                    case DragMode.Y:
                        UpdateDragY();
                        break;
                }
            }

            // End drag
            if (GetInputUp())
            {
                currentDragMode = DragMode.None;
                isDragging = false;
            }
        }

        private void StartDragXZ(Vector3 hitPoint)
        {
            currentDragMode = DragMode.XZ;
            isDragging = true;
            dragStartWorldPos = hitPoint;
            dragStartPosition = transform.position;

            // Create horizontal plane at current Y level
            dragPlane = new Plane(Vector3.up, transform.position);
        }

        private void UpdateDragXZ()
        {
            float enter;
            if (dragPlane.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                Vector3 newPosition = dragStartPosition + worldDelta;

                // Snap to grid
                newPosition.x = Mathf.Round(newPosition.x / snapInterval) * snapInterval;
                newPosition.z = Mathf.Round(newPosition.z / snapInterval) * snapInterval;

                // Keep Y unchanged
                newPosition.y = dragStartPosition.y;

                transform.position = newPosition;
                OnPositionChanged?.Invoke(newPosition);
            }
        }

        private void StartDragY(Vector3 hitPoint)
        {
            currentDragMode = DragMode.Y;
            isDragging = true;
            dragStartWorldPos = hitPoint;
            dragStartPosition = transform.position;
        }

        private void UpdateDragY()
        {
            // Create vertical plane perpendicular to camera
            Vector3 cameraForward = buildCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Plane verticalPlane = new Plane(cameraForward, dragStartPosition);

            float enter;
            if (verticalPlane.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                float yDelta = currentWorldPos.y - dragStartWorldPos.y;

                Vector3 newPosition = dragStartPosition;
                newPosition.y = Mathf.Max(yMin, dragStartPosition.y + yDelta);

                // Snap to grid
                newPosition.y = Mathf.Round(newPosition.y / snapInterval) * snapInterval;
                newPosition.y = Mathf.Max(yMin, newPosition.y);

                transform.position = newPosition;
                OnPositionChanged?.Invoke(newPosition);
            }
        }

        protected override void UpdateVisuals()
        {
            // Follow target transform if set (and not currently dragging)
            if (targetTransform != null && !isDragging)
            {
                transform.position = targetTransform.position;
            }

            // Full control point doesn't billboard - it stays oriented
            // Sphere is just a marker, no rotation needed
        }

        /// <summary>
        /// Set the target transform this cursor follows
        /// </summary>
        public void SetTargetTransform(Transform target)
        {
            targetTransform = target;
            if (targetTransform != null)
            {
                transform.position = targetTransform.position;
            }
        }

        /// <summary>
        /// Set minimum Y value
        /// </summary>
        public void SetYMin(float min)
        {
            yMin = min;
        }

        /// <summary>
        /// Get minimum Y value
        /// </summary>
        public float GetYMin()
        {
            return yMin;
        }

        /// <summary>
        /// Set build camera (for raycasting)
        /// </summary>
        public void SetBuildCamera(Camera camera)
        {
            buildCamera = camera;
        }

        /// <summary>
        /// Set UI layer mask (for handler detection)
        /// </summary>
        public void SetUILayer(LayerMask layer)
        {
            uiLayer = layer;
        }

        /// <summary>
        /// Set snap interval
        /// </summary>
        public void SetSnapInterval(float interval)
        {
            snapInterval = interval;
        }
    }
}
