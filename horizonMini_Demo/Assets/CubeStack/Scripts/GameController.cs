using UnityEngine;
using UnityEngine.Events;

public enum GameState { Menu, Playing, GameOver }
public enum SwingAxis { X, Z }

[System.Serializable]
public struct BlockData
{
    public Vector3 position;
    public float width;
    public float depth;

    public BlockData(Vector3 pos, float w, float d)
    {
        position = pos;
        width = w;
        depth = d;
    }
}

public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private EnvironmentManager environmentManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private InputHandler inputHandler;

    [Header("Game Settings")]
    [SerializeField] private float cubeSize = 10f;
    [SerializeField] private float blockHeight = 1f;
    [SerializeField] private float initialBlockSize = 3f;
    [SerializeField] private float swingDistance = 6f;

    [Header("Difficulty")]
    [SerializeField] private float baseSpeed = 1.2f;
    [SerializeField] private float speedIncrement = 0.05f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("Theme")]
    [SerializeField] private StackTheme currentTheme;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent OnGameStarted;
    public UnityEvent OnGameOver;
    public UnityEvent OnBlockPlaced;
    public UnityEvent OnPerfectPlacement;

    // State
    private GameState _state = GameState.Menu;
    private int _score;
    private float _currentSpeed;
    private SwingAxis _swingAxis;
    private BlockData _previousBlock;
    private GameObject _activeBlock;

    // Properties
    public GameState State => _state;
    public int Score => _score;
    public float CurrentSpeed => _currentSpeed;
    public StackTheme Theme => currentTheme;

    private void Awake()
    {
        // Auto-find components if not assigned
        if (blockManager == null) blockManager = GetComponentInChildren<BlockManager>();
        if (cameraController == null) cameraController = GetComponentInChildren<CameraController>();
        if (environmentManager == null) environmentManager = GetComponentInChildren<EnvironmentManager>();
        if (uiManager == null) uiManager = GetComponentInChildren<UIManager>();
        if (audioManager == null) audioManager = GetComponentInChildren<AudioManager>();
        if (inputHandler == null) inputHandler = GetComponent<InputHandler>();

        // Create missing components
        if (blockManager == null)
        {
            var bmObj = new GameObject("BlockManager");
            bmObj.transform.SetParent(transform);
            blockManager = bmObj.AddComponent<BlockManager>();
        }

        if (environmentManager == null)
        {
            var emObj = new GameObject("EnvironmentManager");
            emObj.transform.SetParent(transform);
            environmentManager = emObj.AddComponent<EnvironmentManager>();
        }
    }

    private void Start()
    {
        // Initialize block manager with theme material
        Material blockMat = null;
        if (currentTheme != null && currentTheme.blockMaterial != null)
        {
            blockMat = currentTheme.blockMaterial;
        }
        else
        {
            // Create default material
            blockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            blockMat.color = Color.white;
        }

        blockManager.Initialize(blockMat);

        if (cameraController != null)
        {
            environmentManager.SetCamera(cameraController.GetComponent<Camera>());
        }

        if (audioManager != null && currentTheme != null)
        {
            audioManager.SetTheme(currentTheme);
        }

        // Setup input
        if (inputHandler != null)
        {
            inputHandler.OnTap.AddListener(OnPlayerInput);
        }

        _state = GameState.Menu;
        if (uiManager != null) uiManager.ShowMenu();
    }

    public void OnPlayerInput()
    {
        switch (_state)
        {
            case GameState.Menu:
                StartGame();
                break;
            case GameState.Playing:
                PlaceBlock();
                break;
            case GameState.GameOver:
                StartGame();
                break;
        }
    }

    private void StartGame()
    {
        Debug.Log($"<color=red>========== StartGame() CALLED ==========</color>");
        Debug.Log($"<color=red>Current state: {_state}, score: {_score}</color>");

        // Reset state
        _score = 0;
        _currentSpeed = baseSpeed;
        _swingAxis = SwingAxis.X;
        _previousBlock = new BlockData(Vector3.zero, initialBlockSize, initialBlockSize);

        // Clear scene
        blockManager.ClearAll();
        environmentManager.Reset();

        // Setup
        blockManager.CreateBase(initialBlockSize, currentTheme);
        environmentManager.InitializeZone(0, currentTheme);

        // Camera
        if (cameraController != null)
        {
            cameraController.ResetCamera();
            cameraController.SetTargetHeight(0);
        }

        // UI
        if (uiManager != null)
        {
            uiManager.HideAll();
            uiManager.UpdateScore(0);
        }

        // Start
        _state = GameState.Playing;
        OnGameStarted?.Invoke();
        SpawnNextBlock();
    }

    private void SpawnNextBlock()
    {
        // Alternate axis
        _swingAxis = _swingAxis == SwingAxis.X ? SwingAxis.Z : SwingAxis.X;

        // Calculate spawn position
        // Blocks stack from Y=0.5 (first block sits on base top at Y=0)
        // Each block is blockHeight units apart
        float y = (_score * blockHeight) + (blockHeight * 0.5f);
        float spawnOffset = swingDistance;

        Vector3 spawnPos = new Vector3(
            _swingAxis == SwingAxis.X ? -spawnOffset : _previousBlock.position.x,
            y,
            _swingAxis == SwingAxis.Z ? -spawnOffset : _previousBlock.position.z
        );

        // Get color from theme gradient
        Color blockColor = Color.HSVToRGB((_score * 0.1f) % 1f, 0.7f, 1f); // Default rainbow
        if (currentTheme != null && currentTheme.blockColorGradient != null)
        {
            blockColor = currentTheme.blockColorGradient.Evaluate((_score / 50f) % 1f);
        }

        // Spawn
        Debug.Log($"<color=magenta>[GameController] SpawnNextBlock - score={_score}, Y={y}, spawnPos={spawnPos}</color>");
        _activeBlock = blockManager.SpawnBlock(
            spawnPos,
            _previousBlock.width,
            _previousBlock.depth,
            blockHeight,
            blockColor
        );
    }

    private void Update()
    {
        if (_state != GameState.Playing || _activeBlock == null) return;

        // Oscillate block
        float oscillation = Mathf.Sin(Time.time * _currentSpeed) * swingDistance;
        Vector3 pos = _activeBlock.transform.position;

        if (_swingAxis == SwingAxis.X)
        {
            pos.x = _previousBlock.position.x + oscillation;
            pos.z = _previousBlock.position.z;
        }
        else
        {
            pos.z = _previousBlock.position.z + oscillation;
            pos.x = _previousBlock.position.x;
        }

        _activeBlock.transform.position = pos;
    }

    private void PlaceBlock()
    {
        if (_activeBlock == null) return;

        Vector3 currentPos = _activeBlock.transform.position;
        float delta, overlap, newSize, cutSize;
        Color blockColor = _activeBlock.GetComponent<Renderer>().material.color;

        if (_swingAxis == SwingAxis.X)
        {
            delta = currentPos.x - _previousBlock.position.x;
            overlap = _previousBlock.width - Mathf.Abs(delta);

            if (overlap <= 0)
            {
                TriggerGameOver();
                return;
            }

            newSize = overlap;
            cutSize = _previousBlock.width - newSize;

            // Calculate new center
            float shift = delta > 0 ? -cutSize / 2f : cutSize / 2f;
            float newX = _previousBlock.position.x + delta + shift;

            // Create debris if cut is significant
            if (cutSize > 0.01f)
            {
                float debrisX = delta > 0
                    ? newX + newSize / 2f + cutSize / 2f
                    : newX - newSize / 2f - cutSize / 2f;
                Vector3 debrisPos = new Vector3(debrisX, currentPos.y, currentPos.z);
                blockManager.CreateDebris(debrisPos, cutSize, _previousBlock.depth, blockHeight, blockColor);
            }

            // Update block
            blockManager.TrimBlock(_activeBlock, newSize, _previousBlock.depth, blockHeight);
            _activeBlock.transform.position = new Vector3(newX, currentPos.y, currentPos.z);

            Debug.Log($"<color=lime>### PLACED BLOCK (X-axis) ###</color>");
            Debug.Log($"  World Position: {_activeBlock.transform.position}");
            Debug.Log($"  Local Position: {_activeBlock.transform.localPosition}");

            // Check for perfect placement
            bool isPerfect = overlap >= _previousBlock.width * 0.95f;

            // Update state
            _previousBlock = new BlockData(
                new Vector3(newX, currentPos.y, currentPos.z),
                newSize,
                _previousBlock.depth
            );

            ProcessSuccessfulPlacement(isPerfect, overlap, _previousBlock.width);
        }
        else // Z axis
        {
            delta = currentPos.z - _previousBlock.position.z;
            overlap = _previousBlock.depth - Mathf.Abs(delta);

            if (overlap <= 0)
            {
                TriggerGameOver();
                return;
            }

            newSize = overlap;
            cutSize = _previousBlock.depth - newSize;

            float shift = delta > 0 ? -cutSize / 2f : cutSize / 2f;
            float newZ = _previousBlock.position.z + delta + shift;

            if (cutSize > 0.01f)
            {
                float debrisZ = delta > 0
                    ? newZ + newSize / 2f + cutSize / 2f
                    : newZ - newSize / 2f - cutSize / 2f;
                Vector3 debrisPos = new Vector3(currentPos.x, currentPos.y, debrisZ);
                blockManager.CreateDebris(debrisPos, _previousBlock.width, cutSize, blockHeight, blockColor);
            }

            blockManager.TrimBlock(_activeBlock, _previousBlock.width, newSize, blockHeight);
            _activeBlock.transform.position = new Vector3(currentPos.x, currentPos.y, newZ);

            Debug.Log($"<color=lime>### PLACED BLOCK (Z-axis) ###</color>");
            Debug.Log($"  World Position: {_activeBlock.transform.position}");
            Debug.Log($"  Local Position: {_activeBlock.transform.localPosition}");

            bool isPerfect = overlap >= _previousBlock.depth * 0.95f;

            _previousBlock = new BlockData(
                new Vector3(currentPos.x, currentPos.y, newZ),
                _previousBlock.width,
                newSize
            );

            ProcessSuccessfulPlacement(isPerfect, overlap, _previousBlock.depth);
        }
    }

    private void ProcessSuccessfulPlacement(bool isPerfect, float overlap, float originalSize)
    {
        // Success!
        _score++;
        _currentSpeed = Mathf.Min(_currentSpeed + speedIncrement, maxSpeed);

        // Events and feedback
        OnScoreChanged?.Invoke(_score);
        OnBlockPlaced?.Invoke();

        if (uiManager != null) uiManager.UpdateScore(_score);

        if (isPerfect)
        {
            OnPerfectPlacement?.Invoke();
            if (uiManager != null) uiManager.ShowPerfect();
            if (audioManager != null) audioManager.PlayPerfect();
        }
        else
        {
            if (audioManager != null) audioManager.PlayPlace();
        }

        // Camera follow
        if (cameraController != null)
        {
            cameraController.SetTargetHeight(_score * blockHeight);
        }

        // Check zone transitions
        if (environmentManager != null)
        {
            environmentManager.CheckZoneTransition(_score, currentTheme);
        }

        // Next block
        SpawnNextBlock();
    }

    private void TriggerGameOver()
    {
        _state = GameState.GameOver;

        // Drop current block as debris
        if (_activeBlock != null)
        {
            blockManager.ConvertToDebris(_activeBlock);
            _activeBlock = null;
        }

        // Effects
        // Try to shake camera
        if (cameraController != null && cameraController.gameObject.activeInHierarchy)
        {
            // Game's own camera is active (standalone mode)
            cameraController.Shake(0.5f, 0.3f);
        }
        else
        {
            // Game camera is inactive, we're in Browse mode - shake the main camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                StartCoroutine(ShakeCamera(mainCam.transform, 0.5f, 0.3f));
            }
        }

        if (audioManager != null) audioManager.PlayFail();

        // UI
        if (uiManager != null) uiManager.ShowGameOver(_score);

        OnGameOver?.Invoke();
    }

    private System.Collections.IEnumerator ShakeCamera(Transform cameraTransform, float duration, float intensity)
    {
        Vector3 originalPos = cameraTransform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float z = UnityEngine.Random.Range(-1f, 1f) * intensity;

            cameraTransform.position = originalPos + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = originalPos;
    }
}
