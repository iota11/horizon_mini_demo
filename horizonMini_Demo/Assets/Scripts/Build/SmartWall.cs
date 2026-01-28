using UnityEngine;
using System.Collections.Generic;
using ProjectDawn.CozyBuilder;

namespace HorizonMini.Build
{
    /// <summary>
    /// Smart procedural wall system using Cozy Builder
    /// Control points define the wall profile (bottom), height points define the top
    /// </summary>
    public class SmartWall : MonoBehaviour
    {
        [Header("Cozy Builder References")]
        [SerializeField] private CozySpline bottomSpline; // Control points (profile)
        [SerializeField] private CozySpline topSpline;    // Height points
        [SerializeField] private CozyPlane plane;
        [SerializeField] private CozyRenderer cozyRenderer;

        [Header("Settings")]
        [SerializeField] private float wallHeight = 2.5f; // Unified wall height
        [SerializeField] private float minHeight = 0.5f;
        [SerializeField] private float maxHeight = 10f;
        [SerializeField] private float snapInterval = 0.25f;

        // Data for serialization
        private List<Vector3> controlPointPositions = new List<Vector3>();

        private void Awake()
        {
            // Auto-find components if not assigned
            if (bottomSpline == null)
                bottomSpline = transform.Find("Spline")?.GetComponent<CozySpline>();

            if (topSpline == null)
                topSpline = transform.Find("Spline (1)")?.GetComponent<CozySpline>();

            if (plane == null)
                plane = GetComponentInChildren<CozyPlane>();

            if (cozyRenderer == null)
                cozyRenderer = GetComponent<CozyRenderer>();

            // Initialize with default points if empty
            if (bottomSpline != null && bottomSpline.Points.Count == 0)
            {
                InitializeDefaultWall();
            }

            // Register with manager
            var manager = SmartWallManager.Instance;
            if (manager != null)
            {
                manager.RegisterWall(this);
            }
        }

        private void Start()
        {
            // Generate colliders for wall segments
            UpdateColliders();
        }

        private void OnDestroy()
        {
            // Unregister from manager
            if (SmartWallManager.Instance != null)
            {
                SmartWallManager.Instance.UnregisterWall(this);
            }
        }

        /// <summary>
        /// Initialize wall with default 2 control points
        /// </summary>
        private void InitializeDefaultWall()
        {
            // Create 2 control points by default
            AddControlPointAt(new Vector3(0, 0, 0));
            AddControlPointAt(new Vector3(2, 0, 0));
        }

        /// <summary>
        /// Get number of control points
        /// </summary>
        public int GetControlPointCount()
        {
            return bottomSpline != null ? bottomSpline.Points.Count : 0;
        }

        /// <summary>
        /// Get control point position (bottom)
        /// </summary>
        public Vector3 GetControlPointPosition(int index)
        {
            if (bottomSpline == null || index < 0 || index >= bottomSpline.Points.Count)
                return Vector3.zero;

            CozyPoint point = bottomSpline.Points[index];
            return point != null ? point.transform.localPosition : Vector3.zero;
        }

        /// <summary>
        /// Get control point transform (for cursor tracking)
        /// </summary>
        public Transform GetControlPointTransform(int index)
        {
            if (bottomSpline == null || index < 0 || index >= bottomSpline.Points.Count)
                return null;

            CozyPoint point = bottomSpline.Points[index];
            return point != null ? point.transform : null;
        }

        /// <summary>
        /// Get unified wall height
        /// </summary>
        public float GetWallHeight()
        {
            return wallHeight;
        }

        /// <summary>
        /// Set control point position in XZ plane only (Y stays 0)
        /// </summary>
        public void SetControlPointPosition(int index, Vector3 position)
        {
            if (bottomSpline == null || index < 0 || index >= bottomSpline.Points.Count)
                return;

            // Clamp to XZ plane (y = 0)
            position.y = 0;

            CozyPoint bottomPoint = bottomSpline.Points[index];
            if (bottomPoint != null)
            {
                bottomPoint.transform.localPosition = position;
            }

            // Update corresponding top point XZ but keep unified height
            if (topSpline != null && index < topSpline.Points.Count)
            {
                CozyPoint topPoint = topSpline.Points[index];
                if (topPoint != null)
                {
                    Vector3 newTopPos = position;
                    newTopPos.y = wallHeight;
                    topPoint.transform.localPosition = newTopPos;
                }
            }

            // Update colliders after position change
            UpdateColliders();
        }

