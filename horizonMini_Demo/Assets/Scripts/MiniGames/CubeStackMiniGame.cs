using UnityEngine;
using UnityEngine.Events;
using HorizonMini.Controllers;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Manages CubeStack game integration with HorizonMini worlds
    /// Handles starting/stopping the game and managing world state
    /// </summary>
    public class CubeStackMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("References")]
        [SerializeField] private GameObject cubeStackGamePrefab;
        [SerializeField] private PlayController playController;

        [Header("UI")]
        [SerializeField] private GameObject exitButton;
        [SerializeField] private Canvas gameCanvas;

        [Header("Settings")]
        [SerializeField] private bool pauseWorldWhenPlaying = true;
        [SerializeField] private bool hideWorldUIWhenPlaying = true;

        public event UnityAction OnGameExit;

        private GameObject _activeGameInstance;
        private GameController _gameController;
        private Camera _originalCamera;
        private Camera _gameCamera;
        private bool _isActive;

        public bool IsActive => _isActive;

        private void Awake()
        {
            // Auto-find PlayController if not assigned
            if (playController == null)
            {
                playController = FindObjectOfType<PlayController>();
            }

            // Load prefab if not assigned
            if (cubeStackGamePrefab == null)
            {
                cubeStackGamePrefab = Resources.Load<GameObject>("CubeStackGame");
                if (cubeStackGamePrefab == null)
                {
                    Debug.LogError("[CubeStackMiniGame] CubeStackGame prefab not found! Please assign it in Inspector or place it in Resources folder.");
                }
            }
        }

        public void StartGame()
        {
            if (_isActive || cubeStackGamePrefab == null)
            {
                Debug.LogWarning("[CubeStackMiniGame] Cannot start game - already active or prefab missing");
                return;
            }

            Debug.Log("<color=cyan>[CubeStackMiniGame] Starting CubeStack game...</color>");

            // Pause world
            if (pauseWorldWhenPlaying && playController != null)
            {
                playController.PauseGame();
            }

            // Store original camera
            _originalCamera = Camera.main;

            // Instantiate game
            _activeGameInstance = Instantiate(cubeStackGamePrefab);
            _activeGameInstance.name = "CubeStackGame_Active";

            // Get game controller
            _gameController = _activeGameInstance.GetComponent<GameController>();
            if (_gameController != null)
            {
                // Subscribe to game events
                _gameController.OnGameOver.AddListener(OnGameOverHandler);
            }

            // Setup camera
            SetupGameCamera();

            // Hide world UI
            if (hideWorldUIWhenPlaying && playController != null)
            {
                playController.SetUIVisible(false);
            }

            // Show exit button
            if (exitButton != null)
            {
                exitButton.SetActive(true);
            }

            _isActive = true;

            Debug.Log("<color=green>[CubeStackMiniGame] ✓ Game started!</color>");
        }

        public void StopGame()
        {
            if (!_isActive)
            {
                return;
            }

            Debug.Log("<color=yellow>[CubeStackMiniGame] Stopping game...</color>");

            // Cleanup game instance
            if (_activeGameInstance != null)
            {
                Destroy(_activeGameInstance);
                _activeGameInstance = null;
            }

            // Restore camera
            if (_originalCamera != null)
            {
                _originalCamera.gameObject.SetActive(true);
                if (_gameCamera != null)
                {
                    _gameCamera.gameObject.SetActive(false);
                }
            }

            // Resume world
            if (pauseWorldWhenPlaying && playController != null)
            {
                playController.ResumeGame();
            }

            // Restore world UI
            if (hideWorldUIWhenPlaying && playController != null)
            {
                playController.SetUIVisible(true);
            }

            // Hide exit button
            if (exitButton != null)
            {
                exitButton.SetActive(false);
            }

            _isActive = false;

            // Notify listeners
            OnGameExit?.Invoke();

            Debug.Log("<color=green>[CubeStackMiniGame] ✓ Game stopped, world resumed</color>");
        }

        private void SetupGameCamera()
        {
            // Find CameraController in the game instance
            CameraController cameraController = _activeGameInstance.GetComponentInChildren<CameraController>();
            if (cameraController != null)
            {
                _gameCamera = cameraController.GetComponent<Camera>();
                if (_gameCamera == null)
                {
                    _gameCamera = cameraController.gameObject.AddComponent<Camera>();
                }

                // Configure game camera
                _gameCamera.clearFlags = CameraClearFlags.SolidColor;
                _gameCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
                _gameCamera.depth = 10; // Higher depth than world camera

                // Disable original camera
                if (_originalCamera != null)
                {
                    _originalCamera.gameObject.SetActive(false);
                }

                Debug.Log("[CubeStackMiniGame] Game camera setup complete");
            }
            else
            {
                Debug.LogWarning("[CubeStackMiniGame] CameraController not found in game prefab");
            }
        }

        private void OnGameOverHandler()
        {
            Debug.Log("[CubeStackMiniGame] Game over event received");
            // Don't auto-exit, let player see the score and choose to restart or exit
        }

        /// <summary>
        /// Called by UI Exit button
        /// </summary>
        public void OnExitButtonClicked()
        {
            StopGame();
        }

        private void OnDestroy()
        {
            // Cleanup if component is destroyed while game is active
            if (_isActive)
            {
                StopGame();
            }
        }

        public void SetCubeStackPrefab(GameObject prefab)
        {
            cubeStackGamePrefab = prefab;
        }
    }
}
