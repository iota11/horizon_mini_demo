using UnityEngine;
using UnityEditor;
using HorizonMini.Build;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to create SmartTerrain prefab and PlaceableAsset
    /// </summary>
    public class CreateSmartTerrainAsset
    {
        [MenuItem("HorizonMini/Create SmartTerrain Asset")]
        public static void CreateAsset()
        {
            // Create directories if needed
            string prefabPath = "Assets/Prefabs/SmartTerrain";
            string assetPath = "Assets/ScriptableObjects/PlaceableAssets/SmartTerrain";
            string iconPath = "Assets/Textures/Icons/SmartTerrain";

            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "SmartTerrain");
            }
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/PlaceableAssets", "SmartTerrain");
            }
            if (!AssetDatabase.IsValidFolder(iconPath))
            {
                AssetDatabase.CreateFolder("Assets/Textures/Icons", "SmartTerrain");
            }

            // Create SmartTerrain GameObject
            GameObject terrainObj = new GameObject("SmartTerrain");

            // Add components
            MeshFilter meshFilter = terrainObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = terrainObj.AddComponent<MeshRenderer>();
            BoxCollider boxCollider = terrainObj.AddComponent<BoxCollider>();
            SmartTerrain smartTerrain = terrainObj.AddComponent<SmartTerrain>();

            // Create material
            Material terrainMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            terrainMat.color = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown terrain color
            AssetDatabase.CreateAsset(terrainMat, $"{prefabPath}/SmartTerrainMaterial.mat");

            // Assign material
            meshRenderer.sharedMaterial = terrainMat;

            // Set references in SmartTerrain component via reflection/serialization
            SerializedObject so = new SerializedObject(smartTerrain);
            so.FindProperty("meshFilter").objectReferenceValue = meshFilter;
            so.FindProperty("meshRenderer").objectReferenceValue = meshRenderer;
            so.FindProperty("boxCollider").objectReferenceValue = boxCollider;
            so.FindProperty("terrainMaterial").objectReferenceValue = terrainMat;
            so.ApplyModifiedProperties();

            // Create control point (SmartTerrain.Awake() will create it, but we'll set it up here)
            GameObject controlPointObj = new GameObject("ControlPoint");
            controlPointObj.transform.SetParent(terrainObj.transform);
            controlPointObj.transform.localPosition = new Vector3(2f, 1f, 2f); // Smaller default size

            // Visual indicator for control point
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(controlPointObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.6f; // Double size (was 0.3f)

            Material sphereMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sphereMat.color = new Color(1f, 0.5f, 0f, 0.7f);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = sphereMat;
            AssetDatabase.CreateAsset(sphereMat, $"{prefabPath}/ControlPointMaterial.mat");

            if (sphere.TryGetComponent<Collider>(out var sphereCollider))
            {
                Object.DestroyImmediate(sphereCollider);
            }

            // Set control point reference
            so = new SerializedObject(smartTerrain);
            so.FindProperty("controlPoint").objectReferenceValue = controlPointObj.transform;
            so.ApplyModifiedProperties();

            // Create Cursor GameObject
            GameObject cursorObj = new GameObject("SmartTerrainCursor");
            cursorObj.transform.SetParent(terrainObj.transform);
            cursorObj.transform.localPosition = Vector3.zero;

            SmartTerrainCursor cursor = cursorObj.AddComponent<SmartTerrainCursor>();

            // Set cursor references
            so = new SerializedObject(cursor);
            so.FindProperty("smartTerrain").objectReferenceValue = smartTerrain;
            so.FindProperty("xzHandlePrefab").objectReferenceValue = null; // User will assign prefab
            so.FindProperty("yHandlePrefab").objectReferenceValue = null;  // User will assign prefab
            so.FindProperty("uiLayer").intValue = LayerMask.GetMask("UI");
            so.ApplyModifiedProperties();

            // Save as prefab
            string prefabFullPath = $"{prefabPath}/SmartTerrain.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(terrainObj, prefabFullPath);

            // Clean up scene instance
            Object.DestroyImmediate(terrainObj);

            Debug.Log($"Created SmartTerrain prefab at: {prefabFullPath}");

            // Create icon (simple texture)
            Texture2D icon = CreateIcon();
            string iconFullPath = $"{iconPath}/SmartTerrain.png";
            System.IO.File.WriteAllBytes(iconFullPath, icon.EncodeToPNG());
            AssetDatabase.ImportAsset(iconFullPath);

            TextureImporter importer = AssetImporter.GetAtPath(iconFullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconFullPath);
            Debug.Log($"Created icon at: {iconFullPath}");

            // Create PlaceableAsset
            PlaceableAsset asset = ScriptableObject.CreateInstance<PlaceableAsset>();
            asset.assetId = "smart_terrain_default";
            asset.displayName = "Smart Terrain";
            asset.category = AssetCategory.SmartTerrain;
            asset.prefab = prefab;
            asset.icon = iconSprite;
            asset.defaultScale = Vector3.one;
            asset.pivotOffset = Vector3.zero;

            string assetFullPath = $"{assetPath}/SmartTerrain.asset";
            AssetDatabase.CreateAsset(asset, assetFullPath);
            Debug.Log($"Created PlaceableAsset at: {assetFullPath}");

            // Add to AssetCatalog
            AddToAssetCatalog(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>SmartTerrain asset creation complete!</color>");
            EditorUtility.DisplayDialog("Success", "SmartTerrain prefab and asset created successfully!\n\nPrefab: " + prefabFullPath + "\nAsset: " + assetFullPath, "OK");
        }

        private static Texture2D CreateIcon()
        {
            // Create simple 256x256 icon with terrain-like appearance
            int size = 256;
            Texture2D tex = new Texture2D(size, size);

            Color terrainColor = new Color(0.6f, 0.4f, 0.2f);
            Color highlightColor = new Color(0.8f, 0.6f, 0.3f);
            Color shadowColor = new Color(0.4f, 0.3f, 0.15f);
            Color controlPointColor = new Color(1f, 0.5f, 0f);

            // Fill background
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            // Draw cube representation
            int centerX = size / 2;
            int centerY = size / 2;
            int cubeSize = 100;

            // Top face (lighter)
            for (int y = centerY; y < centerY + cubeSize / 2; y++)
            {
                for (int x = centerX - (y - centerY); x < centerX + (y - centerY); x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        tex.SetPixel(x, y, highlightColor);
                }
            }

            // Front face (base color)
            for (int y = centerY - cubeSize / 2; y < centerY; y++)
            {
                int width = cubeSize / 2 + (centerY - y) / 2;
                for (int x = centerX - width; x < centerX; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        tex.SetPixel(x, y, terrainColor);
                }
            }

            // Side face (darker)
            for (int y = centerY - cubeSize / 2; y < centerY; y++)
            {
                int width = cubeSize / 2 + (centerY - y) / 2;
                for (int x = centerX; x < centerX + width; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        tex.SetPixel(x, y, shadowColor);
                }
            }

            // Draw control point indicator
            int cpX = centerX + cubeSize / 3;
            int cpY = centerY + cubeSize / 3;
            int cpRadius = 15;
            for (int y = cpY - cpRadius; y <= cpY + cpRadius; y++)
            {
                for (int x = cpX - cpRadius; x <= cpX + cpRadius; x++)
                {
                    if ((x - cpX) * (x - cpX) + (y - cpY) * (y - cpY) <= cpRadius * cpRadius)
                    {
                        if (x >= 0 && x < size && y >= 0 && y < size)
                            tex.SetPixel(x, y, controlPointColor);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static void AddToAssetCatalog(PlaceableAsset asset)
        {
            // Find AssetCatalog
            string[] catalogGuids = AssetDatabase.FindAssets("t:AssetCatalog");
            if (catalogGuids.Length == 0)
            {
                Debug.LogWarning("AssetCatalog not found. Please add the asset manually.");
                return;
            }

            string catalogPath = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
            AssetCatalog catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);

            if (catalog != null)
            {
                SerializedObject catalogSO = new SerializedObject(catalog);
                SerializedProperty allAssetsProp = catalogSO.FindProperty("allAssets");

                // Check if already exists
                for (int i = 0; i < allAssetsProp.arraySize; i++)
                {
                    if (allAssetsProp.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                    {
                        Debug.Log("Asset already in catalog.");
                        return;
                    }
                }

                // Add to catalog
                allAssetsProp.InsertArrayElementAtIndex(allAssetsProp.arraySize);
                allAssetsProp.GetArrayElementAtIndex(allAssetsProp.arraySize - 1).objectReferenceValue = asset;
                catalogSO.ApplyModifiedProperties();

                Debug.Log($"<color=green>Added SmartTerrain to AssetCatalog</color>");
            }
            else
            {
                Debug.LogWarning("Could not load AssetCatalog.");
            }
        }
    }
}
