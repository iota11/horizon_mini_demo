using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HorizonMini.Core;
using HorizonMini.Controllers;
using HorizonMini.Data;
using HorizonMini.Build;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to configure Main scene AppRoot with required references
    /// </summary>
    public class SetupMainScene
    {
        [MenuItem("HorizonMini/Configure Main Scene AppRoot")]
        public static void ConfigureAppRoot()
        {
            // Open Main scene if not already open
            Scene mainScene = SceneManager.GetActiveScene();
            if (mainScene.name != "Main")
            {
                bool openScene = EditorUtility.DisplayDialog(
                    "Open Main Scene?",
                    "Main scene is not currently open.\n\n" +
                    "Would you like to open it now?",
                    "Yes",
                    "Cancel"
                );

                if (!openScene)
                {
                    Debug.Log("[SetupMainScene] Cancelled by user");
                    return;
                }

                mainScene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            }

            // Find AppRoot in scene
            AppRoot appRoot = Object.FindFirstObjectByType<AppRoot>();
            if (appRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "AppRoot not found in Main scene!\n\n" +
                    "Please ensure AppRoot GameObject exists in the scene.",
                    "OK"
                );
                return;
            }

            // Find WorldLibrary component
            WorldLibrary worldLibrary = appRoot.GetComponent<WorldLibrary>();
            if (worldLibrary == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "WorldLibrary component not found on AppRoot!\n\n" +
                    "Please ensure AppRoot has WorldLibrary component.",
                    "OK"
                );
                return;
            }

            // Use SerializedObject to set private fields
            SerializedObject worldLibrarySO = new SerializedObject(worldLibrary);

            // Find and assign volumePrefab
            GameObject volumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Volume.prefab");
            if (volumePrefab == null)
            {
                // Try alternate paths
                string[] guids = AssetDatabase.FindAssets("t:Prefab Volume");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (candidate != null && candidate.name == "Volume")
                    {
                        volumePrefab = candidate;
                        break;
                    }
                }
            }

            // Find and assign gridSettings
            GridSettings gridSettings = AssetDatabase.LoadAssetAtPath<GridSettings>("Assets/ScriptableObjects/GridSettings.asset");
            if (gridSettings == null)
            {
                // Search for GridSettings asset
                string[] guids = AssetDatabase.FindAssets("t:GridSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    gridSettings = AssetDatabase.LoadAssetAtPath<GridSettings>(path);
                }
            }

            // Find and assign assetCatalog
            AssetCatalog assetCatalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/ScriptableObjects/AssetCatalog.asset");
            if (assetCatalog == null)
            {
                // Search for AssetCatalog asset
                string[] guids = AssetDatabase.FindAssets("t:AssetCatalog");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    assetCatalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(path);
                }
            }

            // Apply references to WorldLibrary
            bool anyAssigned = false;
            string assignmentReport = "WorldLibrary Configuration:\n\n";

            if (volumePrefab != null)
            {
                worldLibrarySO.FindProperty("volumePrefab").objectReferenceValue = volumePrefab;
                assignmentReport += $"✅ Volume Prefab: {AssetDatabase.GetAssetPath(volumePrefab)}\n";
                anyAssigned = true;
            }
            else
            {
                assignmentReport += "❌ Volume Prefab: Not found\n";
            }

            if (gridSettings != null)
            {
                worldLibrarySO.FindProperty("gridSettings").objectReferenceValue = gridSettings;
                assignmentReport += $"✅ Grid Settings: {AssetDatabase.GetAssetPath(gridSettings)}\n";
                anyAssigned = true;
            }
            else
            {
                assignmentReport += "❌ Grid Settings: Not found\n";
            }

            if (assetCatalog != null)
            {
                worldLibrarySO.FindProperty("assetCatalog").objectReferenceValue = assetCatalog;
                assignmentReport += $"✅ Asset Catalog: {AssetDatabase.GetAssetPath(assetCatalog)}\n";
                anyAssigned = true;
            }
            else
            {
                assignmentReport += "❌ Asset Catalog: Not found\n";
            }

            worldLibrarySO.ApplyModifiedProperties();

            // Mark scene as dirty
            if (anyAssigned)
            {
                EditorSceneManager.MarkSceneDirty(mainScene);
            }

            // Show results
            if (volumePrefab != null && gridSettings != null && assetCatalog != null)
            {
                bool saveScene = EditorUtility.DisplayDialog(
                    "Success",
                    assignmentReport + "\n" +
                    "All references configured successfully!\n\n" +
                    "Would you like to save the scene now?",
                    "Save Scene",
                    "Don't Save"
                );

                if (saveScene)
                {
                    EditorSceneManager.SaveScene(mainScene);
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Partial Success",
                    assignmentReport + "\n" +
                    "⚠️ Some references could not be found.\n\n" +
                    "Please assign missing references manually in the Inspector.",
                    "OK"
                );
            }

            // Select AppRoot to show in Inspector
            Selection.activeGameObject = appRoot.gameObject;

            Debug.Log($"[SetupMainScene] {assignmentReport}");
        }
    }
}
