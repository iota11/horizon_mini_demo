using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HorizonMini.UI
{
    /// <summary>
    /// Virtual joystick for touch-based movement control
    /// Compatible with Top Down Engine's input system
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("Settings")]
        [SerializeField] private float handleRange = 50f;
        [SerializeField] private bool returnToCenter = true;

        private Vector2 inputVector;
        private Vector2 joystickCenter;
        private bool isDragging = false;
        private Canvas canvas;

        public Vector2 InputVector => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (background != null)
            {
                joystickCenter = background.position;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            inputVector = Vector2.zero;

            if (returnToCenter && handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            Vector2 position;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out position))
            {
                // Clamp to handle range
                position = Vector2.ClampMagnitude(position, handleRange);

                // Update handle position
                if (handle != null)
                {
                    handle.anchoredPosition = position;
                }

                // Calculate input vector (-1 to 1)
                inputVector = position / handleRange;
            }
        }

        private void Update()
        {
            // Optional: Debug display
            if (isDragging)
            {
                // Input vector is ready for character controller
            }
        }

        /// <summary>
        /// Get input as normalized direction
        /// </summary>
        public Vector3 GetWorldDirection(Transform cameraTransform)
        {
            if (inputVector.magnitude < 0.01f)
                return Vector3.zero;

            // Convert joystick input to world space relative to camera
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Flatten to XZ plane
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            return (forward * inputVector.y + right * inputVector.x).normalized;
        }
    }
}
