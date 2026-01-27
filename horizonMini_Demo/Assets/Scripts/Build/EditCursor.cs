using UnityEngine;
using System;

namespace HorizonMini.Build
{
    /// <summary>
    /// Edit cursor with controls: vertical arrow, rotate button, delete button
    /// Handles dragging operations for moving and rotating objects
    /// </summary>
    public class EditCursor : MonoBehaviour
    {
        [Header("Cursor Components")]
        [SerializeField] private GameObject upArrow;
        [SerializeField] private GameObject rotateButton;
        [SerializeField] private GameObject deleteButton;

        public GameObject UpArrow => upArrow;
        public GameObject RotateButton => rotateButton;
        public GameObject DeleteButton => deleteButton;

        [Header("Settings")]
        [SerializeField] private float verticalSnapUnit = 0.5f; // Vertical movement snap
        [SerializeField] private float horizontalSnapUnit = 0.5f; // XZ plane movement snap
        [SerializeField] private float rotationSnapDegrees = 15f; // Rotation snap

        [Header("Virtual Grid Plane")]
        [SerializeField] private GameObject gridPlanePrefab;
        [SerializeField] private Material gridPlaneMaterial;

        [Header("Rendering")]
        [SerializeField] private string cursorLayerName = "UI"; // Layer for cursor components to be always on top
        [SerializeField] private Color cursorColor = new Color(0.5f, 0.8f, 1f, 1f); // Light blue color

        [Header("Effects")]
        [SerializeField] private PlacementEffectSettings effectSettings;

        // Events
        public event Action<Vector3> OnPositionChanged;
        public event Action<Quaternion> OnRotationChanged;
        public event Action OnDeleteRequested;

        private Transform targetObject;
        private Camera buildCamera;
        private GameObject virtualGridPlane;

        private enum DragMode { None, VerticalMove, Rotate, HorizontalMove }
        private DragMode currentDragMode = DragMode.None;

        private Vector3 dragStartPosition;
        private Quaternion dragStartRotation;
        private Vector2 dragStartScreenPos;
        private float dragStartHeight;

        // Track snapping for audio feedback
        private float lastSnappedY = 0f;
        private float lastSnappedRotation = 0f;
        private Vector3 lastSnappedXZ = Vector3.zero;

        public void Initialize(Transform target, Camera camera)
        {
            targetObject = target;
            buildCamera = camera;

            // Position cursor at target
            transform.position = targetObject.position;
            transform.rotation = targetObject.rotation;

            // Validate components
            if (upArrow == null) Debug.LogError("EditCursor: Up Arrow not assigned!");
            if (rotateButton == null) Debug.LogError("EditCursor: Rotate Button not assigned!");
            if (deleteButton == null) Debug.LogError("EditCursor: Delete Button not assigned!");

            // Set cursor components to UI layer for rendering priority
            SetupCursorLayers();

            // Apply cursor unlit material to all cursor components
            ApplyCursorMaterial();
        }

        private void ApplyCursorMaterial()
        {
            Shader cursorShader = Shader.Find("HorizonMini/CursorUnlit_URP");
            if (cursorShader == null)
            {
                Debug.LogWarning("CursorUnlit_URP shader not found, cursor may be affected by lighting");
                return;
            }

            // Apply to up arrow
            if (upArrow != null)
            {
                ApplyShaderToObject(upArrow, cursorShader);
            }

            // Apply to rotate button
            if (rotateButton != null)
            {
                ApplyShaderToObject(rotateButton, cursorShader);
            }

            // Apply to delete button
            if (deleteButton != null)
            {
                ApplyShaderToObject(deleteButton, cursorShader);
            }
        }

