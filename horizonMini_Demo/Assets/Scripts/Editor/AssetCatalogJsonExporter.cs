using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using System.Collections.Generic;
using System.IO;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor tool to export AssetCatalog to JSON for AI scene generation
    /// </summary>
    public class AssetCatalogJsonExporter : EditorWindow
    {
        private AssetCatalog assetCatalog;
        private string outputPath = "";
        private bool prettyPrint = false;  // Changed to false by default to save tokens
        private bool useShortFieldNames = true;  // Use "n" and "s" instead of "name" and "size"
        private Vector2 scrollPosition;
        private string generatedJson = "";
        private int assetCount = 0;

        [MenuItem("Tools/Asset Catalog JSON Exporter")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetCatalogJsonExporter>("Asset Catalog JSON Exporter");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            // Try to find AssetCatalog automatically
            string[] guids = AssetDatabase.FindAssets("t:AssetCatalog");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                assetCatalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(path);
            }

            // Set default output path
            outputPath = Path.Combine(Application.dataPath, "AssetCatalog.json");
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Catalog JSON Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Export AssetCatalog to JSON format for AI scene generation.\n" +
                "This generates the same JSON that is sent to OpenAI.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Asset Catalog selection
            assetCatalog = (AssetCatalog)EditorGUILayout.ObjectField(
                "Asset Catalog",
                assetCatalog,
                typeof(AssetCatalog),
                false
            );

            EditorGUILayout.Space();

            // Output path
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.SaveFilePanel(
                    "Save Asset Catalog JSON",
                    Path.GetDirectoryName(outputPath),
                    "AssetCatalog.json",
                    "json"
                );
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    outputPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Token optimization options
            prettyPrint = EditorGUILayout.Toggle("Pretty Print (Readable)", prettyPrint);
            useShortFieldNames = EditorGUILayout.Toggle("Short Field Names (Save Tokens)", useShortFieldNames);

            if (useShortFieldNames)
            {
                EditorGUILayout.HelpBox("Short names: 'n' = name, 's' = size. Saves ~30% tokens.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Generate button
            GUI.enabled = assetCatalog != null;
            if (GUILayout.Button("Generate JSON", GUILayout.Height(40)))
            {
                GenerateJson();
            }
            GUI.enabled = true;

            // Show asset count
            if (assetCount > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Total Assets: {assetCount}", EditorStyles.boldLabel);
            }

            // Preview area
            if (!string.IsNullOrEmpty(generatedJson))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("JSON Preview (first 2000 chars):", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                {
                    string preview = generatedJson.Length > 2000
                        ? generatedJson.Substring(0, 2000) + "\n\n... (truncated, see full file)"
                        : generatedJson;

                    EditorGUILayout.TextArea(preview, GUILayout.ExpandHeight(true));
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // Action buttons
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Save to File", GUILayout.Height(30)))
                    {
                        SaveToFile();
                    }

                    if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30)))
                    {
                        GUIUtility.systemCopyBuffer = generatedJson;
                        Debug.Log("<color=green>[AssetCatalogJsonExporter] JSON copied to clipboard!</color>");
                    }

                    if (GUILayout.Button("Open in Default App", GUILayout.Height(30)))
                    {
                        if (File.Exists(outputPath))
                        {
                            System.Diagnostics.Process.Start(outputPath);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("File Not Found", "Please save the file first.", "OK");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void GenerateJson()
        {
            if (assetCatalog == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an AssetCatalog first.", "OK");
                return;
            }

            var allAssets = assetCatalog.GetAllAssets();
            assetCount = 0;

            if (useShortFieldNames)
            {
                // Use short field names to save tokens
                AssetCatalogJsonCompact jsonData = new AssetCatalogJsonCompact();
                jsonData.a = new List<AssetInfoJsonCompact>();

                foreach (var asset in allAssets)
                {
                    if (asset == null || asset.prefab == null) continue;

                    Vector3 boundingBoxSize = CalculateBoundingBox(asset.prefab);

                    AssetInfoJsonCompact jsonAsset = new AssetInfoJsonCompact
                    {
                        n = asset.displayName,
                        s = new float[] { boundingBoxSize.x, boundingBoxSize.y, boundingBoxSize.z }
                    };
                    jsonData.a.Add(jsonAsset);
                    assetCount++;
                }

                // Manual JSON generation for compact format (saves ~50% tokens vs JsonUtility)
                generatedJson = GenerateCompactJson(jsonData.a);
            }
            else
            {
                // Use readable field names
                AssetCatalogJson jsonData = new AssetCatalogJson();
                jsonData.assets = new List<AssetInfoJson>();

                foreach (var asset in allAssets)
                {
                    if (asset == null || asset.prefab == null) continue;

                    Vector3 boundingBoxSize = CalculateBoundingBox(asset.prefab);

                    AssetInfoJson jsonAsset = new AssetInfoJson
                    {
                        name = asset.displayName,
                        size = new float[] { boundingBoxSize.x, boundingBoxSize.y, boundingBoxSize.z }
                    };
                    jsonData.assets.Add(jsonAsset);
                    assetCount++;
                }

                generatedJson = JsonUtility.ToJson(jsonData, prettyPrint);
            }

            Debug.Log($"<color=green>[AssetCatalogJsonExporter] Generated JSON for {assetCount} assets</color>");
        }

        private void SaveToFile()
        {
            if (string.IsNullOrEmpty(generatedJson))
            {
                EditorUtility.DisplayDialog("Error", "Please generate JSON first.", "OK");
                return;
            }

            try
            {
                File.WriteAllText(outputPath, generatedJson);
                Debug.Log($"<color=green>[AssetCatalogJsonExporter] Saved to: {outputPath}</color>");
                EditorUtility.DisplayDialog("Success", $"JSON saved to:\n{outputPath}", "OK");

                // Refresh asset database if saved within Assets folder
                if (outputPath.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AssetCatalogJsonExporter] Failed to save: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to save file:\n{e.Message}", "OK");
            }
        }

        /// <summary>
        /// Generate ultra-compact JSON manually to save maximum tokens
        /// - Removes trailing zeros: 1.00 → 1
        /// - Removes .0: 2.0 → 2
        /// - No spaces, no newlines
        /// </summary>
        private string GenerateCompactJson(List<AssetInfoJsonCompact> assets)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"a\":[");

            for (int i = 0; i < assets.Count; i++)
            {
                if (i > 0) sb.Append(",");

                sb.Append("{\"n\":\"");
                sb.Append(assets[i].n);
                sb.Append("\",\"s\":[");

                // Format floats compactly: remove unnecessary zeros and decimal points
                sb.Append(FormatCompactFloat(assets[i].s[0]));
                sb.Append(",");
                sb.Append(FormatCompactFloat(assets[i].s[1]));
                sb.Append(",");
                sb.Append(FormatCompactFloat(assets[i].s[2]));

                sb.Append("]}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Format float to most compact representation
        /// Examples: 1.00 → 1, 2.50 → 2.5, 0.75 → .75, 10.0 → 10
        /// </summary>
        private string FormatCompactFloat(float value)
        {
            // Round to 2 decimal places
            value = Mathf.Round(value * 100f) / 100f;

            // Check if it's a whole number
            if (value == Mathf.Floor(value))
            {
                return ((int)value).ToString();
            }

            // Format with minimal decimals
            string str = value.ToString("0.##");

            // Remove leading zero for values < 1: 0.75 → .75
            if (str.StartsWith("0."))
            {
                str = str.Substring(1);
            }

            return str;
        }

        private Vector3 CalculateBoundingBox(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Vector3(1f, 1f, 1f);
            }

            Bounds combinedBounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            Vector3 size = combinedBounds.size;
            size.x = Mathf.Round(size.x * 100f) / 100f;
            size.y = Mathf.Round(size.y * 100f) / 100f;
            size.z = Mathf.Round(size.z * 100f) / 100f;

            return size;
        }

        // Readable JSON format
        [System.Serializable]
        private class AssetCatalogJson
        {
            public List<AssetInfoJson> assets;
        }

        [System.Serializable]
        private class AssetInfoJson
        {
            public string name;      // displayName only
            public float[] size;     // [width, height, depth] in meters
        }

        // Compact JSON format (saves ~30% tokens)
        [System.Serializable]
        private class AssetCatalogJsonCompact
        {
            public List<AssetInfoJsonCompact> a;  // "a" = assets
        }

        [System.Serializable]
        private class AssetInfoJsonCompact
        {
            public string n;      // "n" = name
            public float[] s;     // "s" = size [width, height, depth]
        }
    }
}
