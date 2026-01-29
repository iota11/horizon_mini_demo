using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles all input for KotobaMatch.
/// During menu/gameover: any tap triggers OnTap
/// During gameplay: raycasts to find cards, fires OnCardClicked
/// </summary>
public class KotobaInputHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float debounceTime = 0.1f;
    [SerializeField] private LayerMask cardLayerMask = ~0;

    [Header("References")]
    [SerializeField] private Camera gameCamera;

    public UnityEvent OnTap;
    public UnityEvent<CardController> OnCardClicked;

    private float _lastInputTime;
    private KotobaMatchController _gameController;

    private void Awake()
    {
        if (OnTap == null) OnTap = new UnityEvent();
        if (OnCardClicked == null) OnCardClicked = new UnityEvent<CardController>();

        // Find camera
        if (gameCamera == null)
            gameCamera = GetComponentInChildren<Camera>();
        if (gameCamera == null)
            gameCamera = Camera.main;

        // Find game controller
        _gameController = GetComponentInParent<KotobaMatchController>();
        if (_gameController == null)
            _gameController = FindObjectOfType<KotobaMatchController>();
    }

    private void Update()
    {
        if (Time.time - _lastInputTime < debounceTime) return;

        bool inputDetected = false;
        Vector3 inputPosition = Vector3.zero;

        // Touch input
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputDetected = true;
            inputPosition = Input.GetTouch(0).position;
            Debug.Log($"[KotobaInputHandler] Touch input detected at {inputPosition}");
        }

        // Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Input.mousePosition;
            Debug.Log($"[KotobaInputHandler] Mouse input detected at {inputPosition}");
        }

        // Keyboard input (space bar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputDetected = true;
            Debug.Log("[KotobaInputHandler] Space key detected");
            // No position for keyboard - will just trigger OnTap
        }

        if (inputDetected)
        {
            _lastInputTime = Time.time;
            HandleInput(inputPosition);
        }
    }

    private void HandleInput(Vector3 screenPosition)
    {
        // If in Playing state, try to raycast to cards first
        if (_gameController != null && _gameController.State == KotobaGameState.Playing)
        {
            if (gameCamera != null && screenPosition != Vector3.zero)
            {
                Ray ray = gameCamera.ScreenPointToRay(screenPosition);
                RaycastHit hit;

                Debug.Log($"[KotobaInputHandler] Raycast from camera: {gameCamera.name}, position: {screenPosition}");

                if (Physics.Raycast(ray, out hit, 100f, cardLayerMask))
                {
                    Debug.Log($"[KotobaInputHandler] Hit: {hit.collider.gameObject.name}");
                    CardController card = hit.collider.GetComponent<CardController>();
                    if (card != null)
                    {
                        Debug.Log($"[KotobaInputHandler] Card clicked: {card.gameObject.name}");
                        OnCardClicked?.Invoke(card);
                        return;
                    }
                    else
                    {
                        Debug.Log("[KotobaInputHandler] Hit object has no CardController");
                    }
                }
                else
                {
                    Debug.Log("[KotobaInputHandler] Raycast hit nothing");
                }
            }
            else
            {
                Debug.LogWarning($"[KotobaInputHandler] Cannot raycast - camera={gameCamera}, screenPos={screenPosition}");
            }
        }
        else
        {
            Debug.Log($"[KotobaInputHandler] Not in Playing state - controller={_gameController}, state={_gameController?.State}");
        }

        // If not playing or didn't hit a card, fire general tap
        OnTap?.Invoke();
    }

    public void SetCamera(Camera camera)
    {
        gameCamera = camera;
    }
}
