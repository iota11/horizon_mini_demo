using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using HorizonMini.Data;
using HorizonMini.Build;
using System.Collections.Generic;
using System.Linq;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor tool to manually create a World from current scene
    /// Scans scene for PlacedObject components and creates a permanent WorldData asset
    /// </summary>
    public class ManualWorldCreator : EditorWindow
    {
        [MenuItem("Tools/HorizonMini/Manual World Creator")]
        static void OpenWindow()
        {
            ManualWorldCreator window = GetWindow<ManualWorldCreator>("Manual World Creator");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private string worldTitle = "My Custom World";
        private string worldAuthor = "Designer";
        private Vector3Int gridDimensions = new Vector3Int(4, 1, 4);
        private bool markAsPermanent = true;
        private string savePath = "Assets/Data/ManualWorlds";

        private Color skyColor = new Color(0.5f, 0.7f, 1f);
        private float gravity = -9.81f;

        private Vector2 scrollPosition;
        private List<PlacedObject> foundObjects = new List<PlacedObject>();
        private List<GameObject> objectsWithoutPlacedObject = new List<GameObject>();

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Manual World Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool creates a permanent WorldData asset from the current scene.\n\n" +
                "Workflow:\n" +
                "1. Place objects in your scene\n" +
                "2. Add PlacedObject component to each object\n" +
                "3. Click 'Scan Current Scene'\n" +
                "4. Configure world settings\n" +
                "5. Click 'Create Permanent World'\n\n" +
                "Permanent worlds cannot be deleted by the in-game delete system.",
                MessageType.Info);

            EditorGUILayout.Space();

            // World Settings
            GUILayout.Label("World Settings", EditorStyles.boldLabel);
            worldTitle = EditorGUILayout.TextField("World Title", worldTitle);
            worldAuthor = EditorGUILayout.TextField("World Author", worldAuthor);

            EditorGUILayout.Space();

            gridDimensions = EditorGUILayout.Vector3IntField("Grid Dimensions", gridDimensions);
            skyColor = EditorGUILayout.ColorField("Sky Color", skyColor);
            gravity = EditorGUILayout.FloatField("Gravity", gravity);

            EditorGUILayout.Space();

            // Permanent flag
            markAsPermanent = EditorGUILayout.Toggle("Mark as Permanent", markAsPermanent);
            if (markAsPermanent)
            {
                EditorGUILayout.HelpBox("✓ This world will be protected from deletion", MessageType.None);
            }

            EditorGUILayout.Space();

            // Save path
            GUILayout.Label("Save Location", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField("Save Path", savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert absolute path to relative
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Scan button
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Scan Current Scene", GUILayout.Height(35)))
            {
                ScanCurrentScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            // Show scan results
            if (foundObjects.Count > 0 || objectsWithoutPlacedObject.Count > 0)
            {
                GUILayout.Label("Scan Results", EditorStyles.boldLabel);

                if (foundObjects.Count > 0)
                {
                    EditorGUILayout.HelpBox($"✓ Found {foundObjects.Count} objects with PlacedObject component", MessageType.Info);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Objects to Include:", EditorStyles.miniBoldLabel);
                    foreach (var obj in foundObjects)
                    {
                        EditorGUILayout.LabelField($"• {obj.gameObject.name}", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }

                if (objectsWithoutPlacedObject.Count > 0)
                {
                    EditorGUILayout.HelpBox($"⚠ Found {objectsWithoutPlacedObject.Count} objects without PlacedObject component", MessageType.Warning);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Objects to Fix:", EditorStyles.miniBoldLabel);
                    foreach (var obj in objectsWithoutPlacedObject)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {obj.name}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Add Component", GUILayout.Width(120)))
                        {
                            AddPlacedObjectComponent(obj);
                            ScanCurrentScene(); // Re-scan
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Add PlacedObject to All"))
                    {
                        foreach (var obj in objectsWithoutPlacedObject.ToList())
                        {
                            AddPlacedObjectComponent(obj);
                        }
                        ScanCurrentScene(); // Re-scan
                    }
                }

                EditorGUILayout.Space();

                // Create button
                GUI.enabled = foundObjects.Count > 0;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Create Permanent World", GUILayout.Height(40)))
                {
                    CreateWorld();
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }

            EditorGUILayout.EndScrollView();
        }

        void ScanCurrentScene()
        {
            foundObjects.Clear();
            objectsWithoutPlacedObject.Clear();

            // Find all PlacedObject components in scene
            PlacedObject[] allPlacedObjects = FindObjectsByType<PlacedObject>(FindObjectsSortMode.None);
            foundObjects.AddRange(allPlacedObjects);

            // Find objects that might need PlacedObject component
            // Look for objects with MeshRenderer or other indicators
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rootObj in allObjects)
            {
                ScanGameObjectRecursive(rootObj);
            }

            Debug.Log($"[ManualWorldCreator] Scan complete: {foundObjects.Count} objects with PlacedObject, {objectsWithoutPlacedObject.Count} objects without");
        }

        void ScanGameObjectRecursive(GameObject obj)
        {
            // Skip objects that already have PlacedObject
            if (obj.GetComponent<PlacedObject>() != null)
                return;

            // Skip certain system objects
            if (obj.name.Contains("Camera") ||
                obj.name.Contains("Light") ||
                obj.name.Contains("EventSystem") ||
                obj.name.Contains("Canvas"))
                return;

            // Check if this looks like a prop/object
            if (obj.GetComponent<MeshRenderer>() != null ||
                obj.GetComponent<MeshFilter>() != null ||
                obj.GetComponentInChildren<MeshRenderer>() != null)
            {
                objectsWithoutPlacedObject.Add(obj);
            }

            // Recurse to children
            foreach (Transform child in obj.transform)
            {
                ScanGameObjectRecursive(child.gameObject);
            }
        }

        void AddPlacedObjectComponent(GameObject obj)
        {
            if (obj.GetComponent<PlacedObject>() == null)
            {
                PlacedObject placedObj = obj.AddComponent<PlacedObject>();

                // Try to find prefab name
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefab != null)
                {
                    // Set prefab reference via reflection or serialization
                    Debug.Log($"Added PlacedObject to {obj.name} (prefab: {prefab.name})");
                }
                else
                {
                    Debug.Log($"Added PlacedObject to {obj.name} (no prefab source)");
                }

                EditorUtility.SetDirty(obj);
            }
        }

        void CreateWorld()
        {
            // Create directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                string[] folders = savePath.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            // Create WorldData asset
            WorldData worldData = ScriptableObject.CreateInstance<WorldData>();
            worldData.worldId = System.Guid.NewGuid().ToString();
            worldData.worldTitle = worldTitle;
            worldData.worldAuthor = worldAuthor;
            worldData.isDraft = false; // Published
            worldData.gridDimensions = gridDimensions;
            worldData.skyColor = skyColor;
            worldData.gravity = gravity;

            // Add props from scanned objects
            foreach (var placedObj in foundObjects)
            {
                PropData propData = new PropData();
                propData.position = placedObj.transform.position;
                propData.rotation = placedObj.transform.rotation;
                propData.scale = placedObj.transform.localScale;

                // Try to get prefab reference
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(placedObj.gameObject);
                if (prefab != null)
                {
                    // Get asset path for the prefab
                    string prefabPath = AssetDatabase.GetAssetPath(prefab);

                    // Store both name and path
                    propData.prefabName = prefab.name;

                    // Use a custom field to store full path for non-catalog prefabs
                    // We'll store it in the prefabName with a special prefix
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        propData.prefabName = $"PREFAB_PATH:{prefabPath}";
                        Debug.Log($"Saving prefab with path: {prefabPath}");
                    }
                }
                else
                {
                    propData.prefabName = placedObj.gameObject.name;
                    Debug.LogWarning($"Object '{placedObj.gameObject.name}' has no prefab source - may not load correctly");
                }

                // Save SmartTerrain data if present
                var terrain = placedObj.GetComponent<HorizonMini.Build.SmartTerrain>();
                if (terrain != null && terrain.controlPoint != null)
                {
                    propData.smartTerrainControlPoint = terrain.controlPoint.localPosition;
                }

                // Save SmartWall data if present
                var wall = placedObj.GetComponent<HorizonMini.Build.SmartWall>();
                if (wall != null)
                {
                    propData.smartWallControlPoints = wall.GetAllControlPointPositions();
                    propData.smartWallHeight = wall.GetWallHeight();
                }

                worldData.props.Add(propData);
            }

            // Create volumes (simple grid)
            for (int x = 0; x < gridDimensions.x; x++)
            {
                for (int y = 0; y < gridDimensions.y; y++)
                {
                    for (int z = 0; z < gridDimensions.z; z++)
                    {
                        VolumeCell cell = new VolumeCell(x, y, z, "default", 0);
                        worldData.volumes.Add(cell);
                    }
                }
            }

            // Save asset
            string fileName = $"World_{worldTitle.Replace(" ", "_")}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
            string assetPath = $"{savePath}/{fileName}";
            AssetDatabase.CreateAsset(worldData, assetPath);

            // Mark as permanent by adding to a special list or category
            if (markAsPermanent)
            {
                // Create or update permanent worlds registry
                CreateOrUpdatePermanentRegistry(worldData.worldId, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "World Created!",
                $"World '{worldTitle}' has been created successfully!\n\n" +
                $"Asset: {assetPath}\n" +
                $"Objects: {foundObjects.Count}\n" +
                $"Permanent: {(markAsPermanent ? "Yes" : "No")}\n\n" +
                $"World ID: {worldData.worldId}",
                "OK");

            Selection.activeObject = worldData;
            EditorGUIUtility.PingObject(worldData);
        }

        void CreateOrUpdatePermanentRegistry(string worldId, string assetPath)
        {
            string registryPath = "Assets/Data/PermanentWorldsRegistry.asset";

            PermanentWorldsRegistry registry = AssetDatabase.LoadAssetAtPath<PermanentWorldsRegistry>(registryPath);

            if (registry == null)
            {
                // Create new registry
                registry = ScriptableObject.CreateInstance<PermanentWorldsRegistry>();

                // Ensure Data folder exists
                if (!AssetDatabase.IsValidFolder("Assets/Data"))
                {
                    AssetDatabase.CreateFolder("Assets", "Data");
                }

                AssetDatabase.CreateAsset(registry, registryPath);
            }

            // Add to registry
            if (!registry.permanentWorldIds.Contains(worldId))
            {
                registry.permanentWorldIds.Add(worldId);
                registry.permanentWorldPaths.Add(assetPath);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();

                Debug.Log($"✓ Added world {worldId} to permanent registry");
            }
        }
    }
}
