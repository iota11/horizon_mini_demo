using UnityEngine;
using UnityEditor;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to find and list all colliders in selected GameObject and its children
    /// </summary>
    public class FindColliders : EditorWindow
    {
        [MenuItem("HorizonMini/Debug/Find Colliders in Selection")]
        public static void FindCollidersInSelection()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("[FindColliders] No GameObject selected!");
                EditorUtility.DisplayDialog("Warning", "Please select a GameObject in the Hierarchy first.", "OK");
                return;
            }

            GameObject selected = Selection.activeGameObject;
            Debug.Log($"[FindColliders] Searching for colliders in: {selected.name}");

            // Find all colliders in this object and children
            Collider[] colliders = selected.GetComponentsInChildren<Collider>(true); // true = include inactive

            if (colliders.Length == 0)
            {
                Debug.Log("[FindColliders] No colliders found!");
                EditorUtility.DisplayDialog("Result", "No colliders found in this GameObject or its children.", "OK");
                return;
            }

            Debug.Log($"[FindColliders] Found {colliders.Length} collider(s):");

            string report = $"Found {colliders.Length} collider(s) in '{selected.name}':\n\n";

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                string path = GetGameObjectPath(col.gameObject);
                string type = col.GetType().Name;
                string active = col.enabled ? "Enabled" : "DISABLED";
                string isTrigger = col.isTrigger ? "TRIGGER" : "Solid";

                string info = $"{i + 1}. [{type}] {path}\n   Status: {active}, {isTrigger}\n";
                Debug.Log($"[FindColliders] {info}");
                report += info + "\n";

                // Highlight specific collider info
                if (col is BoxCollider box)
                {
                    Debug.Log($"   BoxCollider: Center={box.center}, Size={box.size}");
                    report += $"   Center={box.center}, Size={box.size}\n";
                }
                else if (col is SphereCollider sphere)
                {
                    Debug.Log($"   SphereCollider: Center={sphere.center}, Radius={sphere.radius}");
                    report += $"   Center={sphere.center}, Radius={sphere.radius}\n";
                }
                else if (col is CapsuleCollider capsule)
                {
                    Debug.Log($"   CapsuleCollider: Center={capsule.center}, Radius={capsule.radius}, Height={capsule.height}");
                    report += $"   Center={capsule.center}, Radius={capsule.radius}, Height={capsule.height}\n";
                }
                else if (col is MeshCollider mesh)
                {
                    Debug.Log($"   MeshCollider: Convex={mesh.convex}, Mesh={mesh.sharedMesh?.name ?? "null"}");
                    report += $"   Convex={mesh.convex}, Mesh={mesh.sharedMesh?.name ?? "null"}\n";
                }
            }

            // Show dialog with results
            EditorUtility.DisplayDialog("Colliders Found", report, "OK");

            // Select first collider's GameObject in hierarchy
            if (colliders.Length > 0)
            {
                Selection.activeGameObject = colliders[0].gameObject;
                EditorGUIUtility.PingObject(colliders[0].gameObject);
            }
        }

        /// <summary>
        /// Get full hierarchy path of a GameObject
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
