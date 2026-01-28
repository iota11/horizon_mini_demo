using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Smart procedural house with control points
    /// - XZ control point: controls horizontal size/position
    /// - Y control point 1: controls floor height
    /// - Y control point 2: controls roof height
    /// </summary>
    public class SmartHouse : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private Material houseMaterial;

        [Header("Control Points")]
        [Tooltip("Control point for XZ size (horizontal dimensions)")]
        public Transform xzControlPoint;

        [Tooltip("Control point for floor/wall height (Y1)")]
        public Transform yControlPoint1;

        [Tooltip("Control point for roof height (Y2)")]
        public Transform yControlPoint2;

        [Header("Settings")]
        [SerializeField] private Vector3 minSize = new Vector3(2f, 1.5f, 2f);
        [SerializeField] private Vector3 maxSize = new Vector3(20f, 10f, 20f);
        [SerializeField] private bool autoUpdateMesh = true;

        private Mesh houseMesh;
        private Vector3 lastXZControlPointLocalPosition;
        private Vector3 lastY1ControlPointLocalPosition;
        private Vector3 lastY2ControlPointLocalPosition;
        private bool isDirty = false;

        private void Awake()
        {
            // Auto-find or create components
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();

            // Create control points if they don't exist
            if (xzControlPoint == null)
                CreateXZControlPoint();

            if (yControlPoint1 == null)
                CreateYControlPoint1();

            if (yControlPoint2 == null)
                CreateYControlPoint2();

            // Initialize mesh
            if (houseMesh == null)
            {
                houseMesh = new Mesh();
                houseMesh.name = "SmartHouseMesh";
                if (meshFilter != null)
                    meshFilter.mesh = houseMesh;
            }

            // Apply material
            if (meshRenderer != null)
            {
                if (houseMaterial != null)
                {
                    meshRenderer.material = houseMaterial;
                }
                else
                {
                    // Create default URP material if none assigned
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                    {
                        houseMaterial = new Material(urpShader);
                        houseMaterial.name = "DefaultHouseMaterial";
                        houseMaterial.color = new Color(0.9f, 0.85f, 0.7f); // Beige color
                        meshRenderer.material = houseMaterial;
                    }
                }
            }

            UpdateMesh();

            // Register with SmartHouseManager if exists
            var manager = SmartHouseManager.Instance;
            if (manager != null)
            {
                manager.RegisterHouse(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from manager
            if (SmartHouseManager.Instance != null)
            {
                SmartHouseManager.Instance.UnregisterHouse(this);
            }
        }

        private void CreateXZControlPoint()
        {
            GameObject cpObj = new GameObject("XZControlPoint");
            cpObj.transform.SetParent(transform);
            cpObj.transform.localPosition = new Vector3(3f, 0f, 3f); // Default horizontal size
            xzControlPoint = cpObj.transform;

            // Hide control point by default (only shown in Edit mode)
            cpObj.SetActive(false);
        }

        private void CreateYControlPoint1()
        {
            GameObject cpObj = new GameObject("YControlPoint1");
            cpObj.transform.SetParent(transform);
            cpObj.transform.localPosition = new Vector3(0f, 2f, 0f); // Default floor/wall height
            yControlPoint1 = cpObj.transform;

            // Hide control point by default (only shown in Edit mode)
            cpObj.SetActive(false);
        }

        private void CreateYControlPoint2()
        {
            GameObject cpObj = new GameObject("YControlPoint2");
            cpObj.transform.SetParent(transform);
            cpObj.transform.localPosition = new Vector3(0f, 4f, 0f); // Default roof height
            yControlPoint2 = cpObj.transform;

            // Hide control point by default (only shown in Edit mode)
            cpObj.SetActive(false);
        }

        private void Update()
        {
            if (autoUpdateMesh)
            {
                // Check if any control point moved
                bool changed = false;

                if (xzControlPoint != null && xzControlPoint.localPosition != lastXZControlPointLocalPosition)
                {
                    lastXZControlPointLocalPosition = xzControlPoint.localPosition;
                    changed = true;
                }

                if (yControlPoint1 != null && yControlPoint1.localPosition != lastY1ControlPointLocalPosition)
                {
                    lastY1ControlPointLocalPosition = yControlPoint1.localPosition;
                    changed = true;
                }

                if (yControlPoint2 != null && yControlPoint2.localPosition != lastY2ControlPointLocalPosition)
                {
                    lastY2ControlPointLocalPosition = yControlPoint2.localPosition;
                    changed = true;
                }

                if (changed)
                {
                    isDirty = true;
                }

                if (isDirty)
                {
                    UpdateMesh();
                    isDirty = false;
                }
            }
        }

        /// <summary>
        /// Update the house mesh based on control point positions
        /// </summary>
        public void UpdateMesh()
        {
            if (xzControlPoint == null || yControlPoint1 == null || yControlPoint2 == null || houseMesh == null)
                return;

            // Calculate dimensions from control points
            Vector3 size = CalculateSizeFromControlPoints();

            // Generate house mesh (simple box for now, can be extended to include roof)
            GenerateHouseMesh(size);

            // Update box collider
            UpdateBoxCollider(size);
        }

        private Vector3 CalculateSizeFromControlPoints()
        {
            // XZ size from xzControlPoint
            Vector3 xzPos = xzControlPoint.localPosition;
            float sizeX = Mathf.Abs(xzPos.x) * 2f; // Symmetric
            float sizeZ = Mathf.Abs(xzPos.z) * 2f; // Symmetric

            // Y sizes from yControlPoints
            float floorHeight = Mathf.Abs(yControlPoint1.localPosition.y);
            float roofHeight = Mathf.Abs(yControlPoint2.localPosition.y);

            // Total height is max of the two Y control points
            float sizeY = Mathf.Max(floorHeight, roofHeight);

            // Clamp to min/max
            sizeX = Mathf.Clamp(sizeX, minSize.x, maxSize.x);
            sizeY = Mathf.Clamp(sizeY, minSize.y, maxSize.y);
            sizeZ = Mathf.Clamp(sizeZ, minSize.z, maxSize.z);

            return new Vector3(sizeX, sizeY, sizeZ);
        }

        private void GenerateHouseMesh(Vector3 size)
        {
            if (houseMesh == null)
                return;

            houseMesh.Clear();

            // Calculate half extents
            float hx = size.x * 0.5f;
            float hy = size.y;
            float hz = size.z * 0.5f;

            // Simple box mesh (similar to SmartTerrain)
            Vector3[] vertices = new Vector3[]
            {
                // Bottom face (y=0)
                new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz),
                // Top face (y=height)
                new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz),
                // Front face (-z)
                new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz),
                // Back face (+z)
                new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz),
                // Left face (-x)
                new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz),
                // Right face (+x)
                new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz)
            };

            // UVs
            Vector2[] uvs = new Vector2[]
            {
                // Bottom
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Top
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Front
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Back
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Left
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Right
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };

            // Triangles
            int[] triangles = new int[]
            {
                // Bottom (facing down)
                0, 1, 2, 0, 2, 3,
                // Top (facing up)
                4, 6, 5, 4, 7, 6,
                // Front
                8, 10, 9, 8, 11, 10,
                // Back
                12, 13, 14, 12, 14, 15,
                // Left
                16, 17, 18, 16, 18, 19,
                // Right
                20, 22, 21, 20, 23, 22
            };

            houseMesh.vertices = vertices;
            houseMesh.uv = uvs;
            houseMesh.triangles = triangles;
            houseMesh.RecalculateNormals();
            houseMesh.RecalculateBounds();
        }

        private void UpdateBoxCollider(Vector3 size)
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = gameObject.AddComponent<BoxCollider>();
                }
            }

            boxCollider.center = new Vector3(0f, size.y * 0.5f, 0f);
            boxCollider.size = size;
        }

        /// <summary>
        /// Get current house size
        /// </summary>
        public Vector3 GetSize()
        {
            return CalculateSizeFromControlPoints();
        }

        /// <summary>
        /// Set control point positions (in local space)
        /// </summary>
        public void SetXZControlPointPosition(Vector3 localPosition, bool forceImmediate = false)
        {
            if (xzControlPoint != null)
            {
                xzControlPoint.localPosition = localPosition;
                isDirty = true;

                if (forceImmediate)
                    UpdateMesh();
            }
        }

        public void SetY1ControlPointPosition(Vector3 localPosition, bool forceImmediate = false)
        {
            if (yControlPoint1 != null)
            {
                yControlPoint1.localPosition = localPosition;
                isDirty = true;

                if (forceImmediate)
                    UpdateMesh();
            }
        }

        public void SetY2ControlPointPosition(Vector3 localPosition, bool forceImmediate = false)
        {
            if (yControlPoint2 != null)
            {
                yControlPoint2.localPosition = localPosition;
                isDirty = true;

                if (forceImmediate)
                    UpdateMesh();
            }
        }

        /// <summary>
        /// Get control point positions (in local space)
        /// </summary>
        public Vector3 GetXZControlPointPosition()
        {
            return xzControlPoint != null ? xzControlPoint.localPosition : Vector3.zero;
        }

        public Vector3 GetY1ControlPointPosition()
        {
            return yControlPoint1 != null ? yControlPoint1.localPosition : Vector3.zero;
        }

        public Vector3 GetY2ControlPointPosition()
        {
            return yControlPoint2 != null ? yControlPoint2.localPosition : Vector3.zero;
        }

        /// <summary>
        /// Show or hide control point visuals (for Edit/View mode)
        /// </summary>
        public void SetControlPointsVisible(bool visible)
        {
            if (xzControlPoint != null)
                xzControlPoint.gameObject.SetActive(visible);

            if (yControlPoint1 != null)
                yControlPoint1.gameObject.SetActive(visible);

            if (yControlPoint2 != null)
                yControlPoint2.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Check if control points are currently visible
        /// </summary>
        public bool AreControlPointsVisible()
        {
            return xzControlPoint != null && xzControlPoint.gameObject.activeSelf;
        }

        /// <summary>
        /// Called by FullControlPointCursor when XZ control point is moved
        /// </summary>
        public void OnXZControlPointPositionChanged(Vector3 newWorldPosition)
        {
            Vector3 localPos = transform.InverseTransformPoint(newWorldPosition);
            SetXZControlPointPosition(localPos);
        }

        /// <summary>
        /// Called by HeightControlPointCursor when Y1 control point is moved
        /// </summary>
        public void OnY1ControlPointPositionChanged(Vector3 newWorldPosition)
        {
            // Only update Y, keep XZ unchanged
            Vector3 currentLocalPos = yControlPoint1.localPosition;
            Vector3 newLocalPos = transform.InverseTransformPoint(newWorldPosition);
            currentLocalPos.y = newLocalPos.y;
            SetY1ControlPointPosition(currentLocalPos);
        }

        /// <summary>
        /// Called by HeightControlPointCursor when Y2 control point is moved
        /// </summary>
        public void OnY2ControlPointPositionChanged(Vector3 newWorldPosition)
        {
            // Only update Y, keep XZ unchanged
            Vector3 currentLocalPos = yControlPoint2.localPosition;
            Vector3 newLocalPos = transform.InverseTransformPoint(newWorldPosition);
            currentLocalPos.y = newLocalPos.y;
            SetY2ControlPointPosition(currentLocalPos);
        }

        private void OnValidate()
        {
            // Ensure min/max constraints are valid
            minSize.x = Mathf.Max(0.5f, minSize.x);
            minSize.y = Mathf.Max(0.5f, minSize.y);
            minSize.z = Mathf.Max(0.5f, minSize.z);

            maxSize.x = Mathf.Max(minSize.x, maxSize.x);
            maxSize.y = Mathf.Max(minSize.y, maxSize.y);
            maxSize.z = Mathf.Max(minSize.z, maxSize.z);

            if (Application.isPlaying)
            {
                isDirty = true;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (xzControlPoint != null)
            {
                // Draw XZ control point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(xzControlPoint.position, 0.3f);
                Gizmos.DrawLine(transform.position, xzControlPoint.position);
            }

            if (yControlPoint1 != null)
            {
                // Draw Y1 control point
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(yControlPoint1.position, 0.3f);
                Gizmos.DrawLine(transform.position, yControlPoint1.position);
            }

            if (yControlPoint2 != null)
            {
                // Draw Y2 control point
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(yControlPoint2.position, 0.3f);
                Gizmos.DrawLine(transform.position, yControlPoint2.position);
            }
        }
#endif
    }
}
