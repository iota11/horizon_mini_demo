using UnityEngine;
using UnityEngine.Events;

namespace HorizonMini.Build.Cursors
{
    /// <summary>
    /// Object Cursor - for moving, rotating, and deleting placed objects
    /// Handlers: XZ drag, Y drag, Rotate, Delete button, Confirm button
    /// </summary>
    public class ObjectCursor : BaseCursor
    {
        [Header("Handler References")]
        [SerializeField] private Transform xzHandler;
        [SerializeField] private Transform yHandler;
        [SerializeField] private Transform rotateHandler;
        [SerializeField] private Transform deleteButton;
        [SerializeField] private Transform confirmButton;

        [Header("Settings")]
        [SerializeField] private float snapInterval = 0.5f;
        [SerializeField] private float rotationSnapDegrees = 15f;
        [SerializeField] private float yMin = 0f; // Minimum Y position

        [Header("Virtual Grid Plane")]
        [SerializeField] private GameObject gridPlanePrefab;
        [SerializeField] private Material gridPlaneMaterial;

        [Header("Audio")]
        [SerializeField] private PlacementEffectSettings effectSettings;

        [Header("Events")]
        public UnityEvent<Vector3> OnPositionChanged;
        public UnityEvent<Quaternion> OnRotationChanged;
        public UnityEvent OnDeleteRequested;
        public UnityEvent OnConfirmRequested;

        private enum DragMode { None, XZ, Y, Rotate }
        private DragMode currentDragMode = DragMode.None;

        private Vector3 dragStartWorldPos;
        private Vector3 dragStartPosition;
        private float dragStartRotation;
        private Plane dragPlane;
        private Ray currentRay;

        private Transform targetObject; // The object this cursor is controlling
        private GameObject virtualGridPlane;
        private AudioSource audioSource;

