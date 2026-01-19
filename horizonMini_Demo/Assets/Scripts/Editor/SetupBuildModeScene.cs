using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HorizonMini.Core;
using HorizonMini.Controllers;
using HorizonMini.Build;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Setup BuildMode scene with SaveService and WorldLibrary
    /// </summary>
    public class SetupBuildModeScene : EditorWindow
    {
        [MenuItem("Tools/Setup BuildMode Scene")]
        public static void ShowWindow()
        {
            GetWindow<SetupBuildModeScene>("Setup BuildMode Scene");
        }

        private void OnGUI()
        {
            GUILayout.Label("Setup BuildMode Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will add SaveService and WorldLibrary to BuildMode scene.\n" +
                "Required for saving and loading worlds in standalone mode.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Auto Setup All", GUILayout.Height(40)))
            {
                AutoSetup();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Setup SaveService Only", GUILayout.Height(30)))
            {
                SetupSaveService();
            }

            if (GUILayout.Button("Setup WorldLibrary Only", GUILayout.Height(30)))
            {
                SetupWorldLibrary();
            }
        }

        private void AutoSetup()
        {
            int setupCount = 0;

            // Setup SaveService
            if (FindFirstObjectByType<SaveService>() == null)
            {
                GameObject saveServiceObj = new GameObject("SaveService");
                saveServiceObj.AddComponent<SaveService>();
                Debug.Log("✓ Created SaveService");
                setupCount++;
            }

            // Setup WorldLibrary
            if (FindFirstObjectByType<WorldLibrary>() == null)
            {
                GameObject worldLibraryObj = new GameObject("WorldLibrary");
                WorldLibrary worldLibrary = worldLibraryObj.AddComponent<WorldLibrary>();

                // Find and assign AssetCatalog
                string[] catalogGuids = AssetDatabase.FindAssets("t:AssetCatalog");
                if (catalogGuids.Length > 0)
                {
                    string catalogPath = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
                    AssetCatalog catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);

                    SerializedObject so = new SerializedObject(worldLibrary);
                    so.FindProperty("assetCatalog").objectReferenceValue = catalog;
                    so.ApplyModifiedProperties();

                    Debug.Log($"✓ Created WorldLibrary with AssetCatalog: {catalogPath}");
                }
                else
                {
                    Debug.LogWarning("AssetCatalog not found! Please assign manually.");
                }

                setupCount++;
            }

            if (setupCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("Success",
                    $"Setup complete! Added {setupCount} components.\n\n" +
                    "SaveService and WorldLibrary are now available.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Already Setup",
                    "SaveService and WorldLibrary already exist in scene.",
                    "OK");
            }
        }

        private void SetupSaveService()
        {
            SaveService saveService = FindFirstObjectByType<SaveService>();

            if (saveService != null)
            {
                Debug.Log("SaveService already exists in scene");
                EditorUtility.DisplayDialog("Already Setup", "SaveService already exists in the scene.", "OK");
                return;
            }

            GameObject saveServiceObj = new GameObject("SaveService");
            saveService = saveServiceObj.AddComponent<SaveService>();

            Debug.Log("✓ Created SaveService in scene");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Success",
                "SaveService has been added to the scene!",
                "OK");
        }

        private void SetupWorldLibrary()
        {
            WorldLibrary worldLibrary = FindFirstObjectByType<WorldLibrary>();

            if (worldLibrary != null)
            {
                Debug.Log("WorldLibrary already exists in scene");
                EditorUtility.DisplayDialog("Already Setup", "WorldLibrary already exists in the scene.", "OK");
                return;
            }

            GameObject worldLibraryObj = new GameObject("WorldLibrary");
            worldLibrary = worldLibraryObj.AddComponent<WorldLibrary>();

            // Find and assign AssetCatalog
            string[] catalogGuids = AssetDatabase.FindAssets("t:AssetCatalog");
            if (catalogGuids.Length > 0)
            {
                string catalogPath = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
                AssetCatalog catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);

                SerializedObject so = new SerializedObject(worldLibrary);
                so.FindProperty("assetCatalog").objectReferenceValue = catalog;
                so.ApplyModifiedProperties();

                Debug.Log($"✓ Created WorldLibrary with AssetCatalog: {catalogPath}");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Success",
                "WorldLibrary has been added to the scene!",
                "OK");
        }
    }
}
