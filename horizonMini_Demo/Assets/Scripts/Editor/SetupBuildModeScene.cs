using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using HorizonMini.Core;
using HorizonMini.Controllers;
using HorizonMini.Build;
using HorizonMini.UI;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Setup BuildMode scene with SaveService, WorldLibrary, and Volume Drawing configuration
    /// </summary>
    public class SetupBuildModeScene : EditorWindow
    {
        private Vector2 scrollPos;
        private bool showValidation = false;
        private string validationLog = "";

        [MenuItem("Tools/Setup BuildMode Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupBuildModeScene>("Setup BuildMode Scene");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Setup BuildMode Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool sets up BuildMode scene with:\n" +
                "1. SaveService and WorldLibrary (for saving worlds)\n" +
                "2. Volume Drawing configuration (3D cursor for volume size)\n\n" +
                "Required for standalone Build mode operation.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // === SECTION 1: Basic Setup ===
            GUILayout.Label("Basic Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup SaveService & WorldLibrary", GUILayout.Height(30)))
            {
                AutoSetup();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Setup SaveService Only", GUILayout.Height(25)))
            {
                SetupSaveService();
            }

            if (GUILayout.Button("Setup WorldLibrary Only", GUILayout.Height(25)))
            {
                SetupWorldLibrary();
            }

            EditorGUILayout.Space(15);

            // === SECTION 2: Volume Drawing Setup ===
            GUILayout.Label("Volume Drawing Setup (NEW)", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Configure Volume Drawing mode:\n" +
                "• Assigns Full Control Point Cursor prefab to BuildController\n" +
                "• Configures VolumeSizePickerUI for 3D cursor drawing\n" +
                "• Optionally hides old slider controls",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            if (GUILayout.Button("1. Validate Scene Configuration", GUILayout.Height(30)))
            {
                ValidateVolumeDrawingSetup();
                showValidation = true;
            }

            if (showValidation)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.TextArea(validationLog, GUILayout.Height(120));
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("2. Configure Volume Drawing", GUILayout.Height(30)))
            {
                ConfigureVolumeDrawing();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("3. Hide Old Sliders (Optional)", GUILayout.Height(25)))
            {
                HideSliders();
            }

            EditorGUILayout.Space(15);

            // === SECTION 3: Auto Setup All ===
            GUILayout.Label("Complete Auto Setup", EditorStyles.boldLabel);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("AUTO-CONFIGURE EVERYTHING", GUILayout.Height(50)))
            {
                AutoConfigureEverything();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "After configuration, save the scene and test:\n" +
                "1. Play the Build scene\n" +
                "2. Volume Size Picker UI should show\n" +
                "3. A red Full Control Point cursor should appear at (8,8,8)\n" +
                "4. Drag it to adjust volume size (1x1x1 to 4x4x4)\n" +
                "5. Click Create to confirm",
                MessageType.Warning
            );

            EditorGUILayout.EndScrollView();
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

        // ===== Volume Drawing Configuration Methods =====

        private void ValidateVolumeDrawingSetup()
        {
            validationLog = "=== VOLUME DRAWING VALIDATION ===\n\n";

            // Check BuildController
            var buildController = FindFirstObjectByType<BuildController>();
            if (buildController == null)
            {
                validationLog += "❌ ERROR: BuildController not found!\n";
            }
            else
            {
                validationLog += "✓ BuildController found\n";
            }

            // Check VolumeSizePickerUI
            var sizePickerUI = FindFirstObjectByType<VolumeSizePickerUI>();
            if (sizePickerUI == null)
            {
                validationLog += "❌ ERROR: VolumeSizePickerUI not found!\n";
            }
            else
            {
                validationLog += "✓ VolumeSizePickerUI found\n";
            }

            // Check Full Control Point Cursor prefab
            validationLog += "\nPrefab Checks:\n";
            string fullCursorPath = "Assets/Prefab/UI/FullControlPointCursor.prefab";
            var fullCursorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullCursorPath);
            if (fullCursorPrefab == null)
            {
                validationLog += $"❌ ERROR: FullControlPointCursor not found at:\n  {fullCursorPath}\n";
            }
            else
            {
                validationLog += "✓ FullControlPointCursor prefab found\n";
            }

            // Check Volume Grid prefab
            string volumeGridPath = "Assets/Prefab/VolumeGridPrefab.prefab";
            var volumeGridPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(volumeGridPath);
            if (volumeGridPrefab == null)
            {
                validationLog += $"❌ ERROR: VolumeGridPrefab not found at:\n  {volumeGridPath}\n";
            }
            else
            {
                validationLog += "✓ VolumeGridPrefab found\n";
            }

            validationLog += "\n=== END VALIDATION ===\n";
        }

        private void ConfigureVolumeDrawing()
        {
            var buildController = FindFirstObjectByType<BuildController>();
            if (buildController == null)
            {
                EditorUtility.DisplayDialog("Error", "BuildController not found in scene!", "OK");
                return;
            }

            var sizePickerUI = FindFirstObjectByType<VolumeSizePickerUI>();
            if (sizePickerUI == null)
            {
                EditorUtility.DisplayDialog("Error", "VolumeSizePickerUI not found in scene!", "OK");
                return;
            }

            Undo.RecordObject(buildController, "Configure Volume Drawing");
            Undo.RecordObject(sizePickerUI, "Configure Volume Drawing");

            int configCount = 0;

            // === Configure BuildController ===

            // Load and assign Full Control Point Cursor prefab
            string fullCursorPath = "Assets/Prefab/UI/FullControlPointCursor.prefab";
            var fullCursorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullCursorPath);
            if (fullCursorPrefab != null)
            {
                var field = typeof(BuildController).GetField("fullControlPointCursorPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(buildController, fullCursorPrefab);
                    Debug.Log("✓ Assigned fullControlPointCursorPrefab to BuildController");
                    configCount++;
                }
            }
            else
            {
                Debug.LogError($"❌ Full Control Point Cursor prefab not found at {fullCursorPath}");
            }

            // Verify/assign Volume Grid Prefab
            string volumeGridPath = "Assets/Prefab/VolumeGridPrefab.prefab";
            var volumeGridPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(volumeGridPath);
            if (volumeGridPrefab != null)
            {
                var volumeField = typeof(BuildController).GetField("volumeGridPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (volumeField != null)
                {
                    volumeField.SetValue(buildController, volumeGridPrefab);
                    Debug.Log("✓ Assigned volumeGridPrefab to BuildController");
                    configCount++;
                }
            }

            // Set volume settings
            var volumeUnitSizeField = typeof(BuildController).GetField("volumeUnitSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxVolumeSizeField = typeof(BuildController).GetField("maxVolumeSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (volumeUnitSizeField != null)
            {
                volumeUnitSizeField.SetValue(buildController, 8f);
                Debug.Log("✓ Set volumeUnitSize = 8.0");
                configCount++;
            }

            if (maxVolumeSizeField != null)
            {
                maxVolumeSizeField.SetValue(buildController, new Vector3Int(4, 4, 4));
                Debug.Log("✓ Set maxVolumeSize = (4, 4, 4)");
                configCount++;
            }

            // === Configure VolumeSizePickerUI ===

            // Set BuildController reference
            var buildControllerField = typeof(VolumeSizePickerUI).GetField("buildController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (buildControllerField != null)
            {
                buildControllerField.SetValue(sizePickerUI, buildController);
                Debug.Log("✓ Assigned BuildController to VolumeSizePickerUI");
                configCount++;
            }

            EditorUtility.SetDirty(buildController);
            EditorUtility.SetDirty(sizePickerUI);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Success",
                $"Volume Drawing configured successfully!\n\n" +
                $"Configured {configCount} settings:\n" +
                "• Full Control Point Cursor prefab\n" +
                "• Volume Grid prefab\n" +
                "• Volume settings (8m units, 4x4x4 max)\n" +
                "• VolumeSizePickerUI references\n\n" +
                "Don't forget to save the scene!",
                "OK");
        }

        private void HideSliders()
        {
            var sizePickerUI = FindFirstObjectByType<VolumeSizePickerUI>();
            if (sizePickerUI == null)
            {
                EditorUtility.DisplayDialog("Error", "VolumeSizePickerUI not found!", "OK");
                return;
            }

            // Get slider references via reflection
            var xSliderField = typeof(VolumeSizePickerUI).GetField("xSlider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ySliderField = typeof(VolumeSizePickerUI).GetField("ySlider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var zSliderField = typeof(VolumeSizePickerUI).GetField("zSlider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int hiddenCount = 0;

            if (xSliderField != null)
            {
                var xSlider = xSliderField.GetValue(sizePickerUI) as Slider;
                if (xSlider != null && xSlider.gameObject.activeSelf)
                {
                    Undo.RecordObject(xSlider.gameObject, "Hide X Slider");
                    xSlider.gameObject.SetActive(false);
                    hiddenCount++;
                    Debug.Log("✓ Hidden X Slider");
                }
            }

            if (ySliderField != null)
            {
                var ySlider = ySliderField.GetValue(sizePickerUI) as Slider;
                if (ySlider != null && ySlider.gameObject.activeSelf)
                {
                    Undo.RecordObject(ySlider.gameObject, "Hide Y Slider");
                    ySlider.gameObject.SetActive(false);
                    hiddenCount++;
                    Debug.Log("✓ Hidden Y Slider");
                }
            }

            if (zSliderField != null)
            {
                var zSlider = zSliderField.GetValue(sizePickerUI) as Slider;
                if (zSlider != null && zSlider.gameObject.activeSelf)
                {
                    Undo.RecordObject(zSlider.gameObject, "Hide Z Slider");
                    zSlider.gameObject.SetActive(false);
                    hiddenCount++;
                    Debug.Log("✓ Hidden Z Slider");
                }
            }

            if (hiddenCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("Success",
                    $"Hidden {hiddenCount} slider(s)!\n\n" +
                    "Sliders are hidden but not deleted.\n" +
                    "You can re-enable them in Hierarchy if needed.\n\n" +
                    "Don't forget to save the scene!",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info",
                    "No active sliders found to hide.\n" +
                    "They may already be hidden or not assigned.",
                    "OK");
            }
        }

        private void AutoConfigureEverything()
        {
            if (!EditorUtility.DisplayDialog("Auto-Configure Everything",
                "This will:\n\n" +
                "1. Setup SaveService & WorldLibrary\n" +
                "2. Configure Volume Drawing (prefabs, settings)\n" +
                "3. Hide old sliders\n\n" +
                "Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            // Step 1: Basic setup
            AutoSetup();

            // Step 2: Validate
            ValidateVolumeDrawingSetup();
            if (validationLog.Contains("❌ ERROR"))
            {
                EditorUtility.DisplayDialog("Validation Failed",
                    "Volume Drawing validation found errors.\n\n" +
                    "Click 'Validate Scene Configuration' to see details.",
                    "OK");
                showValidation = true;
                return;
            }

            // Step 3: Configure Volume Drawing
            ConfigureVolumeDrawing();

            // Step 4: Hide sliders
            HideSliders();

            // Save scene
            if (EditorUtility.DisplayDialog("Configuration Complete",
                "✅ All configuration completed!\n\n" +
                "Save the scene now?",
                "Save", "Later"))
            {
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("✓ Scene saved");
            }

            EditorUtility.DisplayDialog("Done",
                "✅ Build scene fully configured!\n\n" +
                "Test it by:\n" +
                "1. Play the Build scene\n" +
                "2. Volume Size Picker UI shows\n" +
                "3. Red Full Control Point cursor appears\n" +
                "4. Drag cursor to adjust volume (1x1x1 to 4x4x4)\n" +
                "5. Click Create to confirm\n\n" +
                "Camera is locked during drawing mode.",
                "OK");
        }
    }
}
