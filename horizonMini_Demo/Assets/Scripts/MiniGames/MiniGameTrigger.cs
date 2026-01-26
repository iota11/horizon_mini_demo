using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Trigger component for mini games
    /// Detects player proximity and shows interaction prompt
    /// </summary>
    public class MiniGameTrigger : MonoBehaviour
    {
        [Header("Game Reference")]
        [SerializeField] private CubeStackMiniGame miniGame;
        [SerializeField] private string gameName = "Stack Game";

        [Header("Trigger Settings")]
        [SerializeField] private float triggerRadius = 2f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("UI")]
        [SerializeField] private GameObject promptUI;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private float uiHeightOffset = 2f;

        [Header("Visual Feedback")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.cyan;

        private bool _playerInRange;
        private Transform _playerTransform;
        private Canvas _promptCanvas;

        private void Awake()
        {
            // Auto-find mini game if not assigned
            if (miniGame == null)
            {
                miniGame = FindObjectOfType<CubeStackMiniGame>();
            }

            // Create UI if not assigned
            if (promptUI == null)
            {
                CreatePromptUI();
            }

            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }
        }

        private void Update()
        {
            // Check for player in range
            CheckPlayerProximity();

            // Handle input
            if (_playerInRange && Input.GetKeyDown(interactKey))
            {
                TriggerGame();
            }

            // Update UI position
            if (_playerInRange && promptUI != null && promptUI.activeSelf)
            {
                UpdatePromptPosition();
            }
        }

        private void CheckPlayerProximity()
        {
            // Find player by tag
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                }
            }

            if (_playerTransform == null)
            {
                if (_playerInRange)
                {
                    OnPlayerExit();
                }
                return;
            }

            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = distance <= triggerRadius;

            if (inRange && !_playerInRange)
            {
                OnPlayerEnter();
            }
            else if (!inRange && _playerInRange)
            {
                OnPlayerExit();
            }
        }

        private void OnPlayerEnter()
        {
            _playerInRange = true;

            if (promptUI != null)
            {
                promptUI.SetActive(true);
                UpdatePromptPosition();
            }

            Debug.Log($"[MiniGameTrigger] Player entered {gameName} trigger zone");
        }

        private void OnPlayerExit()
        {
            _playerInRange = false;

            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }

            Debug.Log($"[MiniGameTrigger] Player exited {gameName} trigger zone");
        }

        private void TriggerGame()
        {
            if (miniGame == null)
            {
                Debug.LogError("[MiniGameTrigger] MiniGame reference is missing!");
                return;
            }

            if (miniGame.IsActive)
            {
                Debug.LogWarning("[MiniGameTrigger] Game is already active");
                return;
            }

            Debug.Log($"<color=cyan>[MiniGameTrigger] Starting {gameName}...</color>");

            // Hide prompt
            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }

            // Start game
            miniGame.StartGame();
        }

        private void UpdatePromptPosition()
        {
            if (promptUI == null || _promptCanvas == null) return;

            // Position UI above the trigger
            Vector3 worldPos = transform.position + Vector3.up * uiHeightOffset;
            promptUI.transform.position = worldPos;

            // Make UI face camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                promptUI.transform.LookAt(mainCam.transform);
                promptUI.transform.Rotate(0, 180, 0); // Flip to face camera
            }
        }

        private void CreatePromptUI()
        {
            // Create canvas
            GameObject canvasObj = new GameObject("PromptCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.up * uiHeightOffset;

            _promptCanvas = canvasObj.AddComponent<Canvas>();
            _promptCanvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 50);

            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panelObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            promptText = textObj.AddComponent<TextMeshProUGUI>();
            promptText.text = $"Press [{interactKey}] to play {gameName}";
            promptText.fontSize = 14;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;

            promptUI = canvasObj;
            promptUI.SetActive(false);

            Debug.Log("[MiniGameTrigger] Created prompt UI");
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, triggerRadius);
        }
    }
}