        /// <summary>
        /// Called by FullControlPointCursor when control point is moved (in world space)
        /// </summary>
        public void OnControlPointPositionChanged(int index, Vector3 newWorldPosition)
        {
            // Convert world position to local position
            Vector3 localPos = transform.InverseTransformPoint(newWorldPosition);

            // Update control point position
            SetControlPointPosition(index, localPos);

            // Refresh bounding box in SelectionSystem
            HorizonMini.Build.SelectionSystem selectionSystem = FindFirstObjectByType<HorizonMini.Build.SelectionSystem>();
            if (selectionSystem != null)
            {
                selectionSystem.RefreshBoundingBox();
            }
        }

        /// <summary>
        /// Set unified wall height (affects all points)
        /// </summary>
        public void SetWallHeight(float height)
        {
            wallHeight = Mathf.Clamp(height, minHeight, maxHeight);

            // Update all top points to new height
            if (topSpline != null)
            {
                for (int i = 0; i < topSpline.Points.Count; i++)
                {
                    CozyPoint topPoint = topSpline.Points[i];
                    if (topPoint != null)
                    {
                        Vector3 bottomPos = GetControlPointPosition(i);
                        Vector3 newTopPos = bottomPos;
                        newTopPos.y = wallHeight;
                        topPoint.transform.localPosition = newTopPos;
                    }
                }
            }

            RefreshRenderer();
        }

        /// <summary>
        /// Called by HeightControlPointCursor when height is changed (world space Y position)
        /// </summary>
        public void OnHeightCursorPositionChanged(Vector3 newWorldPosition)
        {
            // Convert world Y to local Y (which is the wall height)
            Vector3 localPos = transform.InverseTransformPoint(newWorldPosition);
            float newHeight = Mathf.Abs(localPos.y);

            SetWallHeight(newHeight);

            // Refresh bounding box in SelectionSystem
            HorizonMini.Build.SelectionSystem selectionSystem = FindFirstObjectByType<HorizonMini.Build.SelectionSystem>();
            if (selectionSystem != null)
            {
                selectionSystem.RefreshBoundingBox();
            }
        }

        /// <summary>
        /// Add new control point at specified position
        /// </summary>
        public int AddControlPointAt(Vector3 position)
        {
            if (bottomSpline == null || topSpline == null)
                return -1;

            Debug.Log($"[SmartWall] AddControlPointAt position={position}, wallHeight={wallHeight}");
            Debug.Log($"[SmartWall] bottomSpline.transform localPos={bottomSpline.transform.localPosition}");
            Debug.Log($"[SmartWall] topSpline.transform localPos={topSpline.transform.localPosition}");

            // Create bottom point (control point) - parented to SmartWall root, not Spline
            GameObject bottomPointObj = new GameObject($"CP_{bottomSpline.Points.Count}");
            bottomPointObj.transform.SetParent(transform); // Parent to SmartWall root
            position.y = 0; // Force to ground
            bottomPointObj.transform.localPosition = position;
            CozyPoint bottomPoint = bottomPointObj.AddComponent<CozyPoint>();
            bottomSpline.Points.Add(bottomPoint);

            Debug.Log($"[SmartWall] Created bottom point at localPos={position}, worldPos={bottomPointObj.transform.position}");

            // Create top point (height point) - parented to SmartWall root, not Spline
            GameObject topPointObj = new GameObject($"HP_{topSpline.Points.Count}");
            topPointObj.transform.SetParent(transform); // Parent to SmartWall root
            Vector3 topPos = position;
            topPos.y = wallHeight;
            topPointObj.transform.localPosition = topPos;
            CozyPoint topPoint = topPointObj.AddComponent<CozyPoint>();
            topSpline.Points.Add(topPoint);

            Debug.Log($"[SmartWall] Created top point at localPos={topPos}, worldPos={topPointObj.transform.position}");
            Debug.Log($"[SmartWall] Distance between bottom and top: {Vector3.Distance(bottomPointObj.transform.position, topPointObj.transform.position)}");

            int newIndex = bottomSpline.Points.Count - 1;

            // Create cursor for the new control point
            CreateCursorForControlPoint(newIndex);

            RefreshRenderer();

            return newIndex;
        }

