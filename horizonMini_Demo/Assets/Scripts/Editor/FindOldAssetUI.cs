using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using HorizonMini.UI;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Finds old Asset Catalog UI components in the scene
    /// </summary>
    public class FindOldAssetUI : EditorWindow
    {
        [MenuItem("Tools/Find Old Asset UI")]
        public static void ShowWindow()
        {
            GetWindow<FindOldAssetUI>("Find Old Asset UI");
        }

        private Vector2 scrollPos;
        private GameObject[] foundObjects;

        private void OnGUI()
        {
            GUILayout.Label("Find Old Asset Catalog UI", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will search the current scene for old Asset Catalog UI components.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Search Current Scene", GUILayout.Height(40)))
            {
                SearchScene();
            }

            if (foundObjects != null && foundObjects.Length > 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Found {foundObjects.Length} objects:", EditorStyles.boldLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
                {
                    foreach (GameObject obj in foundObjects)
                    {
                        if (obj == null) continue;

                        EditorGUILayout.BeginHorizontal("box");
                        {
                            EditorGUILayout.LabelField(obj.name, GUILayout.Width(200));
                            EditorGUILayout.LabelField(GetPath(obj), GUILayout.ExpandWidth(true));

                            if (GUILayout.Button("Select", GUILayout.Width(60)))
                            {
                                Selection.activeGameObject = obj;
                                EditorGUIUtility.PingObject(obj);
                            }

                            GUI.backgroundColor = Color.red;
                            if (GUILayout.Button("Delete", GUILayout.Width(60)))
                            {
                                if (EditorUtility.DisplayDialog(
                                    "Confirm Delete",
                                    $"Delete '{obj.name}'?",
                                    "Yes", "No"))
                                {
                                    DestroyImmediate(obj);
                                    SearchScene(); // Refresh
                                }
                            }
                            GUI.backgroundColor = Color.white;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete All Found Objects", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Confirm Delete All",
                        $"Delete all {foundObjects.Length} objects?",
                        "Yes", "No"))
                    {
                        DeleteAll();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            else if (foundObjects != null)
            {
                EditorGUILayout.HelpBox("No old Asset UI found in the scene.", MessageType.Info);
            }
        }

        private void SearchScene()
        {
            System.Collections.Generic.List<GameObject> results = new System.Collections.Generic.List<GameObject>();

            // Find by component
            AssetCatalogUI[] catalogUIs = FindObjectsOfType<AssetCatalogUI>(true);
            foreach (var ui in catalogUIs)
            {
                if (!results.Contains(ui.gameObject))
                {
                    results.Add(ui.gameObject);
                }
            }

            // Find by name patterns
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in allObjects)
            {
                SearchRecursive(root.transform, results);
            }

            foundObjects = results.ToArray();
            Debug.Log($"Found {foundObjects.Length} old Asset UI objects");
        }

        private void SearchRecursive(Transform parent, System.Collections.Generic.List<GameObject> results)
        {
            string[] patterns = new[]
            {
                "AssetCatalog",
                "AssetPanel",
                "AssetLibrary",
                "AssetGrid",
                "CategoryTab",
                "AssetItem"
            };

            foreach (string pattern in patterns)
            {
                if (parent.name.Contains(pattern) && !results.Contains(parent.gameObject))
                {
                    // Check if it's likely old UI (not the new one we're creating)
                    bool isOldUI = true;

                    // New UI will have specific parent structure
                    if (parent.parent != null && parent.parent.name == "AssetCatalogPanel")
                    {
                        // This might be new UI, check more carefully
                        Transform root = parent.root;
                        if (root.GetComponent<AssetCatalogUI>() != null)
                        {
                            // Has AssetCatalogUI component - could be old or new
                            // Add it to let user decide
                            isOldUI = true;
                        }
                    }

                    if (isOldUI)
                    {
                        results.Add(parent.gameObject);
                    }
                }
            }

            foreach (Transform child in parent)
            {
                SearchRecursive(child, results);
            }
        }

        private void DeleteAll()
        {
            if (foundObjects == null) return;

            int count = 0;
            foreach (GameObject obj in foundObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                    count++;
                }
            }

            foundObjects = null;
            Debug.Log($"<color=green>Deleted {count} old Asset UI objects</color>");
        }

        private string GetPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
