using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using HorizonMini.Core;
using HorizonMini.Controllers;
using HorizonMini.UI;
using HorizonMini.Build;
using HorizonMini.Data;
using System.Linq;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Create Main scene with AppRoot, Controllers, and Tab Bar UI
    /// </summary>
    public class CreateMainScene : EditorWindow
    {
        [MenuItem("Tools/Create Main Scene")]
        public static void ShowWindow()
        {
            GetWindow<CreateMainScene>("Create Main Scene");
        }

        private void OnGUI()
        {
            GUILayout.Label("Create Main Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will create a complete Main scene with:\n" +
                "• AppRoot\n" +
                "• All Controllers (Browse, Build, Home, Play)\n" +
                "• Bottom Tab Bar UI (Browse/Build/Home)\n" +
                "• SaveService and WorldLibrary",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Main Scene", GUILayout.Height(40)))
            {
                CreateScene();
            }
        }

        private void CreateScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create AppRoot GameObject
            GameObject appRootObj = new GameObject("AppRoot");
            AppRoot appRoot = appRootObj.AddComponent<AppRoot>();

            // Create Core Systems
            GameObject saveServiceObj = new GameObject("SaveService");
            saveServiceObj.transform.SetParent(appRootObj.transform);
            SaveService saveService = saveServiceObj.AddComponent<SaveService>();

            GameObject worldLibraryObj = new GameObject("WorldLibrary");
            worldLibraryObj.transform.SetParent(appRootObj.transform);
            WorldLibrary worldLibrary = worldLibraryObj.AddComponent<WorldLibrary>();

            // Assign AssetCatalog and GridSettings to WorldLibrary
            SerializedObject wlSO = new SerializedObject(worldLibrary);

            string[] catalogGuids = AssetDatabase.FindAssets("t:AssetCatalog");
            if (catalogGuids.Length > 0)
            {
                string catalogPath = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
                AssetCatalog catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);
                wlSO.FindProperty("assetCatalog").objectReferenceValue = catalog;
            }

            string[] gridSettingsGuids = AssetDatabase.FindAssets("t:GridSettings");
            if (gridSettingsGuids.Length > 0)
            {
                string gridSettingsPath = AssetDatabase.GUIDToAssetPath(gridSettingsGuids[0]);
                HorizonMini.Data.GridSettings gridSettings = AssetDatabase.LoadAssetAtPath<HorizonMini.Data.GridSettings>(gridSettingsPath);
                wlSO.FindProperty("gridSettings").objectReferenceValue = gridSettings;
                Debug.Log($"Assigned GridSettings: {gridSettingsPath}");
            }
            else
            {
                Debug.LogWarning("GridSettings not found! WorldLibrary may not work correctly.");
            }

            wlSO.ApplyModifiedProperties();

            // Create Controllers with Cameras
            GameObject browseCtrObj = new GameObject("BrowseController");
            browseCtrObj.transform.SetParent(appRootObj.transform);
            BrowseController browseController = browseCtrObj.AddComponent<BrowseController>();
            Camera browseCamera = CreateControllerCamera(browseCtrObj.transform, "BrowseCamera");

            GameObject buildCtrObj = new GameObject("BuildController");
            buildCtrObj.transform.SetParent(appRootObj.transform);
            BuildController buildController = buildCtrObj.AddComponent<BuildController>();
            Camera buildCamera = CreateControllerCamera(buildCtrObj.transform, "BuildCamera");

            GameObject homeCtrObj = new GameObject("HomeController");
            homeCtrObj.transform.SetParent(appRootObj.transform);
            HomeController homeController = homeCtrObj.AddComponent<HomeController>();
            Camera homeCamera = CreateControllerCamera(homeCtrObj.transform, "HomeCamera");

            GameObject playCtrObj = new GameObject("PlayController");
            playCtrObj.transform.SetParent(appRootObj.transform);
            PlayController playController = playCtrObj.AddComponent<PlayController>();
            Camera playCamera = CreateControllerCamera(playCtrObj.transform, "PlayCamera");

            // Assign cameras to controllers
            SerializedObject browseSO = new SerializedObject(browseController);
            browseSO.FindProperty("browseCamera").objectReferenceValue = browseCamera;
            browseSO.ApplyModifiedProperties();

            SerializedObject buildSO = new SerializedObject(buildController);
            buildSO.FindProperty("buildCamera").objectReferenceValue = buildCamera;
            buildSO.ApplyModifiedProperties();

            SerializedObject homeSO = new SerializedObject(homeController);
            homeSO.FindProperty("homeCamera").objectReferenceValue = homeCamera;
            homeSO.ApplyModifiedProperties();

            SerializedObject playSO = new SerializedObject(playController);
            playSO.FindProperty("playerCamera").objectReferenceValue = playCamera;
            playSO.ApplyModifiedProperties();

            // Assign controllers to AppRoot
            SerializedObject appRootSO = new SerializedObject(appRoot);
            appRootSO.FindProperty("worldLibrary").objectReferenceValue = worldLibrary;
            appRootSO.FindProperty("saveService").objectReferenceValue = saveService;
            appRootSO.FindProperty("browseController").objectReferenceValue = browseController;
            appRootSO.FindProperty("buildController").objectReferenceValue = buildController;
            appRootSO.FindProperty("homeController").objectReferenceValue = homeController;
            appRootSO.FindProperty("playController").objectReferenceValue = playController;
            appRootSO.ApplyModifiedProperties();

            // Create UI Canvas
            GameObject canvasObj = CreateUICanvas();

            // Create EventSystem (required for UI interaction)
            CreateEventSystem();

            GameObject uiRouterObj = canvasObj;
            UIRouter uiRouter = uiRouterObj.AddComponent<UIRouter>();

            // Create Tab Bar
            GameObject tabBarObj = CreateTabBar(canvasObj.transform);

            // Assign UIRouter to AppRoot
            appRootSO.FindProperty("uiRouter").objectReferenceValue = uiRouter;
            appRootSO.ApplyModifiedProperties();

            // Assign tab buttons to UIRouter
            Transform worldTabBtn = tabBarObj.transform.Find("WorldTab");
            Transform buildTabBtn = tabBarObj.transform.Find("BuildTab");
            Transform homeTabBtn = tabBarObj.transform.Find("HomeTab");

            SerializedObject uiRouterSO = new SerializedObject(uiRouter);
            if (worldTabBtn != null)
                uiRouterSO.FindProperty("worldTabButton").objectReferenceValue = worldTabBtn.GetComponent<Button>();
            if (buildTabBtn != null)
                uiRouterSO.FindProperty("addTabButton").objectReferenceValue = buildTabBtn.GetComponent<Button>();
            if (homeTabBtn != null)
                uiRouterSO.FindProperty("homeTabButton").objectReferenceValue = homeTabBtn.GetComponent<Button>();
            uiRouterSO.ApplyModifiedProperties();

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(newScene);

            // Save scene
            string scenePath = "Assets/Scenes/Main.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"✓ Created Main scene at {scenePath}");

            EditorUtility.DisplayDialog("Success",
                "Main scene created successfully!\n\n" +
                "Scene saved to: Assets/Scenes/Main.unity\n\n" +
                "Next steps:\n" +
                "• Assign cameras to controllers\n" +
                "• Configure UI elements",
                "OK");
        }

        private GameObject CreateUICanvas()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Portrait orientation
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            return canvasObj;
        }

        private void CreateEventSystem()
        {
            // Check if EventSystem already exists
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                Debug.Log("EventSystem already exists, skipping creation");
                return;
            }

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("Created EventSystem for UI interaction");
        }

        private Camera CreateControllerCamera(Transform parent, string name)
        {
            GameObject cameraObj = new GameObject(name);
            cameraObj.transform.SetParent(parent);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.gameObject.SetActive(false); // Start disabled, controller will enable it

            // Add AudioListener only to the first camera to avoid warnings
            if (name == "BrowseCamera")
            {
                cameraObj.AddComponent<AudioListener>();
            }

            return camera;
        }

        private GameObject CreateTabBar(Transform parent)
        {
            // Create Tab Bar container
            GameObject tabBarObj = new GameObject("TabBar");
            RectTransform tabBarRect = tabBarObj.AddComponent<RectTransform>();
            tabBarRect.SetParent(parent);

            // Anchor to bottom
            tabBarRect.anchorMin = new Vector2(0, 0);
            tabBarRect.anchorMax = new Vector2(1, 0);
            tabBarRect.pivot = new Vector2(0.5f, 0);
            tabBarRect.anchoredPosition = Vector2.zero;
            tabBarRect.sizeDelta = new Vector2(0, 120); // Height

            // Background
            Image bgImage = tabBarObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Horizontal layout
            HorizontalLayoutGroup layout = tabBarObj.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 10, 10);

            // Create 3 tab buttons
            CreateTabButton(tabBarObj.transform, "WorldTab", "Browse", "🌍");
            CreateTabButton(tabBarObj.transform, "BuildTab", "Build", "+");
            CreateTabButton(tabBarObj.transform, "HomeTab", "Home", "🏠");

            return tabBarObj;
        }

        private GameObject CreateTabButton(Transform parent, string name, string label, string icon)
        {
            GameObject btnObj = new GameObject(name);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.SetParent(parent);

            // Button component
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
            colors.selectedColor = new Color(0.25f, 0.5f, 0.8f);
            btn.colors = colors;

            // Button background
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = colors.normalColor;

            // Vertical layout for icon + label
            VerticalLayoutGroup vLayout = btnObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = true;
            vLayout.spacing = 5;
            vLayout.padding = new RectOffset(5, 5, 10, 10);

            // Icon
            GameObject iconObj = new GameObject("Icon");
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.SetParent(btnRect);
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = 36;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = Color.white;

            LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.flexibleHeight = 1;

            // Label
            GameObject labelObj = new GameObject("Label");
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.SetParent(btnRect);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;

            LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;

            return btnObj;
        }
    }
}
