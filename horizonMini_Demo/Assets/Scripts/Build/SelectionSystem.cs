using UnityEngine;
using HorizonMini.Controllers;

namespace HorizonMini.Build
{
    /// <summary>
    /// Manages object selection and visual feedback
    /// </summary>
    public class SelectionSystem : MonoBehaviour
    {
        [Header("Visual Feedback")]
        public Color bboxColor = Color.green;
        public float lineWidth = 0.02f;

        private BuildController buildController;
        private Camera cam;
        private PlacedObject currentSelection;
        private GameObject bboxWireframe;
        private bool isEnabled = false;

        public void Initialize(BuildController controller, Camera camera)
        {
            buildController = controller;
            cam = camera;
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public void SetSelection(PlacedObject obj)
        {
            ClearSelection();

            currentSelection = obj;

            if (obj != null)
            {
                CreateBBoxWireframe(obj);
            }
        }

        public void ClearSelection()
        {
            if (bboxWireframe != null)
            {
                Destroy(bboxWireframe);
                bboxWireframe = null;
            }

            currentSelection = null;
        }

        /// <summary>
        /// Refresh the bounding box wireframe (useful when object geometry changes)
        /// </summary>
        public void RefreshBoundingBox()
        {
            if (currentSelection != null && bboxWireframe != null)
            {
                // Destroy old wireframe and recreate
                GameObject oldWireframe = bboxWireframe;
                bboxWireframe = null;
                CreateBBoxWireframe(currentSelection);

                // Destroy old wireframe after creating new one to avoid null reference in Inspector
                if (oldWireframe != null)
                {
                    Destroy(oldWireframe);
                }
            }
        }

        private void CreateBBoxWireframe(PlacedObject obj)
        {
            // Get bounding box in local space (oriented bounding box)
            Bounds localBounds = GetLocalBounds(obj.gameObject);

            // Get rotation for SmartWall (oriented bounding box)
            Quaternion bboxRotation = Quaternion.identity;
            SmartWall wall = obj.GetComponent<SmartWall>();
            if (wall != null)
            {
                bboxRotation = GetSmartWallBBoxRotation(wall);
            }

            // Create wireframe cube
            bboxWireframe = new GameObject("BBoxWireframe");
            bboxWireframe.transform.SetParent(obj.transform);
            bboxWireframe.transform.localPosition = localBounds.center;
            bboxWireframe.transform.localRotation = bboxRotation;

            // Create line renderer for each edge of the bbox
            Vector3[] corners = GetBBoxCorners(localBounds);

            // Define 12 edges of a cube
            int[][] edges = new int[][]
            {
                // Bottom face
                new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0},
                // Top face
                new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4},
                // Vertical edges
                new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}
            };

