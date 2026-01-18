using UnityEngine;
using UnityEngine.EventSystems;
using HorizonMini.Core;
using HorizonMini.Build;
using HorizonMini.UI;
using System.Collections.Generic;

namespace HorizonMini.Controllers
{
    /// <summary>
    /// Main Build Mode controller - manages state machine and coordinates all build systems
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera buildCamera;
        [SerializeField] private Transform buildContainer;
        [SerializeField] private BuildModeUI buildModeUI;

        [Header("Systems")]
        [SerializeField] private BuildCameraController cameraController;
        [SerializeField] private SelectionSystem selectionSystem;
        [SerializeField] private PlacementSystem placementSystem;
        [SerializeField] private TouchGestureDetector gestureDetector;

        [Header("Prefabs")]
        [SerializeField] private GameObject volumeGridPrefab;
        [SerializeField] private GameObject editCursorPrefab;

        [Header("Standalone Mode")]
        [SerializeField] private bool autoInitialize = true; // Auto-initialize in standalone mode

        [Header("Volume Settings")]
        [SerializeField] private Vector3Int maxVolumeSize = new Vector3Int(4, 4, 4); // Max slider values
        [SerializeField] private float volumeUnitSize = 8f; // Size of each volume unit in world space

        private AppRoot appRoot;
        private BuildMode currentMode = BuildMode.SizePicker;
        private bool isActive = false;
        private bool isInitialized = false;
        private bool cameraSetupForVolume = false; // Track if camera was setup for volume preview
        private bool isDraggingCursor = false; // Track if currently dragging cursor component
        private Vector2 cursorDragStartPos; // Screen position where cursor drag started

        // Build state
        private VolumeGrid currentVolumeGrid;
        private List<PlacedObject> placedObjects = new List<PlacedObject>();
        private PlacedObject selectedObject = null;
        private EditCursor currentEditCursor = null;

        // Temp data for new world
        private Vector3Int selectedVolumeSize = new Vector3Int(2, 1, 2);

        public BuildMode CurrentMode => currentMode;
        public PlacedObject SelectedObject => selectedObject;
        public VolumeGrid VolumeGrid => currentVolumeGrid;

        private void Start()
        {
            // Standalone mode: auto-initialize
            if (autoInitialize && !isInitialized)
            {
                Initialize(null);
                SetActive(true);
                Debug.Log("BuildController started in standalone mode");
            }
        }

        public void Initialize(AppRoot root = null)
        {
            appRoot = root; // Can be null for standalone mode

            if (buildContainer == null)
            {
                GameObject container = new GameObject("BuildContainer");
                buildContainer = container.transform;
                buildContainer.SetParent(transform);
            }

            if (buildCamera == null)
            {
                buildCamera = Camera.main;
            }

            // Auto-find BuildModeUI if not set
            if (buildModeUI == null)
            {
                buildModeUI = FindFirstObjectByType<BuildModeUI>();
            }

            // Initialize systems
            if (cameraController == null)
            {
                cameraController = gameObject.AddComponent<BuildCameraController>();
            }
            cameraController.Initialize(buildCamera);

            if (selectionSystem == null)
            {
                selectionSystem = gameObject.AddComponent<SelectionSystem>();
            }
            selectionSystem.Initialize(this, buildCamera);

            if (placementSystem == null)
            {
                placementSystem = gameObject.AddComponent<PlacementSystem>();
            }
            placementSystem.Initialize(this, buildCamera);

            if (gestureDetector == null)
            {
                gestureDetector = gameObject.AddComponent<TouchGestureDetector>();
            }

            SetupGestureEvents();

            isInitialized = true;
        }

        private void SetupGestureEvents()
        {
            gestureDetector.OnSingleTap += HandleSingleTap;
            gestureDetector.OnDragStart += HandleDragStart;
            gestureDetector.OnDrag += HandleDrag;
            gestureDetector.OnDragEnd += HandleDragEnd;
            gestureDetector.OnPinch += HandlePinch;
            gestureDetector.OnTwoFingerDrag += HandleTwoFingerDrag;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (buildCamera != null)
            {
                buildCamera.gameObject.SetActive(active);
            }

            if (active)
            {
                SwitchMode(BuildMode.SizePicker);
            }
        }

        public void SwitchMode(BuildMode newMode)
        {
            // Exit current mode
            ExitMode(currentMode);

            // Enter new mode
            currentMode = newMode;
            EnterMode(newMode);

            // Notify UI
            if (buildModeUI != null)
            {
                buildModeUI.OnModeChanged(newMode);
            }
        }