        private void ApplyShaderToObject(GameObject obj, Shader shader)
        {
            // Get all renderers in object and children
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                // Create new materials with cursor shader
                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material oldMat = renderer.sharedMaterials[i];
                    Material newMat = new Material(shader);

                    // Use the cursor color from inspector
                    newMat.SetColor("_BaseColor", cursorColor);

                    // Preserve texture if available
                    if (oldMat != null && oldMat.HasProperty("_MainTex") && oldMat.mainTexture != null)
                    {
                        newMat.SetTexture("_MainTex", oldMat.mainTexture);
                    }

                    newMaterials[i] = newMat;
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        private void SetupCursorLayers()
        {
            int cursorLayer = LayerMask.NameToLayer(cursorLayerName);
            if (cursorLayer == -1)
            {
                Debug.LogWarning($"Layer '{cursorLayerName}' not found, cursor components may be occluded");
                return;
            }

            Debug.Log($"Setting up cursor layers to layer: {cursorLayerName} (index: {cursorLayer})");

            // Set layer for all cursor components and their children
            if (upArrow != null)
            {
                SetLayerRecursive(upArrow, cursorLayer);
                Debug.Log($"Set upArrow to layer {cursorLayer}");
            }
            if (rotateButton != null)
            {
                SetLayerRecursive(rotateButton, cursorLayer);
                Debug.Log($"Set rotateButton to layer {cursorLayer}");
            }
            if (deleteButton != null)
            {
                SetLayerRecursive(deleteButton, cursorLayer);
                Debug.Log($"Set deleteButton '{deleteButton.name}' to layer {cursorLayer}");
            }
            else
            {
                Debug.LogWarning("DeleteButton is null during layer setup!");
            }
        }

        private void SetLayerRecursive(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            // Set layer for all children
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        private void Update()
        {
            if (targetObject == null) return;

            // Keep cursor synced with target position
            transform.position = targetObject.position;

            // Cursor always faces camera (billboard)
            if (buildCamera != null)
            {
                // Look at camera
                Vector3 directionToCamera = buildCamera.transform.position - transform.position;

                // Keep Y axis always up, only rotate around Y axis
                directionToCamera.y = 0;

                if (directionToCamera.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(directionToCamera);
                }
            }
        }

        // Called when user starts dragging a cursor component
        public void OnDragStart(GameObject draggedObject, Vector2 screenPos)
        {
            if (draggedObject == upArrow)
            {
                currentDragMode = DragMode.VerticalMove;
                ShowVirtualGridPlane();
            }
            else if (draggedObject == rotateButton)
            {
                currentDragMode = DragMode.Rotate;
            }
            else if (draggedObject == targetObject.gameObject)
            {
                currentDragMode = DragMode.HorizontalMove;
                ShowVirtualGridPlane();
            }
            else
            {
                currentDragMode = DragMode.None;
                return;
            }

            dragStartPosition = targetObject.position;
            dragStartRotation = targetObject.rotation;
            dragStartScreenPos = screenPos;
            dragStartHeight = targetObject.position.y;

            // Reset snap tracking for audio feedback
            lastSnappedY = targetObject.position.y;
            lastSnappedRotation = 0f;
            lastSnappedXZ = targetObject.position;
        }

        // Called while user is dragging
        public void OnDragUpdate(Vector2 currentScreenPos)
        {
            if (currentDragMode == DragMode.None || targetObject == null) return;

            switch (currentDragMode)
            {
                case DragMode.VerticalMove:
                    HandleVerticalMove(currentScreenPos);
                    break;

                case DragMode.Rotate:
                    HandleRotation(currentScreenPos);
                    break;

                case DragMode.HorizontalMove:
                    HandleHorizontalMove(currentScreenPos);
                    break;
            }
        }

        // Called when user releases drag
        public void OnDragEnd()
        {
            // Play effect based on drag mode
            if (targetObject != null)
            {
                ObjectPlacementEffect effect = targetObject.GetComponent<ObjectPlacementEffect>();
                if (effect == null)
                {
                    effect = targetObject.gameObject.AddComponent<ObjectPlacementEffect>();

                    // Apply settings if available
                    if (effectSettings != null)
                    {
                        effectSettings.ApplyToEffect(effect);
                    }
                }

                if (currentDragMode == DragMode.VerticalMove || currentDragMode == DragMode.HorizontalMove)
                {
                    effect.PlayMoveEffect(targetObject.position);
                    // Play completion sound when releasing after move
                    effect.PlayPlacementCompleteSound();
                }
                else if (currentDragMode == DragMode.Rotate)
                {
                    effect.PlayRotateEffect(targetObject.position);
                    // Play completion sound when releasing after rotate
                    effect.PlayPlacementCompleteSound();
                }
            }

            currentDragMode = DragMode.None;
            HideVirtualGridPlane();
        }

        // Called when delete button is clicked
        public void OnDeleteClicked()
        {
            Debug.Log("EditCursor: Delete clicked, invoking OnDeleteRequested");
            OnDeleteRequested?.Invoke();
        }

        private void HandleVerticalMove(Vector2 screenPos)
        {
            // Calculate vertical delta based on screen drag
            Vector2 screenDelta = screenPos - dragStartScreenPos;
            float verticalDelta = screenDelta.y * 0.01f; // Convert screen pixels to world units (positive Y = up)

            // Apply snap
            float newY = dragStartHeight + verticalDelta;
            newY = Mathf.Round(newY / verticalSnapUnit) * verticalSnapUnit;

            // Clamp to reasonable bounds (0 to volume height)
            newY = Mathf.Max(0f, newY);

            // Update target position
            Vector3 newPosition = new Vector3(dragStartPosition.x, newY, dragStartPosition.z);
            targetObject.position = newPosition;

            // Update virtual grid plane height to bbox bottom
            if (virtualGridPlane != null)
            {
                float bboxBottomY = GetObjectBBoxBottomY();
                Vector3 planePos = virtualGridPlane.transform.position;
                planePos.y = bboxBottomY;
                virtualGridPlane.transform.position = planePos;
            }

            // Play audio feedback when snapping to new Y position
            if (Mathf.Abs(newY - lastSnappedY) >= verticalSnapUnit - 0.01f)
            {
                lastSnappedY = newY;
                ObjectPlacementEffect effect = targetObject.GetComponent<ObjectPlacementEffect>();
                if (effect != null)
                {
                    effect.PlayMoveSnapSound();
                }
            }

            OnPositionChanged?.Invoke(newPosition);
        }

        private void HandleRotation(Vector2 screenPos)
        {
            // Calculate rotation based on horizontal screen drag
            Vector2 screenDelta = screenPos - dragStartScreenPos;
            float rotationDelta = -screenDelta.x * 0.5f; // Convert screen pixels to degrees (negative for natural direction)

            // Apply snap
            rotationDelta = Mathf.Round(rotationDelta / rotationSnapDegrees) * rotationSnapDegrees;

            // Rotate around local Y axis
            Quaternion newRotation = dragStartRotation * Quaternion.Euler(0, rotationDelta, 0);
            targetObject.rotation = newRotation;

            // Play audio feedback when snapping to new rotation (every 15 degrees)
            if (Mathf.Abs(rotationDelta - lastSnappedRotation) >= rotationSnapDegrees - 0.01f)
            {
                lastSnappedRotation = rotationDelta;
                ObjectPlacementEffect effect = targetObject.GetComponent<ObjectPlacementEffect>();
                if (effect != null)
                {
                    effect.PlayRotationSnapSound();
                }
            }

            OnRotationChanged?.Invoke(newRotation);
        }

        private void HandleHorizontalMove(Vector2 screenPos)
        {
            // Raycast to XZ plane at current object height
            Plane xzPlane = new Plane(Vector3.up, new Vector3(0, dragStartHeight, 0));

            Ray ray = buildCamera.ScreenPointToRay(screenPos);
            float enter;

            if (xzPlane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                // Apply snap
                hitPoint.x = Mathf.Round(hitPoint.x / horizontalSnapUnit) * horizontalSnapUnit;
                hitPoint.z = Mathf.Round(hitPoint.z / horizontalSnapUnit) * horizontalSnapUnit;
                hitPoint.y = dragStartHeight; // Maintain current height

                // Update target position
                targetObject.position = hitPoint;

                // Play audio feedback when snapping to new XZ position
                Vector2 currentXZ = new Vector2(hitPoint.x, hitPoint.z);
                Vector2 lastXZ = new Vector2(lastSnappedXZ.x, lastSnappedXZ.z);
                if (Vector2.Distance(currentXZ, lastXZ) >= horizontalSnapUnit - 0.01f)
                {
                    lastSnappedXZ = hitPoint;
                    ObjectPlacementEffect effect = targetObject.GetComponent<ObjectPlacementEffect>();
                    if (effect != null)
                    {
                        effect.PlayMoveSnapSound();
                    }
                }

                OnPositionChanged?.Invoke(hitPoint);
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

            // Create virtual grid plane at current object height
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
                    // Try to load VirtualGridPlane material
                    Material gridMat = Resources.Load<Material>("Materials/VirtualGridPlane");
                    if (gridMat != null)
                    {
                        renderer.material = new Material(gridMat);
                    }
                    else
                    {
                        // Fallback: use depth intersection shader if available
                        Shader depthShader = Shader.Find("HorizonMini/DepthIntersection");
                        if (depthShader != null)
                        {
                            Material mat = new Material(depthShader);
                            mat.SetColor("_GridColor", new Color(1f, 1f, 1f, 0.6f));
                            mat.SetColor("_BaseColor", new Color(0.3f, 0.6f, 1f, 0.15f));
                            mat.SetFloat("_GridSize", horizontalSnapUnit);
                            mat.SetFloat("_GridThickness", 0.02f);
                            mat.SetColor("_IntersectionColor", new Color(0f, 1f, 1f, 1f)); // Cyan highlight
                            mat.SetFloat("_IntersectionThickness", 0.5f); // Increased from 0.1 to 0.5
                            renderer.material = mat;
                        }
                        else
                        {
                            // Fallback: use basic grid shader
                            Shader gridShader = Shader.Find("HorizonMini/GridPlane_URP");
                            if (gridShader != null)
                            {
                                Material mat = new Material(gridShader);
                                mat.SetColor("_GridColor", new Color(1f, 1f, 1f, 0.6f));
                                mat.SetColor("_PlaneColor", new Color(0.3f, 0.6f, 1f, 0.15f));
                                mat.SetFloat("_GridSize", horizontalSnapUnit);
                                mat.SetFloat("_GridThickness", 0.02f);
                                renderer.material = mat;
                            }
                            else
                            {
                                // Last resort: simple transparent material
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

            // Get from SelectionSystem which reads actual LineRenderer positions
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

        private void OnDestroy()
        {
            HideVirtualGridPlane();
        }
    }
}
