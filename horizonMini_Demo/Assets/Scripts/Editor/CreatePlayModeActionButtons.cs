using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to create A (Jump) and B (Attack) action buttons in Play scene
    /// </summary>
    public class CreatePlayModeActionButtons : EditorWindow
    {
        [MenuItem("HorizonMini/Setup Play Action Buttons")]
        public static void ShowWindow()
        {
            GetWindow<CreatePlayModeActionButtons>("Create Action Buttons");
        }

        private void OnGUI()
        {
            GUILayout.Label("Create Play Mode Action Buttons", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will create A (Jump) and B (Attack) buttons in the current scene.\n" +
                "Make sure you have a Canvas in the scene.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Action Buttons", GUILayout.Height(40)))
            {
                CreateActionButtons();
            }
        }

        private static void CreateActionButtons()
        {
            // Find or create Canvas
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[CreateActionButtons] No Canvas found in scene! Please create a Canvas first.");
                EditorUtility.DisplayDialog("Error", "No Canvas found! Please create a Canvas first.", "OK");
                return;
            }

            // Create buttons
            GameObject jumpButton = CreateButton(canvas.transform, "JumpButton", "A", new Vector2(150, 150));
            GameObject attackButton = CreateButton(canvas.transform, "AttackButton", "B", new Vector2(150, 50));

            // Position buttons on right side of screen
            RectTransform jumpRect = jumpButton.GetComponent<RectTransform>();
            jumpRect.anchorMin = new Vector2(1, 0);
            jumpRect.anchorMax = new Vector2(1, 0);
            jumpRect.anchoredPosition = new Vector2(-100, 150);

            RectTransform attackRect = attackButton.GetComponent<RectTransform>();
            attackRect.anchorMin = new Vector2(1, 0);
            attackRect.anchorMax = new Vector2(1, 0);
            attackRect.anchoredPosition = new Vector2(-250, 100);

            // Try to auto-assign to PlayController
            AutoAssignToPlayController(jumpButton, attackButton);

            Debug.Log("[CreateActionButtons] ✓ Created Jump (A) and Attack (B) buttons");
            EditorUtility.DisplayDialog("Success",
                "Action buttons created!\n\n" +
                "Jump Button (A): Right side, bottom\n" +
                "Attack Button (B): Right side, bottom-left\n\n" +
                "Buttons have been assigned to PlayController if found.",
                "OK");

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 size)
        {
            // Create button GameObject
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            // Add Image component (button background)
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 0.8f); // Blue semi-transparent

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();

            // Configure button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 1f, 0.8f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
            colors.selectedColor = new Color(0.2f, 0.6f, 1f, 0.8f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            // Set size
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 48;
            textComponent.fontStyle = FontStyle.Bold;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return buttonObj;
        }

        private static void AutoAssignToPlayController(GameObject jumpButton, GameObject attackButton)
        {
            var playController = GameObject.FindObjectOfType<HorizonMini.Controllers.PlayController>();
            if (playController != null)
            {
                SerializedObject so = new SerializedObject(playController);

                SerializedProperty jumpButtonProp = so.FindProperty("jumpButton");
                if (jumpButtonProp != null)
                {
                    jumpButtonProp.objectReferenceValue = jumpButton.GetComponent<Button>();
                }

                SerializedProperty attackButtonProp = so.FindProperty("attackButton");
                if (attackButtonProp != null)
                {
                    attackButtonProp.objectReferenceValue = attackButton.GetComponent<Button>();
                }

                so.ApplyModifiedProperties();
                Debug.Log("[CreateActionButtons] ✓ Buttons assigned to PlayController");
            }
            else
            {
                Debug.LogWarning("[CreateActionButtons] PlayController not found. Please assign buttons manually.");
            }
        }
    }
}
