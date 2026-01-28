using UnityEngine;
using System;

namespace HorizonMini.Build
{
    /// <summary>
    /// Detects touch gestures: double tap, long press, drag, pinch, etc.
    /// Mobile-first implementation
    /// </summary>
    public class TouchGestureDetector : MonoBehaviour
    {
        [Header("Settings")]
        public float doubleTapMaxTime = 0.3f;
        public float doubleTapMaxDistance = 50f;
        public float longPressTime = 0.5f; // Changed to 0.5s for object selection
        public float longPressMaxMovement = 20f;

        // Events
        public event Action<Vector2> OnSingleTap;
        public event Action<Vector2> OnDoubleTap;
        public event Action<Vector2> OnLongPress;
        public event Action<Vector2> OnDragStart; // Called when drag starts
        public event Action<Vector2, Vector2> OnDrag; // last, current (called during drag)
        public event Action OnDragEnd; // Called when drag ends
        public event Action<float> OnPinch; // delta distance
        public event Action<Vector2> OnTwoFingerDrag;

        // State
        private Vector2 lastTapPosition;
        private float lastTapTime;
        private bool waitingForDoubleTap = false;

        private bool isLongPressing = false;
        private float longPressTimer = 0f;
        private Vector2 longPressStartPos;

        private bool isDragging = false;
        private Vector2 dragStartPos;
        private Vector2 lastDragPos;
        private int dragFingerId = -1;
        private bool dragStartedOverUI = false;

        public bool IsDragging => isDragging;
        public bool DragStartedOverUI => dragStartedOverUI;

        private bool isPinching = false;
        private float lastPinchDistance = 0f;

        private bool isTwoFingerDragging = false;
        private Vector2 twoFingerStartPos;

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Use touch if available, mouse as fallback for editor
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
            {
                HandleMouseInput();
            }

            // Always check for mouse scroll wheel (independent of mouse button state)
            HandleMouseScrollWheel();

            // Update timers
            if (waitingForDoubleTap)
            {
                if (Time.time - lastTapTime > doubleTapMaxTime)
                {
                    waitingForDoubleTap = false;
                    Debug.Log($"Single tap triggered at {lastTapPosition}");
                    OnSingleTap?.Invoke(lastTapPosition);
                }
            }

            // Update long press timer (only when holding but not dragging)
            if (isLongPressing && !isDragging)
            {
                longPressTimer += Time.deltaTime;
                if (longPressTimer >= longPressTime)
                {
                    OnLongPress?.Invoke(longPressStartPos);
                    isLongPressing = false; // Trigger once
                }
            }
        }

        private void HandleTouchInput()
        {
            int touchCount = Input.touchCount;

            if (touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                HandleSingleTouch(touch);
            }
            else if (touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                HandleTwoTouches(touch0, touch1);
            }
        }

        private void HandleSingleTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchMoved(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Ended:
                    OnTouchEnded(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Canceled:
                    ResetState();
                    break;
            }
        }

