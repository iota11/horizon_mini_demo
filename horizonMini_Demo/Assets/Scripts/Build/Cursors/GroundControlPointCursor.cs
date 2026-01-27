using UnityEngine;
using UnityEngine.Events;

namespace HorizonMini.Build.Cursors
{
    /// <summary>
    /// Ground Control Point Cursor - for adjusting XZ size
    /// Renders a sphere (configured in prefab - typically blue)
    /// Handler: XZ drag only
    /// </summary>
    public class GroundControlPointCursor : BaseCursor
    {
        [Header("Handler References")]
        [SerializeField] private Transform xzHandler;

        [Header("Visual")]
        [SerializeField] private GameObject sphere; // The visual sphere (configured in prefab)

        [Header("Settings")]
        [SerializeField] private float snapInterval = 0.5f;

        [Header("Events")]
        public UnityEvent<Vector3> OnPositionChanged;

        private bool isDraggingXZ = false;
        private Vector3 dragStartWorldPos;
        private Vector3 dragStartPosition;
        private Plane dragPlane;
        private Ray currentRay;

        protected override void HandleInput()
        {
            Vector2 screenPos = GetScreenPosition();
            currentRay = buildCamera.ScreenPointToRay(screenPos);

            // Start drag
            if (GetInputDown() && !isDraggingXZ)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    Transform hitTransform = hit.transform;

                    // Check for XZ handler
                    if (xzHandler != null && IsChildOf(hitTransform, xzHandler))
                    {
                        StartDragXZ(hit.point);
                        return;
                    }
                }
            }

            // Continue dragging
            if (GetInput() && isDraggingXZ)
            {
                UpdateDragXZ();
            }

            // End drag
            if (GetInputUp())
            {
                isDraggingXZ = false;
                isDragging = false;
            }
        }

        private void StartDragXZ(Vector3 hitPoint)
        {
            isDraggingXZ = true;
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

        protected override void UpdateVisuals()
        {
            // Ground control point doesn't billboard - it stays oriented
            // Sphere is just a marker, no rotation needed
        }
    }
}
