using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using HorizonMini.UI;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor tool to setup AI Scene Generator UI in Build scene
    /// </summary>
    public class AISceneGeneratorUISetup
    {
        [MenuItem("HorizonMini/Setup AI Scene Generator UI")]
        public static void SetupUI()
        {
            // Find Canvas
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in scene!", "OK");
                return;
            }

            // Find or create AISceneGeneratorPanel
            GameObject aiPanel = CreateAIGeneratorPanel(canvas.transform);

            // Find ViewModeUI and add AI Generate button
            Transform viewModeUI = canvas.transform.Find("ViewModeUI");
            if (viewModeUI == null)
            {
                EditorUtility.DisplayDialog("Error", "ViewModeUI not found in Canvas!", "OK");
                return;
            }

            Button aiButton = CreateAIGenerateButton(viewModeUI);

            // Find BuildModeUI and wire up references
            BuildModeUI buildModeUI = GameObject.FindObjectOfType<BuildModeUI>();
            if (buildModeUI == null)
            {
                EditorUtility.DisplayDialog("Error", "BuildModeUI not found in scene!", "OK");
                return;
            }

            WireUpBuildModeUI(buildModeUI, aiPanel, aiButton);

            EditorUtility.DisplayDialog(
                "Success",
                "✓ AI Scene Generator UI setup complete!\n\n" +
                "Created:\n" +
                "- AISceneGeneratorPanel\n" +
                "- AI Generate Button in ViewModeUI\n" +
                "- Wired references in BuildModeUI",
                "OK"
            );

            Debug.Log("<color=green>✓ AI Scene Generator UI setup complete!</color>");
        }

        private static GameObject CreateAIGeneratorPanel(Transform canvasTransform)
        {
            // Create simple prompt panel from scratch
            GameObject aiPanel = new GameObject("AISceneGeneratorPanel");
            aiPanel.transform.SetParent(canvasTransform, false);

            // Setup RectTransform (centered dialog)
            RectTransform rectTransform = aiPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(600, 400);

            // Add background
            Image bgImage = aiPanel.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // Add AISceneGeneratorUI component
            AISceneGeneratorUI aiComponent = aiPanel.AddComponent<AISceneGeneratorUI>();

            // Create simple prompt UI structure
            CreateSimplePromptUI(aiPanel.transform, aiComponent);

            // Hide by default
            aiPanel.SetActive(false);

            Debug.Log("✓ Created simple AISceneGeneratorPanel");

            return aiPanel;
        }

        private static void CreateSimplePromptUI(Transform panelTransform, AISceneGeneratorUI aiComponent)
        {
            // Create main content area
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(panelTransform, false);

            RectTransform contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -20);

            // Add vertical layout
            VerticalLayoutGroup layout = contentArea.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(contentArea.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "AI Scene Generator";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 40);

            // Create input field
            GameObject inputFieldObj = new GameObject("PromptInputField");
            inputFieldObj.transform.SetParent(contentArea.transform, false);

            Image inputBg = inputFieldObj.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            RectTransform inputRect = inputFieldObj.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(0, 150);

            // Create text area for input field
            GameObject textAreaObj = new GameObject("TextArea");
            textAreaObj.transform.SetParent(inputFieldObj.transform, false);
            RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 10);
            textAreaRect.offsetMax = new Vector2(-10, -10);

            // Create placeholder text
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textAreaObj.transform, false);
            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Enter your prompt here... (e.g., 'Create a medieval village')";
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.fontStyle = FontStyles.Italic;

            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            // Create input text
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textAreaObj.transform, false);
            TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputText.text = "";
            inputText.fontSize = 14;
            inputText.color = Color.white;

            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;

            // Create button row
            GameObject buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(contentArea.transform, false);

            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childControlWidth = true;

            RectTransform buttonRowRect = buttonRow.GetComponent<RectTransform>();
            buttonRowRect.sizeDelta = new Vector2(0, 50);

            // Create Generate button
            GameObject generateButtonObj = new GameObject("GenerateButton");
            generateButtonObj.transform.SetParent(buttonRow.transform, false);

            Image generateBg = generateButtonObj.AddComponent<Image>();
            generateBg.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            Button generateButton = generateButtonObj.AddComponent<Button>();
            generateButton.targetGraphic = generateBg;

            GameObject generateTextObj = new GameObject("Text");
            generateTextObj.transform.SetParent(generateButtonObj.transform, false);
            TextMeshProUGUI generateText = generateTextObj.AddComponent<TextMeshProUGUI>();
            generateText.text = "Generate Scene";
            generateText.fontSize = 16;
            generateText.alignment = TextAlignmentOptions.Center;
            generateText.color = Color.white;

            RectTransform generateTextRect = generateTextObj.GetComponent<RectTransform>();
            generateTextRect.anchorMin = Vector2.zero;
            generateTextRect.anchorMax = Vector2.one;
            generateTextRect.offsetMin = Vector2.zero;
            generateTextRect.offsetMax = Vector2.zero;

            // Create Close button
            GameObject closeButtonObj = new GameObject("CloseButton");
            closeButtonObj.transform.SetParent(buttonRow.transform, false);

            Image closeBg = closeButtonObj.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeButton = closeButtonObj.AddComponent<Button>();
            closeButton.targetGraphic = closeBg;

            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeButtonObj.transform, false);
            TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
            closeText.text = "Close";
            closeText.fontSize = 16;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;

            RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            // Create status text
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(contentArea.transform, false);
            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.yellow;

            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(0, 30);

            // Wire up to AISceneGeneratorUI using SerializedObject
            SerializedObject so = new SerializedObject(aiComponent);
            so.FindProperty("promptInputField").objectReferenceValue = inputField;
            so.FindProperty("generateButton").objectReferenceValue = generateButton;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedProperties();

            Debug.Log("✓ Created simple prompt UI");
        }

        private static Button CreateAIGenerateButton(Transform viewModeUI)
        {
            // Check if button already exists
            Transform existingButton = viewModeUI.Find("AIGenerateButton");
            if (existingButton != null)
            {
                Debug.Log("AI Generate Button already exists");
                return existingButton.GetComponent<Button>();
            }

            // Find the Public button to copy style
            Transform publicButton = viewModeUI.Find("PublicButton");

            GameObject aiButtonObj;

            if (publicButton != null)
            {
                // Duplicate Public button
                aiButtonObj = GameObject.Instantiate(publicButton.gameObject, viewModeUI);
                aiButtonObj.name = "AIGenerateButton";

                // Update text
                TextMeshProUGUI buttonText = aiButtonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "AI Generate";
                }

                // Position it next to Public button
                RectTransform aiRect = aiButtonObj.GetComponent<RectTransform>();
                RectTransform publicRect = publicButton.GetComponent<RectTransform>();

                aiRect.anchoredPosition = publicRect.anchoredPosition + new Vector2(0, -80); // Below public button

                Debug.Log("✓ Duplicated PublicButton -> AIGenerateButton");
            }
            else
            {
                // Create from scratch
                aiButtonObj = new GameObject("AIGenerateButton");
                aiButtonObj.transform.SetParent(viewModeUI, false);

                RectTransform rect = aiButtonObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-20, -100);
                rect.sizeDelta = new Vector2(120, 50);

                Image bg = aiButtonObj.AddComponent<Image>();
                bg.color = new Color(0.3f, 0.6f, 0.9f, 1f);

                Button button = aiButtonObj.AddComponent<Button>();
                button.targetGraphic = bg;

                // Create text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(aiButtonObj.transform, false);

                TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = "AI Generate";
                buttonText.fontSize = 16;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = Color.white;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                Debug.Log("✓ Created AIGenerateButton from scratch");
            }

            return aiButtonObj.GetComponent<Button>();
        }

        private static void WireUpBuildModeUI(BuildModeUI buildModeUI, GameObject aiPanel, Button aiButton)
        {
            SerializedObject so = new SerializedObject(buildModeUI);

            // Wire up AI Scene Generator UI panel
            SerializedProperty aiSceneGeneratorProp = so.FindProperty("aiSceneGeneratorUI");
            if (aiSceneGeneratorProp != null)
            {
                AISceneGeneratorUI aiComponent = aiPanel.GetComponent<AISceneGeneratorUI>();
                aiSceneGeneratorProp.objectReferenceValue = aiComponent;
                Debug.Log("✓ Wired aiSceneGeneratorUI reference");
            }

            // Wire up AI Generate button
            SerializedProperty aiButtonProp = so.FindProperty("aiGenerateButton");
            if (aiButtonProp != null)
            {
                aiButtonProp.objectReferenceValue = aiButton;
                Debug.Log("✓ Wired aiGenerateButton reference");
            }

            so.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            Debug.Log("✓ BuildModeUI wired up successfully");
        }
    }
}
