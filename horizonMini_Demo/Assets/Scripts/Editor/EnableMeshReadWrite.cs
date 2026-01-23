using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to enable Read/Write on all mesh assets in the project
    /// This is needed for NavMesh baking but will increase memory usage
    /// </summary>
    public class EnableMeshReadWrite : EditorWindow
    {
        [MenuItem("HorizonMini/Enable Mesh Read/Write on All Assets")]
        public static void EnableReadWriteOnAllMeshes()
        {
            if (!EditorUtility.DisplayDialog(
                "Enable Read/Write on All Meshes",
                "This will enable Read/Write on ALL mesh and model assets in your project.\n\n" +
                "Pros:\n" +
                "- Fixes NavMesh baking warnings\n" +
                "- Allows runtime mesh modification\n\n" +
                "Cons:\n" +
                "- Increases memory usage\n" +
                "- May increase build size\n\n" +
                "This operation may take a while. Continue?",
                "Yes, Enable All",
                "Cancel"))
            {
                return;
            }

            // Find all model and mesh assets
            string[] guids = AssetDatabase.FindAssets("t:Model t:Mesh");
            int totalCount = guids.Length;
            int processedCount = 0;
            int enabledCount = 0;

            List<string> processedAssets = new List<string>();

            Debug.Log($"[EnableMeshReadWrite] Found {totalCount} mesh/model assets to process...");

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string guid = guids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // Show progress bar
                    float progress = (float)i / totalCount;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Enabling Mesh Read/Write",
                        $"Processing: {path}\n({i + 1}/{totalCount})",
                        progress))
                    {
                        Debug.LogWarning("[EnableMeshReadWrite] Operation cancelled by user");
                        break;
                    }

                    // Get the asset importer
                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                    if (importer != null)
                    {
                        processedCount++;

                        // Check if already readable
                        if (!importer.isReadable)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                            enabledCount++;
                            processedAssets.Add(path);
                            Debug.Log($"[EnableMeshReadWrite] ✓ Enabled Read/Write: {path}");
                        }
                        else
                        {
                            Debug.Log($"[EnableMeshReadWrite] - Already enabled: {path}");
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Refresh asset database
            AssetDatabase.Refresh();

            // Show summary
            string summary = $"Mesh Read/Write Enable Complete!\n\n" +
                           $"Total assets found: {totalCount}\n" +
                           $"Assets processed: {processedCount}\n" +
                           $"Read/Write enabled: {enabledCount}\n" +
                           $"Already enabled: {processedCount - enabledCount}";

            Debug.Log($"[EnableMeshReadWrite] {summary}");

            EditorUtility.DisplayDialog("Complete", summary, "OK");

            // Log processed assets
            if (processedAssets.Count > 0)
            {
                Debug.Log($"[EnableMeshReadWrite] Modified assets:\n" + string.Join("\n", processedAssets));
            }
        }
    }
}
