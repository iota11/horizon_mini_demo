using UnityEngine;
using System.Collections.Generic;

namespace HorizonMini.Build
{
    /// <summary>
    /// Smart terrain chunk that fills a volume with 1m³ prefab instances
    /// Similar to SmartTerrain but uses multiple small prefabs instead of one large mesh
    /// </summary>
    public class SmartTerrainChunk : MonoBehaviour
    {
        [Header("Control Point")]
        [Tooltip("Control point for XYZ size")]
        public Transform xyzControlPoint;

        [Header("Chunk Prefabs")]
        [Tooltip("List of 1m³ prefab variants to randomly place in the grid")]
        [SerializeField] private GameObject[] chunkPrefabs;

        [Header("Settings")]
        [SerializeField] private Vector3 minSize = new Vector3(1f, 1f, 1f);
        [SerializeField] private Vector3 maxSize = new Vector3(20f, 20f, 20f);
        [SerializeField] private float chunkSize = 1f; // Size of each chunk (1m)
        [SerializeField] private bool autoUpdate = true;

        private Transform chunksContainer; // Parent for all chunk instances
        private Vector3 lastControlPointLocalPosition;
        private Vector3 lastWorldPosition; // Track position changes for seed update
        private bool isDirty = false;
        private System.Random random;
        private BoxCollider boxCollider; // Dynamic box collider based on chunks

        private void Awake()
        {
            // Create control point if it doesn't exist
            if (xyzControlPoint == null)
                CreateXYZControlPoint();

            // Create container for chunk instances
            if (chunksContainer == null)
            {
                GameObject containerObj = new GameObject("ChunksContainer");
                containerObj.transform.SetParent(transform);
                containerObj.transform.localPosition = Vector3.zero;
                containerObj.transform.localRotation = Quaternion.identity;
                chunksContainer = containerObj.transform;
            }

            // Initialize random with seed based on object position for consistency
            lastWorldPosition = transform.position;
            UpdateRandomSeed();

            // Get or create BoxCollider
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            UpdateChunks();
        }

        private void CreateXYZControlPoint()
        {
            GameObject cpObj = new GameObject("XYZControlPoint");
            cpObj.transform.SetParent(transform);
            cpObj.transform.localPosition = new Vector3(3f, 2f, 3f); // Default size
            xyzControlPoint = cpObj.transform;

            // Hide control point by default (only shown in Edit mode)
            cpObj.SetActive(false);
        }

        private void Update()
        {
            if (autoUpdate)
            {
                // Check if world position changed (snapped to new location)
                if (transform.position != lastWorldPosition)
                {
                    lastWorldPosition = transform.position;
                    UpdateRandomSeed();
                    isDirty = true;
                }

                // Check if control point moved
                if (xyzControlPoint != null && xyzControlPoint.localPosition != lastControlPointLocalPosition)
                {
                    lastControlPointLocalPosition = xyzControlPoint.localPosition;
                    isDirty = true;
                }

                if (isDirty)
                {
                    UpdateChunks();
                    isDirty = false;
                }
            }
        }

        /// <summary>
        /// Update chunk instances based on control point position
        /// </summary>
        public void UpdateChunks()
        {
            if (xyzControlPoint == null || chunksContainer == null)
                return;

            // Clear existing chunks
            ClearChunks();

            // Calculate size from control point
            Vector3 size = CalculateSizeFromControlPoint();

            // Generate chunks to fill the volume
            GenerateChunks(size);

            // Update BoxCollider to match generated chunks
            UpdateBoxCollider();

            // Refresh bounding box in SelectionSystem
            HorizonMini.Build.SelectionSystem selectionSystem = FindFirstObjectByType<HorizonMini.Build.SelectionSystem>();
            if (selectionSystem != null)
            {
                selectionSystem.RefreshBoundingBox();
            }
        }

        private Vector3 CalculateSizeFromControlPoint()
        {
            if (xyzControlPoint == null)
                return minSize;

            Vector3 cpPos = xyzControlPoint.localPosition;
            float sizeX = Mathf.Abs(cpPos.x) * 2f; // Symmetric
            float sizeY = Mathf.Abs(cpPos.y);
            float sizeZ = Mathf.Abs(cpPos.z) * 2f; // Symmetric

            // Clamp to min/max
            sizeX = Mathf.Clamp(sizeX, minSize.x, maxSize.x);
            sizeY = Mathf.Clamp(sizeY, minSize.y, maxSize.y);
            sizeZ = Mathf.Clamp(sizeZ, minSize.z, maxSize.z);

            return new Vector3(sizeX, sizeY, sizeZ);
        }