            foreach (var edge in edges)
            {
                GameObject lineObj = new GameObject("Edge");
                lineObj.transform.SetParent(bboxWireframe.transform);
                lineObj.transform.localPosition = Vector3.zero;  // Reset to parent's origin
                lineObj.transform.localRotation = Quaternion.identity;

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.SetPosition(0, corners[edge[0]]);
                lr.SetPosition(1, corners[edge[1]]);

                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.useWorldSpace = false;

                // Use URP Unlit shader for consistent color
                Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                lineMat.SetColor("_BaseColor", bboxColor);
                lr.material = lineMat;
            }
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            // Get combined bounds from all renderers (world space)
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                return combinedBounds;
            }

            // Fallback to collider bounds
            Collider collider = obj.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            return new Bounds(obj.transform.position, Vector3.one);
        }

        private Bounds GetLocalBounds(GameObject obj)
        {
            // Special handling for SmartWall - calculate bounds from wall segments
            SmartWall wall = obj.GetComponent<SmartWall>();
            if (wall != null)
            {
                return GetSmartWallLocalBounds(wall);
            }

            // Special handling for SmartHouse - use BoxCollider bounds directly
            SmartHouse house = obj.GetComponent<SmartHouse>();
            if (house != null)
            {
                return GetSmartHouseLocalBounds(house);
            }

            // Special handling for SmartTerrainChunk - calculate bounds from control point
            SmartTerrainChunk chunk = obj.GetComponent<SmartTerrainChunk>();
            if (chunk != null)
            {
                return GetSmartTerrainChunkLocalBounds(chunk);
            }

            // Get all renderers in local space
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                // Fallback to collider bounds in local space
                Collider collider = obj.GetComponentInChildren<Collider>();
                if (collider != null)
                {
                    Bounds worldBounds = collider.bounds;
                    Vector3 localCenter = obj.transform.InverseTransformPoint(worldBounds.center);
                    Vector3 localSize = obj.transform.InverseTransformVector(worldBounds.size);
                    return new Bounds(localCenter, localSize);
                }
                return new Bounds(Vector3.zero, Vector3.one);
            }

            // Calculate local space bounds by transforming all vertices
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool firstVertex = true;

            foreach (Renderer renderer in renderers)
            {
                // Get world space bounds
                Bounds worldBounds = renderer.bounds;

                // Get 8 corners of world bounds
                Vector3[] worldCorners = new Vector3[8];
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                worldCorners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
                worldCorners[1] = center + new Vector3(+extents.x, -extents.y, -extents.z);
                worldCorners[2] = center + new Vector3(+extents.x, -extents.y, +extents.z);
                worldCorners[3] = center + new Vector3(-extents.x, -extents.y, +extents.z);
                worldCorners[4] = center + new Vector3(-extents.x, +extents.y, -extents.z);
                worldCorners[5] = center + new Vector3(+extents.x, +extents.y, -extents.z);
                worldCorners[6] = center + new Vector3(+extents.x, +extents.y, +extents.z);
                worldCorners[7] = center + new Vector3(-extents.x, +extents.y, +extents.z);

                // Transform to local space and encapsulate
                foreach (Vector3 worldCorner in worldCorners)
                {
                    Vector3 localCorner = obj.transform.InverseTransformPoint(worldCorner);
                    if (firstVertex)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        firstVertex = false;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            return localBounds;
        }

        private Bounds GetSmartWallLocalBounds(SmartWall wall)
        {
            // Calculate oriented bounding box from control points
            int controlPointCount = wall.GetControlPointCount();
            if (controlPointCount < 2)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            // Get wall properties
            float wallHeight = wall.GetWallHeight();
            float wallThickness = 0.3f;
            float halfThickness = wallThickness * 0.5f;

            // Find the minimum and maximum in local space along the wall's natural axes
            // First, determine the wall's oriented bounding box axes
            Vector3 minPoint = wall.GetControlPointPosition(0);
            Vector3 maxPoint = wall.GetControlPointPosition(0);

            // Get all control point positions to find extents
            for (int i = 1; i < controlPointCount; i++)
            {
                Vector3 cp = wall.GetControlPointPosition(i);
                minPoint = Vector3.Min(minPoint, cp);
                maxPoint = Vector3.Max(maxPoint, cp);
            }

            // Calculate center and size
            Vector3 center = (minPoint + maxPoint) * 0.5f;
            center.y = wallHeight * 0.5f; // Center Y at half height

            Vector3 size = maxPoint - minPoint;
            size.y = wallHeight;

            // Add thickness to X and Z
            size.x += wallThickness;
            size.z += wallThickness;

            // For a proper OBB, we need to find the longest direction of the wall path
            // Calculate the overall direction from first to last control point
            Vector3 wallDirection = wall.GetControlPointPosition(controlPointCount - 1) - wall.GetControlPointPosition(0);
            wallDirection.y = 0;

            if (wallDirection.sqrMagnitude > 0.001f)
            {
                wallDirection.Normalize();
                Vector3 perpDirection = new Vector3(-wallDirection.z, 0, wallDirection.x);

                // Project all control points onto the wall direction and perpendicular direction
                float minAlong = float.MaxValue, maxAlong = float.MinValue;
                float minPerp = float.MaxValue, maxPerp = float.MinValue;

                for (int i = 0; i < controlPointCount; i++)
                {
                    Vector3 cp = wall.GetControlPointPosition(i);
                    float alongDist = Vector3.Dot(cp, wallDirection);
                    float perpDist = Vector3.Dot(cp, perpDirection);

                    minAlong = Mathf.Min(minAlong, alongDist);
                    maxAlong = Mathf.Max(maxAlong, alongDist);
                    minPerp = Mathf.Min(minPerp, perpDist);
                    maxPerp = Mathf.Max(maxPerp, perpDist);
                }

                // Reconstruct center and size in oriented space
                float centerAlong = (minAlong + maxAlong) * 0.5f;
                float centerPerp = (minPerp + maxPerp) * 0.5f;
                center = wallDirection * centerAlong + perpDirection * centerPerp;
                center.y = wallHeight * 0.5f;

                float sizeAlong = (maxAlong - minAlong) + wallThickness;
                float sizePerp = (maxPerp - minPerp) + wallThickness;

                size.x = sizeAlong;
                size.z = sizePerp;
                size.y = wallHeight;
            }

            return new Bounds(center, size);
        }

        private Quaternion GetSmartWallBBoxRotation(SmartWall wall)
        {
            int controlPointCount = wall.GetControlPointCount();
            if (controlPointCount < 2)
            {
                return Quaternion.identity;
            }

            // Calculate rotation from first to last control point
            Vector3 wallDirection = wall.GetControlPointPosition(controlPointCount - 1) - wall.GetControlPointPosition(0);
            wallDirection.y = 0;

            if (wallDirection.sqrMagnitude < 0.001f)
            {
                return Quaternion.identity;
            }

            // Rotation that aligns X-axis with wall direction (bbox size.x is the long dimension)
            // LookRotation aligns Z-axis, so we rotate by -90 degrees around Y to align X-axis
            Vector3 perpDirection = new Vector3(-wallDirection.z, 0, wallDirection.x).normalized;
            return Quaternion.LookRotation(perpDirection, Vector3.up);
        }

        private Bounds GetSmartHouseLocalBounds(SmartHouse house)
        {
            // Use local positions (not affected by rotation)
            Vector3 xzLocalPos = house.xzControlPoint != null ? house.xzControlPoint.localPosition : new Vector3(3f, 0f, 3f);
            Vector3 y1LocalPos = house.yControlPoint1 != null ? house.yControlPoint1.localPosition : new Vector3(0f, 2f, 0f);
            Vector3 y2LocalPos = house.yControlPoint2 != null ? house.yControlPoint2.localPosition : new Vector3(0f, 4f, 0f);

            // Calculate size from local positions (symmetric in XZ)
            float sizeX = Mathf.Abs(xzLocalPos.x) * 2f;
            float sizeZ = Mathf.Abs(xzLocalPos.z) * 2f;
            float sizeY = Mathf.Max(Mathf.Abs(y1LocalPos.y), Mathf.Abs(y2LocalPos.y));

            // Bounds center in local space
            Vector3 center = new Vector3(0f, sizeY * 0.5f, 0f);
            Vector3 size = new Vector3(sizeX, sizeY, sizeZ);

            return new Bounds(center, size);
        }

        private Bounds GetSmartTerrainChunkLocalBounds(SmartTerrainChunk chunk)
        {
            // Calculate bounds from actual generated chunks (not control point)
            Transform chunksContainer = chunk.transform.Find("ChunksContainer");
            if (chunksContainer == null || chunksContainer.childCount == 0)
            {
                // Fallback: use default size if no chunks generated yet
                return new Bounds(Vector3.zero, Vector3.one);
            }

            // Get all renderers in chunks
            Renderer[] renderers = chunksContainer.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            // Calculate local space bounds by transforming all vertices
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool firstVertex = true;

            foreach (Renderer renderer in renderers)
            {
                // Get world space bounds
                Bounds worldBounds = renderer.bounds;

                // Get 8 corners of world bounds
                Vector3[] worldCorners = new Vector3[8];
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;

                worldCorners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
                worldCorners[1] = center + new Vector3(+extents.x, -extents.y, -extents.z);
                worldCorners[2] = center + new Vector3(+extents.x, -extents.y, +extents.z);
                worldCorners[3] = center + new Vector3(-extents.x, -extents.y, +extents.z);
                worldCorners[4] = center + new Vector3(-extents.x, +extents.y, -extents.z);
                worldCorners[5] = center + new Vector3(+extents.x, +extents.y, -extents.z);
                worldCorners[6] = center + new Vector3(+extents.x, +extents.y, +extents.z);
                worldCorners[7] = center + new Vector3(-extents.x, +extents.y, +extents.z);

                // Transform to local space and encapsulate
                foreach (Vector3 worldCorner in worldCorners)
                {
                    Vector3 localCorner = chunk.transform.InverseTransformPoint(worldCorner);
                    if (firstVertex)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        firstVertex = false;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            return localBounds;
        }

        private Vector3[] GetBBoxCorners(Bounds bounds)
        {
            // Return corners relative to origin (not relative to bounds.center)
            // because bboxWireframe is already positioned at bounds.center
            Vector3 extents = bounds.extents;

            return new Vector3[]
            {
                // Bottom face (Y = min)
                new Vector3(-extents.x, -extents.y, -extents.z), // 0: left-bottom-back
                new Vector3(+extents.x, -extents.y, -extents.z), // 1: right-bottom-back
                new Vector3(+extents.x, -extents.y, +extents.z), // 2: right-bottom-front
                new Vector3(-extents.x, -extents.y, +extents.z), // 3: left-bottom-front

                // Top face (Y = max)
                new Vector3(-extents.x, +extents.y, -extents.z), // 4: left-top-back
                new Vector3(+extents.x, +extents.y, -extents.z), // 5: right-top-back
                new Vector3(+extents.x, +extents.y, +extents.z), // 6: right-top-front
                new Vector3(-extents.x, +extents.y, +extents.z)  // 7: left-top-front
            };
        }

        public PlacedObject GetSelection()
        {
            return currentSelection;
        }

        public float GetBBoxBottomY()
        {
            if (currentSelection == null) return float.NaN;
            if (bboxWireframe == null) return float.NaN;

            // Read the actual position from the LineRenderer
            // Find any LineRenderer child of bboxWireframe
            LineRenderer[] lineRenderers = bboxWireframe.GetComponentsInChildren<LineRenderer>();
            if (lineRenderers.Length == 0) return float.NaN;

            // Get the bottom corner position from the first line renderer
            // Since useWorldSpace = false, we need to convert to world space
            LineRenderer lr = lineRenderers[0];
            Vector3 localPos0 = lr.GetPosition(0);
            Vector3 localPos1 = lr.GetPosition(1);

            // Convert to world space
            Vector3 worldPos0 = lr.transform.TransformPoint(localPos0);
            Vector3 worldPos1 = lr.transform.TransformPoint(localPos1);

            // Find the lowest Y among all line renderer positions
            float lowestY = Mathf.Min(worldPos0.y, worldPos1.y);

            foreach (LineRenderer line in lineRenderers)
            {
                Vector3 p0 = line.transform.TransformPoint(line.GetPosition(0));
                Vector3 p1 = line.transform.TransformPoint(line.GetPosition(1));
                lowestY = Mathf.Min(lowestY, Mathf.Min(p0.y, p1.y));
            }

            return lowestY;
        }
    }
}