        private void EnterMode(BuildMode mode)
        {
            switch (mode)
            {
                case BuildMode.SizePicker:
                    // Enable camera for volume preview
                    cameraController.SetEnabled(true);
                    Debug.Log("Entered Size Picker mode");
                    break;

                case BuildMode.View:
                    cameraController.SetEnabled(true);
                    selectionSystem.SetEnabled(true);
                    placementSystem.SetEnabled(true);

                    // Focus camera on volume center
                    if (currentVolumeGrid != null)
                    {
                        cameraController.UpdateTarget(currentVolumeGrid.GetCenter());
                    }

                    Debug.Log("Entered View mode");
                    break;

                case BuildMode.Edit:
                    cameraController.SetEnabled(true);
                    ShowEditCursor();

                    // Focus camera on selected object (only once when entering Edit mode)
                    // Don't continuously track object position during editing
                    if (selectedObject != null)
                    {
                        Debug.Log($"Edit mode - focusing camera on {selectedObject.name} at {selectedObject.transform.position}");
                        cameraController.UpdateTarget(selectedObject.transform.position);
                    }

                    Debug.Log("Entered Edit mode");
                    break;

                case BuildMode.Play:
                    // Enter FPS test mode
                    if (appRoot != null)
                    {
                        // Use PlayController
                        Debug.Log("Entered Play mode");
                    }
                    break;
            }
        }

        private void ExitMode(BuildMode mode)
        {
            switch (mode)
            {
                case BuildMode.Edit:
                    HideEditCursor();
                    break;
            }
        }

        // Called by UI - Update volume preview while adjusting sliders
        public void UpdateVolumePreview(Vector3Int dimensions)
        {
            // Check if initialized
            if (!isInitialized || cameraController == null || buildContainer == null)
            {
                Debug.LogWarning("BuildController not fully initialized, skipping volume preview");
                return;
            }

            selectedVolumeSize = dimensions;

            // Create or update volume grid preview
            if (currentVolumeGrid == null)
            {
                GameObject gridObj;

                // Use prefab if provided, otherwise create from scratch
                if (volumeGridPrefab != null)
                {
                    gridObj = Instantiate(volumeGridPrefab, buildContainer);
                    gridObj.name = "VolumeGrid";
                    currentVolumeGrid = gridObj.GetComponent<VolumeGrid>();

                    // If prefab doesn't have VolumeGrid component, add it
                    if (currentVolumeGrid == null)
                    {
                        currentVolumeGrid = gridObj.AddComponent<VolumeGrid>();
                    }
                }
                else
                {
                    // No prefab - create from scratch
                    gridObj = new GameObject("VolumeGrid");
                    gridObj.transform.SetParent(buildContainer);
                    currentVolumeGrid = gridObj.AddComponent<VolumeGrid>();
                }

                gridObj.transform.localPosition = Vector3.zero;
                currentVolumeGrid.Initialize(dimensions);

                // Setup camera to focus on volume preview
                if (cameraController != null)
                {
                    cameraController.SetupForMaxVolume(dimensions, currentVolumeGrid.volumeSize);
                }
            }
            else
            {
                // Update existing volume grid
                currentVolumeGrid.Initialize(dimensions);

                // Update camera to focus on new volume size
                if (cameraController != null)
                {
                    cameraController.SetupForMaxVolume(dimensions, currentVolumeGrid.volumeSize);
                }
            }
        }

        // Called by UI - Create button from Size Picker (confirm and enter View mode)
        public void CreateVolumeGrid(Vector3Int dimensions)
        {
            selectedVolumeSize = dimensions;

            // Volume grid already exists from preview, just update it
            if (currentVolumeGrid == null)
            {
                UpdateVolumePreview(dimensions);
            }
            else
            {
                // Make sure it's up to date
                currentVolumeGrid.Initialize(dimensions);
            }

            // NOW setup camera to fit the selected volume
            if (!cameraSetupForVolume && cameraController != null)
            {
                cameraController.SetupForMaxVolume(selectedVolumeSize, volumeUnitSize);
                cameraSetupForVolume = true;
                Debug.Log($"Camera setup for volume: {selectedVolumeSize}");
            }

            // Switch to View mode
            SwitchMode(BuildMode.View);
        }