        protected override void HandleInput()
        {
            Vector2 screenPos = GetScreenPosition();
            currentRay = buildCamera.ScreenPointToRay(screenPos);

            // Start drag
            if (GetInputDown() && currentDragMode == DragMode.None)
            {
                RaycastHit hit;
                if (Physics.Raycast(currentRay, out hit, 1000f, uiLayer))
                {
                    Transform hitTransform = hit.transform;

                    // Check for delete button
                    if (deleteButton != null && IsChildOf(hitTransform, deleteButton))
                    {
                        OnDeleteRequested?.Invoke();
                        return;
                    }

                    // Check for confirm button
                    if (confirmButton != null && IsChildOf(hitTransform, confirmButton))
                    {
                        OnConfirmRequested?.Invoke();
                        return;
                    }

                    // Check for rotate handler
                    if (rotateHandler != null && IsChildOf(hitTransform, rotateHandler))
                    {
                        StartDragRotate(hit.point);
                        return;
                    }

                    // Check for Y handler
                    if (yHandler != null && IsChildOf(hitTransform, yHandler))
                    {
                        StartDragY(hit.point);
                        return;
                    }

                    // Check for XZ handler
                    if (xzHandler != null && IsChildOf(hitTransform, xzHandler))
                    {
                        StartDragXZ(hit.point);
                        return;
                    }
                }
            }

            // Continue dragging
            if (GetInput())
            {
                switch (currentDragMode)
                {
                    case DragMode.XZ:
                        UpdateDragXZ();
                        break;
                    case DragMode.Y:
                        UpdateDragY();
                        break;
                    case DragMode.Rotate:
                        UpdateDragRotate();
                        break;
                }
            }

            // End drag
            if (GetInputUp())
            {
                currentDragMode = DragMode.None;
                isDragging = false;
                HideVirtualGridPlane();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // Create AudioSource for sound effects
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }

        private void OnDestroy()
        {
            HideVirtualGridPlane();
        }

        /// <summary>
        /// Set effect settings (called by BuildController)
        /// </summary>
        public void SetEffectSettings(PlacementEffectSettings settings)
        {
            effectSettings = settings;
        }

        private void PlaySnapSound(AudioClip clip)
        {
            if (effectSettings == null || clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip, effectSettings.snapSoundVolume);
        }

        private void StartDragXZ(Vector3 hitPoint)
        {
            currentDragMode = DragMode.XZ;
            isDragging = true;
            dragStartPosition = targetObject != null ? targetObject.position : transform.position;

            // Create horizontal plane at current Y level
            dragPlane = new Plane(Vector3.up, dragStartPosition);

            // Project current ray onto the drag plane to get accurate start position
            float enter;
            if (dragPlane.Raycast(currentRay, out enter))
            {
                dragStartWorldPos = currentRay.GetPoint(enter);
            }
            else
            {
                // Fallback to hit point if raycast fails
                dragStartWorldPos = hitPoint;
            }

            // Show virtual grid plane
            ShowVirtualGridPlane();
        }

        private void UpdateDragXZ()
        {
            float enter;
            if (dragPlane.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - dragStartWorldPos;

                Vector3 newPosition = dragStartPosition + worldDelta;
                Vector3 oldPosition = targetObject != null ? targetObject.position : transform.position;

                // Snap to grid
                newPosition.x = Mathf.Round(newPosition.x / snapInterval) * snapInterval;
                newPosition.z = Mathf.Round(newPosition.z / snapInterval) * snapInterval;

                // Keep Y unchanged
                newPosition.y = dragStartPosition.y;

                // Check if position actually changed (snapped to new grid position)
                bool positionChanged = (newPosition - oldPosition).sqrMagnitude > 0.001f;

                if (targetObject != null)
                {
                    targetObject.position = newPosition;
                    OnPositionChanged?.Invoke(newPosition);

                    // Play snap sound when position changes
                    if (positionChanged && effectSettings != null)
                    {
                        PlaySnapSound(effectSettings.moveSnapSound);
                    }
                }
            }
        }

        private void StartDragY(Vector3 hitPoint)
        {
            currentDragMode = DragMode.Y;
            isDragging = true;
            dragStartPosition = targetObject != null ? targetObject.position : transform.position;

            // Create vertical plane perpendicular to camera
            Vector3 cameraForward = buildCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            Plane verticalPlane = new Plane(cameraForward, dragStartPosition);

            // Project current ray onto the vertical plane to get accurate start position
            float enter;
            if (verticalPlane.Raycast(currentRay, out enter))
            {
                dragStartWorldPos = currentRay.GetPoint(enter);
            }
            else
            {
                // Fallback to hit point if raycast fails
                dragStartWorldPos = hitPoint;
            }

            // Show virtual grid plane
            ShowVirtualGridPlane();
        }

        private void UpdateDragY()
        {
            // Create vertical plane perpendicular to camera
            Vector3 cameraForward = buildCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Plane verticalPlane = new Plane(cameraForward, dragStartPosition);

            float enter;
            if (verticalPlane.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                float yDelta = currentWorldPos.y - dragStartWorldPos.y;

                Vector3 oldPosition = targetObject != null ? targetObject.position : transform.position;

                // Calculate desired bottom Y position
                float desiredBottomY = Mathf.Max(yMin, dragStartPosition.y + yDelta);

                // Snap to grid
                desiredBottomY = Mathf.Round(desiredBottomY / snapInterval) * snapInterval;
                desiredBottomY = Mathf.Max(yMin, desiredBottomY);

                // Calculate offset from object position to bbox bottom
                float currentBBoxBottomY = GetObjectBBoxBottomY();
                float offsetToBottom = currentBBoxBottomY - oldPosition.y;

                // Set object position so bbox bottom is at desired height
                Vector3 newPosition = oldPosition;
                newPosition.y = desiredBottomY - offsetToBottom;

                // Check if position actually changed
                bool positionChanged = Mathf.Abs(newPosition.y - oldPosition.y) > 0.001f;

                if (targetObject != null)
                {
                    targetObject.position = newPosition;
                    OnPositionChanged?.Invoke(newPosition);

                    // Play snap sound when position changes
                    if (positionChanged && effectSettings != null)
                    {
                        PlaySnapSound(effectSettings.moveSnapSound);
                    }
                }

                // Update virtual grid plane height to bbox bottom
                if (virtualGridPlane != null)
                {
                    Vector3 planePos = virtualGridPlane.transform.position;
                    planePos.y = desiredBottomY;
                    virtualGridPlane.transform.position = planePos;
                }
            }
        }

        private void StartDragRotate(Vector3 hitPoint)
        {
            currentDragMode = DragMode.Rotate;
            isDragging = true;
            dragStartWorldPos = hitPoint;
            if (targetObject != null)
            {
                dragStartRotation = targetObject.eulerAngles.y;
            }

            // Show virtual grid plane
            ShowVirtualGridPlane();
        }

        private void UpdateDragRotate()
        {
            Vector3 currentPos = targetObject != null ? targetObject.position : transform.position;
            Plane xzPlane = new Plane(Vector3.up, currentPos);

            float enter;
            if (xzPlane.Raycast(currentRay, out enter))
            {
                Vector3 currentWorldPos = currentRay.GetPoint(enter);
                Vector3 startDir = (dragStartWorldPos - currentPos).normalized;
                Vector3 currentDir = (currentWorldPos - currentPos).normalized;

                // Calculate angle change
                float angle = Vector3.SignedAngle(startDir, currentDir, Vector3.up);
                float oldRotation = targetObject != null ? targetObject.rotation.eulerAngles.y : 0f;
                float newRotation = dragStartRotation + angle;

                // Snap to rotation intervals
                newRotation = Mathf.Round(newRotation / rotationSnapDegrees) * rotationSnapDegrees;

                // Check if rotation actually changed
                float rotationDiff = Mathf.Abs(Mathf.DeltaAngle(oldRotation, newRotation));
                bool rotationChanged = rotationDiff > 0.1f;

                if (targetObject != null)
                {
                    Quaternion newRot = Quaternion.Euler(0, newRotation, 0);
                    targetObject.rotation = newRot;
                    OnRotationChanged?.Invoke(newRot);

                    // Play snap sound when rotation changes
                    if (rotationChanged && effectSettings != null)
                    {
                        PlaySnapSound(effectSettings.rotationSnapSound);
                    }
                }
            }
        }

        protected override void UpdateVisuals()
        {
            // Follow target object position (always, even when dragging)
            if (targetObject != null)
            {
                transform.position = targetObject.position;
            }

            // Billboard effect
            base.UpdateVisuals();
        }

        /// <summary>
        /// Set the target object this cursor controls
        /// </summary>
        public void SetTargetObject(Transform target)
        {
            targetObject = target;
        }

        /// <summary>
        /// Get the target object
        /// </summary>
        public Transform GetTargetObject()
        {
            return targetObject;
        }

        /// <summary>
        /// Set minimum Y position
        /// </summary>
        public void SetYMin(float min)
        {
            yMin = min;
        }

        /// <summary>
        /// Enable or disable the delete button
        /// </summary>
        public void SetDeleteEnabled(bool enabled)
        {
            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(enabled);
            }
        }

