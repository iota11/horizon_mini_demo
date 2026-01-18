using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using HorizonMini.Build;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Automatically generate icons for prefabs and create PlaceableAssets with icons assigned
    /// </summary>
    public class GenerateAssetIconsAndAssets : EditorWindow
    {
        [MenuItem("Tools/Generate Asset Icons & Assets")]
        public static void ShowWindow()
        {
            GetWindow<GenerateAssetIconsAndAssets>("Generate Icons & Assets");
        }

        private string prefabFolderPath = "Assets/Prefabs/CubeWorld";
        private string iconOutputPath = "Assets/Textures/Icons/CubeWorld";
        private string assetOutputPath = "Assets/ScriptableObjects/PlaceableAssets/CubeWorld";

        private int iconSize = 256;
        private AssetCategory defaultCategory = AssetCategory.Structures;
        private bool updateExistingAssets = true; // Update existing assets instead of creating new ones
        private Vector2 scrollPos;
        private int totalPrefabs = 0;
        private int processedPrefabs = 0;
        private int updatedAssets = 0;
        private int createdAssets = 0;

        private void OnGUI()
        {
            GUILayout.Label("Generate Asset Icons & Assets", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will:\n" +
                "1. Scan prefab folder recursively\n" +
                "2. Render icon for each prefab\n" +
                "3. Save icons with same folder structure\n" +
                "4. Create PlaceableAsset with icon auto-assigned",
                MessageType.Info
            );

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            {
                GUILayout.Label("Input Settings", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                prefabFolderPath = EditorGUILayout.TextField("Prefab Folder:", prefabFolderPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        prefabFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.Label("Output Settings", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                iconOutputPath = EditorGUILayout.TextField("Icon Output Path:", iconOutputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Icon Output Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        iconOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                assetOutputPath = EditorGUILayout.TextField("Asset Output Path:", assetOutputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Asset Output Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        assetOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.Label("Generation Settings", EditorStyles.boldLabel);
                iconSize = EditorGUILayout.IntSlider("Icon Size:", iconSize, 64, 512);
                defaultCategory = (AssetCategory)EditorGUILayout.EnumPopup("Default Category:", defaultCategory);
                updateExistingAssets = EditorGUILayout.Toggle("Update Existing Assets:", updateExistingAssets);

                EditorGUILayout.Space();

                if (updateExistingAssets)
                {
                    EditorGUILayout.HelpBox(
                        "✓ Will update existing PlaceableAssets with new icons\n" +
                        "✓ Preserves existing category and other settings\n" +
                        "✓ Creates new assets if they don't exist",
                        MessageType.Info
                    );
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "⚠ Will skip existing PlaceableAssets\n" +
                        "Only creates new assets for prefabs without PlaceableAssets",
                        MessageType.Warning
                    );
                }

                EditorGUILayout.Space();

                if (totalPrefabs > 0)
                {
                    EditorGUILayout.LabelField($"Progress: {processedPrefabs}/{totalPrefabs}");
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), (float)processedPrefabs / totalPrefabs, $"{processedPrefabs}/{totalPrefabs}");
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Icons & Assets", GUILayout.Height(40)))
            {
                GenerateAll();
            }
        }

        private void GenerateAll()
        {
            if (!Directory.Exists(prefabFolderPath))
            {
                EditorUtility.DisplayDialog("Error", $"Prefab folder does not exist: {prefabFolderPath}", "OK");
                return;
            }

            // Find all prefabs recursively
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
            totalPrefabs = prefabGuids.Length;
            processedPrefabs = 0;
            updatedAssets = 0;
            createdAssets = 0;

            Debug.Log($"Found {totalPrefabs} prefabs in {prefabFolderPath}");

            // Create output directories
            Directory.CreateDirectory(iconOutputPath);
            Directory.CreateDirectory(assetOutputPath);

            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                ProcessPrefab(prefabPath);
                processedPrefabs++;

                // Update progress
                EditorUtility.DisplayProgressBar("Generating Icons & Assets",
                    $"Processing {Path.GetFileName(prefabPath)}",
                    (float)processedPrefabs / totalPrefabs);

                Repaint();
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Processed {totalPrefabs} prefabs!\n\n" +
                $"Created: {createdAssets} new assets\n" +
                $"Updated: {updatedAssets} existing assets\n\n" +
                $"Icons: {iconOutputPath}\n" +
                $"Assets: {assetOutputPath}",
                "OK");
        }

        private void ProcessPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Failed to load prefab: {prefabPath}");
                return;
            }

            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            // Get relative path from prefab folder
            string relativePath = GetRelativePath(prefabFolderPath, Path.GetDirectoryName(prefabPath));

            // Create corresponding directories
            string iconDir = Path.Combine(iconOutputPath, relativePath);
            string assetDir = Path.Combine(assetOutputPath, relativePath);
            Directory.CreateDirectory(iconDir);
            Directory.CreateDirectory(assetDir);

            // Generate icon
            string iconPath = Path.Combine(iconDir, prefabName + ".png");
            Texture2D icon = RenderPrefabIcon(prefab, iconSize);
            if (icon != null)
            {
                SaveTexture(icon, iconPath);
                Object.DestroyImmediate(icon);

                // Reimport as sprite
                AssetDatabase.ImportAsset(iconPath);
                TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            // Load icon sprite
            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            // Check if PlaceableAsset already exists
            string assetPath = Path.Combine(assetDir, prefabName + ".asset");
            PlaceableAsset placeableAsset = AssetDatabase.LoadAssetAtPath<PlaceableAsset>(assetPath);

            if (placeableAsset != null)
            {
                // Asset exists
                if (updateExistingAssets)
                {
                    // Update existing asset with new icon
                    placeableAsset.icon = iconSprite;
                    EditorUtility.SetDirty(placeableAsset);
                    updatedAssets++;
                    Debug.Log($"✓ Updated: {assetPath} with new icon: {iconPath}");
                }
                else
                {
                    Debug.Log($"⊘ Skipped: {assetPath} (already exists)");
                }
            }
            else
            {
                // Create new PlaceableAsset
                placeableAsset = ScriptableObject.CreateInstance<PlaceableAsset>();
                placeableAsset.assetId = System.Guid.NewGuid().ToString();
                placeableAsset.displayName = prefabName;
                placeableAsset.category = defaultCategory;
                placeableAsset.prefab = prefab;
                placeableAsset.icon = iconSprite;

                AssetDatabase.CreateAsset(placeableAsset, assetPath);
                createdAssets++;
                Debug.Log($"✓ Created: {assetPath} with icon: {iconPath}");
            }
        }

        private string GetRelativePath(string fromPath, string toPath)
        {
            if (toPath.StartsWith(fromPath))
            {
                string relative = toPath.Substring(fromPath.Length);
                return relative.TrimStart('/', '\\');
            }
            return "";
        }

        private Texture2D RenderPrefabIcon(GameObject prefab, int size)
        {
            // Create temporary scene camera
            GameObject cameraObj = new GameObject("IconCamera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f); // Transparent background
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            // Instantiate prefab
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Object.DestroyImmediate(cameraObj);
                return null;
            }

            // Calculate bounds
            Bounds bounds = CalculateBounds(instance);

            // Position camera to fit object
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            camera.orthographicSize = maxSize * 0.6f;

            // Camera looks at object from 45 degree angle
            Vector3 cameraDirection = new Vector3(1, 1, 1).normalized;
            camera.transform.position = bounds.center - cameraDirection * maxSize * 2f;
            camera.transform.LookAt(bounds.center);

            // Render to texture
            RenderTexture renderTexture = new RenderTexture(size, size, 24);
            camera.targetTexture = renderTexture;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            camera.Render();

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            texture.Apply();

            // Cleanup
            RenderTexture.active = null;
            camera.targetTexture = null;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(cameraObj);

            return texture;
        }

        private Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private void SaveTexture(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }
    }
}
