using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace CubeStack.Editor
{
    /// <summary>
    /// Editor tool to automatically setup a CubeStack game scene
    /// </summary>
    public class CubeStackSceneSetup : EditorWindow
    {
        private GameObject cubeStackPrefab;
        private StackTheme defaultTheme;
        private bool createUI = true;
        private bool createCamera = true;
        private bool createLighting = true;
        private Vector2 scrollPosition;

        [MenuItem("Tools/CubeStack/Setup Scene")]
        public static void ShowWindow()
        {
            var window = GetWindow<CubeStackSceneSetup>("CubeStack Scene Setup");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            // Try to find the CubeStackGame prefab automatically
            string[] guids = AssetDatabase.FindAssets("CubeStackGame t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                cubeStackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            // Try to find the default theme
            guids = AssetDatabase.FindAssets("DefaultTheme t:StackTheme");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                defaultTheme = AssetDatabase.LoadAssetAtPath<StackTheme>(path);
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("CubeStack Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will automatically setup a complete CubeStack game scene with all required components.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Prefab reference
            cubeStackPrefab = (GameObject)EditorGUILayout.ObjectField(
                "CubeStack Prefab",
                cubeStackPrefab,
                typeof(GameObject),
                false
            );

            // Theme reference
            defaultTheme = (StackTheme)EditorGUILayout.ObjectField(
                "Default Theme",
                defaultTheme,
                typeof(StackTheme),
                false
            );

            EditorGUILayout.Space();

            // Options
            GUILayout.Label("Setup Options:", EditorStyles.boldLabel);
            createUI = EditorGUILayout.Toggle("Create UI Elements", createUI);
            createCamera = EditorGUILayout.Toggle("Create Camera", createCamera);
            createLighting = EditorGUILayout.Toggle("Create Lighting", createLighting);

            EditorGUILayout.Space();

            // Setup button
            GUI.enabled = cubeStackPrefab != null;
            if (GUILayout.Button("Setup Scene", GUILayout.Height(40)))
            {
                SetupScene();
            }
            GUI.enabled = true;

            if (cubeStackPrefab == null)
            {
                EditorGUILayout.HelpBox("Please assign the CubeStackGame prefab first!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Instructions
            GUILayout.Label("After Setup:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Press Play\n" +
                "2. Click anywhere to start the game\n" +
                "3. Click to place blocks as they swing\n" +
                "4. Try to align perfectly for higher scores!",
                MessageType.Info
            );

            EditorGUILayout.EndScrollView();
        }

        private void SetupScene()
        {
            Debug.Log("<color=cyan>[CubeStack Setup] Starting scene setup...</color>");

            // 1. Create/Find Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null || createUI)
            {
                canvas = CreateCanvas();
            }

            // 2. Create UI elements
            if (createUI)
            {
                CreateUIElements(canvas);
            }

            // 3. Instantiate CubeStackGame prefab
            GameObject gameInstance = (GameObject)PrefabUtility.InstantiatePrefab(cubeStackPrefab);
            gameInstance.name = "CubeStackGame";

            // 4. Configure GameController
            ConfigureGameController(gameInstance, canvas);

            // 5. Create Camera
            if (createCamera)
            {
                CreateOrConfigureCamera(gameInstance);
            }

            // 6. Create Lighting
            if (createLighting)
            {
                CreateLighting();
            }

            // 7. Ensure EventSystem exists
            EnsureEventSystem();

            Debug.Log("<color=green>[CubeStack Setup] ✓ Scene setup complete!</color>");
            EditorUtility.DisplayDialog(
                "Setup Complete",
                "CubeStack scene has been setup successfully!\n\nPress Play to test the game.",
                "OK"
            );
        }

        private Canvas CreateCanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[CubeStack Setup] Created Canvas");
            return canvas;
        }

        private void CreateUIElements(Canvas canvas)
        {
            // 1. Score Text
            GameObject scoreTextObj = CreateTextMeshPro(canvas.transform, "ScoreText");
            RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0, -50);
            scoreRect.sizeDelta = new Vector2(200, 100);

            TextMeshProUGUI scoreText = scoreTextObj.GetComponent<TextMeshProUGUI>();
            scoreText.text = "0";
            scoreText.fontSize = 72;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = Color.white;

            // 2. Menu Panel
            GameObject menuPanel = CreatePanel(canvas.transform, "MenuPanel");
            GameObject menuText = CreateTextMeshPro(menuPanel.transform, "TapToStart");
            TextMeshProUGUI menuTmp = menuText.GetComponent<TextMeshProUGUI>();
            menuTmp.text = "TAP TO START";
            menuTmp.fontSize = 48;
            menuTmp.alignment = TextAlignmentOptions.Center;
            menuTmp.color = Color.white;

            // 3. Game Over Panel
            GameObject gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel");
            gameOverPanel.SetActive(false);

            // Game Over Text
            GameObject goText = CreateTextMeshPro(gameOverPanel.transform, "GameOverText");
            RectTransform goRect = goText.GetComponent<RectTransform>();
            goRect.anchoredPosition = new Vector2(0, 100);
            TextMeshProUGUI goTmp = goText.GetComponent<TextMeshProUGUI>();
            goTmp.text = "GAME OVER";
            goTmp.fontSize = 60;
            goTmp.alignment = TextAlignmentOptions.Center;
            goTmp.color = Color.red;

            // Final Score Text
            GameObject finalScoreObj = CreateTextMeshPro(gameOverPanel.transform, "FinalScoreText");
            RectTransform fsRect = finalScoreObj.GetComponent<RectTransform>();
            fsRect.anchoredPosition = new Vector2(0, 0);
            TextMeshProUGUI fsTmp = finalScoreObj.GetComponent<TextMeshProUGUI>();
            fsTmp.text = "Score: 0";
            fsTmp.fontSize = 48;
            fsTmp.alignment = TextAlignmentOptions.Center;
            fsTmp.color = Color.white;

            // Tap to Restart
            GameObject restartText = CreateTextMeshPro(gameOverPanel.transform, "TapToRestart");
            RectTransform restartRect = restartText.GetComponent<RectTransform>();
            restartRect.anchoredPosition = new Vector2(0, -100);
            TextMeshProUGUI restartTmp = restartText.GetComponent<TextMeshProUGUI>();
            restartTmp.text = "TAP TO RESTART";
            restartTmp.fontSize = 36;
            restartTmp.alignment = TextAlignmentOptions.Center;
            restartTmp.color = Color.white;

            // 4. Perfect Text
            GameObject perfectTextObj = CreateTextMeshPro(canvas.transform, "PerfectText");
            RectTransform perfectRect = perfectTextObj.GetComponent<RectTransform>();
            perfectRect.anchorMin = new Vector2(0.5f, 0.5f);
            perfectRect.anchorMax = new Vector2(0.5f, 0.5f);
            perfectRect.anchoredPosition = new Vector2(0, 0);
            perfectRect.sizeDelta = new Vector2(400, 100);

            TextMeshProUGUI perfectText = perfectTextObj.GetComponent<TextMeshProUGUI>();
            perfectText.text = "PERFECT!";
            perfectText.fontSize = 60;
            perfectText.alignment = TextAlignmentOptions.Center;
            perfectText.color = Color.yellow;
            perfectTextObj.SetActive(false);

            Debug.Log("[CubeStack Setup] Created UI elements");
        }

        private GameObject CreateTextMeshPro(Transform parent, string name)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 100);
            rect.localScale = Vector3.one;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;

            return textObj;
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent);

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            Image image = panelObj.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);

            return panelObj;
        }

        private void ConfigureGameController(GameObject gameInstance, Canvas canvas)
        {
            GameController controller = gameInstance.GetComponent<GameController>();
            if (controller == null)
            {
                Debug.LogError("[CubeStack Setup] GameController not found on prefab!");
                return;
            }

            // Set theme
            if (defaultTheme != null)
            {
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty themeProp = so.FindProperty("currentTheme");
                themeProp.objectReferenceValue = defaultTheme;
                so.ApplyModifiedProperties();
            }

            // Find and assign UI elements
            UIManager uiManager = gameInstance.GetComponentInChildren<UIManager>();
            if (uiManager != null && canvas != null)
            {
                SerializedObject soUI = new SerializedObject(uiManager);

                Transform scoreText = canvas.transform.Find("ScoreText");
                if (scoreText != null)
                {
                    soUI.FindProperty("scoreText").objectReferenceValue = scoreText.GetComponent<TextMeshProUGUI>();
                }

                Transform menuPanel = canvas.transform.Find("MenuPanel");
                if (menuPanel != null)
                {
                    soUI.FindProperty("menuPanel").objectReferenceValue = menuPanel.gameObject;
                }

                Transform gameOverPanel = canvas.transform.Find("GameOverPanel");
                if (gameOverPanel != null)
                {
                    soUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel.gameObject;

                    Transform finalScoreText = gameOverPanel.Find("FinalScoreText");
                    if (finalScoreText != null)
                    {
                        soUI.FindProperty("finalScoreText").objectReferenceValue = finalScoreText.GetComponent<TextMeshProUGUI>();
                    }
                }

                Transform perfectText = canvas.transform.Find("PerfectText");
                if (perfectText != null)
                {
                    soUI.FindProperty("perfectText").objectReferenceValue = perfectText.GetComponent<TextMeshProUGUI>();
                }

                soUI.ApplyModifiedProperties();
            }

            Debug.Log("[CubeStack Setup] Configured GameController and UIManager");
        }

        private void CreateOrConfigureCamera(GameObject gameInstance)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Create new camera
                GameObject cameraObj = new GameObject("Main Camera");
                cameraObj.tag = "MainCamera";
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();

                Debug.Log("[CubeStack Setup] Created Main Camera");
            }

            // Configure camera position
            mainCamera.transform.position = new Vector3(0, 10, -10);
            mainCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
            mainCamera.fieldOfView = 60;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f);

            // Try to assign to CameraController
            CameraController cameraController = gameInstance.GetComponentInChildren<CameraController>();
            if (cameraController != null)
            {
                cameraController.gameObject.AddComponent<Camera>();
                cameraController.transform.position = mainCamera.transform.position;
                cameraController.transform.rotation = mainCamera.transform.rotation;

                // Disable the main camera, use CameraController's camera instead
                mainCamera.gameObject.SetActive(false);

                Debug.Log("[CubeStack Setup] Configured CameraController");
            }
        }

        private void CreateLighting()
        {
            Light existingLight = FindObjectOfType<Light>();
            if (existingLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

                Debug.Log("[CubeStack Setup] Created Directional Light");
            }
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();

                Debug.Log("[CubeStack Setup] Created EventSystem");
            }
        }
    }
}