        private void GenerateChunks(Vector3 size)
        {
            if (chunkPrefabs == null || chunkPrefabs.Length == 0)
            {
                Debug.LogWarning($"[SmartTerrainChunk] No chunk prefabs assigned on {gameObject.name}");
                return;
            }

            // Calculate number of chunks in each dimension
            int countX = Mathf.FloorToInt(size.x / chunkSize);
            int countY = Mathf.FloorToInt(size.y / chunkSize);
            int countZ = Mathf.FloorToInt(size.z / chunkSize);

            // Starting position (corner of the volume)
            float startX = -size.x * 0.5f + chunkSize * 0.5f;
            float startY = chunkSize * 0.5f; // Start at half chunk height above ground
            float startZ = -size.z * 0.5f + chunkSize * 0.5f;

            // Generate grid of chunks
            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    for (int z = 0; z < countZ; z++)
                    {
                        // Calculate position for this chunk
                        Vector3 chunkLocalPos = new Vector3(
                            startX + x * chunkSize,
                            startY + y * chunkSize,
                            startZ + z * chunkSize
                        );

                        // Randomly select a prefab
                        GameObject prefab = chunkPrefabs[random.Next(chunkPrefabs.Length)];

                        // Randomly select rotation in 90-degree increments (0, 90, 180, 270)
                        int rotationSteps = random.Next(4); // 0-3
                        float yRotation = rotationSteps * 90f;
                        Quaternion chunkRotation = Quaternion.Euler(0f, yRotation, 0f);

                        // Instantiate chunk
                        GameObject chunk = Instantiate(prefab, chunksContainer);
                        chunk.transform.localPosition = chunkLocalPos;
                        chunk.transform.localRotation = chunkRotation;
                        chunk.name = $"Chunk_{x}_{y}_{z}";
                    }
                }
            }
        }

        private void ClearChunks()
        {
            if (chunksContainer == null)
                return;

            // Destroy all child chunks
            int childCount = chunksContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = chunksContainer.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// Update BoxCollider to match bounds of generated chunks
        /// </summary>
        private void UpdateBoxCollider()
        {
            if (boxCollider == null)
                return;

            // If no chunks generated yet (e.g., no chunk prefabs assigned), use default size
            if (chunksContainer == null || chunksContainer.childCount == 0)
            {
                Vector3 size = CalculateSizeFromControlPoint();
                boxCollider.center = new Vector3(0f, size.y * 0.5f, 0f);
                boxCollider.size = size;
                return;
            }

            // Get all renderers in chunks
            Renderer[] renderers = chunksContainer.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                // Fallback to default size
                Vector3 size = CalculateSizeFromControlPoint();
                boxCollider.center = new Vector3(0f, size.y * 0.5f, 0f);
                boxCollider.size = size;
                return;
            }

            // Calculate combined bounds in local space
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool firstRenderer = true;

            foreach (Renderer renderer in renderers)
            {
                // Get world space bounds
                Bounds worldBounds = renderer.bounds;

                // Get 8 corners of world bounds
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                Vector3[] worldCorners = new Vector3[8]
                {
                    center + new Vector3(-extents.x, -extents.y, -extents.z),
                    center + new Vector3(+extents.x, -extents.y, -extents.z),
                    center + new Vector3(+extents.x, -extents.y, +extents.z),
                    center + new Vector3(-extents.x, -extents.y, +extents.z),
                    center + new Vector3(-extents.x, +extents.y, -extents.z),
                    center + new Vector3(+extents.x, +extents.y, -extents.z),
                    center + new Vector3(+extents.x, +extents.y, +extents.z),
                    center + new Vector3(-extents.x, +extents.y, +extents.z)
                };

                // Transform to local space and encapsulate
                foreach (Vector3 worldCorner in worldCorners)
                {
                    Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
                    if (firstRenderer)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        firstRenderer = false;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            // Update BoxCollider
            boxCollider.center = localBounds.center;
            boxCollider.size = localBounds.size;
        }

        /// <summary>
        /// Update random seed based on object's world position
        /// </summary>
        private void UpdateRandomSeed()
        {
            // Generate seed from world position (grid-snapped to 0.5m)
            // This ensures same position always gets same seed
            Vector3 pos = transform.position;

            // Snap to grid to avoid floating point issues
            int x = Mathf.RoundToInt(pos.x * 2f); // *2 for 0.5m grid
            int y = Mathf.RoundToInt(pos.y * 2f);
            int z = Mathf.RoundToInt(pos.z * 2f);

            // Combine XYZ into a single seed using prime number multiplication
            int seed = x * 73856093 ^ y * 19349663 ^ z * 83492791;

            random = new System.Random(seed);
        }

        /// <summary>
        /// Get current chunk volume size
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
            if (xyzControlPoint != null)
            {
                xyzControlPoint.localPosition = localPosition;
                isDirty = true;

                if (forceImmediate)
                    UpdateChunks();
            }
        }

        /// <summary>
        /// Get control point position (in local space)
        /// </summary>
        public Vector3 GetControlPointPosition()
        {
            return xyzControlPoint != null ? xyzControlPoint.localPosition : Vector3.zero;
        }

        /// <summary>
        /// Show or hide control point visuals (for Edit/View mode)
        /// </summary>
        public void SetControlPointVisible(bool visible)
        {
            if (xyzControlPoint != null)
                xyzControlPoint.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Check if control point is currently visible
        /// </summary>
        public bool IsControlPointVisible()
        {
            return xyzControlPoint != null && xyzControlPoint.gameObject.activeSelf;
        }

        /// <summary>
        /// Called by FullControlPointCursor when control point is moved
        /// </summary>
        public void OnControlPointPositionChanged(Vector3 newWorldPosition)
        {
            Vector3 localPos = transform.InverseTransformPoint(newWorldPosition);
            SetControlPointPosition(localPos);
        }

        private void OnValidate()
        {
            // Ensure min/max constraints are valid
            minSize.x = Mathf.Max(1f, minSize.x);
            minSize.y = Mathf.Max(1f, minSize.y);
            minSize.z = Mathf.Max(1f, minSize.z);

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
            if (xyzControlPoint != null)
            {
                // Draw XYZ control point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(xyzControlPoint.position, 0.3f);
                Gizmos.DrawLine(transform.position, xyzControlPoint.position);
            }

            // Draw volume bounds
            Vector3 size = CalculateSizeFromControlPoint();
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, size.y * 0.5f, 0), size);
        }
#endif
    }
}