        /// <summary>
        /// Create a cursor for a control point at runtime
        /// </summary>
        private void CreateCursorForControlPoint(int index)
        {
            Debug.Log($"[SmartWall] Creating cursor for control point {index}");

            GameObject cursorObj = new GameObject($"SmartWallCursor_{index}");

            // Parent to SmartWall root (same level as Splines)
            cursorObj.transform.SetParent(transform);

            // Get bottom point world position
            CozyPoint bottomPoint = bottomSpline.Points[index];
            Vector3 bottomWorldPos = bottomPoint.transform.position;

            // Set cursor world position at mid-height of wall
            cursorObj.transform.position = bottomWorldPos + new Vector3(0, wallHeight * 0.5f, 0);

            Debug.Log($"[SmartWall] Cursor {index} - bottomPoint world pos={bottomWorldPos}, cursorPos={cursorObj.transform.position}");

            SmartWallCursor cursor = cursorObj.AddComponent<SmartWallCursor>();
            cursor.SetSmartWall(this);
            cursor.SetControlPointIndex(index);

            // If this wall is currently active, show the cursor immediately
            var manager = SmartWallManager.Instance;
            if (manager != null && manager.GetActiveWall() == this)
            {
                // Cursor will call SetVisible(false) in Start(), so we need to override it after Start()
                // We'll set it visible in the next frame after Start() completes
                StartCoroutine(ShowCursorNextFrame(cursor));
            }

            Debug.Log($"[SmartWall] Cursor {index} created successfully");
        }

        private System.Collections.IEnumerator ShowCursorNextFrame(SmartWallCursor cursor)
        {
            yield return null; // Wait one frame for Start() to complete
            if (cursor != null && SmartWallManager.Instance.GetActiveWall() == this)
            {
                cursor.SetVisible(true);
            }
        }

        /// <summary>
        /// Insert control point between two existing points
        /// </summary>
        public int InsertControlPointAfter(int index)
        {
            if (bottomSpline == null || topSpline == null)
                return -1;

            if (index < 0 || index >= bottomSpline.Points.Count - 1)
                return -1;

            // Calculate midpoint position
            Vector3 pos0 = GetControlPointPosition(index);
            Vector3 pos1 = GetControlPointPosition(index + 1);
            Vector3 midPos = (pos0 + pos1) * 0.5f;

            // Create new points - parented to SmartWall root
            GameObject bottomPointObj = new GameObject($"CP_{index + 1}");
            bottomPointObj.transform.SetParent(transform); // Parent to SmartWall root
            midPos.y = 0;
            bottomPointObj.transform.localPosition = midPos;
            CozyPoint bottomPoint = bottomPointObj.AddComponent<CozyPoint>();

            GameObject topPointObj = new GameObject($"HP_{index + 1}");
            topPointObj.transform.SetParent(transform); // Parent to SmartWall root
            Vector3 topPos = midPos;
            topPos.y = wallHeight; // Use unified wall height
            topPointObj.transform.localPosition = topPos;
            CozyPoint topPoint = topPointObj.AddComponent<CozyPoint>();

            // Insert into splines
            bottomSpline.Points.Insert(index + 1, bottomPoint);
            topSpline.Points.Insert(index + 1, topPoint);

            // Update cursor indices for all cursors after the insertion point
            SmartWallCursor[] cursors = GetComponentsInChildren<SmartWallCursor>();
            foreach (var cursor in cursors)
            {
                if (cursor != null && cursor.GetControlPointIndex() > index)
                {
                    cursor.SetControlPointIndex(cursor.GetControlPointIndex() + 1);
                }
            }

            // Create cursor for the new control point
            CreateCursorForControlPoint(index + 1);

            RefreshRenderer();

            return index + 1;
        }

