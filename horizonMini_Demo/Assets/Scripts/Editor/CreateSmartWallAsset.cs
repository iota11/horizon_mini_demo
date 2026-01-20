using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using ProjectDawn.CozyBuilder;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to create SmartWall prefab and PlaceableAsset based on WallTest
    /// </summary>
    public class CreateSmartWallAsset
    {
        [MenuItem("HorizonMini/Create SmartWall Asset")]
        public static void CreateAsset()
        {
            // Load WallTest prefab as template
            GameObject wallTestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/WallTest.prefab");
            if (wallTestPrefab == null)
            {
                Debug.LogError("WallTest.prefab not found at Assets/Prefab/WallTest.prefab");
                return;
            }

            // Create directories if needed
            string prefabPath = "Assets/Prefabs/SmartWall";
            string assetPath = "Assets/ScriptableObjects/PlaceableAssets/SmartWall";
            string iconPath = "Assets/Textures/Icons/SmartWall";

            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "SmartWall");
            }
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/PlaceableAssets", "SmartWall");
            }
            if (!AssetDatabase.IsValidFolder(iconPath))
            {
                AssetDatabase.CreateFolder("Assets/Textures/Icons", "SmartWall");
            }

            // Instantiate WallTest as a base
            GameObject smartWallObj = PrefabUtility.InstantiatePrefab(wallTestPrefab) as GameObject;
            smartWallObj.name = "SmartWall";

            // Add SmartWall component
            SmartWall smartWall = smartWallObj.AddComponent<SmartWall>();

            // Find transforms
            Transform splineTransform = smartWallObj.transform.Find("Spline");
            Transform topSplineTransform = smartWallObj.transform.Find("Spline (1)");
            Transform planeTransform = smartWallObj.transform.Find("Plane");

            // Get components
            CozySpline bottomSpline = splineTransform?.GetComponent<CozySpline>();
            CozySpline topSpline = topSplineTransform?.GetComponent<CozySpline>();
            CozyPlane plane = planeTransform?.GetComponent<CozyPlane>();
            CozyRenderer cozyRenderer = smartWallObj.GetComponent<CozyRenderer>();

            // Set references using SerializedObject
            SerializedObject so = new SerializedObject(smartWall);
            so.FindProperty("bottomSpline").objectReferenceValue = bottomSpline;
            so.FindProperty("topSpline").objectReferenceValue = topSpline;
            so.FindProperty("plane").objectReferenceValue = plane;
            so.FindProperty("cozyRenderer").objectReferenceValue = cozyRenderer;
            so.ApplyModifiedProperties();

            // Create cursors for each control point
            if (bottomSpline != null)
            {
                int pointCount = bottomSpline.Points.Count;
                for (int i = 0; i < pointCount; i++)
                {
                    CreateCursorForControlPoint(smartWallObj, smartWall, i);
                }
            }

            // Save as prefab
            string prefabFullPath = $"{prefabPath}/SmartWall.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(smartWallObj, prefabFullPath);

            // Clean up scene instance
            Object.DestroyImmediate(smartWallObj);

            Debug.Log($"Created SmartWall prefab at: {prefabFullPath}");

            // Create icon (reuse wall-like appearance)
            Texture2D icon = CreateIcon();
            string iconFullPath = $"{iconPath}/SmartWall.png";
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
            asset.assetId = "smart_wall_default";
            asset.displayName = "Smart Wall";
            asset.category = AssetCategory.SmartWall;
            asset.prefab = prefab;
            asset.icon = iconSprite;
            asset.defaultScale = Vector3.one;
            asset.pivotOffset = Vector3.zero;

            string assetFullPath = $"{assetPath}/SmartWall.asset";
            AssetDatabase.CreateAsset(asset, assetFullPath);
            Debug.Log($"Created PlaceableAsset at: {assetFullPath}");

            // Add to AssetCatalog
            AddToAssetCatalog(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>SmartWall asset creation complete!</color>");
            EditorUtility.DisplayDialog("Success", "SmartWall prefab and asset created successfully!\\n\\nPrefab: " + prefabFullPath + "\\nAsset: " + assetFullPath, "OK");
        }

        private static void CreateCursorForControlPoint(GameObject parent, SmartWall smartWall, int index)
        {
            GameObject cursorObj = new GameObject($"SmartWallCursor_{index}");
            cursorObj.transform.SetParent(parent.transform);
            cursorObj.transform.localPosition = Vector3.zero;

            SmartWallCursor cursor = cursorObj.AddComponent<SmartWallCursor>();

            // Set cursor references using SerializedObject
            SerializedObject so = new SerializedObject(cursor);
            so.FindProperty("smartWall").objectReferenceValue = smartWall;
            so.FindProperty("controlPointIndex").intValue = index;
            so.FindProperty("uiLayer").intValue = LayerMask.GetMask("UI");
            so.ApplyModifiedProperties();
        }

        private static Texture2D CreateIcon()
        {
            // Create simple 256x256 icon with wall-like appearance
            int size = 256;
            Texture2D tex = new Texture2D(size, size);

            Color wallColor = new Color(0.7f, 0.6f, 0.5f);
            Color highlightColor = new Color(0.9f, 0.8f, 0.7f);
            Color shadowColor = new Color(0.5f, 0.4f, 0.3f);
            Color accentColor = new Color(0f, 0.5f, 1f); // Blue for control points

            // Fill background
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            // Draw wall representation (vertical brick pattern)
            int centerX = size / 2;
            int centerY = size / 2;
            int wallWidth = 120;
            int wallHeight = 160;

            // Fill wall base
            for (int y = centerY - wallHeight/2; y < centerY + wallHeight/2; y++)
            {
                for (int x = centerX - wallWidth/2; x < centerX + wallWidth/2; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        // Simple brick pattern
                        int brickRow = (y - (centerY - wallHeight/2)) / 20;
                        int brickCol = (x - (centerX - wallWidth/2)) / 30;
                        bool offsetRow = (brickRow % 2 == 1);
                        int xOffset = offsetRow ? 15 : 0;
                        bool isEdge = ((x - (centerX - wallWidth/2) + xOffset) % 30 < 2) ||
                                      ((y - (centerY - wallHeight/2)) % 20 < 2);

                        Color pixelColor = isEdge ? shadowColor : wallColor;
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            }

            // Draw control point indicators (blue circles)
            int[] cpX = { centerX - wallWidth/3, centerX + wallWidth/3 };
            int cpY = centerY;
            int cpRadius = 12;

            for (int i = 0; i < cpX.Length; i++)
            {
                for (int y = cpY - cpRadius; y <= cpY + cpRadius; y++)
                {
                    for (int x = cpX[i] - cpRadius; x <= cpX[i] + cpRadius; x++)
                    {
                        if ((x - cpX[i]) * (x - cpX[i]) + (y - cpY) * (y - cpY) <= cpRadius * cpRadius)
                        {
                            if (x >= 0 && x < size && y >= 0 && y < size)
                                tex.SetPixel(x, y, accentColor);
                        }
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

                Debug.Log($"<color=green>Added SmartWall to AssetCatalog</color>");
            }
            else
            {
                Debug.LogWarning("Could not load AssetCatalog.");
            }
        }
    }
}