        // Gesture handlers
        private void HandleSingleTap(Vector2 screenPosition)
        {
            // Ignore taps over UI
            if (IsPointerOverUI(screenPosition))
            {
                Debug.Log("Tap ignored - over UI");
                return;
            }

            Ray ray = buildCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            Debug.Log($"HandleSingleTap at {screenPosition}, Mode: {currentMode}");

            if (currentMode == BuildMode.View)
            {
                // Try to select object and enter Edit mode
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");

                    PlacedObject obj = hit.collider.GetComponentInParent<PlacedObject>();
                    if (obj != null)
                    {
                        Debug.Log($"PlacedObject found: {obj.name}");
                        SelectObject(obj);
                        SwitchMode(BuildMode.Edit);
                        return;
                    }
                    else
                    {
                        Debug.Log("Hit object has no PlacedObject component");
                    }
                }
                else
                {
                    Debug.Log("Raycast hit nothing");
                }
            }
            else if (currentMode == BuildMode.Edit)
            {
                // First check cursor components (UI layer - highest priority)
                int cursorLayerMask = 1 << LayerMask.NameToLayer("UI");
                RaycastHit[] cursorHits = Physics.RaycastAll(ray, 1000f, cursorLayerMask);

                if (cursorHits.Length > 0 && currentEditCursor != null)
                {
                    // Hit cursor component
                    GameObject hitObj = cursorHits[0].collider.gameObject;
                    GameObject deleteButton = GetDeleteButton();

                    if (deleteButton != null && (hitObj == deleteButton || hitObj.transform.IsChildOf(deleteButton.transform)))
                    {
                        Debug.Log("Delete button clicked");
                        currentEditCursor.OnDeleteClicked();
                        return;
                    }
                    else
                    {
                        Debug.Log($"Clicked cursor component (not delete): {hitObj.name}");
                        // Clicked on cursor but not delete button - do nothing (stay in Edit mode)
                        return;
                    }
                }

                // Check if clicked another object or empty space
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    PlacedObject obj = hit.collider.GetComponentInParent<PlacedObject>();
                    if (obj != null && obj != selectedObject)
                    {
                        // Switch to editing different object
                        Debug.Log($"Switching to edit another object: {obj.name}");
                        SelectObject(obj);
                        SwitchMode(BuildMode.Edit);
                        return;
                    }
                    else if (obj == selectedObject)
                    {
                        Debug.Log("Clicked on selected object - stay in Edit mode");
                        // Clicked on the selected object itself - stay in Edit mode
                        return;
                    }
                    else
                    {
                        Debug.Log($"Clicked on non-placeable object: {hit.collider.name}");
                        // Clicked something else (like volume grid) - exit to View mode
                        DeselectObject();
                        SwitchMode(BuildMode.View);
                    }
                }
                else
                {
                    // Clicked empty space - exit to View mode
                    Debug.Log("Clicked empty space - exiting Edit mode");
                    DeselectObject();
                    SwitchMode(BuildMode.View);
                }
            }
        }

        private void HandleDragStart(Vector2 screenPos)
        {
            isDraggingCursor = false;
            cursorDragStartPos = screenPos;

            // In Edit mode, check if drag started on cursor component or target object
            if (currentMode == BuildMode.Edit && currentEditCursor != null && selectedObject != null)
            {
                Ray ray = buildCamera.ScreenPointToRay(screenPos);

                // First raycast: Check cursor components (UI layer)
                int cursorLayerMask = 1 << LayerMask.NameToLayer("UI");
                RaycastHit[] cursorHits = Physics.RaycastAll(ray, 1000f, cursorLayerMask);

                if (cursorHits.Length > 0)
                {
                    // Hit cursor component - prioritize it
                    RaycastHit hit = cursorHits[0];
                    GameObject hitObj = hit.collider.gameObject;

                    isDraggingCursor = true;

                    // Determine which component was hit
                    GameObject draggedComponent = null;
                    if (currentEditCursor.UpArrow != null && (hitObj == currentEditCursor.UpArrow || hitObj.transform.IsChildOf(currentEditCursor.UpArrow.transform)))
                        draggedComponent = currentEditCursor.UpArrow;
                    else if (currentEditCursor.RotateButton != null && (hitObj == currentEditCursor.RotateButton || hitObj.transform.IsChildOf(currentEditCursor.RotateButton.transform)))
                        draggedComponent = currentEditCursor.RotateButton;
                    else if (currentEditCursor.DeleteButton != null && (hitObj == currentEditCursor.DeleteButton || hitObj.transform.IsChildOf(currentEditCursor.DeleteButton.transform)))
                        draggedComponent = currentEditCursor.DeleteButton;

                    currentEditCursor.OnDragStart(draggedComponent, screenPos);
                }
                else
                {
                    // Second raycast: Check other objects (target object for XZ movement)
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 1000f))
                    {
                        if (hit.collider.GetComponentInParent<PlacedObject>() == selectedObject)
                        {
                            isDraggingCursor = true;
                            currentEditCursor.OnDragStart(selectedObject.gameObject, screenPos);
                        }
                    }
                }
            }
        }

        private void HandleDrag(Vector2 lastPos, Vector2 currentPos)
        {
            // If dragging cursor, update cursor
            if (isDraggingCursor && currentEditCursor != null)
            {
                currentEditCursor.OnDragUpdate(currentPos);
                return; // Don't move camera
            }

            // Don't move camera if drag started over UI
            if (gestureDetector != null && gestureDetector.DragStartedOverUI)
                return;

            // Camera orbit in View or Edit mode (when not dragging cursor)
            if (currentMode == BuildMode.View || currentMode == BuildMode.Edit)
            {
                Vector2 delta = currentPos - lastPos;
                cameraController.Orbit(delta);
            }
        }

        private void HandleDragEnd()
        {
            if (isDraggingCursor && currentEditCursor != null)
            {
                currentEditCursor.OnDragEnd();

                // Refocus camera on object's new position after drag
                if (selectedObject != null && cameraController != null)
                {
                    cameraController.UpdateTarget(selectedObject.transform.position);
                }
            }

            isDraggingCursor = false;
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            // Check if touch/mouse is over UI element
            if (EventSystem.current == null)
                return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            return results.Count > 0;
        }

        private void HandlePinch(float delta)
        {
            // Allow zoom in SizePicker, View, and Edit modes
            if (currentMode == BuildMode.SizePicker || currentMode == BuildMode.View || currentMode == BuildMode.Edit)
            {
                cameraController.Zoom(delta);
            }
        }

        private void HandleTwoFingerDrag(Vector2 delta)
        {
            if (currentMode == BuildMode.View || currentMode == BuildMode.Edit)
            {
                cameraController.Pan(delta);
            }
        }

        // Object management
        public void SelectObject(PlacedObject obj)
        {
            selectedObject = obj;
            selectionSystem.SetSelection(obj);
        }

        public void DeselectObject()
        {
            selectedObject = null;
            selectionSystem.ClearSelection();
        }

        private void ShowEditCursor()
        {
            HideEditCursor();

            if (selectedObject != null)
            {
                GameObject cursorObj;

                // Create or use prefab
                if (editCursorPrefab != null)
                {
                    cursorObj = Instantiate(editCursorPrefab, selectedObject.transform.position, selectedObject.transform.rotation);
                }
                else
                {
                    // Create default cursor
                    cursorObj = new GameObject("EditCursor");
                    cursorObj.transform.position = selectedObject.transform.position;
                    cursorObj.transform.rotation = selectedObject.transform.rotation;
                    cursorObj.AddComponent<EditCursor>();

                    Debug.LogWarning("EditCursor prefab not assigned! Using default cursor without visuals.");
                }

                // Initialize cursor
                currentEditCursor = cursorObj.GetComponent<EditCursor>();
                if (currentEditCursor != null)
                {
                    currentEditCursor.Initialize(selectedObject.transform, buildCamera);
                    currentEditCursor.OnPositionChanged += OnObjectPositionChanged;
                    currentEditCursor.OnRotationChanged += OnObjectRotationChanged;
                    currentEditCursor.OnDeleteRequested += OnObjectDeleteRequested;
                }
            }
        }

        private void OnObjectPositionChanged(Vector3 newPosition)
        {
            if (selectedObject != null)
            {
                selectedObject.UpdateSavedTransform();
            }
        }

        private void OnObjectRotationChanged(Quaternion newRotation)
        {
            if (selectedObject != null)
            {
                selectedObject.UpdateSavedTransform();
            }
        }

        private void HideEditCursor()
        {
            if (currentEditCursor != null)
            {
                Destroy(currentEditCursor.gameObject);
                currentEditCursor = null;
            }
        }

        private void OnObjectDeleteRequested()
        {
            Debug.Log("BuildController: Delete requested");
            DeleteSelectedObject();
        }

        private bool IsCursorComponent(GameObject obj)
        {
            if (currentEditCursor == null) return false;

            // Check if object is part of cursor hierarchy
            Transform current = obj.transform;
            while (current != null)
            {
                if (current.gameObject == currentEditCursor.gameObject)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private GameObject GetDeleteButton()
        {
            if (currentEditCursor == null)
            {
                Debug.LogWarning("GetDeleteButton: currentEditCursor is null");
                return null;
            }

            GameObject deleteBtn = currentEditCursor.DeleteButton;
            if (deleteBtn == null)
            {
                Debug.LogWarning("GetDeleteButton: DeleteButton is null");
            }
            else
            {
                Debug.Log($"GetDeleteButton: {deleteBtn.name}");
            }
            return deleteBtn;
        }

        public void PlaceObject(PlaceableAsset asset, Vector3 position)
        {
            if (asset == null || asset.prefab == null)
                return;

            GameObject obj = Instantiate(asset.prefab, position, Quaternion.identity, buildContainer);

            // Fix materials to use URP shader
            FixMaterialsToURP(obj);

            PlacedObject placedObj = obj.AddComponent<PlacedObject>();
            placedObj.assetId = asset.assetId;
            placedObj.sourceAsset = asset;
            placedObj.UpdateSavedTransform();

            placedObjects.Add(placedObj);
        }

        private void FixMaterialsToURP(GameObject obj)
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("URP Lit shader not found!");
                return;
            }

            // Get all renderers in object and children
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                    continue;

                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material oldMat = renderer.sharedMaterials[i];

                    if (oldMat == null)
                    {
                        newMaterials[i] = oldMat;
                        continue;
                    }

                    // Check if material is using wrong shader (pink/purple material)
                    bool needsFix = oldMat.shader == null ||
                                   oldMat.shader.name.Contains("Standard") ||
                                   oldMat.shader.name.Contains("Legacy") ||
                                   oldMat.shader.name == "Hidden/InternalErrorShader";

                    if (needsFix)
                    {
                        // Create new material with URP shader
                        Material newMat = new Material(urpLitShader);

                        // Preserve color
                        if (oldMat.HasProperty("_Color"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.color);
                        }
                        else if (oldMat.HasProperty("_BaseColor"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.GetColor("_BaseColor"));
                        }

                        // Preserve texture
                        if (oldMat.HasProperty("_MainTex") && oldMat.mainTexture != null)
                        {
                            newMat.SetTexture("_MainTex", oldMat.mainTexture);
                        }

                        newMat.name = oldMat.name + "_URP";
                        newMaterials[i] = newMat;
                    }
                    else
                    {
                        // Material is fine, keep it
                        newMaterials[i] = oldMat;
                    }
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        public void DeleteSelectedObject()
        {
            if (selectedObject != null)
            {
                Debug.Log($"Deleting object: {selectedObject.name}");
                placedObjects.Remove(selectedObject);
                Destroy(selectedObject.gameObject);
                DeselectObject();
                SwitchMode(BuildMode.View);
                Debug.Log("Object deleted, switched to View mode");
            }
            else
            {
                Debug.LogWarning("DeleteSelectedObject called but selectedObject is null");
            }
        }

        // UI Callbacks
        public void OnGoButtonPressed()
        {
            SwitchMode(BuildMode.Play);
            // TODO: Activate PlayController
        }

        public void OnPublicButtonPressed()
        {
            SaveWorld();
        }

        private void SaveWorld()
        {
            // Create WorldData
            HorizonMini.Data.WorldData worldData = ScriptableObject.CreateInstance<HorizonMini.Data.WorldData>();
            worldData.Initialize();
            worldData.worldTitle = "My Build";
            worldData.worldAuthor = "Me";
            worldData.gridDimensions = selectedVolumeSize;

            // Save placed objects
            foreach (var obj in placedObjects)
            {
                HorizonMini.Data.PropData propData = new HorizonMini.Data.PropData
                {
                    prefabName = obj.assetId,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    scale = obj.transform.localScale
                };
                worldData.props.Add(propData);
            }

            // Save via AppRoot
            if (appRoot != null)
            {
                appRoot.SaveService.SaveCreatedWorld(worldData);
                Debug.Log($"World published: {worldData.worldTitle}");
            }
        }
    }
}