        /// <summary>
        /// Extend wall by adding a new point in the direction of the last segment
        /// </summary>
        public int ExtendWall(int fromIndex)
        {
            if (bottomSpline == null || topSpline == null)
                return -1;

            int count = bottomSpline.Points.Count;
            if (count < 1 || fromIndex < 0 || fromIndex >= count)
                return -1;

            Vector3 newPos;

            if (count == 1)
            {
                // If only one point, extend in +X direction
                newPos = GetControlPointPosition(0) + new Vector3(2f, 0, 0);
            }
            else
            {
                // Calculate direction from previous point to current point
                Vector3 currentPos = GetControlPointPosition(fromIndex);
                Vector3 direction;

                if (fromIndex == count - 1)
                {
                    // Last point - use direction from second-to-last to last
                    Vector3 prevPos = GetControlPointPosition(fromIndex - 1);
                    direction = (currentPos - prevPos).normalized;
                }
                else
                {
                    // Not last point - use direction to next point
                    Vector3 nextPos = GetControlPointPosition(fromIndex + 1);
                    direction = (nextPos - currentPos).normalized;
                }

                // Extend 2 meters in that direction
                newPos = currentPos + direction * 2f;
            }

            return AddControlPointAt(newPos);
        }

        /// <summary>
        /// Delete control point at index (must keep at least 2 points)
        /// </summary>
        public bool DeleteControlPoint(int index)
        {
            if (bottomSpline == null || topSpline == null)
                return false;

            // Must keep at least 2 points to form a wall
            if (bottomSpline.Points.Count <= 2)
            {
                Debug.LogWarning("Cannot delete control point - minimum 2 points required for wall");
                return false;
            }

            if (index < 0 || index >= bottomSpline.Points.Count)
                return false;

            // Find and destroy the cursor for this control point
            SmartWallCursor[] cursors = GetComponentsInChildren<SmartWallCursor>();
            foreach (var cursor in cursors)
            {
                if (cursor.GetControlPointIndex() == index)
                {
                    Destroy(cursor.gameObject);
                    break;
                }
            }

            // Destroy GameObjects
            if (bottomSpline.Points[index] != null)
                Destroy(bottomSpline.Points[index].gameObject);

            if (topSpline.Points[index] != null)
                Destroy(topSpline.Points[index].gameObject);

            // Remove from lists
            bottomSpline.Points.RemoveAt(index);
            topSpline.Points.RemoveAt(index);

            // Update cursor indices for all cursors after the deleted one
            foreach (var cursor in cursors)
            {
                if (cursor != null && cursor.GetControlPointIndex() > index)
                {
                    cursor.SetControlPointIndex(cursor.GetControlPointIndex() - 1);
                }
            }

            RefreshRenderer();

            return true;
        }

        /// <summary>
        /// Get all control point positions for serialization
        /// </summary>
        public List<Vector3> GetAllControlPointPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            if (bottomSpline != null)
            {
                foreach (var point in bottomSpline.Points)
                {
                    if (point != null)
                        positions.Add(point.transform.localPosition);
                }
            }
            return positions;
        }

        /// <summary>
        /// Restore wall from saved data
        /// </summary>
        public void RestoreFromData(List<Vector3> controlPoints, float savedWallHeight)
        {
            if (bottomSpline == null || topSpline == null)
                return;

            if (controlPoints == null || controlPoints.Count < 2)
                return;

            // Set wall height first
            wallHeight = savedWallHeight;

            // Clear existing points
            ClearAllPoints();

            // Recreate points from data
            for (int i = 0; i < controlPoints.Count; i++)
            {
                AddControlPointAt(controlPoints[i]);
            }
        }

        /// <summary>
        /// Clear all control points
        /// </summary>
        private void ClearAllPoints()
        {
            if (bottomSpline != null)
            {
                foreach (var point in bottomSpline.Points)
                {
                    if (point != null)
                        Destroy(point.gameObject);
                }
                bottomSpline.Points.Clear();
            }

            if (topSpline != null)
            {
                foreach (var point in topSpline.Points)
                {
                    if (point != null)
                        Destroy(point.gameObject);
                }
                topSpline.Points.Clear();
            }
        }

