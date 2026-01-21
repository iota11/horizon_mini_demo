using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using HorizonMini.Controllers;
using HorizonMini.Build;
using HorizonMini.UI;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to automatically setup Play Scene
    /// </summary>
    public class SetupPlayScene
    {
        [MenuItem("HorizonMini/Setup Play Scene")]
        public static void Setup()
        {
            // Create or load Play scene
            Scene playScene = CreatePlayScene();
            if (!playScene.IsValid())
            {
                Debug.LogError("[SetupPlayScene] Failed to create/load Play scene");
                return;
            }

            // Create PlayController
            GameObject playControllerObj = CreatePlayController();

            // Create Play Camera
            Camera playCamera = CreatePlayCamera();

            // Create EventSystem (required for UI interaction)
            CreateEventSystem();

            // Create Virtual Joystick UI
            GameObject joystickCanvas = CreateVirtualJoystickUI();

            // Create Play UI Panel (Exit/Home/Build buttons)
            GameObject playUI = CreatePlayUIPanel(playControllerObj.GetComponent<PlayController>());

            // Connect references
            ConnectReferences(playControllerObj, playCamera, joystickCanvas);

            // Save scene
            EditorSceneManager.MarkSceneDirty(playScene);
            EditorSceneManager.SaveScene(playScene);

            bool addToBuildSettings = EditorUtility.DisplayDialog("Success",
                "Play Scene setup completed!\n\n" +
                "Created:\n" +
                "✅ Play scene (Assets/Scenes/Play.unity)\n" +
                "✅ PlayController\n" +
                "✅ PlayCamera\n" +
                "✅ VirtualJoystickCanvas\n" +
                "✅ PlayUI Panel with buttons\n\n" +
                "⚠️ IMPORTANT - Next Steps:\n\n" +
                "1. Assign Player Prefab to PlayController\n" +
                "   Recommended: TopDownEngine/Demos/Minimal3D/Prefabs/MinimalCharacter\n\n" +
                "2. Add scenes to Build Settings:\n" +
                "   File → Build Settings → Add Open Scenes\n" +
                "   Required scenes: Main, Build, Play\n\n" +
                "Would you like to open Build Settings now?",
                "Yes, Open Build Settings",
                "No, I'll do it later");

            if (addToBuildSettings)
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }

            // Select PlayController
            Selection.activeGameObject = playControllerObj;
        }

        private static Scene CreatePlayScene()
        {
            // Check if Play scene already exists
            Scene existingScene = EditorSceneManager.GetSceneByName("Play");
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                Debug.Log("[SetupPlayScene] Play scene already loaded, using existing");
                return existingScene;
            }

            // Try to load existing scene file
            string scenePath = "Assets/Scenes/Play.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Debug.Log("[SetupPlayScene] Loading existing Play scene");
                return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Create new scene
            Debug.Log("[SetupPlayScene] Creating new Play scene");
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ensure Scenes directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // Save scene
            EditorSceneManager.SaveScene(newScene, scenePath);

            return newScene;
        }

        private static GameObject CreatePlayController()
        {
            // Check if already exists
            PlayController existing = Object.FindFirstObjectByType<PlayController>();
            if (existing != null)
            {
                Debug.Log("[SetupPlayScene] PlayController already exists, reusing");
                return existing.gameObject;
            }

            GameObject obj = new GameObject("PlayController");
            PlayController controller = obj.AddComponent<PlayController>();

            Debug.Log("[SetupPlayScene] Created PlayController");
            return obj;
        }

        private static Camera CreatePlayCamera()
        {
            // Check if already exists
            Camera existingCam = Object.FindFirstObjectByType<Camera>();
            if (existingCam != null)
            {
                Debug.Log("[SetupPlayScene] Camera already exists, reusing");
                return existingCam;
            }

            // Create camera
            GameObject camObj = new GameObject("PlayCamera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.tag = "MainCamera";

            camObj.AddComponent<AudioListener>();

            Debug.Log("[SetupPlayScene] Created PlayCamera");
            return cam;
        }

        private static void CreateEventSystem()
        {
            // Check if EventSystem already exists
            UnityEngine.EventSystems.EventSystem existingEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem != null)
            {
                Debug.Log("[SetupPlayScene] EventSystem already exists");
                return;
            }

            // Create EventSystem GameObject
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("[SetupPlayScene] Created EventSystem");
        }

        private static GameObject CreateVirtualJoystickUI()
        {
            // Check if already exists
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var existingCanvas in canvases)
            {
                if (existingCanvas.name == "VirtualJoystickCanvas")
                {
                    Debug.Log("[SetupPlayScene] VirtualJoystickCanvas already exists, reusing");
                    existingCanvas.gameObject.SetActive(false); // Should start inactive
                    return existingCanvas.gameObject;
                }
            }

            // Create Canvas
            GameObject canvasObj = new GameObject("VirtualJoystickCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Above other UI

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Joystick Background
            GameObject bgObj = new GameObject("JoystickBackground");
            bgObj.transform.SetParent(canvasObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0); // Bottom left
            bgRect.anchorMax = new Vector2(0, 0);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(150, 150);
            bgRect.sizeDelta = new Vector2(200, 200);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.3f); // Semi-transparent white

            // Create circle sprite
            Texture2D circleTex = CreateCircleTexture(256);
            Sprite circleSprite = Sprite.Create(circleTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            bgImage.sprite = circleSprite;

            // Create Joystick Handle
            GameObject handleObj = new GameObject("JoystickHandle");
            handleObj.transform.SetParent(bgObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80, 80);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.8f);
            handleImage.sprite = circleSprite;

            // Add VirtualJoystick component
            VirtualJoystick joystick = bgObj.AddComponent<VirtualJoystick>();

            // Use SerializedObject to set private fields
            SerializedObject joystickSO = new SerializedObject(joystick);
            joystickSO.FindProperty("background").objectReferenceValue = bgRect;
            joystickSO.FindProperty("handle").objectReferenceValue = handleRect;
            joystickSO.FindProperty("handleRange").floatValue = 50f;
            joystickSO.FindProperty("returnToCenter").boolValue = true;
            joystickSO.ApplyModifiedProperties();

            // Start inactive
            canvasObj.SetActive(false);

            Debug.Log("[SetupPlayMode] Created VirtualJoystickCanvas with joystick");
            return canvasObj;
        }

        private static GameObject CreatePlayUIPanel(PlayController playController)
        {
            // Find or create main canvas
            Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
            if (mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Check if already exists - delete and recreate to ensure proper setup
            Transform existing = mainCanvas.transform.Find("PlayUI");
            if (existing != null)
            {
                Debug.Log("[SetupPlayScene] PlayUI already exists, deleting to recreate with proper event bindings");
                Object.DestroyImmediate(existing.gameObject);
            }

            // Create Panel
            GameObject panelObj = new GameObject("PlayUI");
            panelObj.transform.SetParent(mainCanvas.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0); // Transparent background

            // Create Main Button (top left)
            CreateButton(panelObj, "MainButton", "Main", new Vector2(20, -20), new Vector2(0, 1), new Vector2(0, 1),
                new Color(0.3f, 0.5f, 0.8f), () => playController.ReturnToMain());

            // Create Build Button (top middle)
            CreateButton(panelObj, "BuildButton", "Build", new Vector2(0, -20), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Color(0.5f, 0.7f, 0.3f), () => playController.ReturnToBuild());

            Debug.Log("[SetupPlayScene] Created PlayUI panel with buttons");
            return panelObj;
        }

        private static void CreateButton(GameObject parent, string name, string label, Vector2 anchoredPos,
            Vector2 anchorMin, Vector2 anchorMax, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.pivot = new Vector2(0.5f, 1);
            buttonRect.anchoredPosition = anchoredPos;
            buttonRect.sizeDelta = new Vector2(140, 60);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;

            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            // Set button colors
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }

        private static void ConnectReferences(GameObject playControllerObj, Camera playCamera, GameObject joystickCanvas)
        {
            PlayController controller = playControllerObj.GetComponent<PlayController>();
            VirtualJoystick joystick = joystickCanvas.GetComponentInChildren<VirtualJoystick>();

            // Set PlayController fields
            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("playCamera").objectReferenceValue = playCamera;
            controllerSO.FindProperty("virtualJoystickUI").objectReferenceValue = joystickCanvas;
            controllerSO.FindProperty("virtualJoystick").objectReferenceValue = joystick;
            controllerSO.ApplyModifiedProperties();

            Debug.Log("[SetupPlayScene] All references connected");
        }

        /// <summary>
        /// Create a simple circle texture for joystick
        /// </summary>
        private static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius)
                    {
                        // Smooth edge with anti-aliasing
                        float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }
}
