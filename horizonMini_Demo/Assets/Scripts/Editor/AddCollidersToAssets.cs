using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to batch add colliders to prefabs that don't have them
    /// </summary>
    public class AddCollidersToAssets : EditorWindow
    {
        private string prefabFolderPath = "Assets/Prefabs";
        private bool addMeshColliders = false;
        private bool convexMeshColliders = true;
        private bool addBoxColliders = true;
        private bool overwriteExisting = false;
        private bool processRecursively = true;
        private Vector2 scrollPos;

        private int totalPrefabs = 0;
        private int processedPrefabs = 0;
        private int skippedPrefabs = 0;

        [MenuItem("HorizonMini/Add Colliders to Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<AddCollidersToAssets>("Add Colliders to Prefabs");
        }

        private void OnGUI()
        {
            GUILayout.Label("Batch Add Colliders to Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will:\n" +
                "1. Scan prefab folder recursively\n" +
                "2. Check each prefab for existing colliders\n" +
                "3. Add appropriate collider if missing\n" +
                "4. Save modified prefabs",
                MessageType.Info
            );

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Settings
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            prefabFolderPath = EditorGUILayout.TextField("Prefab Folder Path", prefabFolderPath);
            processRecursively = EditorGUILayout.Toggle("Process Recursively", processRecursively);

            EditorGUILayout.Space();
            GUILayout.Label("Collider Type", EditorStyles.boldLabel);
            addBoxColliders = EditorGUILayout.Toggle("Add Box Colliders (default)", addBoxColliders);
            addMeshColliders = EditorGUILayout.Toggle("Add Mesh Colliders (if no mesh renderer)", addMeshColliders);

            if (addMeshColliders)
            {
                EditorGUI.indentLevel++;
                convexMeshColliders = EditorGUILayout.Toggle("Convex Mesh Colliders", convexMeshColliders);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Colliders", overwriteExisting);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Progress
            if (totalPrefabs > 0)
            {
                EditorGUILayout.LabelField($"Progress: {processedPrefabs}/{totalPrefabs} prefabs");
                EditorGUILayout.LabelField($"Skipped: {skippedPrefabs} (already had colliders)");
                float progress = (float)processedPrefabs / totalPrefabs;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{progress * 100:F1}%");
            }

            EditorGUILayout.Space();

            // Buttons
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(prefabFolderPath));
            if (GUILayout.Button("Add Colliders to All Prefabs", GUILayout.Height(40)))
            {
                ProcessAllPrefabs();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ProcessAllPrefabs()
        {
            totalPrefabs = 0;
            processedPrefabs = 0;
            skippedPrefabs = 0;

            // Find all prefabs
            string searchPattern = processRecursively ? "t:Prefab" : "";
            string[] guids = AssetDatabase.FindAssets(searchPattern, new[] { prefabFolderPath });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Found",
                    $"No prefabs found in {prefabFolderPath}", "OK");
                return;
            }

            totalPrefabs = guids.Length;
            Debug.Log($"<color=cyan>Found {totalPrefabs} prefabs to process...</color>");

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // Show progress
                    float progress = (float)processedPrefabs / totalPrefabs;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Adding Colliders to Prefabs",
                        $"Processing: {path}",
                        progress))
                    {
                        Debug.LogWarning("Process cancelled by user");
                        break;
                    }

                    ProcessPrefab(path);
                    processedPrefabs++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"<color=green>Collider addition complete!</color>");
            Debug.Log($"Total prefabs: {totalPrefabs}");
            Debug.Log($"Modified: {processedPrefabs - skippedPrefabs}");
            Debug.Log($"Skipped: {skippedPrefabs}");

            EditorUtility.DisplayDialog("Complete",
                $"Processed {totalPrefabs} prefabs\n" +
                $"Modified: {processedPrefabs - skippedPrefabs}\n" +
                $"Skipped: {skippedPrefabs}",
                "OK");
        }

        private void ProcessPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            // Check if already has collider
            Collider existingCollider = prefab.GetComponentInChildren<Collider>();
            if (existingCollider != null && !overwriteExisting)
            {
                skippedPrefabs++;
                return;
            }

            // Load prefab for editing
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                bool modified = false;

                // Remove existing colliders if overwriting
                if (overwriteExisting && existingCollider != null)
                {
                    Collider[] existingColliders = prefabInstance.GetComponentsInChildren<Collider>();
                    foreach (var col in existingColliders)
                    {
                        DestroyImmediate(col);
                    }
                }

                // Add colliders to all renderers
                Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>();

                if (renderers.Length == 0)
                {
                    // No renderers, add to root
                    if (AddColliderToGameObject(prefabInstance))
                    {
                        modified = true;
                    }
                }
                else
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (AddColliderToGameObject(renderer.gameObject))
                        {
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                    Debug.Log($"Added colliders to: {prefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        private bool AddColliderToGameObject(GameObject go)
        {
            // Skip if already has collider
            if (go.GetComponent<Collider>() != null)
                return false;

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();

            if (addBoxColliders)
            {
                // Add box collider (works for most cases)
                go.AddComponent<BoxCollider>();
                return true;
            }
            else if (addMeshColliders && meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Add mesh collider
                MeshCollider meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.convex = convexMeshColliders;
                return true;
            }
            else
            {
                // Fallback to box collider
                go.AddComponent<BoxCollider>();
                return true;
            }
        }
    }
}
