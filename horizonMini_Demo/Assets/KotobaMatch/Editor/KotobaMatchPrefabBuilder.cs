#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility to create the complete KotobaMatch game prefab.
/// Access via menu: GameObject > KotobaMatch > Create Game Prefab
/// </summary>
public class KotobaMatchPrefabBuilder : Editor
{
    private static readonly Color StageColor = new Color(0.875f, 0.902f, 0.914f, 1f);
    private static readonly Color BorderColor = new Color(0.698f, 0.745f, 0.765f, 1f);
    private static readonly Color BackgroundColor = new Color(0.969f, 0.969f, 0.969f, 1f);
    private static readonly Color TextColor = new Color(0.176f, 0.204f, 0.212f, 1f);

    [MenuItem("GameObject/KotobaMatch/Create Game Prefab", false, 10)]
    public static void CreateKotobaMatchPrefab()
    {
        GameObject root = CreateGameHierarchy();

        string prefabPath = "Assets/KotobaMatch/Prefabs/KotobaMatchGame.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/KotobaMatch"))
            AssetDatabase.CreateFolder("Assets", "KotobaMatch");
        if (!AssetDatabase.IsValidFolder("Assets/KotobaMatch/Prefabs"))
            AssetDatabase.CreateFolder("Assets/KotobaMatch", "Prefabs");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Selection.activeObject = prefab;
        Debug.Log($"KotobaMatch prefab created at: {prefabPath}");
    }

    [MenuItem("GameObject/KotobaMatch/Create Game In Scene", false, 11)]
    public static void CreateKotobaMatchInScene()
    {
        GameObject root = CreateGameHierarchy();
        Selection.activeObject = root;
        Debug.Log("KotobaMatch game created in scene");
    }

    private static GameObject CreateGameHierarchy()
    {
        GameObject root = new GameObject("KotobaMatchGame");

        // Game Controller with Input Handler
        GameObject gameManager = new GameObject("GameController");
        gameManager.transform.SetParent(root.transform);
        gameManager.transform.localPosition = Vector3.zero;
        gameManager.AddComponent<KotobaMatchController>();
        gameManager.AddComponent<KotobaInputHandler>();

        // Card Manager (rotated 180 on Y so cards face camera)
        GameObject cardManagerObj = new GameObject("CardManager");
        cardManagerObj.transform.SetParent(root.transform);
        cardManagerObj.transform.localPosition = Vector3.zero;
        // cardManagerObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
        cardManagerObj.AddComponent<CardManager>();

        // Stage
        CreateStage(root);

        // Camera
        GameObject cameraRig = new GameObject("CameraController");
        cameraRig.transform.SetParent(root.transform);
        cameraRig.AddComponent<KotobaCameraController>();

        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.transform.SetParent(cameraRig.transform);
        cameraObj.transform.localPosition = new Vector3(10, 10, 10);
        cameraObj.tag = "MainCamera";

        Camera cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.backgroundColor = BackgroundColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cameraObj.transform.LookAt(Vector3.zero);
        cameraObj.AddComponent<AudioListener>();

        // Lighting
        GameObject lighting = new GameObject("Lighting");
        lighting.transform.SetParent(root.transform);

        GameObject dirLight = new GameObject("Directional Light");
        dirLight.transform.SetParent(lighting.transform);
        dirLight.transform.position = new Vector3(5, 10, 5);
        dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
        Light light = dirLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;

        // Audio Manager
        GameObject audioObj = new GameObject("AudioManager");
        audioObj.transform.SetParent(root.transform);
        audioObj.AddComponent<KotobaAudioManager>();

        // UI Manager with Canvas
        GameObject uiManagerObj = new GameObject("UIManager");
        uiManagerObj.transform.SetParent(root.transform);
        uiManagerObj.AddComponent<KotobaUIManager>();

        // Canvas
        GameObject canvas = new GameObject("Canvas");
        canvas.transform.SetParent(uiManagerObj.transform);
        Canvas canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.AddComponent<GraphicRaycaster>();

        // Timer Text (top left)
        CreateText("TimerText", canvas, "Time: 60s", new Vector2(0, 1), new Vector2(150, -50), 36);

        // Score Text (top right)
        GameObject scoreText = CreateText("ScoreText", canvas, "Score: 0", new Vector2(1, 1), new Vector2(-150, -50), 36);

        // Menu Panel - just "Tap to Play"
        GameObject menuPanel = CreatePanel("MenuPanel", canvas);
        CreateCenteredText("TapText", menuPanel, "Tap to Play", 64);

        // Game Over Panel - just "Tap to Retry"
        GameObject gameOverPanel = CreatePanel("GameOverPanel", canvas);
        gameOverPanel.SetActive(false);
        CreateCenteredText("ScoreText", gameOverPanel, "You matched 0 pairs!", 36, 100f);
        CreateCenteredText("TapText", gameOverPanel, "Tap to Retry", 64, -50f);

        // Wire references
        WireReferences(root);
        CreateAndAssignTheme(root);

        return root;
    }

    private static void CreateStage(GameObject root)
    {
        GameObject stage = new GameObject("Stage");
        stage.transform.SetParent(root.transform);
        stage.transform.localPosition = Vector3.zero;

        float stageSize = 10f;

        // Platform
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.SetParent(stage.transform);
        platform.transform.localPosition = new Vector3(0, -0.25f, 0);
        platform.transform.localScale = new Vector3(stageSize, 0.5f, stageSize);

        Material stageMat = CreateURPMaterial(StageColor);
        platform.GetComponent<MeshRenderer>().material = stageMat;
        platform.GetComponent<MeshRenderer>().receiveShadows = true;

        // Borders
        Material borderMat = CreateURPMaterial(BorderColor);

        CreateBorder("BorderN", stage, borderMat, new Vector3(0, 0, 5.1f), new Vector3(10.4f, 0.8f, 0.2f));
        CreateBorder("BorderS", stage, borderMat, new Vector3(0, 0, -5.1f), new Vector3(10.4f, 0.8f, 0.2f));
        CreateBorder("BorderE", stage, borderMat, new Vector3(5.1f, 0, 0), new Vector3(0.2f, 0.8f, 10.4f));
        CreateBorder("BorderW", stage, borderMat, new Vector3(-5.1f, 0, 0), new Vector3(0.2f, 0.8f, 10.4f));
    }

    private static Material CreateURPMaterial(Color color)
    {
        // Try URP Lit shader first, fall back to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        // Set URP Lit properties
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        // Set surface properties like BlockMaterial
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.5f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    private static void CreateBorder(string name, GameObject parent, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = name;
        border.transform.SetParent(parent.transform);
        border.transform.localPosition = pos;
        border.transform.localScale = scale;
        border.GetComponent<MeshRenderer>().material = mat;
    }

    private static GameObject CreatePanel(string name, GameObject parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0f); // Fully transparent

        return panel;
    }

    private static GameObject CreateText(string name, GameObject parent, string text, Vector2 anchor, Vector2 position, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(400, 80);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextColor;

        return textObj;
    }

    private static GameObject CreateCenteredText(string name, GameObject parent, string text, int fontSize, float yOffset = 0f)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, yOffset);
        rt.sizeDelta = new Vector2(800, 100);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextColor;

        return textObj;
    }

    private static void WireReferences(GameObject root)
    {
        KotobaMatchController controller = root.GetComponentInChildren<KotobaMatchController>();
        CardManager cardManager = root.GetComponentInChildren<CardManager>();
        KotobaUIManager uiManager = root.GetComponentInChildren<KotobaUIManager>();
        KotobaAudioManager audioManager = root.GetComponentInChildren<KotobaAudioManager>();
        KotobaCameraController cameraController = root.GetComponentInChildren<KotobaCameraController>();
        KotobaInputHandler inputHandler = root.GetComponentInChildren<KotobaInputHandler>();
        Camera mainCamera = root.GetComponentInChildren<Camera>();

        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("cardManager").objectReferenceValue = cardManager;
            so.FindProperty("uiManager").objectReferenceValue = uiManager;
            so.FindProperty("audioManager").objectReferenceValue = audioManager;
            so.FindProperty("cameraController").objectReferenceValue = cameraController;
            so.FindProperty("inputHandler").objectReferenceValue = inputHandler;
            so.ApplyModifiedProperties();
        }

        if (inputHandler != null)
        {
            SerializedObject so = new SerializedObject(inputHandler);
            so.FindProperty("gameCamera").objectReferenceValue = mainCamera;
            so.ApplyModifiedProperties();
        }

        if (cameraController != null)
        {
            SerializedObject so = new SerializedObject(cameraController);
            so.FindProperty("gameCamera").objectReferenceValue = mainCamera;
            so.ApplyModifiedProperties();
        }

        Transform canvas = root.transform.Find("UIManager/Canvas");
        if (uiManager != null && canvas != null)
        {
            SerializedObject so = new SerializedObject(uiManager);

            Transform timerText = canvas.Find("TimerText");
            Transform scoreText = canvas.Find("ScoreText");
            Transform menuPanel = canvas.Find("MenuPanel");
            Transform gameOverPanel = canvas.Find("GameOverPanel");
            Transform finalScoreText = gameOverPanel?.Find("ScoreText");

            if (timerText != null)
                so.FindProperty("timerText").objectReferenceValue = timerText.GetComponent<TextMeshProUGUI>();
            if (scoreText != null)
                so.FindProperty("scoreText").objectReferenceValue = scoreText.GetComponent<TextMeshProUGUI>();
            if (menuPanel != null)
                so.FindProperty("menuPanel").objectReferenceValue = menuPanel.gameObject;
            if (gameOverPanel != null)
                so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel.gameObject;
            if (finalScoreText != null)
                so.FindProperty("finalScoreText").objectReferenceValue = finalScoreText.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedProperties();
        }
    }

    private static void CreateAndAssignTheme(GameObject root)
    {
        string themePath = "Assets/KotobaMatch/Themes/DefaultKotobaTheme.asset";

        if (!AssetDatabase.IsValidFolder("Assets/KotobaMatch/Themes"))
            AssetDatabase.CreateFolder("Assets/KotobaMatch", "Themes");

        KotobaTheme theme = AssetDatabase.LoadAssetAtPath<KotobaTheme>(themePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<KotobaTheme>();
            theme.themeName = "Default";
            AssetDatabase.CreateAsset(theme, themePath);
            AssetDatabase.SaveAssets();
        }

        KotobaMatchController controller = root.GetComponentInChildren<KotobaMatchController>();
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("currentTheme").objectReferenceValue = theme;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
