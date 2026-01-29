using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Game state enumeration
/// </summary>
public enum KotobaGameState
{
    Menu,
    Playing,
    GameOver
}

/// <summary>
/// Main game controller for KotobaMatch.
/// Uses the same OnTap pattern as CubeStack for menu/game over states.
/// </summary>
public class KotobaMatchController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardManager cardManager;
    [SerializeField] private KotobaUIManager uiManager;
    [SerializeField] private KotobaAudioManager audioManager;
    [SerializeField] private KotobaCameraController cameraController;
    [SerializeField] private KotobaInputHandler inputHandler;

    [Header("Theme")]
    [SerializeField] private KotobaTheme currentTheme;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnTimerChanged;
    public UnityEvent OnGameStarted;
    public UnityEvent OnGameOver;
    public UnityEvent OnMatchFound;
    public UnityEvent OnMismatch;
    public UnityEvent OnRoundComplete;

    // Game state
    private KotobaGameState _state = KotobaGameState.Menu;
    private int _score;
    private float _timeLeft;
    private CardController _selectedCard;
    private Coroutine _timerCoroutine;

    // Properties
    public KotobaGameState State => _state;
    public int Score => _score;
    public float TimeLeft => _timeLeft;
    public KotobaTheme Theme => currentTheme;

    private void Awake()
    {
        // Auto-find components
        if (cardManager == null)
            cardManager = GetComponentInChildren<CardManager>();
        if (uiManager == null)
            uiManager = GetComponentInChildren<KotobaUIManager>();
        if (audioManager == null)
            audioManager = GetComponentInChildren<KotobaAudioManager>();
        if (cameraController == null)
            cameraController = GetComponentInChildren<KotobaCameraController>();
        if (inputHandler == null)
            inputHandler = GetComponent<KotobaInputHandler>();
        if (inputHandler == null)
            inputHandler = GetComponentInChildren<KotobaInputHandler>();

        // Create missing components
        if (cardManager == null)
        {
            var cmObj = new GameObject("CardManager");
            cmObj.transform.SetParent(transform);
            cardManager = cmObj.AddComponent<CardManager>();
        }
    }

    private void Start()
    {
        // Initialize managers
        if (currentTheme != null)
        {
            cardManager.Initialize(currentTheme);
            if (audioManager != null)
                audioManager.SetTheme(currentTheme);
            if (cameraController != null)
                cameraController.Initialize(currentTheme);
        }

        // Setup input
        if (inputHandler != null)
        {
            inputHandler.OnTap.AddListener(OnPlayerTap);
            inputHandler.OnCardClicked.AddListener(OnCardClicked);
        }

        // Show menu
        _state = KotobaGameState.Menu;
        if (uiManager != null)
            uiManager.ShowMenu();
    }

    /// <summary>
    /// Called when player taps anywhere (not on a card)
    /// </summary>
    public void OnPlayerTap()
    {
        switch (_state)
        {
            case KotobaGameState.Menu:
                StartGame();
                break;
            case KotobaGameState.GameOver:
                StartGame();
                break;
            case KotobaGameState.Playing:
                // Tapping empty space during play deselects current card
                if (_selectedCard != null)
                {
                    _selectedCard.Deselect();
                    _selectedCard = null;
                }
                break;
        }
    }

    /// <summary>
    /// Called when player clicks on a card
    /// </summary>
    public void OnCardClicked(CardController card)
    {
        if (_state != KotobaGameState.Playing) return;
        if (card == null || card.isMatched) return;

        // Same card clicked - deselect
        if (_selectedCard == card)
        {
            _selectedCard.Deselect();
            _selectedCard = null;
            return;
        }

        // No card selected - select this one
        if (_selectedCard == null)
        {
            _selectedCard = card;
            _selectedCard.Select();
            return;
        }

        // Different card selected - check for match
        CheckMatch(card);
    }

    /// <summary>
    /// Start a new game
    /// </summary>
    private void StartGame()
    {
        // Reset state
        _score = 0;
        _timeLeft = currentTheme != null ? currentTheme.gameDuration : 60;
        _selectedCard = null;

        // Update UI
        if (uiManager != null)
        {
            uiManager.HideAll();
            uiManager.UpdateScore(_score);
            uiManager.UpdateTimer(Mathf.CeilToInt(_timeLeft));
        }

        // Reset camera background
        if (cameraController != null && currentTheme != null)
        {
            cameraController.SetBackgroundColor(currentTheme.backgroundColor);
        }

        // Clear and spawn
        cardManager.ClearCards();
        SpawnNewRound();

        // Start timer
        _state = KotobaGameState.Playing;
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(TimerCoroutine());

        OnGameStarted?.Invoke();
        OnScoreChanged?.Invoke(_score);
    }

    /// <summary>
    /// Spawn a new round of cards
    /// </summary>
    private void SpawnNewRound()
    {
        if (currentTheme == null) return;

        WordPair[] pairs = currentTheme.GetRandomPairs(currentTheme.pairsPerRound);
        cardManager.SpawnCards(pairs);
    }

    /// <summary>
    /// Timer coroutine
    /// </summary>
    private IEnumerator TimerCoroutine()
    {
        while (_timeLeft > 0 && _state == KotobaGameState.Playing)
        {
            yield return new WaitForSeconds(1f);

            if (_state != KotobaGameState.Playing)
                yield break;

            _timeLeft--;
            if (uiManager != null)
                uiManager.UpdateTimer(Mathf.CeilToInt(_timeLeft));
            OnTimerChanged?.Invoke(Mathf.CeilToInt(_timeLeft));

            if (_timeLeft <= 0)
            {
                EndGame();
            }
        }
    }

    /// <summary>
    /// Check if the selected card matches with the clicked card
    /// </summary>
    private void CheckMatch(CardController clickedCard)
    {
        // Match validation: same pairId, different type
        bool isMatch = _selectedCard.pairId == clickedCard.pairId &&
                       _selectedCard.cardType != clickedCard.cardType;

        if (isMatch)
        {
            // Match found!
            _selectedCard.MarkMatched();
            clickedCard.MarkMatched();

            _score++;
            if (uiManager != null)
                uiManager.UpdateScore(_score);
            OnScoreChanged?.Invoke(_score);
            OnMatchFound?.Invoke();

            if (audioManager != null)
                audioManager.PlayMatch();

            _selectedCard = null;

            // Check if round is complete
            StartCoroutine(CheckRoundComplete());
        }
        else
        {
            // Mismatch
            clickedCard.Shake();
            _selectedCard.Deselect();
            _selectedCard = null;

            OnMismatch?.Invoke();

            if (audioManager != null)
                audioManager.PlayMismatch();
        }
    }

    /// <summary>
    /// Check if all cards are matched and spawn new round
    /// </summary>
    private IEnumerator CheckRoundComplete()
    {
        // Brief delay to let the match animation play
        yield return new WaitForSeconds(0.3f);

        if (cardManager.AreAllMatched())
        {
            OnRoundComplete?.Invoke();

            // Wait before spawning new round
            float delay = currentTheme != null ? currentTheme.respawnDelay : 0.8f;
            yield return new WaitForSeconds(delay);

            if (_state == KotobaGameState.Playing)
            {
                SpawnNewRound();
            }
        }
    }

    /// <summary>
    /// End the game
    /// </summary>
    private void EndGame()
    {
        _state = KotobaGameState.GameOver;

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        // Visual feedback
        if (cameraController != null && currentTheme != null)
        {
            cameraController.SetBackgroundColor(currentTheme.gameOverBackgroundColor);
        }

        // Show game over UI
        if (uiManager != null)
        {
            uiManager.ShowGameOver(_score);
        }

        // Play sound
        if (audioManager != null)
        {
            audioManager.PlayGameOver();
        }

        OnGameOver?.Invoke();
    }

    /// <summary>
    /// Reset game to menu state (for preview mode)
    /// </summary>
    public void ResetToMenu()
    {
        Debug.Log("[KotobaMatchController] Resetting to menu state");

        // Stop timer if running
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        // Clear all cards
        if (cardManager != null)
        {
            cardManager.ClearCards();
        }

        // Reset state
        _state = KotobaGameState.Menu;
        _score = 0;
        _timeLeft = 0;
        _selectedCard = null;

        // Reset camera background
        if (cameraController != null && currentTheme != null)
        {
            cameraController.SetBackgroundColor(currentTheme.backgroundColor);
        }

        // Show menu UI
        if (uiManager != null)
        {
            uiManager.ShowMenu();
        }

        Debug.Log("[KotobaMatchController] Reset complete");
    }

    /// <summary>
    /// Pause game for preview mode (keeps cards visible but stops game logic)
    /// </summary>
    public void PauseForPreview()
    {
        Debug.Log("[KotobaMatchController] Pausing for preview mode");

        // Stop timer if running
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        // Set to menu state but keep cards visible
        _state = KotobaGameState.Menu;
        _selectedCard = null;

        Debug.Log("[KotobaMatchController] Preview pause complete");
    }

    /// <summary>
    /// Resume game from preview mode (cards already on stage, just start playing)
    /// </summary>
    public void ResumeFromPreview()
    {
        Debug.Log("[KotobaMatchController] Resuming from preview mode");

        if (_state != KotobaGameState.Menu)
        {
            Debug.LogWarning($"[KotobaMatchController] Cannot resume - current state: {_state}");
            return;
        }

        // Check if cards are already spawned
        if (cardManager.GetUnmatchedCount() == 0)
        {
            Debug.LogWarning("[KotobaMatchController] No cards on stage - starting fresh game");
            StartGame();
            return;
        }

        // Cards already exist from preview - just start the game logic
        _score = 0;
        _timeLeft = currentTheme != null ? currentTheme.gameDuration : 60;
        _selectedCard = null;

        // Update UI
        if (uiManager != null)
        {
            uiManager.HideAll();
            uiManager.UpdateScore(_score);
            uiManager.UpdateTimer(Mathf.CeilToInt(_timeLeft));
        }

        // Start timer
        _state = KotobaGameState.Playing;
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(TimerCoroutine());

        OnGameStarted?.Invoke();
        OnScoreChanged?.Invoke(_score);

        Debug.Log("[KotobaMatchController] Resumed - cards stay in place, timer started");
    }

    /// <summary>
    /// Set the theme at runtime
    /// </summary>
    public void SetTheme(KotobaTheme theme)
    {
        currentTheme = theme;
        if (cardManager != null)
            cardManager.Initialize(theme);
        if (audioManager != null)
            audioManager.SetTheme(theme);
        if (cameraController != null)
            cameraController.Initialize(theme);
    }
}
