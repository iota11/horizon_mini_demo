using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using HorizonMini.Core;
using HorizonMini.Build;
using HorizonMini.UI;
using System.Collections.Generic;
using SaveService = HorizonMini.Core.SaveService;

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

        [Header("Asset References")]
        [SerializeField] private HorizonMini.Build.AssetCatalog assetCatalog;

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
        private string currentWorldId = null; // Track if editing existing world

        public BuildMode CurrentMode => currentMode;
        public PlacedObject SelectedObject => selectedObject;
        public VolumeGrid VolumeGrid => currentVolumeGrid;

        private void Start()
        {
            // Standalone mode: auto-initialize
            if (autoInitialize && !isInitialized)
            {
                Initialize(null);

                // Check if we should load an existing world for editing
                if (HorizonMini.Core.SceneTransitionData.HasWorldToEdit())
                {
                    string worldId = HorizonMini.Core.SceneTransitionData.GetAndClearWorldToEdit();
                    Debug.Log($"BuildController: Loading world {worldId} for editing");

                    // Activate first, then load world (LoadWorldForEditing will set the correct mode)
                    isActive = true;
                    gameObject.SetActive(true);
                    if (buildCamera != null)
                    {
                        buildCamera.gameObject.SetActive(true);
                    }

                    LoadWorldForEditing(worldId);
                }
                else
                {
                    // Start with fresh world
                    StartNewWorld();
                    Debug.Log("BuildController started in standalone mode with new world");
                    SetActive(true); // This will trigger SizePicker mode
                }
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

            // Ensure SaveService exists in standalone mode
            if (appRoot == null)
            {
                SaveService existingSaveService = FindFirstObjectByType<SaveService>();
                if (existingSaveService == null)
                {
                    GameObject saveServiceObj = new GameObject("SaveService");
                    saveServiceObj.AddComponent<SaveService>();
                    Debug.Log("Created SaveService for standalone mode");
                }
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

        public void StartNewWorld()
        {
            Debug.Log("StartNewWorld called - clearing all build data");

            // Clear worldId - this is a new world
            currentWorldId = null;

            // Clear existing volume grid
            if (currentVolumeGrid != null)
            {
                Debug.Log($"Destroying existing VolumeGrid: {currentVolumeGrid.name}");
                Destroy(currentVolumeGrid.gameObject);
                currentVolumeGrid = null;
            }

            // Clear placed objects
            Debug.Log($"Clearing {placedObjects.Count} placed objects");
            foreach (var obj in placedObjects)
            {
                if (obj != null)
                {
                    Debug.Log($"Destroying placed object: {obj.name}");
                    Destroy(obj.gameObject);
                }
            }
            placedObjects.Clear();

            // Clear selection
            selectedObject = null;
            if (currentEditCursor != null)
            {
                Destroy(currentEditCursor.gameObject);
                currentEditCursor = null;
            }

            // Also find and destroy any orphaned VolumeGrid or PlacedObject in the scene
            VolumeGrid[] allGrids = FindObjectsByType<VolumeGrid>(FindObjectsSortMode.None);
            Debug.Log($"Found {allGrids.Length} VolumeGrid objects in scene");
            foreach (var grid in allGrids)
            {
                if (grid != currentVolumeGrid) // Already null, but just in case
                {
                    Debug.Log($"Destroying orphaned VolumeGrid: {grid.name}");
                    Destroy(grid.gameObject);
                }
            }

            PlacedObject[] allPlacedObjects = FindObjectsByType<PlacedObject>(FindObjectsSortMode.None);
            Debug.Log($"Found {allPlacedObjects.Length} PlacedObject objects in scene");
            foreach (var obj in allPlacedObjects)
            {
                Debug.Log($"Destroying orphaned PlacedObject: {obj.name}");
                Destroy(obj.gameObject);
            }

            // Reset to default size
            selectedVolumeSize = new Vector3Int(2, 1, 2);
            cameraSetupForVolume = false;

            Debug.Log("StartNewWorld complete - scene cleared");
        }

        public void LoadWorldForEditing(string worldId)
        {
            Debug.Log($"LoadWorldForEditing: {worldId}");

            // Store the worldId for updating when saving
            currentWorldId = worldId;

            // Clear existing data first (but keep currentWorldId)
            string savedWorldId = currentWorldId;
            StartNewWorld();
            currentWorldId = savedWorldId;

            // Load world data from SaveService
            SaveService saveService = FindFirstObjectByType<SaveService>();
            if (saveService == null)
            {
                Debug.LogError("SaveService not found! Cannot load world.");
                return;
            }

            HorizonMini.Data.WorldData worldData = saveService.LoadCreatedWorld(worldId);
            if (worldData == null)
            {
                Debug.LogError($"World {worldId} not found in SaveService!");
                return;
            }

            Debug.Log($"Loaded world: {worldData.worldTitle}, Grid: {worldData.gridDimensions}, Props: {worldData.props.Count}");

            // Set volume size
            selectedVolumeSize = worldData.gridDimensions;

            // Create volume grid
            CreateVolumeGrid(worldData.gridDimensions);

            // Load placed objects
            if (worldData.props != null && worldData.props.Count > 0)
            {
                Debug.Log($"Loading {worldData.props.Count} props...");

                // Get AssetCatalog - try serialized field first, then search scene
                HorizonMini.Build.AssetCatalog catalog = assetCatalog;
                if (catalog == null)
                {
                    Debug.LogWarning("AssetCatalog not assigned, searching in scene...");
                    catalog = FindFirstObjectByType<HorizonMini.Build.AssetCatalog>();
                }

                if (catalog == null)
                {
                    Debug.LogError("AssetCatalog not found! Cannot load props.");
                    Debug.LogError("Please assign AssetCatalog to BuildController in Inspector.");
                    return;
                }
                Debug.Log($"Found AssetCatalog with {catalog.GetAllAssets().Count} assets");

                int loadedCount = 0;
                foreach (var propData in worldData.props)
                {
                    Debug.Log($"Attempting to load prop: {propData.prefabName}");

                    HorizonMini.Build.PlaceableAsset asset = catalog.GetAssetById(propData.prefabName);
                    if (asset == null)
                    {
                        Debug.LogWarning($"Asset not found for ID: {propData.prefabName}");
                        continue;
                    }
                    if (asset.prefab == null)
                    {
                        Debug.LogWarning($"Asset {propData.prefabName} has null prefab!");
                        continue;
                    }

                    GameObject obj = Instantiate(asset.prefab, buildContainer);
                    obj.transform.position = propData.position;
                    obj.transform.rotation = propData.rotation;
                    obj.transform.localScale = propData.scale;
                    obj.name = asset.displayName;

                    PlacedObject placedObj = obj.AddComponent<PlacedObject>();
                    placedObj.assetId = propData.prefabName;
                    placedObj.sourceAsset = asset;
                    placedObj.UpdateSavedTransform();

                    placedObjects.Add(placedObj);
                    loadedCount++;

                    Debug.Log($"✓ Loaded prop #{loadedCount}: {obj.name} at {obj.transform.position}");
                }

                Debug.Log($"Finished loading props: {loadedCount}/{worldData.props.Count} successful");
            }
            else
            {
                Debug.LogWarning("worldData.props is null or empty - no props to load");
            }

            Debug.Log($"World loaded successfully. Switching to View mode.");
            SwitchMode(BuildMode.View);
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
            // Get world name from UI
            string worldName = "My World";
            if (buildModeUI != null)
            {
                VolumeSizePickerUI sizePickerUI = FindFirstObjectByType<VolumeSizePickerUI>();
                if (sizePickerUI != null)
                {
                    worldName = sizePickerUI.GetWorldName();
                }
            }

            // Create or update WorldData
            HorizonMini.Data.WorldData worldData;

            if (!string.IsNullOrEmpty(currentWorldId))
            {
                // Editing existing world - load and update it
                Debug.Log($"Updating existing world: {currentWorldId}");
                SaveService saveService = appRoot != null ? appRoot.SaveService : FindFirstObjectByType<SaveService>();
                worldData = saveService?.LoadCreatedWorld(currentWorldId);

                if (worldData == null)
                {
                    Debug.LogWarning($"Could not load existing world {currentWorldId}, creating new one");
                    worldData = ScriptableObject.CreateInstance<HorizonMini.Data.WorldData>();
                    worldData.Initialize();
                    worldData.worldId = currentWorldId; // Preserve the ID
                }

                // Update fields
                worldData.worldTitle = worldName;
                worldData.gridDimensions = selectedVolumeSize;
            }
            else
            {
                // Creating new world
                Debug.Log("Creating new world");
                worldData = ScriptableObject.CreateInstance<HorizonMini.Data.WorldData>();
                worldData.Initialize();
                worldData.worldTitle = worldName;
                worldData.worldAuthor = "Creator";
                worldData.gridDimensions = selectedVolumeSize;
            }

            // Clear existing props and save current placed objects
            worldData.props.Clear();
            Debug.Log($"Saving {placedObjects.Count} placed objects...");
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
                Debug.Log($"  - Saved {obj.name} at {obj.transform.position}");
            }

            // Save via AppRoot or SaveService
            if (appRoot != null)
            {
                appRoot.SaveService.SaveCreatedWorld(worldData);
                Debug.Log($"<color=green>✓ World published: {worldData.worldTitle}</color>");
                Debug.Log($"<color=green>  World ID: {worldData.worldId}</color>");
                Debug.Log($"<color=green>  Grid Size: {worldData.gridDimensions}</color>");
                Debug.Log($"<color=green>  Props Count: {worldData.props.Count}</color>");

                // Return to Main scene (AppRoot mode)
                appRoot.SwitchToMode(HorizonMini.Core.AppMode.Home);
            }
            else
            {
                // Standalone mode - find SaveService manually
                SaveService saveService = FindFirstObjectByType<SaveService>();
                if (saveService != null)
                {
                    saveService.SaveCreatedWorld(worldData);
                    Debug.Log($"<color=green>✓ World published (standalone): {worldData.worldTitle}</color>");
                    Debug.Log($"<color=green>  World ID: {worldData.worldId}</color>");
                    Debug.Log($"<color=green>  Grid Size: {worldData.gridDimensions}</color>");
                    Debug.Log($"<color=green>  Props Count: {worldData.props.Count}</color>");

                    // Return to Main scene
                    // LoadSceneMode.Single will destroy everything including DontDestroyOnLoad objects
                    // The new Main scene will create a fresh AppRoot
                    if (Application.CanStreamedLevelBeLoaded("Main"))
                    {
                        Debug.Log("Returning to Main scene");
                        SceneManager.LoadScene("Main", LoadSceneMode.Single);
                    }
                    else
                    {
                        Debug.Log("Main scene not found. Staying in BuildMode.");
                    }
                }
                else
                {
                    Debug.LogError("SaveService not found! Cannot save world.");
                }
            }
        }
    }
}
