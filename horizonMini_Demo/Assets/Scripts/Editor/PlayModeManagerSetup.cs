using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using HorizonMini.Controllers;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor tool to setup PlayModeManager in Main Scene
    /// Automatically finds and assigns all required references
    /// </summary>
    public class PlayModeManagerSetup : EditorWindow
    {
        [MenuItem("HorizonMini/Setup Play Mode Manager")]
        public static void ShowWindow()
        {
            GetWindow<PlayModeManagerSetup>("Play Mode Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Play Mode Manager Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will automatically setup PlayModeManager in the Main Scene:\n\n" +
                "1. Find or create PlayModeManager GameObject\n" +
                "2. Assign Player Prefab\n" +
                "3. Find and assign BrowseController\n" +
                "4. Find and assign Main Camera\n" +
                "5. Find or create Canvas\n" +
                "6. Create Go button (Browse Mode UI)\n" +
                "7. Create Play Mode UI (Back + Build buttons)",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Setup PlayModeManager", GUILayout.Height(40)))
            {
                SetupPlayModeManager();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Remove PlayModeManager", GUILayout.Height(30)))
            {
                RemovePlayModeManager();
            }
        }

        private static void SetupPlayModeManager()
        {
            Debug.Log("[PlayModeSetup] Starting PlayModeManager setup...");

            // Find or create PlayModeManager
            PlayModeManager manager = Object.FindFirstObjectByType<PlayModeManager>();

            if (manager == null)
            {
                Debug.Log("[PlayModeSetup] Creating new PlayModeManager GameObject");
                GameObject managerObj = new GameObject("PlayModeManager");
                manager = managerObj.AddComponent<PlayModeManager>();
                Undo.RegisterCreatedObjectUndo(managerObj, "Create PlayModeManager");
            }
            else
            {
                Debug.Log($"[PlayModeSetup] Found existing PlayModeManager: {manager.gameObject.name}");
                Undo.RecordObject(manager, "Setup PlayModeManager");
            }

            // Get SerializedObject for private field access
            SerializedObject so = new SerializedObject(manager);

            // 1. Assign Player Prefab
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                SerializedProperty playerPrefabProp = so.FindProperty("playerPrefab");
                playerPrefabProp.objectReferenceValue = playerPrefab;
                Debug.Log("[PlayModeSetup] ✓ Assigned Player Prefab");
            }
            else
            {
                Debug.LogWarning("[PlayModeSetup] Player.prefab not found at Assets/Prefabs/Player.prefab");
            }

            // 2. Find and assign BrowseController
            BrowseController browseController = Object.FindFirstObjectByType<BrowseController>();
            if (browseController != null)
            {
                SerializedProperty browseControllerProp = so.FindProperty("browseController");
                browseControllerProp.objectReferenceValue = browseController;
                Debug.Log($"[PlayModeSetup] ✓ Assigned BrowseController: {browseController.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[PlayModeSetup] BrowseController not found in scene!");
            }

            // 3. Find and assign Main Camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                SerializedProperty mainCameraProp = so.FindProperty("mainCamera");
                mainCameraProp.objectReferenceValue = mainCamera;
                Debug.Log($"[PlayModeSetup] ✓ Assigned Main Camera: {mainCamera.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[PlayModeSetup] Main Camera not found in scene!");
            }

            // 4. Find or create Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.Log("[PlayModeSetup] Creating Canvas");
                GameObject canvasObj = new GameObject("PlayModeCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
                Debug.Log("[PlayModeSetup] ✓ Created Canvas");
            }
            else
            {
                Debug.Log($"[PlayModeSetup] ✓ Found existing Canvas: {canvas.gameObject.name}");
            }

            // 5. Create Go Button (Browse Mode UI)
            GameObject goButtonObj = CreateGoButton(canvas.transform, so);

            // 6. Create Play Mode UI (Back + Build buttons)
            GameObject playModeUIObj = CreatePlayModeUI(canvas.transform, so);

            // Apply all changes
            so.ApplyModifiedProperties();

            Debug.Log("<color=green>[PlayModeSetup] ✓ PlayModeManager setup complete!</color>");

            EditorUtility.SetDirty(manager);
        }

        private static GameObject CreateGoButton(Transform canvasTransform, SerializedObject managerSO)
        {
            // Check if Go button already exists
            Transform existingGo = canvasTransform.Find("GoButton");
            if (existingGo != null)
            {
                Debug.Log("[PlayModeSetup] Go button already exists, skipping creation");

                // Assign to manager
                SerializedProperty existingGoButtonProp = managerSO.FindProperty("goButton");
                SerializedProperty existingGoButtonObjProp = managerSO.FindProperty("goButtonObject");

                Button btn = existingGo.GetComponent<Button>();
                if (btn != null)
                {
                    existingGoButtonProp.objectReferenceValue = btn;
                    existingGoButtonObjProp.objectReferenceValue = existingGo.gameObject;
                }

                return existingGo.gameObject;
            }

            Debug.Log("[PlayModeSetup] Creating Go button");

            // Create Go button
            GameObject goButtonObject = new GameObject("GoButton");
            goButtonObject.transform.SetParent(canvasTransform);
            Undo.RegisterCreatedObjectUndo(goButtonObject, "Create Go Button");

            RectTransform rect = goButtonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.1f);
            rect.anchorMax = new Vector2(0.5f, 0.1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(200, 80);

            Image image = goButtonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.8f, 0.3f, 0.9f);

            Button goButton = goButtonObject.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(goButtonObject.transform);
            Undo.RegisterCreatedObjectUndo(textObj, "Create Go Button Text");

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Go";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 32;
            text.fontStyle = FontStyles.Bold;

            // Assign to manager
            SerializedProperty goButtonProp = managerSO.FindProperty("goButton");
            SerializedProperty goButtonObjProp = managerSO.FindProperty("goButtonObject");
            goButtonProp.objectReferenceValue = goButton;
            goButtonObjProp.objectReferenceValue = goButtonObject;

            // Initially hide
            goButtonObject.SetActive(false);

            Debug.Log("[PlayModeSetup] ✓ Created Go button");
            return goButtonObject;
        }

        private static GameObject CreatePlayModeUI(Transform canvasTransform, SerializedObject managerSO)
        {
            // Check if Play Mode UI already exists
            Transform existingUI = canvasTransform.Find("PlayModeUI");
            if (existingUI != null)
            {
                Debug.Log("[PlayModeSetup] Play Mode UI already exists, skipping creation");

                // Assign to manager
                SerializedProperty existingPlayModeUIProp = managerSO.FindProperty("playModeUI");
                SerializedProperty existingBackButtonProp = managerSO.FindProperty("backButton");
                SerializedProperty existingBuildButtonProp = managerSO.FindProperty("buildButton");

                existingPlayModeUIProp.objectReferenceValue = existingUI.gameObject;

                Button backBtn = existingUI.Find("BackButton")?.GetComponent<Button>();
                Button buildBtn = existingUI.Find("BuildButton")?.GetComponent<Button>();

                if (backBtn != null) existingBackButtonProp.objectReferenceValue = backBtn;
                if (buildBtn != null) existingBuildButtonProp.objectReferenceValue = buildBtn;

                return existingUI.gameObject;
            }

            Debug.Log("[PlayModeSetup] Creating Play Mode UI");

            // Create container
            GameObject playModeUI = new GameObject("PlayModeUI");
            playModeUI.transform.SetParent(canvasTransform);
            Undo.RegisterCreatedObjectUndo(playModeUI, "Create Play Mode UI");

            // Create Back button (top-left)
            GameObject backObj = new GameObject("BackButton");
            backObj.transform.SetParent(playModeUI.transform);
            Undo.RegisterCreatedObjectUndo(backObj, "Create Back Button");

            RectTransform backRect = backObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 1);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.anchoredPosition = new Vector2(20, -20);
            backRect.sizeDelta = new Vector2(120, 50);

            Image backImage = backObj.AddComponent<Image>();
            backImage.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);

            Button backButton = backObj.AddComponent<Button>();

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backObj.transform);
            Undo.RegisterCreatedObjectUndo(backTextObj, "Create Back Button Text");

            RectTransform backTextRect = backTextObj.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI backText = backTextObj.AddComponent<TextMeshProUGUI>();
            backText.text = "Back";
            backText.alignment = TextAlignmentOptions.Center;
            backText.color = Color.white;
            backText.fontSize = 20;

            // Create Build button (top-right)
            GameObject buildObj = new GameObject("BuildButton");
            buildObj.transform.SetParent(playModeUI.transform);
            Undo.RegisterCreatedObjectUndo(buildObj, "Create Build Button");

            RectTransform buildRect = buildObj.AddComponent<RectTransform>();
            buildRect.anchorMin = new Vector2(1, 1);
            buildRect.anchorMax = new Vector2(1, 1);
            buildRect.pivot = new Vector2(1, 1);
            buildRect.anchoredPosition = new Vector2(-20, -20);
            buildRect.sizeDelta = new Vector2(120, 50);

            Image buildImage = buildObj.AddComponent<Image>();
            buildImage.color = new Color(0.3f, 0.5f, 0.8f, 0.9f);

            Button buildButton = buildObj.AddComponent<Button>();

            GameObject buildTextObj = new GameObject("Text");
            buildTextObj.transform.SetParent(buildObj.transform);
            Undo.RegisterCreatedObjectUndo(buildTextObj, "Create Build Button Text");

            RectTransform buildTextRect = buildTextObj.AddComponent<RectTransform>();
            buildTextRect.anchorMin = Vector2.zero;
            buildTextRect.anchorMax = Vector2.one;
            buildTextRect.offsetMin = Vector2.zero;
            buildTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buildText = buildTextObj.AddComponent<TextMeshProUGUI>();
            buildText.text = "Build";
            buildText.alignment = TextAlignmentOptions.Center;
            buildText.color = Color.white;
            buildText.fontSize = 20;

            // Assign to manager
            SerializedProperty playModeUIProp = managerSO.FindProperty("playModeUI");
            SerializedProperty backButtonProp = managerSO.FindProperty("backButton");
            SerializedProperty buildButtonProp = managerSO.FindProperty("buildButton");

            playModeUIProp.objectReferenceValue = playModeUI;
            backButtonProp.objectReferenceValue = backButton;
            buildButtonProp.objectReferenceValue = buildButton;

            // Initially hide
            playModeUI.SetActive(false);

            Debug.Log("[PlayModeSetup] ✓ Created Play Mode UI (Back + Build buttons)");
            return playModeUI;
        }

        private static void RemovePlayModeManager()
        {
            PlayModeManager manager = Object.FindFirstObjectByType<PlayModeManager>();

            if (manager != null)
            {
                if (EditorUtility.DisplayDialog(
                    "Remove PlayModeManager",
                    $"Are you sure you want to remove PlayModeManager from '{manager.gameObject.name}'?",
                    "Yes", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(manager.gameObject);
                    Debug.Log("[PlayModeSetup] Removed PlayModeManager");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", "No PlayModeManager found in scene.", "OK");
            }
        }
    }
}
