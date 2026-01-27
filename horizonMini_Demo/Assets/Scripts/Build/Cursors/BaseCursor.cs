using UnityEngine;

namespace HorizonMini.Build.Cursors
{
    /// <summary>
    /// Base class for all cursor types
    /// Handles common functionality like visibility, dragging state, and camera setup
    /// </summary>
    public abstract class BaseCursor : MonoBehaviour, ICursor
    {
        [Header("Base Settings")]
        [SerializeField] protected Camera buildCamera;
        [SerializeField] protected LayerMask uiLayer;

        protected bool isVisible = false;
        protected bool isDragging = false;

        protected virtual void Awake()
        {
            if (buildCamera == null)
                buildCamera = Camera.main;

            if (uiLayer == 0)
                uiLayer = LayerMask.GetMask("UI");

            // Start hidden
            SetVisible(false);
        }

        protected virtual void Update()
        {
            if (!isVisible) return;

            HandleInput();
            UpdateVisuals();
        }

        /// <summary>
        /// Handle input for this cursor (override in derived classes)
        /// </summary>
        protected abstract void HandleInput();

        /// <summary>
        /// Update visual elements (override in derived classes if needed)
        /// </summary>
        protected virtual void UpdateVisuals()
        {
            // Align to camera forward direction projected on XZ plane
            if (buildCamera != null)
            {
                // Get camera forward direction
                Vector3 cameraForward = buildCamera.transform.forward;

                // Project onto XZ plane (remove Y component)
                cameraForward.y = 0;

                if (cameraForward.sqrMagnitude > 0.001f)
                {
                    cameraForward.Normalize();
                    // Rotate 180 degrees around Y axis to face away from camera forward
                    transform.rotation = Quaternion.LookRotation(cameraForward) * Quaternion.Euler(0, 180, 0);
                }
            }
        }

        public virtual void SetVisible(bool visible)
        {
            isVisible = visible;
            gameObject.SetActive(visible);

            if (!visible)
            {
                isDragging = false;
            }
        }

        public bool IsVisible()
        {
            return isVisible;
        }

        public bool IsDragging()
        {
            return isDragging;
        }

        public virtual void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Check if a transform is a child of another transform
        /// </summary>
        protected bool IsChildOf(Transform child, Transform parent)
        {
            if (child == parent) return true;
            if (child.parent == null) return false;
            return IsChildOf(child.parent, parent);
        }

        /// <summary>
        /// Get current screen position for input
        /// </summary>
        protected Vector2 GetScreenPosition()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        /// <summary>
        /// Check if touch/click started this frame
        /// </summary>
        protected bool GetInputDown()
        {
            return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        }

        /// <summary>
        /// Check if touch/click is held
        /// </summary>
        protected bool GetInput()
        {
            return Input.GetMouseButton(0) || Input.touchCount > 0;
        }

        /// <summary>
        /// Check if touch/click ended this frame
        /// </summary>
        protected bool GetInputUp()
        {
            return Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
        }
    }
}
