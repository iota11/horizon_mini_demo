
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
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

        [Header("Cursor Prefabs")]
        [SerializeField] private GameObject objectCursorPrefab;
        [SerializeField] private GameObject groundControlPointCursorPrefab;
        [SerializeField] private GameObject heightControlPointCursorPrefab;
        [SerializeField] private GameObject fullControlPointCursorPrefab;

        [Header("Asset References")]
        [SerializeField] private HorizonMini.Build.AssetCatalog assetCatalog;
        [SerializeField] private GameObject spawnPointPrefab; // Unique spawn point prefab

        [Header("Effects")]
        [SerializeField] private HorizonMini.Build.PlacementEffectSettings effectSettings;

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
        private Build.Cursors.ObjectCursor currentObjectCursor = null;
        private Build.Cursors.FullControlPointCursor currentControlPointCursor = null; // For SmartTerrain/SmartWall/SmartHouse XZ control point
        private Build.Cursors.HeightControlPointCursor currentHeightCursor = null; // For SmartWall/SmartHouse Y1 control
        private Build.Cursors.HeightControlPointCursor currentHeightCursor2 = null; // For SmartHouse Y2 control

        // Temp data for new world
        private Vector3Int selectedVolumeSize = new Vector3Int(2, 1, 2);
        private string currentWorldId = null; // Track if editing existing world

        public BuildMode CurrentMode => currentMode;
        public PlacedObject SelectedObject => selectedObject;
        public VolumeGrid VolumeGrid => currentVolumeGrid;

        // Cursor Factory Methods
        public Build.Cursors.ObjectCursor CreateObjectCursor(Transform targetObject, Transform parent = null)
        {
            if (objectCursorPrefab == null)
            {
                Debug.LogError("[BuildController] ObjectCursor prefab not assigned!");
                return null;
            }

            GameObject cursorObj = Instantiate(objectCursorPrefab, parent ?? buildContainer);
            Build.Cursors.ObjectCursor cursor = cursorObj.GetComponent<Build.Cursors.ObjectCursor>();
            if (cursor != null)
            {
                cursor.SetTargetObject(targetObject);
                cursor.UpdatePosition(targetObject.position);
                cursor.SetVisible(true);

                // Apply effect settings for audio feedback
                if (effectSettings != null)
                {
                    cursor.SetEffectSettings(effectSettings);
                }
            }
            else
            {
                Debug.LogError("[BuildController] ObjectCursor component not found on prefab!");
            }
            return cursor;
        }

        public Build.Cursors.GroundControlPointCursor CreateGroundControlPointCursor(Vector3 position, Transform parent = null)
        {
            if (groundControlPointCursorPrefab == null)
            {
                Debug.LogError("[BuildController] GroundControlPointCursor prefab not assigned!");
                return null;
            }

            GameObject cursorObj = Instantiate(groundControlPointCursorPrefab, parent ?? buildContainer);
            Build.Cursors.GroundControlPointCursor cursor = cursorObj.GetComponent<Build.Cursors.GroundControlPointCursor>();
            if (cursor != null)
            {
                cursor.UpdatePosition(position);
                cursor.SetVisible(true);
            }
            else
            {
                Debug.LogError("[BuildController] GroundControlPointCursor component not found on prefab!");
            }
            return cursor;
        }

        public Build.Cursors.HeightControlPointCursor CreateHeightControlPointCursor(Vector3 position, float yMin, Transform parent = null)
        {
            if (heightControlPointCursorPrefab == null)
            {
                Debug.LogError("[BuildController] HeightControlPointCursor prefab not assigned!");
                return null;
            }

            GameObject cursorObj = Instantiate(heightControlPointCursorPrefab, parent ?? buildContainer);
            Build.Cursors.HeightControlPointCursor cursor = cursorObj.GetComponent<Build.Cursors.HeightControlPointCursor>();
            if (cursor != null)
            {
                cursor.SetYMin(yMin);
                cursor.UpdatePosition(position);
                cursor.SetVisible(true);
            }
            else
            {
                Debug.LogError("[BuildController] HeightControlPointCursor component not found on prefab!");
            }
            return cursor;
        }

        public Build.Cursors.FullControlPointCursor CreateFullControlPointCursor(Vector3 position, float yMin, Transform parent = null)
        {
            if (fullControlPointCursorPrefab == null)
            {
                Debug.LogError("[BuildController] FullControlPointCursor prefab not assigned!");
                return null;
            }

            GameObject cursorObj = Instantiate(fullControlPointCursorPrefab, parent ?? buildContainer);
            Build.Cursors.FullControlPointCursor cursor = cursorObj.GetComponent<Build.Cursors.FullControlPointCursor>();
            if (cursor != null)
            {
                cursor.SetYMin(yMin);
                cursor.UpdatePosition(position);
                cursor.SetVisible(true);
            }
            else
            {
                Debug.LogError("[BuildController] FullControlPointCursor component not found on prefab!");
            }
            return cursor;
        }

        private void Start()
        {
            // Clean up draft worlds on startup
            CleanupDraftWorlds();

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

            // Apply effect settings to placement system
            if (effectSettings != null)
            {
                effectSettings.ApplyPickupSoundToPlacementSystem(placementSystem);
            }

            if (gestureDetector == null)
            {
                gestureDetector = gameObject.AddComponent<TouchGestureDetector>();
            }

            SetupGestureEvents();

            isInitialized = true;
        }

        private void SetupGestureEvents()
        {
            gestureDetector.OnLongPress += HandleLongPress; // Long press to enter Edit mode
            gestureDetector.OnSingleTap += HandleSingleTap; // Single tap for other actions
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
            if (currentObjectCursor != null)
            {
                Destroy(currentObjectCursor.gameObject);
                currentObjectCursor = null;
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

            // Create volume grid WITHOUT creating initial terrain/spawn point
            // (those will be loaded from props)
            UpdateVolumePreview(worldData.gridDimensions);

            // Setup camera for this volume size
            if (!cameraSetupForVolume && cameraController != null)
            {
                cameraController.SetupForMaxVolume(selectedVolumeSize, volumeUnitSize);
                cameraSetupForVolume = true;
                Debug.Log($"Camera setup for volume: {selectedVolumeSize}");
            }

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

                    GameObject obj = null;

                    // Special handling for SpawnPoint
                    if (propData.prefabName != null && propData.prefabName.Contains("spawn_point"))
                    {
                        Debug.Log("[LoadWorldForEditing] Loading SpawnPoint");

                        // Prioritize assigned spawn point prefab in BuildController
                        if (spawnPointPrefab != null)
                        {
                            obj = Instantiate(spawnPointPrefab, buildContainer);
                            Debug.Log("[LoadWorldForEditing] Using assigned spawnPointPrefab");
                        }
                        // Fallback to catalog
                        else if (catalog != null)
                        {
                            HorizonMini.Build.PlaceableAsset spawnAsset = catalog.GetAssetById(propData.prefabName);
                            if (spawnAsset != null && spawnAsset.prefab != null)
                            {
                                obj = Instantiate(spawnAsset.prefab, buildContainer);
                                Debug.Log("[LoadWorldForEditing] Using catalog spawn point");
                            }
                        }

                        // Last resort: create programmatically
                        if (obj == null)
                        {
                            // Fallback: Create programmatically
                            Debug.LogWarning("Creating SpawnPoint programmatically (no prefab found)");
                            obj = new GameObject("SpawnPoint");
                            obj.transform.SetParent(buildContainer);

                            SpawnPoint sp = obj.AddComponent<SpawnPoint>();
                            sp.SetSpawnType(SpawnType.Player);
                            sp.SetInitialSpawn(true);

                            // No visual marker - SpawnPoint shows via Gizmos in Editor only
                        }
                    }
                    else
                    {
                        // Normal asset loading
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

                        obj = Instantiate(asset.prefab, buildContainer);
                    }

                    if (obj == null)
                    {
                        Debug.LogError($"Failed to instantiate prop: {propData.prefabName}");
                        continue;
                    }

                    // Check if it's a SmartTerrain - restore control point position BEFORE setting transform
                    // This ensures the mesh is regenerated with correct size
                    SmartTerrain terrain = obj.GetComponent<SmartTerrain>();
                    if (terrain != null)
                    {
                        Debug.Log($"[LOAD] Found SmartTerrain: {obj.name}");
                        Debug.Log($"[LOAD] Saved control point data: {propData.smartTerrainControlPoint}");
                        Debug.Log($"[LOAD] Current control point exists: {terrain.controlPoint != null}");
                        if (terrain.controlPoint != null)
                        {
                            Debug.Log($"[LOAD] Current control point position BEFORE restore: {terrain.controlPoint.localPosition}");
                        }

                        // Check if we have saved control point data (will be zero if not saved or default)
                        if (propData.smartTerrainControlPoint != Vector3.zero)
                        {
                            terrain.SetControlPointPosition(propData.smartTerrainControlPoint, forceImmediate: true);
                            Debug.Log($"[LOAD] ✓ Called SetControlPointPosition with: {propData.smartTerrainControlPoint}");
                            if (terrain.controlPoint != null)
                            {
                                Debug.Log($"[LOAD] Control point position AFTER restore: {terrain.controlPoint.localPosition}");
                            }
                            Debug.Log($"[LOAD] Resulting terrain size: {terrain.GetSize()}");
                        }
                        else
                        {
                            Debug.LogWarning($"[LOAD] SmartTerrain loaded without saved control point data (was zero) - using default");
                        }
                    }

                    // Check if it's a SmartWall - restore control points and height BEFORE setting transform
                    SmartWall wall = obj.GetComponent<SmartWall>();
                    if (wall != null && propData.smartWallControlPoints != null && propData.smartWallControlPoints.Count > 0)
                    {
                        Debug.Log($"[LOAD] Found SmartWall: {obj.name}");
                        Debug.Log($"[LOAD] Restoring {propData.smartWallControlPoints.Count} control points, height: {propData.smartWallHeight}");
                        wall.RestoreFromData(propData.smartWallControlPoints, propData.smartWallHeight);
                        Debug.Log($"[LOAD] ✓ SmartWall restored with {wall.GetControlPointCount()} control points");
                    }

                    obj.transform.position = propData.position;
                    obj.transform.rotation = propData.rotation;
                    obj.transform.localScale = propData.scale;

                    // Set object name (use asset display name if available, otherwise prefab name)
                    HorizonMini.Build.PlaceableAsset assetRef = catalog.GetAssetById(propData.prefabName);
                    if (assetRef != null)
                    {
                        obj.name = assetRef.displayName;
                    }
                    else
                    {
                        obj.name = propData.prefabName;
                    }

                    PlacedObject placedObj = obj.AddComponent<PlacedObject>();
                    placedObj.assetId = propData.prefabName;
                    placedObj.sourceAsset = assetRef; // May be null for SpawnPoint
                    placedObj.UpdateSavedTransform();

                    placedObjects.Add(placedObj);
                    loadedCount++;
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
            // Declare manager variables once for the entire method
            SmartTerrainManager terrainMgr;
            SmartWallManager wallMgr;
            SpawnPointManager spawnMgr;

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

                    // Hide SmartTerrain control points in View mode
                    terrainMgr = SmartTerrainManager.Instance;
                    if (terrainMgr != null) terrainMgr.EnterViewMode();

                    // Hide SmartWall control points in View mode
                    wallMgr = SmartWallManager.Instance;
                    if (wallMgr != null) wallMgr.EnterViewMode();

                    // Hide SpawnPoint cursors in View mode
                    spawnMgr = SpawnPointManager.Instance;
                    if (spawnMgr != null) spawnMgr.EnterViewMode();

                    // Auto-build NavMesh when exiting Edit mode
                    BuildNavMesh();

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

                    // Show SmartTerrain control points in Edit mode
                    terrainMgr = SmartTerrainManager.Instance;
                    if (terrainMgr != null) terrainMgr.EnterEditMode();

                    // Show SmartWall control points in Edit mode
                    wallMgr = SmartWallManager.Instance;
                    if (wallMgr != null) wallMgr.EnterEditMode();

                    // Show SpawnPoint cursors in Edit mode
                    spawnMgr = SpawnPointManager.Instance;
                    if (spawnMgr != null) spawnMgr.EnterEditMode();

                    Debug.Log("Entered Edit mode");
                    break;

                case BuildMode.Play:
                    // Play mode is now a separate scene, this shouldn't be reached
                    Debug.LogWarning("BuildMode.Play entered - this is deprecated, use Play scene instead");
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

            // Create world ID if this is a new world
            if (string.IsNullOrEmpty(currentWorldId))
            {
                currentWorldId = System.Guid.NewGuid().ToString();
                Debug.Log($"[BuildController] Created new world ID: {currentWorldId}");
            }

            // Create initial terrain and spawn point BEFORE saving/switching
            // (this creates the managers that SaveWorld and SwitchMode need)
            CreateInitialTerrain();
            CreateInitialSpawnPoint();

            // Save world immediately after creation
            SaveWorld();

            // Switch to View mode
            SwitchMode(BuildMode.View);
        }

        // Helper methods for safe manager access
        private bool HasTerrainManager()
        {
            return SmartTerrainManager.Instance != null;
        }

        private bool HasWallManager()
        {
            return SmartWallManager.Instance != null;
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

            Debug.Log($"HandleSingleTap at {screenPosition}, Mode: {currentMode}");

            // Single tap in Edit mode: exit to View mode
            if (currentMode == BuildMode.Edit)
            {
                Debug.Log("Single tap in Edit mode - exiting to View mode");
                DeselectObject();
                if (SmartTerrainManager.Instance != null)
                    SmartTerrainManager.Instance.ClearActiveTerrain();
                if (SmartWallManager.Instance != null)
                    SmartWallManager.Instance.ClearActiveWall();
                SwitchMode(BuildMode.View);
                return;
            }

            // Single tap in View mode: do nothing (need long press to select)
        }

        private void HandleLongPress(Vector2 screenPosition)
        {
            // Ignore long press over UI
            if (IsPointerOverUI(screenPosition))
            {
                Debug.Log("Long press ignored - over UI");
                return;
            }

            Ray ray = buildCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            Debug.Log($"HandleLongPress at {screenPosition}, Mode: {currentMode}");

            if (currentMode == BuildMode.View)
            {
                // Try to select object and enter Edit mode
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");

                    SmartTerrain terrain = hit.collider.GetComponentInParent<SmartTerrain>();
                    SmartWall wall = hit.collider.GetComponentInParent<SmartWall>();
                    SpawnPoint spawnPoint = hit.collider.GetComponentInParent<SpawnPoint>();
                    PlacedObject obj = hit.collider.GetComponentInParent<PlacedObject>();

                    // Handle SpawnPoint selection
                    if (spawnPoint != null)
                    {
                        Debug.Log($"SpawnPoint clicked: {spawnPoint.name}");
                        var spawnMgr = SpawnPointManager.Instance;
                        if (spawnMgr != null)
                        {
                            spawnMgr.SetActiveSpawnPoint(spawnPoint);
                        }
                        SwitchMode(BuildMode.Edit);
                        return;
                    }

                    if (obj != null)
                    {
                        Debug.Log($"PlacedObject found: {obj.name}");

                        // Check if it's also a SmartTerrain
                        SmartTerrain objTerrain = obj.GetComponent<SmartTerrain>();
                        if (objTerrain != null)
                        {
                            Debug.Log($"SmartTerrain with PlacedObject: {obj.name}");
                            // Select it as PlacedObject (for dragging) AND set as active terrain (for handlers)
                            SelectObject(obj);
                            var terrainMgr = SmartTerrainManager.Instance;
                            if (terrainMgr != null)
                            {
                                terrainMgr.SetActiveTerrain(objTerrain);
                            }
                        }
                        // Check if it's also a SmartWall
                        else
                        {
                            SmartWall objWall = obj.GetComponent<SmartWall>();
                            if (objWall != null)
                            {
                                Debug.Log($"SmartWall with PlacedObject: {obj.name}");
                                SelectObject(obj);
                                if (HasWallManager())
                                    SmartWallManager.Instance.SetActiveWall(objWall);
                            }
                            else
                            {
                                // Normal PlacedObject
                                SelectObject(obj);
                            }
                        }

                        SwitchMode(BuildMode.Edit);
                        return;
                    }
                    else if (terrain != null)
                    {
                        Debug.Log($"SmartTerrain without PlacedObject: {terrain.name}");
                        if (HasTerrainManager())
                            SmartTerrainManager.Instance.SetActiveTerrain(terrain);
                        SwitchMode(BuildMode.Edit);
                        return;
                    }
                    else if (wall != null)
                    {
                        Debug.Log($"SmartWall without PlacedObject: {wall.name}");
                        if (HasWallManager())
                            SmartWallManager.Instance.SetActiveWall(wall);
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
                    // Clear active terrain when clicking empty space
                    if (HasTerrainManager())
                        SmartTerrainManager.Instance.ClearActiveTerrain();
                    // Clear active wall when clicking empty space
                    if (HasWallManager())
                        SmartWallManager.Instance.ClearActiveWall();
                }
            }
            else if (currentMode == BuildMode.Edit)
            {
                // Long press in Edit mode: switch to another object
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    PlacedObject obj = hit.collider.GetComponentInParent<PlacedObject>();

                    if (obj != null && obj != selectedObject)
                    {
                        // Switch to editing different object
                        Debug.Log($"Long press - switching to edit another object: {obj.name}");

                        // Check if it's also a SmartTerrain
                        SmartTerrain objTerrain = obj.GetComponent<SmartTerrain>();
                        if (objTerrain != null)
                        {
                            SelectObject(obj);
                            if (HasTerrainManager())
                                SmartTerrainManager.Instance.SetActiveTerrain(objTerrain);
                        }
                        // Check if it's also a SmartWall
                        else
                        {
                            SmartWall objWall = obj.GetComponent<SmartWall>();
                            if (objWall != null)
                            {
                                SelectObject(obj);
                                if (HasWallManager())
                                    SmartWallManager.Instance.SetActiveWall(objWall);
                            }
                            else
                            {
                                SelectObject(obj);
                            }
                        }

                        SwitchMode(BuildMode.Edit);
                        return;
                    }
                }
            }
        }

        private void HandleDragStart(Vector2 screenPos)
        {
            isDraggingCursor = false;
            cursorDragStartPos = screenPos;

            Ray ray = buildCamera.ScreenPointToRay(screenPos);

            // Check UI layer - ObjectCursor, SmartTerrainCursor, SmartWallCursor, etc. handle their own input
            // We just need to detect if we hit UI to prevent camera dragging
            int uiLayerMask = 1 << LayerMask.NameToLayer("UI");
            RaycastHit[] uiHits = Physics.RaycastAll(ray, 1000f, uiLayerMask);

            if (uiHits.Length > 0)
            {
                // Hit UI layer - cursor handles its own input, prevent camera drag
                isDraggingCursor = true;
                return;
            }

            // Check if ObjectCursor is dragging (it handles input internally)
            if (currentObjectCursor != null && currentObjectCursor.IsDragging())
            {
                isDraggingCursor = true;
                return;
            }

            // Check if SmartTerrainCursor is dragging
            var terrainMgr = SmartTerrainManager.Instance;
            if (terrainMgr != null && terrainMgr.IsAnyTerrainCursorDragging())
            {
                isDraggingCursor = true;
                return;
            }

            // Check if SmartWallCursor is dragging
            var wallMgr = SmartWallManager.Instance;
            if (wallMgr != null && wallMgr.IsAnyWallCursorDragging())
            {
                isDraggingCursor = true;
                return;
            }

            // SpawnPointCursor no longer used - SpawnPoint now uses ObjectCursor
        }

        private void HandleDrag(Vector2 lastPos, Vector2 currentPos)
        {
            // Cursors handle their own drag updates
            // We just prevent camera drag when any cursor is being dragged

            // Check if ObjectCursor is dragging
            if (currentObjectCursor != null && currentObjectCursor.IsDragging())
            {
                return; // Don't move camera
            }

            // Check if FullControlPointCursor is dragging
            if (currentControlPointCursor != null && currentControlPointCursor.IsDragging())
            {
                return; // Don't move camera
            }

            // Check if HeightControlPointCursor is dragging
            if (currentHeightCursor != null && currentHeightCursor.IsDragging())
            {
                return; // Don't move camera
            }

            // Check if HeightControlPointCursor2 is dragging
            if (currentHeightCursor2 != null && currentHeightCursor2.IsDragging())
            {
                return; // Don't move camera
            }

            // Check if any SmartTerrainCursor is being dragged
            var terrainMgr = SmartTerrainManager.Instance;
            if (terrainMgr != null && terrainMgr.IsAnyTerrainCursorDragging())
            {
                return; // Don't move camera while dragging terrain control points
            }

            // Check if any SmartWallCursor is being dragged
            var wallMgr = SmartWallManager.Instance;
            if (wallMgr != null && wallMgr.IsAnyWallCursorDragging())
            {
                return; // Don't move camera while dragging wall control points
            }

            // SpawnPointCursor no longer used - SpawnPoint now uses ObjectCursor (checked above)

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
            // ObjectCursor handles its own drag end internally
            // We just need to refocus camera if object was dragged
            if (isDraggingCursor && currentObjectCursor != null && selectedObject != null && cameraController != null)
            {
                // Refocus camera on object's new position after drag
                cameraController.UpdateTarget(selectedObject.transform.position);
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
                // Check if this is a SmartTerrain
                SmartTerrain terrain = selectedObject.GetComponent<SmartTerrain>();
                if (terrain != null)
                {
                    // SmartTerrain: create ObjectCursor + FullControlPointCursor
                    // Activate control point (needed for FullControlPointCursor to track position)
                    // The control point itself has no visuals - it's just a transform
                    terrain.SetControlPointVisible(true);

                    // Create ObjectCursor for terrain
                    currentObjectCursor = CreateObjectCursor(selectedObject.transform, buildContainer);
                    if (currentObjectCursor != null)
                    {
                        currentObjectCursor.OnPositionChanged.AddListener(OnObjectPositionChanged);
                        currentObjectCursor.OnRotationChanged.AddListener(OnObjectRotationChanged);
                        currentObjectCursor.OnDeleteRequested.AddListener(OnObjectDeleteRequested);
                        currentObjectCursor.OnConfirmRequested.AddListener(OnObjectConfirmRequested);
                    }

                    // Create FullControlPointCursor for control point
                    if (terrain.controlPoint != null)
                    {
                        Vector3 controlPointWorldPos = terrain.controlPoint.position;
                        float yMin = terrain.transform.position.y + 0.5f; // Use minSize.y

                        currentControlPointCursor = CreateFullControlPointCursor(controlPointWorldPos, yMin, buildContainer);
                        if (currentControlPointCursor != null)
                        {
                            // Set target transform so cursor follows control point when terrain rotates
                            currentControlPointCursor.SetTargetTransform(terrain.controlPoint);
                            currentControlPointCursor.OnPositionChanged.AddListener(terrain.OnControlPointPositionChanged);
                        }
                    }
                }
                else
                {
                    // Check if this is a SmartWall
                    SmartWall wall = selectedObject.GetComponent<SmartWall>();
                    if (wall != null)
                    {
                        // SmartWall: use ObjectCursor for wall itself + FullControlPointCursor for end point + HeightControlPointCursor for height
                        currentObjectCursor = CreateObjectCursor(selectedObject.transform, buildContainer);
                        if (currentObjectCursor != null)
                        {
                            currentObjectCursor.OnPositionChanged.AddListener(OnObjectPositionChanged);
                            currentObjectCursor.OnRotationChanged.AddListener(OnObjectRotationChanged);
                            currentObjectCursor.OnDeleteRequested.AddListener(OnObjectDeleteRequested);
                            currentObjectCursor.OnConfirmRequested.AddListener(OnObjectConfirmRequested);
                        }

                        // Create FullControlPointCursor for the end control point (index 1)
                        // Wall has 2 points: start (index 0) and end (index 1)
                        // Only the end point needs a cursor (start point moves with wall)
                        if (wall.GetControlPointCount() >= 2)
                        {
                            Transform endPointTransform = wall.GetControlPointTransform(1);
                            if (endPointTransform != null)
                            {
                                Vector3 endPointWorldPos = endPointTransform.position;
                                float yMin = wall.transform.position.y; // Wall control points stay at y=0 (local)

                                currentControlPointCursor = CreateFullControlPointCursor(endPointWorldPos, yMin, buildContainer);
                                if (currentControlPointCursor != null)
                                {
                                    // Set target transform so cursor follows control point when wall rotates
                                    currentControlPointCursor.SetTargetTransform(endPointTransform);
                                    currentControlPointCursor.OnPositionChanged.AddListener((newPos) => wall.OnControlPointPositionChanged(1, newPos));
                                }
                            }

                            // Create HeightControlPointCursor at the midpoint of the wall, at current height
                            Vector3 startPos = wall.GetControlPointTransform(0).position;
                            Vector3 endPos = wall.GetControlPointTransform(1).position;
                            Vector3 midPoint = (startPos + endPos) * 0.5f;

                            // Position at current wall height
                            Vector3 heightCursorPos = midPoint;
                            heightCursorPos.y = wall.transform.position.y + wall.GetWallHeight();

                            float minHeight = 0.5f; // Minimum wall height
                            currentHeightCursor = CreateHeightControlPointCursor(heightCursorPos, minHeight, buildContainer);
                            if (currentHeightCursor != null)
                            {
                                // Set target wall so cursor follows wall midpoint automatically
                                currentHeightCursor.SetTargetWall(wall);
                                currentHeightCursor.OnPositionChanged.AddListener(wall.OnHeightCursorPositionChanged);
                            }
                        }
                    }
                    else
                    {
                        // Check if this is a SmartHouse
                        SmartHouse house = selectedObject.GetComponent<SmartHouse>();
                        if (house != null)
                        {
                            // SmartHouse: ObjectCursor + FullControlPointCursor (XZ) + 2x HeightControlPointCursor (Y1, Y2)
                            house.SetControlPointsVisible(true);

                            // Create ObjectCursor for house
                            currentObjectCursor = CreateObjectCursor(selectedObject.transform, buildContainer);
                            if (currentObjectCursor != null)
                            {
                                currentObjectCursor.OnPositionChanged.AddListener(OnObjectPositionChanged);
                                currentObjectCursor.OnRotationChanged.AddListener(OnObjectRotationChanged);
                                currentObjectCursor.OnDeleteRequested.AddListener(OnObjectDeleteRequested);
                                currentObjectCursor.OnConfirmRequested.AddListener(OnObjectConfirmRequested);
                            }

                            // Create FullControlPointCursor for XZ control point
                            if (house.xzControlPoint != null)
                            {
                                Vector3 xzControlPointWorldPos = house.xzControlPoint.position;
                                float yMin = house.transform.position.y;

                                currentControlPointCursor = CreateFullControlPointCursor(xzControlPointWorldPos, yMin, buildContainer);
                                if (currentControlPointCursor != null)
                                {
                                    currentControlPointCursor.SetTargetTransform(house.xzControlPoint);
                                    currentControlPointCursor.OnPositionChanged.AddListener(house.OnXZControlPointPositionChanged);
                                }
                            }

                            // Get house size for positioning height cursors at bbox edges
                            Vector3 houseSize = house.GetSize();
                            float halfWidth = houseSize.x * 0.5f;

                            // Create HeightControlPointCursor for Y1 control point (left edge)
                            if (house.yControlPoint1 != null)
                            {
                                Vector3 y1ControlPointWorldPos = house.yControlPoint1.position;
                                // Offset to left edge of bbox
                                y1ControlPointWorldPos.x = house.transform.position.x - halfWidth;
                                float minHeight = 0.5f;

                                currentHeightCursor = CreateHeightControlPointCursor(y1ControlPointWorldPos, minHeight, buildContainer);
                                if (currentHeightCursor != null)
                                {
                                    currentHeightCursor.OnPositionChanged.AddListener(house.OnY1ControlPointPositionChanged);
                                }
                            }

                            // Create HeightControlPointCursor for Y2 control point (right edge)
                            if (house.yControlPoint2 != null)
                            {
                                Vector3 y2ControlPointWorldPos = house.yControlPoint2.position;
                                // Offset to right edge of bbox
                                y2ControlPointWorldPos.x = house.transform.position.x + halfWidth;
                                float minHeight = 0.5f;

                                currentHeightCursor2 = CreateHeightControlPointCursor(y2ControlPointWorldPos, minHeight, buildContainer);
                                if (currentHeightCursor2 != null)
                                {
                                    currentHeightCursor2.OnPositionChanged.AddListener(house.OnY2ControlPointPositionChanged);
                                }
                            }
                        }
                        else
                        {
                            // Check if this is a SpawnPoint
                            SpawnPoint spawnPoint = selectedObject.GetComponent<SpawnPoint>();

                            // Regular object or SpawnPoint: use ObjectCursor only
                            currentObjectCursor = CreateObjectCursor(selectedObject.transform, buildContainer);
                            if (currentObjectCursor != null)
                            {
                                currentObjectCursor.OnPositionChanged.AddListener(OnObjectPositionChanged);
                                currentObjectCursor.OnRotationChanged.AddListener(OnObjectRotationChanged);
                                currentObjectCursor.OnDeleteRequested.AddListener(OnObjectDeleteRequested);
                                currentObjectCursor.OnConfirmRequested.AddListener(OnObjectConfirmRequested);

                                // If this is the initial spawn point, disable delete button
                                if (spawnPoint != null && spawnPoint.IsInitialSpawn)
                                {
                                    currentObjectCursor.SetDeleteEnabled(false);
                                }
                            }
                        }
                    }
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
            // Hide ObjectCursor
            if (currentObjectCursor != null)
            {
                Destroy(currentObjectCursor.gameObject);
                currentObjectCursor = null;
            }

            // Hide FullControlPointCursor (for SmartTerrain/SmartWall)
            if (currentControlPointCursor != null)
            {
                Destroy(currentControlPointCursor.gameObject);
                currentControlPointCursor = null;
            }

            // Hide HeightControlPointCursor (for SmartWall/SmartHouse Y1)
            if (currentHeightCursor != null)
            {
                Destroy(currentHeightCursor.gameObject);
                currentHeightCursor = null;
            }

            // Hide HeightControlPointCursor2 (for SmartHouse Y2)
            if (currentHeightCursor2 != null)
            {
                Destroy(currentHeightCursor2.gameObject);
                currentHeightCursor2 = null;
            }

            // Hide SmartTerrain control point
            if (selectedObject != null)
            {
                SmartTerrain terrain = selectedObject.GetComponent<SmartTerrain>();
                if (terrain != null)
                {
                    terrain.SetControlPointVisible(false);
                }

                SmartHouse house = selectedObject.GetComponent<SmartHouse>();
                if (house != null)
                {
                    house.SetControlPointsVisible(false);
                }
            }
        }

        private void OnObjectDeleteRequested()
        {
            Debug.Log("BuildController: Delete requested");
            DeleteSelectedObject();
        }

        private void OnObjectConfirmRequested()
        {
            Debug.Log("BuildController: Confirm requested - exiting Edit mode");
            // Same behavior as clicking empty space: deselect and return to View mode
            DeselectObject();
            if (SmartTerrainManager.Instance != null)
                SmartTerrainManager.Instance.ClearActiveTerrain();
            if (SmartWallManager.Instance != null)
                SmartWallManager.Instance.ClearActiveWall();
            SwitchMode(BuildMode.View);
        }

        private bool IsCursorComponent(GameObject obj)
        {
            if (currentObjectCursor == null) return false;

            // Check if object is part of cursor hierarchy
            Transform current = obj.transform;
            while (current != null)
            {
                if (current.gameObject == currentObjectCursor.gameObject)
                    return true;
                current = current.parent;
            }

            return false;
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

            // Add placement effect
            ObjectPlacementEffect effect = obj.AddComponent<ObjectPlacementEffect>();

            // Apply settings if available
            if (effectSettings != null)
            {
                effectSettings.ApplyToEffect(effect);
            }

            effect.PlayPlacementEffect(position);

            placedObjects.Add(placedObj);

            // Auto-save after placing object
            if (!string.IsNullOrEmpty(currentWorldId))
            {
                SaveWorld();
            }

            // Auto-select the placed object and enter Edit mode
            SelectObject(placedObj);
            SwitchMode(BuildMode.Edit);
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

                // Auto-save after deletion
                if (!string.IsNullOrEmpty(currentWorldId))
                {
                    SaveWorld();
                }

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
            Debug.Log($"[BuildController] Go button pressed. Current world ID: {currentWorldId}");

            // Save world before entering play mode
            SaveWorld();

            Debug.Log($"[BuildController] World saved. Verifying save...");

            // Verify the world was saved
            SaveService saveService = appRoot != null ? appRoot.SaveService : FindFirstObjectByType<SaveService>();
            if (saveService != null)
            {
                var savedWorld = saveService.LoadCreatedWorld(currentWorldId);
                if (savedWorld != null)
                {
                    Debug.Log($"[BuildController] ✓ Verified world save: {savedWorld.worldTitle}, Props: {savedWorld.props?.Count ?? 0}");
                }
                else
                {
                    Debug.LogError($"[BuildController] ✗ Failed to verify world save for ID: {currentWorldId}");
                }
            }

            // Pass world ID to Play scene
            HorizonMini.Core.SceneTransitionData.SetWorldToPlay(currentWorldId);

            // Load Play scene
            Debug.Log($"[BuildController] Entering Play Mode for world: {currentWorldId}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
        }

        public void OnDeleteWorldButtonPressed()
        {
            if (string.IsNullOrEmpty(currentWorldId))
            {
                Debug.LogWarning("[BuildController] No world to delete");
                return;
            }

            Debug.Log($"[BuildController] Deleting world: {currentWorldId}");

            // Delete world from SaveService
            SaveService saveService = appRoot != null ? appRoot.SaveService : FindFirstObjectByType<SaveService>();
            if (saveService != null)
            {
                saveService.DeleteCreatedWorld(currentWorldId);
                Debug.Log($"[BuildController] ✓ World deleted: {currentWorldId}");
            }

            // Return to Main scene
            Debug.Log("[BuildController] Returning to Main scene");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }

        public void OnPublicButtonPressed()
        {
            Debug.Log("[BuildController] Publish button pressed - saving world and returning to Main");

            // Save world before publishing
            SaveWorld();

            // Mark world as published (no longer a draft)
            if (!string.IsNullOrEmpty(currentWorldId))
            {
                SaveService saveService = appRoot != null ? appRoot.SaveService : FindFirstObjectByType<SaveService>();
                if (saveService != null)
                {
                    HorizonMini.Data.WorldData worldData = saveService.LoadCreatedWorld(currentWorldId);
                    if (worldData != null)
                    {
                        worldData.isDraft = false;
                        saveService.SaveCreatedWorld(worldData);
                        Debug.Log($"<color=green>✓ World {currentWorldId} marked as PUBLISHED</color>");
                    }
                }
            }

            // Return to Main scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }

        /// <summary>
        /// Build NavMesh for the current scene
        /// </summary>
        private void BuildNavMesh()
        {
            // Find all NavMeshSurface components in the scene
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);

            if (surfaces.Length == 0)
            {
                Debug.LogWarning("No NavMeshSurface found in scene. NavMesh will not be built.");
                return;
            }

            // Build all NavMesh surfaces
            foreach (NavMeshSurface surface in surfaces)
            {
                if (surface != null)
                {
                    surface.BuildNavMesh();
                    Debug.Log($"Built NavMesh for surface: {surface.gameObject.name}");
                }
            }
        }

        /// <summary>
        /// Create initial terrain covering the entire volume XZ with height 0.5m
        /// </summary>
        private void CreateInitialTerrain()
        {
            if (currentVolumeGrid == null)
            {
                Debug.LogWarning("Cannot create initial terrain - no volume grid exists");
                return;
            }

            // Find SmartTerrain prefab in AssetCatalog
            PlaceableAsset terrainAsset = assetCatalog?.GetAssetsByCategory(AssetCategory.SmartTerrain)
                .Find(a => a.assetId.Contains("terrain") || a.displayName.Contains("Terrain"));

            if (terrainAsset == null || terrainAsset.prefab == null)
            {
                Debug.LogWarning("Cannot find SmartTerrain asset in catalog");
                return;
            }

            // Calculate volume dimensions in world space
            float volumeWidth = selectedVolumeSize.x * volumeUnitSize;
            float volumeDepth = selectedVolumeSize.z * volumeUnitSize;
            float initialHeight = 0.5f;

            // Position terrain at volume center (XZ plane), ground level (Y=0)
            Vector3 volumeCenter = currentVolumeGrid.GetCenter();
            Vector3 terrainPosition = new Vector3(volumeCenter.x, 0, volumeCenter.z);
            GameObject terrainObj = Instantiate(terrainAsset.prefab, terrainPosition, Quaternion.identity, buildContainer);
            terrainObj.name = "InitialTerrain";

            SmartTerrain terrain = terrainObj.GetComponent<SmartTerrain>();
            if (terrain == null)
            {
                Debug.LogError("Terrain prefab does not have SmartTerrain component!");
                Destroy(terrainObj);
                return;
            }

            // Set control point position to define terrain size
            // SmartTerrain: controlPoint at (X, Y, Z) creates terrain of size (X*2, Y, Z*2)
            // Terrain is centered at origin, so control point at half-size makes total size = volume size
            if (terrain.controlPoint != null)
            {
                Vector3 controlPointPos = new Vector3(volumeWidth * 0.5f, initialHeight, volumeDepth * 0.5f);
                terrain.SetControlPointPosition(controlPointPos, forceImmediate: true);
            }

            // Fix materials to URP
            FixMaterialsToURP(terrainObj);

            // Add PlacedObject component
            PlacedObject placedObj = terrainObj.GetComponent<PlacedObject>();
            if (placedObj == null)
            {
                placedObj = terrainObj.AddComponent<PlacedObject>();
            }
            placedObj.assetId = terrainAsset.assetId;
            placedObj.sourceAsset = terrainAsset;
            placedObj.UpdateSavedTransform();

            placedObjects.Add(placedObj);

            Debug.Log($"Created initial terrain covering {volumeWidth}m x {volumeDepth}m with height {initialHeight}m");
        }

        /// <summary>
        /// Create initial spawn point at volume center
        /// </summary>
        private void CreateInitialSpawnPoint()
        {
            // Check if initial spawn point already exists
            SpawnPoint[] existingSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var sp in existingSpawnPoints)
            {
                if (sp.IsInitialSpawn)
                {
                    Debug.Log("Initial spawn point already exists, skipping creation");
                    return;
                }
            }

            if (currentVolumeGrid == null)
            {
                Debug.LogWarning("Cannot create initial spawn point - no volume grid exists");
                return;
            }

            // Position at bottom-right corner of volume (assuming camera looks down from above)
            Vector3 volumeSize = currentVolumeGrid.GetWorldSize();
            Vector3 volumeCenter = currentVolumeGrid.GetCenter();

            // Bottom-right corner: positive X (right), negative Z (back/down when viewed from above)
            Vector3 spawnPos = volumeCenter;
            spawnPos.x = volumeCenter.x + volumeSize.x / 2f - 0.5f; // Right edge, slightly inset
            spawnPos.z = volumeCenter.z - volumeSize.z / 2f + 0.5f; // Back edge, slightly inset
            spawnPos.y = 0.5f; // Place on initial terrain surface

            GameObject spawnObj;

            if (spawnPointPrefab != null)
            {
                // Use assigned prefab
                spawnObj = Instantiate(spawnPointPrefab, spawnPos, Quaternion.identity, buildContainer);
                spawnObj.name = "InitialSpawnPoint";
                Debug.Log($"Created initial spawn point at bottom-right corner: {spawnPos}");
            }
            else
            {
                // Fallback: Create simple GameObject
                spawnObj = new GameObject("InitialSpawnPoint");
                spawnObj.transform.SetParent(buildContainer);
                spawnObj.transform.position = spawnPos;
                spawnObj.transform.rotation = Quaternion.identity;
                Debug.LogWarning("SpawnPoint prefab not assigned - using fallback");
            }

            // Ensure SpawnPoint component exists
            SpawnPoint spawnPoint = spawnObj.GetComponent<SpawnPoint>();
            if (spawnPoint == null)
            {
                spawnPoint = spawnObj.AddComponent<SpawnPoint>();
            }

            spawnPoint.SetInitialSpawn(true); // Mark as initial spawn (cannot be deleted)
            spawnPoint.SetSpawnType(SpawnType.Player);
            spawnPoint.UpdateSavedTransform();

            // Add PlacedObject component so it gets saved
            PlacedObject placedObj = spawnObj.GetComponent<PlacedObject>();
            if (placedObj == null)
            {
                placedObj = spawnObj.AddComponent<PlacedObject>();
            }

            // Try to find SpawnPoint asset in catalog by ID
            if (assetCatalog != null)
            {
                PlaceableAsset spawnAsset = assetCatalog.GetAssetById("spawn_point_player");

                if (spawnAsset != null)
                {
                    placedObj.assetId = spawnAsset.assetId;
                    placedObj.sourceAsset = spawnAsset;
                }
                else
                {
                    // Fallback: use a generic ID (WorldLibrary will create procedurally)
                    placedObj.assetId = "spawn_point_player";
                    Debug.LogWarning("Could not find SpawnPoint asset in catalog, using generic ID (will be created procedurally on load)");
                }
            }
            else
            {
                placedObj.assetId = "spawn_point_player";
            }

            placedObj.UpdateSavedTransform();
            placedObjects.Add(placedObj);

            // SpawnPoint now uses ObjectCursor (shown when selected in Edit mode)
            // No need for SpawnPointCursor anymore

            Debug.Log($"Created initial spawn point at {spawnPos}");
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
                worldData.isDraft = true; // Mark as draft until published
                Debug.Log($"Created new world {worldData.worldId} marked as DRAFT");
            }

            // Save volume grid
            worldData.volumes.Clear();
            if (currentVolumeGrid != null)
            {
                // For now, save a single volume at origin covering the entire grid
                HorizonMini.Data.VolumeCell volumeCell = new HorizonMini.Data.VolumeCell(
                    Vector3Int.zero,
                    "default",
                    0
                );
                worldData.volumes.Add(volumeCell);
                Debug.Log($"Saved volume grid: {selectedVolumeSize}");
            }

            // Clear existing props and save current placed objects
            worldData.props.Clear();
            Debug.Log($"[BuildController] Saving {placedObjects.Count} placed objects...");
            foreach (var obj in placedObjects)
            {
                Debug.Log($"[BuildController]   - Saving: {obj.name} (assetId: {obj.assetId})");
                HorizonMini.Data.PropData propData = new HorizonMini.Data.PropData
                {
                    prefabName = obj.assetId,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    scale = obj.transform.localScale
                };

                // Check if it's a SmartTerrain - save control point position
                SmartTerrain terrain = obj.GetComponent<SmartTerrain>();
                if (terrain != null)
                {
                    Vector3 cpPos = terrain.GetControlPointPosition();
                    propData.smartTerrainControlPoint = cpPos;
                    Debug.Log($"  - Saved SmartTerrain {obj.name}:");
                    Debug.Log($"    Position: {obj.transform.position}");
                    Debug.Log($"    Control Point Local Position: {cpPos}");
                    Debug.Log($"    Control Point World Position: {terrain.controlPoint?.position}");
                    Debug.Log($"    Control Point Exists: {terrain.controlPoint != null}");
                    Debug.Log($"    Terrain Size: {terrain.GetSize()}");
                }

                // Check if it's a SmartWall - save control points and unified height
                SmartWall wall = obj.GetComponent<SmartWall>();
                if (wall != null)
                {
                    propData.smartWallControlPoints = wall.GetAllControlPointPositions();
                    propData.smartWallHeight = wall.GetWallHeight();
                    Debug.Log($"  - Saved SmartWall {obj.name}:");
                    Debug.Log($"    Position: {obj.transform.position}");
                    Debug.Log($"    Control Points: {propData.smartWallControlPoints.Count}");
                    Debug.Log($"    Unified Wall Height: {propData.smartWallHeight}");
                }

                if (terrain == null && wall == null)
                {
                    Debug.Log($"  - Saved {obj.name} at {obj.transform.position}");
                }

                worldData.props.Add(propData);
            }

            // Save via AppRoot or SaveService
            if (appRoot != null)
            {
                appRoot.SaveService.SaveCreatedWorld(worldData);
                Debug.Log($"<color=green>✓ World saved: {worldData.worldTitle}</color>");
                Debug.Log($"<color=green>  World ID: {worldData.worldId}</color>");
                Debug.Log($"<color=green>  Grid Size: {worldData.gridDimensions}</color>");
                Debug.Log($"<color=green>  Props Count: {worldData.props.Count}</color>");
            }
            else
            {
                // Standalone mode - find SaveService manually
                SaveService saveService = FindFirstObjectByType<SaveService>();
                if (saveService != null)
                {
                    saveService.SaveCreatedWorld(worldData);
                    Debug.Log($"<color=green>✓ World saved (standalone): {worldData.worldTitle}</color>");
                    Debug.Log($"<color=green>  World ID: {worldData.worldId}</color>");
                    Debug.Log($"<color=green>  Grid Size: {worldData.gridDimensions}</color>");
                    Debug.Log($"<color=green>  Props Count: {worldData.props.Count}</color>");
                }
                else
                {
                    Debug.LogError("SaveService not found! Cannot save world.");
                }
            }
        }

        /// <summary>
        /// Clean up all draft worlds on startup
        /// </summary>
        private void CleanupDraftWorlds()
        {
            SaveService saveService = appRoot != null ? appRoot.SaveService : FindFirstObjectByType<SaveService>();
            if (saveService == null)
            {
                Debug.LogWarning("[BuildController] SaveService not found, cannot cleanup draft worlds");
                return;
            }

            // Get all created world IDs
            var allWorldIds = saveService.GetCreatedWorldIds();
            if (allWorldIds == null || allWorldIds.Count == 0)
            {
                Debug.Log("[BuildController] No worlds found, nothing to cleanup");
                return;
            }

            int draftCount = 0;
            List<string> draftsToDelete = new List<string>();

            // Check each world
            foreach (var worldId in allWorldIds)
            {
                HorizonMini.Data.WorldData worldData = saveService.LoadCreatedWorld(worldId);
                if (worldData != null && worldData.isDraft)
                {
                    Debug.Log($"[BuildController] Found draft world: {worldData.worldTitle} (ID: {worldId})");
                    draftsToDelete.Add(worldId);
                    draftCount++;
                }
            }

            // Delete all drafts
            foreach (var worldId in draftsToDelete)
            {
                saveService.DeleteCreatedWorld(worldId);
            }

            if (draftCount > 0)
            {
                Debug.Log($"<color=yellow>✓ Cleaned up {draftCount} draft world(s)</color>");
            }
            else
            {
                Debug.Log("[BuildController] No draft worlds to cleanup");
            }
        }
    }
}
