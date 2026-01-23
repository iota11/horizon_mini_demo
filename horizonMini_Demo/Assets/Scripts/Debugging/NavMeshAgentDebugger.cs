using UnityEngine;
using UnityEngine.AI;

namespace HorizonMini.Debugging
{
    /// <summary>
    /// Debug tool to visualize and diagnose NavMeshAgent issues
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshAgentDebugger : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showPath = true;
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color destinationColor = Color.red;

        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (showDebugInfo && agent != null)
            {
                // Check if agent is on NavMesh
                if (!agent.isOnNavMesh)
                {
                    Debug.LogWarning($"[NavMeshDebug] {gameObject.name} is NOT on NavMesh! Position: {transform.position}");

                    // Try to find nearest point on NavMesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                    {
                        Debug.Log($"[NavMeshDebug] Nearest NavMesh point is {Vector3.Distance(transform.position, hit.position):F2}m away at {hit.position}");
                    }
                    else
                    {
                        Debug.LogError($"[NavMeshDebug] No NavMesh found within 10m of {gameObject.name}!");
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            if (agent == null)
                return;

            // Draw destination
            if (agent.hasPath || agent.pathPending)
            {
                Gizmos.color = destinationColor;
                Gizmos.DrawSphere(agent.destination, 0.3f);

                // Draw path
                if (showPath && agent.path != null && agent.path.corners.Length > 0)
                {
                    Gizmos.color = pathColor;
                    Vector3[] corners = agent.path.corners;

                    for (int i = 0; i < corners.Length - 1; i++)
                    {
                        Gizmos.DrawLine(corners[i], corners[i + 1]);
                        Gizmos.DrawSphere(corners[i], 0.2f);
                    }

                    if (corners.Length > 0)
                        Gizmos.DrawSphere(corners[corners.Length - 1], 0.2f);
                }
            }

            // Draw current position
            Gizmos.color = agent.isOnNavMesh ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, agent.radius);
        }

        private void OnGUI()
        {
            if (!showDebugInfo || agent == null)
                return;

            GUILayout.BeginArea(new Rect(10, 200, 400, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>{gameObject.name} NavMeshAgent Status</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);

            GUILayout.Label($"On NavMesh: {agent.isOnNavMesh}");
            GUILayout.Label($"Has Path: {agent.hasPath}");
            GUILayout.Label($"Path Pending: {agent.pathPending}");
            GUILayout.Label($"Path Status: {agent.pathStatus}");
            GUILayout.Label($"Velocity: {agent.velocity.magnitude:F2} m/s");
            GUILayout.Label($"Desired Velocity: {agent.desiredVelocity.magnitude:F2} m/s");
            GUILayout.Label($"Remaining Distance: {agent.remainingDistance:F2}m");
            GUILayout.Label($"Is Stopped: {agent.isStopped}");
            GUILayout.Label($"Speed: {agent.speed}");
            GUILayout.Label($"Position: {transform.position}");

            if (agent.hasPath)
            {
                GUILayout.Label($"Destination: {agent.destination}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        [ContextMenu("Test: Set Random Destination")]
        public void TestRandomDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * 10f;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                Debug.Log($"[NavMeshDebug] Set destination to {hit.position}");
            }
            else
            {
                Debug.LogError("[NavMeshDebug] Could not find valid destination on NavMesh!");
            }
        }

        [ContextMenu("Test: Warp to NavMesh")]
        public void WarpToNavMesh()
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.Log($"[NavMeshDebug] Warped to NavMesh at {hit.position}");
            }
            else
            {
                Debug.LogError("[NavMeshDebug] Could not find NavMesh to warp to!");
            }
        }
    }
}
