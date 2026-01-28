using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HorizonMini.MiniGames.Editor
{
    /// <summary>
    /// Editor tool to quickly setup mini games in a World
    /// </summary>
    public class MiniGameWorldSetup : EditorWindow
    {
        private GameObject cubeStackPrefab;
        private Vector3 triggerPosition = Vector3.zero;
        private float triggerRadius = 2f;

        [MenuItem("Tools/MiniGames/Setup in World")]
        public static void ShowWindow()
        {
            var window = GetWindow<MiniGameWorldSetup>("MiniGame World Setup");
            window.minSize = new Vector2(400, 400);
        }

        private void OnEnable()
        {
            // Try to find CubeStackGame prefab
            string[] guids = AssetDatabase.FindAssets("CubeStackGame t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                cubeStackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("MiniGame World Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will setup a CubeStack mini game in your current World scene.\n\n" +
                "It will create:\n" +
                "• CubeStackMiniGame manager\n" +
                "• Exit button UI\n" +
                "• Game trigger object (arcade machine style)\n" +
                "• Proper component connections",
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

            EditorGUILayout.Space();
            GUILayout.Label("Trigger Settings:", EditorStyles.boldLabel);

            triggerPosition = EditorGUILayout.Vector3Field("Trigger Position", triggerPosition);
            triggerRadius = EditorGUILayout.FloatField("Trigger Radius", triggerRadius);

            EditorGUILayout.Space();

            GUI.enabled = cubeStackPrefab != null;
            if (GUILayout.Button("Setup MiniGame in World", GUILayout.Height(40)))
            {
                SetupMiniGameInWorld();
            }
            GUI.enabled = true;

            if (cubeStackPrefab == null)
            {
                EditorGUILayout.HelpBox("Please assign the CubeStackGame prefab first!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            GUILayout.Label("After Setup:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Enter Play mode\n" +
                "2. Walk your character near the trigger object\n" +
                "3. Press [E] when prompt appears\n" +
                "4. Play the Stack game!\n" +
                "5. Click Exit button to return to world",
                MessageType.Info
            );
        }

        private void SetupMiniGameInWorld()
        {
            Debug.Log("<color=cyan>[MiniGame Setup] Setting up CubeStack in World...</color>");

            // 1. Create manager GameObject
            GameObject managerObj = new GameObject("CubeStackMiniGameManager");
            CubeStackMiniGame miniGame = managerObj.AddComponent<CubeStackMiniGame>();

            // Set prefab reference
            SerializedObject so = new SerializedObject(miniGame);
            so.FindProperty("cubeStackGamePrefab").objectReferenceValue = cubeStackPrefab;
            so.ApplyModifiedProperties();

            // 2. Create Exit Button UI
            CreateExitButtonUI(miniGame);

            // 3. Create Trigger Object
            CreateTriggerObject(miniGame);

            Debug.Log("<color=green>[MiniGame Setup] ✓ Setup complete!</color>");

            EditorUtility.DisplayDialog(
                "Setup Complete",
                "CubeStack mini game has been added to your World!\n\n" +
                "Created:\n" +
                "• CubeStackMiniGameManager\n" +
                "• Exit Button UI\n" +
                "• Game Trigger at " + triggerPosition,
                "OK"
            );

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private void CreateExitButtonUI(CubeStackMiniGame miniGame)
        {
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MiniGameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Create Exit Button
            GameObject buttonObj = new GameObject("ExitButton");
            buttonObj.transform.SetParent(canvas.transform);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(120, 50);

            UnityEngine.UI.Image image = buttonObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

            UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = "Exit Game";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;

            // Connect button to miniGame
            button.onClick.AddListener(() => miniGame.OnExitButtonClicked());

            // Hide by default
            buttonObj.SetActive(false);

            // Assign to miniGame
            SerializedObject so = new SerializedObject(miniGame);
            so.FindProperty("exitButton").objectReferenceValue = buttonObj;
            so.FindProperty("gameCanvas").objectReferenceValue = canvas;
            so.ApplyModifiedProperties();

            Debug.Log("[MiniGame Setup] Created Exit Button UI");
        }

        private void CreateTriggerObject(CubeStackMiniGame miniGame)
        {
            // Create trigger GameObject (styled like an arcade machine)
            GameObject triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triggerObj.name = "StackGame_Trigger";
            triggerObj.transform.position = triggerPosition;
            triggerObj.transform.localScale = new Vector3(1.5f, 2f, 1f);

            // Color it bright to stand out
            Renderer renderer = triggerObj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.6f, 1f); // Bright blue
            renderer.material = mat;

            // Add trigger component
            MiniGameTrigger trigger = triggerObj.AddComponent<MiniGameTrigger>();

            // Configure trigger
            SerializedObject soTrigger = new SerializedObject(trigger);
            soTrigger.FindProperty("miniGame").objectReferenceValue = miniGame;
            soTrigger.FindProperty("triggerRadius").floatValue = triggerRadius;
            soTrigger.FindProperty("gameName").stringValue = "Stack";
            soTrigger.ApplyModifiedProperties();

            Debug.Log($"[MiniGame Setup] Created trigger at {triggerPosition}");
        }
    }
}
