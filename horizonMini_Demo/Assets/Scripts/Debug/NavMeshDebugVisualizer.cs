using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace HorizonMini.Debugging
{
    /// <summary>
    /// Visualizes NavMesh in Scene view and provides debug info
    /// </summary>
    public class NavMeshDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        #pragma warning disable 0414
        [SerializeField] private bool showNavMeshInGame = false; // Reserved for runtime visualization
        #pragma warning restore 0414
        [SerializeField] private Color navMeshColor = new Color(0, 0, 1, 0.3f);

        private NavMeshTriangulation triangulation;

        private void Start()
        {
            LogNavMeshInfo();
        }

        private void LogNavMeshInfo()
        {
            // Find all NavMeshSurface components
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);

            if (surfaces.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[NavMeshDebug] ⚠ No NavMeshSurface found in scene!");
                UnityEngine.Debug.LogWarning("[NavMeshDebug] To add NavMesh: Create an empty GameObject → Add Component → NavMeshSurface → Click 'Bake'");
                return;
            }

            UnityEngine.Debug.Log($"[NavMeshDebug] Found {surfaces.Length} NavMeshSurface(s):");

            foreach (var surface in surfaces)
            {
                if (surface.navMeshData != null)
                {
                    UnityEngine.Debug.Log($"[NavMeshDebug] ✓ {surface.gameObject.name} - NavMesh data exists (baked)");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[NavMeshDebug] ✗ {surface.gameObject.name} - No NavMesh data (not baked yet)");
                }
            }

            // Get NavMesh triangulation for area calculation
            triangulation = NavMesh.CalculateTriangulation();

            if (triangulation.vertices.Length > 0)
            {
                float area = CalculateNavMeshArea();
                UnityEngine.Debug.Log($"[NavMeshDebug] Total NavMesh vertices: {triangulation.vertices.Length}");
                UnityEngine.Debug.Log($"[NavMeshDebug] Total NavMesh triangles: {triangulation.indices.Length / 3}");
                UnityEngine.Debug.Log($"[NavMeshDebug] Approximate NavMesh area: {area:F2} square meters");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[NavMeshDebug] NavMesh has no vertices - it may not be baked yet");
            }
        }

        private float CalculateNavMeshArea()
        {
            float totalArea = 0f;

            for (int i = 0; i < triangulation.indices.Length; i += 3)
            {
                Vector3 v1 = triangulation.vertices[triangulation.indices[i]];
                Vector3 v2 = triangulation.vertices[triangulation.indices[i + 1]];
                Vector3 v3 = triangulation.vertices[triangulation.indices[i + 2]];

                totalArea += Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
            }

            return totalArea;
        }

        // Draw NavMesh in Scene view
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (triangulation.vertices == null || triangulation.vertices.Length == 0)
                return;

            Gizmos.color = navMeshColor;

            for (int i = 0; i < triangulation.indices.Length; i += 3)
            {
                Vector3 v1 = triangulation.vertices[triangulation.indices[i]];
                Vector3 v2 = triangulation.vertices[triangulation.indices[i + 1]];
                Vector3 v3 = triangulation.vertices[triangulation.indices[i + 2]];

                // Draw triangle
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v1);
            }
        }

        // Check if a position is on NavMesh
        public static bool IsOnNavMesh(Vector3 position, float maxDistance = 1f)
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas);
        }

        // Get nearest point on NavMesh
        public static Vector3 GetNearestNavMeshPoint(Vector3 position, float maxDistance = 10f)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return position;
        }

        // Debug: Manually trigger NavMesh rebuild
        [ContextMenu("Rebuild NavMesh")]
        public void RebuildNavMesh()
        {
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);

            if (surfaces.Length == 0)
            {
                UnityEngine.Debug.LogError("[NavMeshDebug] No NavMeshSurface found!");
                return;
            }

            foreach (var surface in surfaces)
            {
                surface.BuildNavMesh();
                UnityEngine.Debug.Log($"[NavMeshDebug] ✓ Rebuilt NavMesh for: {surface.gameObject.name}");
            }

            // Refresh info
            Start();
        }
    }
}