        private void ShowVirtualGridPlane()
        {
            if (virtualGridPlane != null) return;

            // Get volume grid to determine size and position
            VolumeGrid volumeGrid = FindFirstObjectByType<VolumeGrid>();
            if (volumeGrid == null)
            {
                Debug.LogWarning("VolumeGrid not found, cannot create virtual grid plane");
                return;
            }

            Vector3 volumeSize = volumeGrid.GetWorldSize();
            Vector3 volumeCenter = volumeGrid.GetCenter();

            // Create virtual grid plane
            if (gridPlanePrefab != null)
            {
                virtualGridPlane = Instantiate(gridPlanePrefab);
            }
            else
            {
                // Create default plane
                virtualGridPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Destroy(virtualGridPlane.GetComponent<Collider>()); // Remove collider
            }

            virtualGridPlane.name = "VirtualGridPlane";

            // Calculate bbox bottom Y position
            float bboxBottomY = GetObjectBBoxBottomY();

            // Position at bbox bottom Y height, centered with volume XZ
            virtualGridPlane.transform.position = new Vector3(volumeCenter.x, bboxBottomY, volumeCenter.z);

            // Plane primitive is 10x10, scale to match volume XZ size
            virtualGridPlane.transform.localScale = new Vector3(volumeSize.x / 10f, 1f, volumeSize.z / 10f);

            // Apply material
            Renderer renderer = virtualGridPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (gridPlaneMaterial != null)
                {
                    renderer.material = new Material(gridPlaneMaterial);
                }
                else
                {
                    // Try common shader options
                    Shader depthShader = Shader.Find("HorizonMini/DepthIntersection");
                    if (depthShader != null)
                    {
                        Material mat = new Material(depthShader);
                        mat.SetColor("_GridColor", new Color(1f, 1f, 1f, 0.6f));
                        mat.SetColor("_BaseColor", new Color(0.3f, 0.6f, 1f, 0.15f));
                        mat.SetFloat("_GridSize", snapInterval);
                        mat.SetFloat("_GridThickness", 0.02f);
                        mat.SetColor("_IntersectionColor", new Color(0f, 1f, 1f, 1f));
                        mat.SetFloat("_IntersectionThickness", 0.5f);
                        renderer.material = mat;
                    }
                    else
                    {
                        Shader gridShader = Shader.Find("HorizonMini/GridPlane_URP");
                        if (gridShader != null)
                        {
                            Material mat = new Material(gridShader);
                            mat.SetColor("_GridColor", new Color(1f, 1f, 1f, 0.6f));
                            mat.SetColor("_PlaneColor", new Color(0.3f, 0.6f, 1f, 0.15f));
                            mat.SetFloat("_GridSize", snapInterval);
                            mat.SetFloat("_GridThickness", 0.02f);
                            renderer.material = mat;
                        }
                        else
                        {
                            // Fallback: simple transparent material
                            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            mat.SetFloat("_Surface", 1); // Transparent
                            mat.SetColor("_BaseColor", new Color(0.5f, 0.7f, 1f, 0.2f));
                            mat.renderQueue = 3000;
                            renderer.material = mat;
                        }
                    }
                }
            }
        }

        private void HideVirtualGridPlane()
        {
            if (virtualGridPlane != null)
            {
                Destroy(virtualGridPlane);
                virtualGridPlane = null;
            }
        }

        private float GetObjectBBoxBottomY()
        {
            if (targetObject == null) return 0f;

            // Try to get from SelectionSystem
            HorizonMini.Build.SelectionSystem selectionSystem = FindFirstObjectByType<HorizonMini.Build.SelectionSystem>();
            if (selectionSystem != null)
            {
                float bottomY = selectionSystem.GetBBoxBottomY();
                if (!float.IsNaN(bottomY))
                {
                    return bottomY;
                }
            }

            // Fallback: use renderer bounds
            Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return targetObject.position.y;

            float lowestY = float.MaxValue;
            foreach (Renderer renderer in renderers)
            {
                float bottomY = renderer.bounds.min.y;
                if (bottomY < lowestY)
                {
                    lowestY = bottomY;
                }
            }

            return lowestY;
        }
    }
}
