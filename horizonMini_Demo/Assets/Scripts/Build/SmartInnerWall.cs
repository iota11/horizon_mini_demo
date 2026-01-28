using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Smart procedural wall with control points
    /// Identical algorithm to SmartTerrain but with wall-like default state
    /// Size is determined by control point positions
    /// </summary>
    public class SmartInnerWall : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject controlPointPrefab;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private Material wallMaterial;

        [Header("Control Points")]
        [Tooltip("Control point that defines the wall size")]
        public Transform controlPoint;

        [Header("Settings")]
        [SerializeField] private Vector3 minSize = new Vector3(0.1f, 0.5f, 0.1f);
        [SerializeField] private Vector3 maxSize = new Vector3(50f, 20f, 50f);
        [SerializeField] private bool autoUpdateMesh = true;

        private Mesh wallMesh;
        private Vector3 lastControlPointLocalPosition;
        private bool isDirty = false;

        private void Awake()
        {
            // Create mesh if not assigned
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();

            // Create control point if it doesn't exist
            if (controlPoint == null)
            {
                CreateControlPoint();
            }

            // Initialize mesh
            if (wallMesh == null)
            {
                wallMesh = new Mesh();
                wallMesh.name = "SmartInnerWallMesh";
                if (meshFilter != null)
                    meshFilter.mesh = wallMesh;
            }

            // Apply material
            if (meshRenderer != null)
            {
                if (wallMaterial != null)
                {
                    meshRenderer.material = wallMaterial;
                }
                else
                {
                    // Create default URP material if none assigned
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                    {
                        wallMaterial = new Material(urpShader);
                        wallMaterial.name = "DefaultWallMaterial";
                        wallMaterial.color = new Color(0.9f, 0.9f, 0.85f); // Light beige color
                        meshRenderer.material = wallMaterial;
                    }
                }
            }

            UpdateMesh();
        }

        private void CreateControlPoint()
        {
            GameObject cpObj = new GameObject("ControlPoint");
            cpObj.transform.SetParent(transform);
            // Wall-like default: thin in one dimension, tall in Y
            cpObj.transform.localPosition = new Vector3(0.25f, 4f, 2f); // 0.5m thick, 4m tall, 4m wide
            controlPoint = cpObj.transform;

            // No visual - control point is just a transform for tracking position
            // Visual representation is handled by FullControlPointCursor in Edit mode

            // Hide control point by default (only shown in Edit mode)
            cpObj.SetActive(false);
        }

        private void Update()
        {
            if (autoUpdateMesh && controlPoint != null)
            {
                // Check if control point moved
                if (controlPoint.localPosition != lastControlPointLocalPosition)
                {
                    isDirty = true;
                    lastControlPointLocalPosition = controlPoint.localPosition;
                }

                if (isDirty)
                {
                    UpdateMesh();
                    isDirty = false;
                }
            }
        }

        /// <summary>
        /// Update the wall mesh based on control point position
        /// </summary>
        public void UpdateMesh()
        {
            if (controlPoint == null || wallMesh == null)
                return;

            // Calculate size from control point position
            Vector3 size = CalculateSizeFromControlPoint();

            // Generate cube mesh
            GenerateCubeMesh(size);

            // Update box collider
            UpdateBoxCollider(size);
        }

        private void UpdateBoxCollider(Vector3 size)
        {
            if (boxCollider == null)
                return;

            // Box collider centered at half height
            boxCollider.center = new Vector3(0f, size.y * 0.5f, 0f);
            boxCollider.size = size;
        }

        private Vector3 CalculateSizeFromControlPoint()
        {
            // Control point local position determines size
            // SAME FORMULA AS SmartTerrain
            Vector3 localPos = controlPoint.localPosition;

            // Use absolute values and clamp
            Vector3 size = new Vector3(
                Mathf.Abs(localPos.x) * 2f, // Symmetric in X
                Mathf.Abs(localPos.y),      // Only positive Y (NOT multiplied by 2!)
                Mathf.Abs(localPos.z) * 2f  // Symmetric in Z
            );

            // Clamp to min/max
            size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
            size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);
            size.z = Mathf.Clamp(size.z, minSize.z, maxSize.z);

            return size;
        }

        private void GenerateCubeMesh(Vector3 size)
        {
            if (wallMesh == null)
                return;

            wallMesh.Clear();

            // Calculate half extents
            float hx = size.x * 0.5f;
            float hy = size.y;
            float hz = size.z * 0.5f;

            // Vertices (box centered at origin, extending upward)
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

            // Triangles (two per face) - correct winding order for culling
            int[] triangles = new int[]
            {
                // Bottom (facing down - reversed winding)
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

            wallMesh.vertices = vertices;
            wallMesh.uv = uvs;
            wallMesh.triangles = triangles;
            wallMesh.RecalculateNormals();
            wallMesh.RecalculateBounds();
        }

        /// <summary>
        /// Get current wall size
        /// </summary>
        public Vector3 GetSize()
        {
            return CalculateSizeFromControlPoint();
        }

        /// <summary>
        /// Set control point position (in local space)
        /// </summary>
        public void SetControlPointPosition(Vector3 localPosition, bool forceImmediate = false)
        {
            if (controlPoint != null)
            {
                controlPoint.localPosition = localPosition;
                isDirty = true;

                // Force immediate mesh update when requested (e.g., during loading)
                if (forceImmediate)
                {
                    UpdateMesh();
                }
            }
        }

        /// <summary>
        /// Get control point position (in local space)
        /// </summary>
        public Vector3 GetControlPointPosition()
        {
            return controlPoint != null ? controlPoint.localPosition : Vector3.zero;
        }

        /// <summary>
        /// Show or hide control point visuals (for Edit/View mode)
        /// </summary>
        public void SetControlPointVisible(bool visible)
        {
            if (controlPoint != null)
            {
                controlPoint.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Check if control point is currently visible
        /// </summary>
        public bool IsControlPointVisible()
        {
            return controlPoint != null && controlPoint.gameObject.activeSelf;
        }

        private void OnDrawGizmos()
        {
            if (controlPoint != null)
            {
                // Draw line from origin to control point
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, controlPoint.position);

                // Draw control point sphere
                Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                Gizmos.DrawWireSphere(controlPoint.position, 0.3f);
            }
        }

        private void OnValidate()
        {
            // Ensure min/max constraints are valid
            minSize.x = Mathf.Max(0.1f, minSize.x);
            minSize.y = Mathf.Max(0.1f, minSize.y);
            minSize.z = Mathf.Max(0.1f, minSize.z);

            maxSize.x = Mathf.Max(minSize.x, maxSize.x);
            maxSize.y = Mathf.Max(minSize.y, maxSize.y);
            maxSize.z = Mathf.Max(minSize.z, maxSize.z);

            if (Application.isPlaying)
            {
                isDirty = true;
            }
        }

        /// <summary>
        /// Called by FullControlPointCursor when control point is moved
        /// </summary>
        public void OnControlPointPositionChanged(Vector3 newWorldPosition)
        {
            // Convert world position to local position
            Vector3 localPos = transform.InverseTransformPoint(newWorldPosition);

            // Update control point position
            SetControlPointPosition(localPos);

            // This will trigger mesh update via Update() loop
        }
    }
}