        private void HandleTwoTouches(Touch touch0, Touch touch1)
        {
            // Reset single touch state
            isLongPressing = false;
            isDragging = false;

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                isPinching = true;
                isTwoFingerDragging = true;
                lastPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                twoFingerStartPos = (touch0.position + touch1.position) * 0.5f;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                float deltaPinch = currentDistance - lastPinchDistance;

                if (isPinching && Mathf.Abs(deltaPinch) > 1f)
                {
                    OnPinch?.Invoke(deltaPinch);
                }

                lastPinchDistance = currentDistance;

                // Two-finger drag (pan)
                Vector2 currentCenter = (touch0.position + touch1.position) * 0.5f;
                if (isTwoFingerDragging)
                {
                    OnTwoFingerDrag?.Invoke(currentCenter - twoFingerStartPos);
                    twoFingerStartPos = currentCenter;
                }
            }
            else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
            {
                isPinching = false;
                isTwoFingerDragging = false;
            }
        }

        private void OnTouchBegan(Vector2 position, int fingerId)
        {
            dragStartPos = position;
            lastDragPos = position;
            dragFingerId = fingerId;
            isDragging = false;
            dragStartedOverUI = IsPointerOverUI(position);

            // Start long press detection
            isLongPressing = true;
            longPressTimer = 0f;
            longPressStartPos = position;
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return false;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = screenPosition;

            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            return results.Count > 0;
        }

        private bool IsPointerOverScrollableUI(Vector2 screenPosition)
        {
            return GetScrollRectUnderPointer(screenPosition) != null;
        }

        private UnityEngine.UI.ScrollRect GetScrollRectUnderPointer(Vector2 screenPosition)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
                return null;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = screenPosition;

            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            // Check if any raycast hit is a ScrollRect or child of ScrollRect
            foreach (var result in results)
            {
                if (result.gameObject != null)
                {
                    // Check if this GameObject or any parent has ScrollRect component
                    UnityEngine.UI.ScrollRect scrollRect = result.gameObject.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                    if (scrollRect != null)
                    {
                        return scrollRect; // Return the ScrollRect component
                    }
                }
            }

            return null; // Not over any ScrollRect
        }

        private void OnTouchMoved(Vector2 position, int fingerId)
        {
            if (fingerId != dragFingerId)
                return;

            // Check for drag
            float distance = Vector2.Distance(dragStartPos, position);

            if (!isDragging && distance > 10f)
            {
                isDragging = true;
                isLongPressing = false; // Cancel long press
                OnDragStart?.Invoke(position); // Notify drag started
            }

            if (isDragging)
            {
                // Pass delta (last frame to current frame)
                Vector2 delta = position - lastDragPos;
                OnDrag?.Invoke(lastDragPos, position);
                lastDragPos = position;
            }

            // Check if movement cancels long press
            if (isLongPressing)
            {
                float movement = Vector2.Distance(longPressStartPos, position);
                if (movement > longPressMaxMovement)
                {
                    isLongPressing = false;
                }
            }
        }

        private void OnTouchEnded(Vector2 position, int fingerId)
        {
            if (fingerId != dragFingerId)
                return;

            Debug.Log($"OnTouchEnded - isDragging: {isDragging}, longPressTimer: {longPressTimer}, longPressTime: {longPressTime}");

            // Consider it a tap if not dragging AND long press timer hasn't reached threshold
            bool isTap = !isDragging && longPressTimer < longPressTime;

            if (isTap)
            {
                // Check for tap / double tap
                float timeSinceLastTap = Time.time - lastTapTime;
                float distanceFromLastTap = Vector2.Distance(position, lastTapPosition);

                if (waitingForDoubleTap &&
                    timeSinceLastTap < doubleTapMaxTime &&
                    distanceFromLastTap < doubleTapMaxDistance)
                {
                    // Double tap detected
                    waitingForDoubleTap = false;
                    OnDoubleTap?.Invoke(position);
                    Debug.Log("Double tap triggered");
                }

                else
                {
                    // First tap, wait for potential double tap
                    waitingForDoubleTap = true;
                    lastTapPosition = position;
                    lastTapTime = Time.time;
                    Debug.Log("Tap registered, waiting for double tap timeout");
                }
            }



            ResetState();
        }

        // Mouse fallback for editor testing
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnTouchBegan(Input.mousePosition, 0);
            }
            else if (Input.GetMouseButton(0))
            {
                OnTouchMoved(Input.mousePosition, 0);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnTouchEnded(Input.mousePosition, 0);
            }
        }

        // Mouse scroll wheel - independent of mouse button state
        private void HandleMouseScrollWheel()
        {
            // Use Input.mouseScrollDelta instead of Input.GetAxis("Mouse ScrollWheel")
            // This works without requiring Input Manager configuration
            Vector2 scrollDelta = Input.mouseScrollDelta;

            if (Mathf.Abs(scrollDelta.y) > 0.01f)
            {
                // Check if over scrollable UI
                UnityEngine.UI.ScrollRect scrollRect = GetScrollRectUnderPointer(Input.mousePosition);

                if (scrollRect != null)
                {
                    // Manually scroll the ScrollRect
                    // ScrollRect expects normalized scroll (0-1), so we need to convert
                    // Typical scroll delta is around 0.1 per tick, sensitivity controls how much to scroll
                    float scrollSensitivity = 0.1f;
                    Vector2 currentScroll = scrollRect.normalizedPosition;

                    // Scroll vertically (Y axis)
                    // Note: ScrollRect's normalizedPosition.y is inverted (1 = top, 0 = bottom)
                    currentScroll.y += scrollDelta.y * scrollSensitivity;
                    currentScroll.y = Mathf.Clamp01(currentScroll.y);

                    scrollRect.normalizedPosition = currentScroll;
                }
                else
                {
                    // Not over ScrollRect, trigger camera zoom
                    // scrollDelta.y is typically in range of -1 to 1 for one scroll tick
                    // Multiply by 100 to match expected zoom speed
                    OnPinch?.Invoke(scrollDelta.y * 100f);
                }
            }
        }

        private void ResetState()
        {
            if (isDragging)
            {
                OnDragEnd?.Invoke(); // Notify drag ended
            }

            isDragging = false;
            isLongPressing = false;
            dragFingerId = -1;
            dragStartedOverUI = false;
        }
    }
}