        /// <summary>
        /// Force refresh the Cozy renderer and update colliders
        /// </summary>
        private void RefreshRenderer()
        {
            if (cozyRenderer != null)
            {
                // CozyRenderer automatically updates when spline hash changes
                // But we can force an update if needed
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            // Update colliders after mesh changes
            UpdateColliders();
        }

        /// <summary>
        /// Generate BoxColliders between each pair of control points
        /// </summary>
        private void UpdateColliders()
        {
            // Remove all existing segment collider children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("WallSegment_"))
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }

            if (bottomSpline == null || bottomSpline.Points.Count < 2)
            {
                Debug.LogWarning($"[SmartWall] UpdateColliders: Not enough control points on {gameObject.name}");
                return;
            }

            // Create BoxCollider for each wall segment
            int segmentCount = bottomSpline.Points.Count - 1;
            Debug.Log($"[SmartWall] Creating {segmentCount} BoxColliders on {gameObject.name}");

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 p0 = GetControlPointPosition(i);
                Vector3 p1 = GetControlPointPosition(i + 1);

                CreateSegmentCollider(i, p0, p1, wallHeight);
            }
        }

        /// <summary>
        /// Create a properly oriented BoxCollider for a wall segment using a child GameObject
        /// </summary>
        private void CreateSegmentCollider(int segmentIndex, Vector3 p0, Vector3 p1, float height)
        {
            // Create child GameObject for this segment
            GameObject segmentObj = new GameObject($"WallSegment_{segmentIndex}");
            segmentObj.transform.SetParent(transform);

            // Calculate segment properties (p0 and p1 are in local space)
            Vector3 direction = p1 - p0;
            direction.y = 0; // Ensure horizontal
            float length = direction.magnitude;

            if (length < 0.01f)
            {
                Destroy(segmentObj);
                return;
            }

            // Calculate midpoint in local space
            Vector3 midpoint = (p0 + p1) * 0.5f;

            // Position the segment object at midpoint, height at half wall height
            segmentObj.transform.localPosition = new Vector3(midpoint.x, height * 0.5f, midpoint.z);

            // Rotate to align with segment direction
            // LookRotation makes the Z-axis point in the direction
            Vector3 directionNormalized = direction.normalized;
            segmentObj.transform.localRotation = Quaternion.LookRotation(directionNormalized);

            // Add BoxCollider centered at origin (of the segment object)
            // Size: X = thickness (wall depth), Y = height, Z = length (along segment)
            BoxCollider boxCollider = segmentObj.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(0.3f, height, length); // Thickness along X, height along Y, length along Z

            Debug.Log($"[SmartWall] Created segment collider {segmentIndex}: p0={p0}, p1={p1}, midpoint={midpoint}, length={length:F2}, rotation={segmentObj.transform.localRotation.eulerAngles}");
        }

        private void OnValidate()
        {
            minHeight = Mathf.Max(0.1f, minHeight);
            maxHeight = Mathf.Max(minHeight, maxHeight);
            snapInterval = Mathf.Max(0.05f, snapInterval);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw control points and segments for debugging
            if (bottomSpline == null || bottomSpline.Points.Count < 2)
                return;

            Gizmos.color = Color.yellow;

            // Draw control points
            for (int i = 0; i < bottomSpline.Points.Count; i++)
            {
                Vector3 cpPos = transform.TransformPoint(GetControlPointPosition(i));
                Gizmos.DrawSphere(cpPos, 0.1f);
            }

            // Draw segments
            Gizmos.color = Color.cyan;
            for (int i = 0; i < bottomSpline.Points.Count - 1; i++)
            {
                Vector3 p0 = transform.TransformPoint(GetControlPointPosition(i));
                Vector3 p1 = transform.TransformPoint(GetControlPointPosition(i + 1));

                // Draw line at ground level
                Gizmos.DrawLine(p0, p1);

                // Draw line at top of wall
                Vector3 p0Top = p0 + Vector3.up * wallHeight;
                Vector3 p1Top = p1 + Vector3.up * wallHeight;
                Gizmos.DrawLine(p0Top, p1Top);

                // Draw vertical lines
                Gizmos.DrawLine(p0, p0Top);
                Gizmos.DrawLine(p1, p1Top);
            }
        }
#endif
    }
}
