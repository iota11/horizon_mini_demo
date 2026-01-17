using UnityEngine;
using UnityEditor;
using HorizonMini.Controllers;
using HorizonMini.UI;
using HorizonMini.Build;
using HorizonMini.Core;
using HorizonMini.Data;

namespace HorizonMini.Editor
{
    /// <summary>
    /// One-click Build Mode setup automation tool
    /// </summary>
    public class BuildModeSetupWizard : EditorWindow
    {
        [MenuItem("HorizonMini/Build Mode Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<BuildModeSetupWizard>("Build Mode Setup");
        }


        private BuildModeUI buildModeUI;
        private BuildController buildController;
        private AssetCatalog assetCatalog;
        private GridSettings gridSettings;

        private Vector2 scrollPos;

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.HelpBox("Build Mode Setup Wizard\nThis tool will help you automatically create and connect all necessary components", MessageType.Info);

            EditorGUILayout.Space(10);

            // Step 1: Find or create BuildModeUI
            DrawStep1();

            EditorGUILayout.Space(10);

            // Step 2: Find or create BuildController
            DrawStep2();

            EditorGUILayout.Space(10);

            // Step 3: Create or load assets
            DrawStep3();

            EditorGUILayout.Space(10);

            // Step 4: Auto-connect references
            DrawStep4();

            EditorGUILayout.Space(10);

            // Step 5: Create test assets
            DrawStep5();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStep1()
        {
            EditorGUILayout.LabelField("Step 1: BuildModeUI", EditorStyles.boldLabel);

            buildModeUI = EditorGUILayout.ObjectField("Build Mode UI", buildModeUI, typeof(BuildModeUI), true) as BuildModeUI;

            if (buildModeUI == null)
            {
                if (GUILayout.Button("Auto-Find BuildModeUI"))
                {
                    buildModeUI = FindFirstObjectByType<BuildModeUI>();
                    if (buildModeUI != null)
                    {
                        Debug.Log("Found BuildModeUI: " + buildModeUI.name);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Not Found", "No BuildModeUI in scene\nPlease add BuildModeUI component to Canvas first", "OK");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ BuildModeUI found", MessageType.None);
            }
        }

        private void DrawStep2()
        {
            EditorGUILayout.LabelField("Step 2: BuildController", EditorStyles.boldLabel);

            buildController = EditorGUILayout.ObjectField("Build Controller", buildController, typeof(BuildController), true) as BuildController;

            if (buildController == null)
            {
                if (GUILayout.Button("Auto-Find BuildController"))
                {
                    buildController = FindFirstObjectByType<BuildController>();
                    if (buildController == null)
                    {
                        // Create BuildController
                        if (EditorUtility.DisplayDialog("BuildController Not Found", "Create new BuildSystem GameObject?", "Create", "Cancel"))
                        {
                            GameObject buildSystem = new GameObject("BuildSystem");
                            buildController = buildSystem.AddComponent<BuildController>();
                            Debug.Log("Created BuildController");
                        }
                    }
                    else
                    {
                        Debug.Log("Found BuildController: " + buildController.name);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ BuildController found", MessageType.None);
            }
        }

        private void DrawStep3()
        {
            EditorGUILayout.LabelField("Step 3: Asset Configuration", EditorStyles.boldLabel);

            // GridSettings
            gridSettings = EditorGUILayout.ObjectField("Grid Settings", gridSettings, typeof(GridSettings), false) as GridSettings;

            if (gridSettings == null)
            {
                if (GUILayout.Button("Create GridSettings"))
                {
                    string path = "Assets/Data";
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        AssetDatabase.CreateFolder("Assets", "Data");
                    }

                    gridSettings = ScriptableObject.CreateInstance<GridSettings>();
                    AssetDatabase.CreateAsset(gridSettings, "Assets/Data/DefaultGridSettings.asset");
                    AssetDatabase.SaveAssets();
                    Debug.Log("Created GridSettings");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ GridSettings created", MessageType.None);
            }

            // AssetCatalog
            assetCatalog = EditorGUILayout.ObjectField("Asset Catalog", assetCatalog, typeof(AssetCatalog), false) as AssetCatalog;

            if (assetCatalog == null)
            {
                if (GUILayout.Button("Create AssetCatalog"))
                {
                    string path = "Assets/Data";
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        AssetDatabase.CreateFolder("Assets", "Data");
                    }

                    assetCatalog = ScriptableObject.CreateInstance<AssetCatalog>();
                    AssetDatabase.CreateAsset(assetCatalog, "Assets/Data/DefaultAssetCatalog.asset");
                    AssetDatabase.SaveAssets();
                    Debug.Log("Created AssetCatalog");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ AssetCatalog created", MessageType.None);
            }
        }

        private void DrawStep4()
        {
            EditorGUILayout.LabelField("Step 4: Auto-Connect", EditorStyles.boldLabel);

            bool canConnect = buildModeUI != null && buildController != null;

            GUI.enabled = canConnect;

            if (GUILayout.Button("🔗 Auto-Connect All References", GUILayout.Height(30)))
            {
                ConnectReferences();
            }

            GUI.enabled = true;

            if (!canConnect)
            {
                EditorGUILayout.HelpBox("Please complete Step 1 and Step 2 first", MessageType.Warning);
            }
        }

        private void DrawStep5()
        {
            EditorGUILayout.LabelField("Step 5: Test Assets", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Test PlaceableAsset"))
            {
                CreateTestAssets();
            }
        }

        private void ConnectReferences()
        {
            Undo.RecordObject(buildModeUI, "Connect BuildModeUI");
            Undo.RecordObject(buildController, "Connect BuildController");

            // Add BuildModeInitializer (if not present)
            BuildModeInitializer initializer = buildModeUI.GetComponent<BuildModeInitializer>();
            if (initializer == null)
            {
                initializer = buildModeUI.gameObject.AddComponent<BuildModeInitializer>();
                Debug.Log("Added BuildModeInitializer component");
            }

            // Connect BuildController to BuildModeUI
            SerializedObject so = new SerializedObject(buildModeUI);
            so.FindProperty("buildController").objectReferenceValue = buildController;
            so.ApplyModifiedProperties();

            // Connect Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                SerializedObject controllerSO = new SerializedObject(buildController);
                controllerSO.FindProperty("buildCamera").objectReferenceValue = mainCam;
                controllerSO.ApplyModifiedProperties();
            }

            // Connect AssetCatalog if exists
            if (assetCatalog != null)
            {
                AssetCatalogUI catalogUI = FindFirstObjectByType<AssetCatalogUI>();
                if (catalogUI != null)
                {
                    SerializedObject catalogSO = new SerializedObject(catalogUI);
                    catalogSO.FindProperty("assetCatalog").objectReferenceValue = assetCatalog;
                    catalogSO.FindProperty("buildController").objectReferenceValue = buildController;
                    catalogSO.ApplyModifiedProperties();
                }
            }

            // Connect VolumeSizePickerUI
            VolumeSizePickerUI sizePickerUI = FindFirstObjectByType<VolumeSizePickerUI>();
            if (sizePickerUI != null)
            {
                SerializedObject pickerSO = new SerializedObject(sizePickerUI);
                pickerSO.FindProperty("buildController").objectReferenceValue = buildController;
                pickerSO.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(buildModeUI);
            EditorUtility.SetDirty(buildController);

            Debug.Log("✓ All references auto-connected!");
            EditorUtility.DisplayDialog("Complete", "All references have been auto-connected!\nPlease check Inspector to confirm.", "OK");
        }

        private void CreateTestAssets()
        {
            // Create folders
            string prefabPath = "Assets/Prefabs/PlaceableObjects";
            string dataPath = "Assets/Data/PlaceableAssets";

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/Prefabs", "PlaceableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(dataPath))
                AssetDatabase.CreateFolder("Assets/Data", "PlaceableAssets");

            // Create test cube prefab
            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.name = "TestCube";

            // Add material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.cyan;
            testCube.GetComponent<Renderer>().sharedMaterial = mat;

            // Save as prefab
            string cubePrefabPath = prefabPath + "/TestCube.prefab";
            GameObject cubePrefab = PrefabUtility.SaveAsPrefabAsset(testCube, cubePrefabPath);
            DestroyImmediate(testCube);

            // Create PlaceableAsset
            PlaceableAsset asset = ScriptableObject.CreateInstance<PlaceableAsset>();
            asset.assetId = "test_cube_001";
            asset.displayName = "Test Cube";
            asset.category = AssetCategory.Furniture;
            asset.prefab = cubePrefab;
            asset.defaultScale = Vector3.one;

            string assetPath = dataPath + "/TestCubeAsset.asset";
            AssetDatabase.CreateAsset(asset, assetPath);

            // Add to catalog if exists
            if (assetCatalog != null)
            {
                SerializedObject catalogSO = new SerializedObject(assetCatalog);
                SerializedProperty allAssetsProp = catalogSO.FindProperty("allAssets");
                allAssetsProp.InsertArrayElementAtIndex(allAssetsProp.arraySize);
                allAssetsProp.GetArrayElementAtIndex(allAssetsProp.arraySize - 1).objectReferenceValue = asset;
                catalogSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(assetCatalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✓ Created test asset: TestCube");
            EditorUtility.DisplayDialog("Complete", "Test assets created!\n- TestCube Prefab\n- TestCubeAsset\nAdded to AssetCatalog", "OK");
        }
    }
}
