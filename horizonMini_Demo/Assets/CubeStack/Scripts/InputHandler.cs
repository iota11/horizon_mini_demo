using UnityEngine;
using UnityEngine.Events;

public class InputHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float debounceTime = 0.1f;

    public UnityEvent OnTap;

    private float _lastInputTime;

    private void Awake()
    {
        if (OnTap == null)
            OnTap = new UnityEvent();
    }

    private void Update()
    {
        if (Time.time - _lastInputTime < debounceTime) return;

        bool inputDetected = false;

        // Touch input
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputDetected = true;
        }

        // Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
        }

        // Keyboard input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputDetected = true;
        }

        if (inputDetected)
        {
            _lastInputTime = Time.time;
            OnTap?.Invoke();
        }
    }
}
