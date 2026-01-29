using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HorizonMini.Data;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Simple trigger for mini games in Browse mode
    /// Shows a button that user can click to start the game
    /// </summary>
    public class BrowseMiniGameTrigger : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private GameObject cubeStackGamePrefab;
        [SerializeField] private string gameName = "Stack Game";

        [Header("UI")]
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject gameUI;
        [SerializeField] private Button exitButton;

        [Header("Camera")]
        [SerializeField] private Camera browseCamera;

        private GameObject _activeGameInstance;
        private GameController _gameController;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private static BrowseMiniGameTrigger _instance;

        public static BrowseMiniGameTrigger Instance => _instance;
        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            Debug.Log("<color=cyan>[BrowseMiniGameTrigger] Awake called</color>");

            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[BrowseMiniGameTrigger] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            Debug.Log("[BrowseMiniGameTrigger] Instance registered");

            // Auto-find browse camera
            if (browseCamera == null)
            {
                browseCamera = Camera.main;
                Debug.Log($"[BrowseMiniGameTrigger] Found Camera.main: {browseCamera != null}");
            }

            // Load prefab if not assigned
            if (cubeStackGamePrefab == null)
            {
                cubeStackGamePrefab = Resources.Load<GameObject>("CubeStackGame");
                Debug.Log($"[BrowseMiniGameTrigger] Loaded CubeStackGame from Resources: {cubeStackGamePrefab != null}");
            }

            // Note: Play button is now handled by unified "Go" button in PlayModeManager
            // Exit button is now handled by unified "Back" button in Play Mode UI
            // Keeping button references for backwards compatibility but not creating UI here

            Debug.Log("<color=green>[BrowseMiniGameTrigger] ✓ Initialization complete</color>");
        }

        /// <summary>
        /// Deprecated: Play button now handled by unified "Go" button in PlayModeManager
        /// </summary>
        [System.Obsolete("Use PlayModeManager.ShowGoButton() instead")]
        public void SetPlayButtonVisible(bool visible, MiniGameData gameData = null)
        {
            // No longer creates/shows mini game specific button
            // Unified "Go" button is handled by PlayModeManager
        }

        public void StartGame()
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[BrowseMiniGame] Cannot start - already playing");
                return;
            }

            Debug.Log($"<color=cyan>[BrowseMiniGame] Starting {gameName}...</color>");

            // Hide play button
            if (playButton != null)
            {
                playButton.gameObject.SetActive(false);
            }

            // Find existing preview instance instead of creating new one
            MiniGamePreview preview = FindObjectOfType<MiniGamePreview>();

            if (preview != null && !preview.isActive)
            {
                Debug.Log("[BrowseMiniGame] Found existing preview, activating it");
                _activeGameInstance = preview.gameObject;
                preview.isActive = true;

                // Enable InputHandler to accept user input
                var inputHandler = _activeGameInstance.GetComponentInChildren<InputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = true;
                    Debug.Log("[BrowseMiniGame] Enabled InputHandler - user can now play");
                }

                // Get game controller and add listener
                _gameController = _activeGameInstance.GetComponent<GameController>();
                if (_gameController != null)
                {
                    _gameController.OnGameOver.AddListener(OnGameOver);
                }

                // Show game UI in world space
                Canvas[] canvases = _activeGameInstance.GetComponentsInChildren<Canvas>(true);
                foreach (var canvas in canvases)
                {
                    canvas.gameObject.SetActive(true);
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.transform.localScale = Vector3.one * 0.01f; // Very small UI
                }
            }
            else
            {
                Debug.LogWarning("[BrowseMiniGame] No preview found - game won't start");
                return;
            }

            // DON'T change camera - keep using Browse camera
            // The game is embedded in the world, not fullscreen

            // Show exit button
            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
            }

            _isPlaying = true;

            Debug.Log("<color=green>[BrowseMiniGame] ✓ Game started!</color>");
        }

        public void StopGame()
        {
            if (!_isPlaying)
            {
                return;
            }

            Debug.Log("<color=yellow>[BrowseMiniGame] Stopping game...</color>");

            // Check if this is a preview instance
            MiniGamePreview preview = _activeGameInstance?.GetComponent<MiniGamePreview>();

            if (preview != null)
            {
                // Reset to preview state instead of destroying
                Debug.Log("[BrowseMiniGame] Resetting to preview state");
                preview.isActive = false;

                // Disable InputHandler again (back to preview mode)
                var inputHandler = _activeGameInstance.GetComponentInChildren<InputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = false;
                    Debug.Log("[BrowseMiniGame] Disabled InputHandler - back to preview mode");
                }

                // Hide UI canvases
                Canvas[] canvases = _activeGameInstance.GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.gameObject.SetActive(false);
                }

                // Reset game state (if game controller has a reset method)
                // TODO: Add reset logic here if needed
            }

            _activeGameInstance = null;

            // DON'T restore camera - we never changed it
            // Game is embedded in world, not fullscreen

            // Show play button again
            if (playButton != null)
            {
                playButton.gameObject.SetActive(true);
            }

            // Hide exit button
            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(false);
            }

            _isPlaying = false;
            _isPaused = false;

            Debug.Log("<color=green>[BrowseMiniGame] ✓ Game stopped</color>");
        }

        /// <summary>
        /// Pause the game (when switching away from this world)
        /// </summary>
        public void PauseGame()
        {
            if (!_isPlaying || _isPaused)
            {
                return;
            }

            Debug.Log("[BrowseMiniGame] Pausing game...");

            // Pause game time
            Time.timeScale = 0f;

            // Hide game UI
            if (_activeGameInstance != null)
            {
                Canvas[] canvases = _activeGameInstance.GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.enabled = false;
                }
            }

            _isPaused = true;

            Debug.Log("<color=yellow>[BrowseMiniGame] ✓ Game paused</color>");
        }

        /// <summary>
        /// Resume the game (when switching back to this world)
        /// </summary>
        public void ResumeGame()
        {
            if (!_isPlaying || !_isPaused)
            {
                return;
            }

            Debug.Log("[BrowseMiniGame] Resuming game...");

            // Resume game time
            Time.timeScale = 1f;

            // Show game UI
            if (_activeGameInstance != null)
            {
                Canvas[] canvases = _activeGameInstance.GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.enabled = true;
                }
            }

            _isPaused = false;

            Debug.Log("<color=green>[BrowseMiniGame] ✓ Game resumed</color>");
        }

        // Camera switching removed - game is embedded in world, uses Browse camera

        private void OnGameOver()
        {
            Debug.Log("[BrowseMiniGame] Game over");
            // Don't auto-exit, let player see score and choose
        }

        private void CreatePlayButtonUI()
        {
            // Find or create canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MiniGameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create Play Button
            GameObject buttonObj = new GameObject("PlayButton_" + gameName);
            buttonObj.transform.SetParent(canvas.transform);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(200, 80);

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 0.9f);

            playButton = buttonObj.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"Play {gameName}";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 24;

            Debug.Log("[BrowseMiniGame] Created Play button");
        }

        private void CreateExitButtonUI()
        {
            // Find canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[BrowseMiniGame] No canvas found for exit button");
                return;
            }

            // Create Exit Button (top-right)
            GameObject buttonObj = new GameObject("ExitButton_" + gameName);
            buttonObj.transform.SetParent(canvas.transform);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(120, 50);

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

            exitButton = buttonObj.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Exit";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 18;

            buttonObj.SetActive(false);

            Debug.Log("[BrowseMiniGame] Created Exit button");
        }

        private void OnDestroy()
        {
            if (_isPlaying)
            {
                StopGame();
            }
        }
    }
}
