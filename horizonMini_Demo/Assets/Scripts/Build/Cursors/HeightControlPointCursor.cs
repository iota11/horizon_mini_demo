using UnityEngine;
using UnityEngine.Events;

namespace HorizonMini.Build.Cursors
{
    /// <summary>
    /// Height Control Point Cursor - for adjusting Y size and procedural modeling
    /// Renders a sphere (configured in prefab - typically yellow)
    /// Handler: Y drag only (with minimum value)
    /// </summary>
    public class HeightControlPointCursor : BaseCursor
    {
        [Header("Handler References")]
        [SerializeField] private Transform yHandler;

        [Header("Visual")]
        [SerializeField] private GameObject sphere; // The visual sphere (configured in prefab)

        [Header("Settings")]
        [SerializeField] private float snapInterval = 0.5f;
        [SerializeField] private float yMin = 0.5f; // Minimum Y value

        [Header("Events")]
        public UnityEvent<Vector3> OnPositionChanged;

        private bool isDraggingY = false;
        private Vector3 dragStartWorldPos;
        private Vector3 dragStartPosition;
        private Ray currentRay;
        private HorizonMini.Build.SmartWall targetWall; // SmartWall to follow
        private HorizonMini.Build.SmartHouse targetHouse; // SmartHouse to follow
        private bool isLeftEdge; // For SmartHouse: true = left edge, false = right edge

        protected override void HandleInput()
        {
            Vector2 screenPos = GetScreenPosition();
            currentRay = buildCamera.ScreenPointToRay(screenPos);

            // Start drag
            if (GetInputDown() && !isDraggingY)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    Transform hitTransform = hit.transform;

                    // Check for Y handler
                    if (yHandler != null && IsChildOf(hitTransform, yHandler))
                    {
                        StartDragY(hit.point);
                        return;
                    }
                }
            }

            // Continue dragging
            if (GetInput() && isDraggingY)
            {
                UpdateDragY();
            }

            // End drag
            if (GetInputUp())
            {
                isDraggingY = false;
                isDragging = false;
            }
        }

        private void StartDragY(Vector3 hitPoint)
        {
            isDraggingY = true;
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
            // Height control point doesn't billboard - it stays oriented
            // Sphere is just a marker, no rotation needed

            // Update XZ position to follow wall midpoint (if wall is set)
            if (targetWall != null && targetWall.GetControlPointCount() >= 2)
            {
                // Calculate midpoint between start and end control points
                Transform startPoint = targetWall.GetControlPointTransform(0);
                Transform endPoint = targetWall.GetControlPointTransform(1);

                if (startPoint != null && endPoint != null)
                {
                    Vector3 midPoint = (startPoint.position + endPoint.position) * 0.5f;

                    // Update only XZ position, keep current Y
                    Vector3 currentPos = transform.position;
                    currentPos.x = midPoint.x;
                    currentPos.z = midPoint.z;
                    transform.position = currentPos;
                }
            }
            // Update XZ position to follow house edge (if house is set)
            else if (targetHouse != null)
            {
                // Follow the control point's world position (including rotation and Y offset)
                Transform controlPoint = isLeftEdge ? targetHouse.yControlPoint1 : targetHouse.yControlPoint2;
                if (controlPoint != null)
                {
                    Vector3 houseSize = targetHouse.GetSize();
                    float halfWidth = houseSize.x * 0.5f;

                    // Calculate offset in local space (left or right edge)
                    Vector3 localOffset = new Vector3(isLeftEdge ? -halfWidth : halfWidth, 0f, 0f);

                    // Transform to world space (applies rotation)
                    Vector3 worldOffset = targetHouse.transform.TransformDirection(localOffset);

                    // Final position = house position + rotated offset + control point's Y
                    Vector3 newPos = targetHouse.transform.position + worldOffset;
                    newPos.y = controlPoint.position.y; // Use control point's Y (which includes house Y + local Y)

                    transform.position = newPos;
                }
            }
        }

        /// <summary>
        /// Set the SmartWall that this cursor should follow
        /// </summary>
        public void SetTargetWall(HorizonMini.Build.SmartWall wall)
        {
            targetWall = wall;
            targetHouse = null; // Clear house target
        }

        /// <summary>
        /// Set the SmartHouse that this cursor should follow
        /// </summary>
        public void SetTargetHouse(HorizonMini.Build.SmartHouse house, bool leftEdge)
        {
            targetHouse = house;
            isLeftEdge = leftEdge;
            targetWall = null; // Clear wall target
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
    }
}
